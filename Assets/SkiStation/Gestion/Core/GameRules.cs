// SkiStation — Gestion : état de partie, règles de construction (devis avant validation), tracé assisté,
// réseau du domaine (GPS), statistiques, prestige. Portage de Web/js/game.js.
using System;
using System.Collections.Generic;
using System.Linq;

namespace SkiStation.Gestion
{
    public sealed class NewGameOptions { public string mode = "career", name = "Mont-Hélios", scn; public bool unlimited, allUnlocked, casual; public int seed = 1337, level = 1; }

    public static partial class Catalog
    {
        public static LiftDef Lift(string id) { foreach (var l in LIFTS) if (l.id == id) return l; return null; }
        public static BuildingDef Building(string id) { foreach (var b in BUILDINGS) if (b.id == id) return b; return null; }
        public static WeatherDef Weather(string id) { foreach (var w in WEATHER) if (w.id == id) return w; return WEATHER[0]; }
        public static ResearchDef Research(string id) { foreach (var r in RESEARCH) if (r.id == id) return r; return null; }
        public static EventDef Event(string id) { foreach (var e in EVENTS) if (e.id == id) return e; return null; }
        public static ScenarioDef Scenario(string id) { foreach (var s in SCENARIOS) if (s.id == id) return s; return null; }
    }

    public static partial class Game
    {
        public static GameState G;
        public static readonly Relief T = new Relief();
        public static readonly Network Net = new Network();
        /// <summary>Bus d'événements vers l'affichage : « piste:add », « lift:add », « building:add », « removed », « world », « toast », « day:end », « season », « career »…</summary>
        public static event Action<string, object, object> Bus;
        public static void Emit(string ev, object a = null, object b = null) { var h = Bus; if (h != null) h(ev, a, b); }
        public static double R(double x) { return Math.Floor(x + 0.5); }               // arrondi identique à Math.round (JS)

        // ---------- nouvelle partie ----------
        public static GameState NewGame(NewGameOptions o)
        {
            if (T.Seed != o.seed || T.Trees.Count == 0) { T.Init(o.seed); T.GenTrees(); }
            G = new GameState
            {
                mode = o.mode, unlimited = o.unlimited, allUnlocked = o.allUnlocked, casual = o.casual, name = o.name, seed = o.seed, level = o.level,
                money = o.mode == "sandbox" ? 2500000 : new double[] { 900000, 650000, 450000 }[GMath.Clamp(o.level, 0, 2)]
            };
            G.roads.Add(new Road { id = G.nextId++, @fixed = true, pts = new List<P2> { new P2(40, 3990), new P2(40, 3400), new P2(60, 3050), new P2(30, 2900) } });
            PlaceStarter("ticket", -40, 2620); PlaceStarter("parking", 120, 2960);
            Mgmt.Migrate(G);
            Weather.Init();
            Net.Rebuild();
            if (o.scn != null) { Scenarios.Setup(G, o); Weather.Init(); }
            Net.Rebuild();
            Sim.StartDay();
            return G;
        }
        static void PlaceStarter(string type, double x, double z)
        {
            var def = Catalog.Building(type); double y = T.GetH(x, z);
            T.Flatten(x, z, def.r, y); G.flats.Add(new Flat { x = x, z = z, r = def.r, h = y });
            G.buildings.Add(MakeBuilding(def, x, z, 0, true));
        }
        static Building MakeBuilding(BuildingDef def, double x, double z, double rot, bool starter = false)
        {
            return Mgmt.InitBuilding(new Building { id = G.nextId++, type = def.id, x = x, z = z, y = T.GetH(x, z), rot = rot, price = def.hasPrice ? def.price : 0, starter = starter, staff = def.type == "school" ? 3 : 0 });
        }

        // ---------- finances ----------
        public static bool CanPay(double c) { return G.unlimited || G.money >= c; }
        public static bool Spend(double c, string cat)
        {
            if (c <= 0) return true;
            if (!G.unlimited) { if (G.money < c) return false; G.money -= c; }
            Add(G.ledger.exp, cat, c); return true;
        }
        public static void Charge(double c, string cat) { if (!G.unlimited) G.money -= c; Add(G.ledger.exp, cat, c); }
        public static void Earn(double c, string cat) { G.money += c; Add(G.ledger.inc, cat, c); }
        public static void Add(Dictionary<string, double> d, string k, double v) { double o; d.TryGetValue(k, out o); d[k] = o + v; }
        public static void Log(string msg, string kind = "info")
        {
            G.log.Insert(0, new LogEntry { t = G.time, d = G.day, msg = msg, kind = kind }); if (G.log.Count > 60) G.log.RemoveAt(G.log.Count - 1);
            Emit("toast", msg, kind);
        }
        public static bool Researched(string id) { return G.allUnlocked || G.research.done.Contains(id); }
        public static bool LiftUnlocked(LiftDef def) { return G.allUnlocked || def.tier <= G.tier; }
        public static bool BuildingUnlocked(BuildingDef def) { return string.IsNullOrEmpty(def.research) || Researched(def.research); }
        public static double MaxLen(LiftDef def) { return R(def.maxLen * (1 + 0.08 * Math.Max(0, G.tier - def.tier))); }

        // ---------- calendrier ----------
        static readonly string[] WD = { "Mer.", "Jeu.", "Ven.", "Sam.", "Dim.", "Lun.", "Mar." };
        static readonly string[] MON = { "déc.", "janv.", "févr.", "mars", "avr." }; static readonly int[] MLEN = { 31, 31, 28, 31, 30 };
        public static string DateStr(int day, bool? summer = null)
        {
            if (summer ?? G.summer) return WD[day % 7] + " " + (day < 21 ? (day + 11) + " juil." : (day - 20) + " août");
            int d = day, m = 0; while (m < MON.Length - 1 && d >= MLEN[m]) { d -= MLEN[m]; m++; }
            return WD[day % 7] + " " + (d + 1) + " " + MON[m];
        }
        public static bool IsWeekend(int day) { return day % 7 == 3 || day % 7 == 4; }
        public static bool IsHoliday(int day) { return (day >= 20 && day <= 35) || (day >= 68 && day <= 96); }

        // ---------- pistes ----------
        static List<P3> Spline(IList<P2> ctrl)
        {
            var o = new List<P3>(); int n = ctrl.Count;
            Func<int, P2> P = i => ctrl[GMath.Clamp(i, 0, n - 1)];
            for (int i = 0; i < n - 1; i++)
            {
                P2 p0 = P(i - 1), p1 = P(i), p2 = P(i + 1), p3 = P(i + 2);
                int seg = Math.Max(2, (int)Math.Ceiling(GMath.Hypot(p2.x - p1.x, p2.z - p1.z) / 10));
                for (int k = 0; k < seg; k++)
                {
                    double t = (double)k / seg, t2 = t * t, t3 = t2 * t;
                    o.Add(new P3(0.5 * (2 * p1.x + (-p0.x + p2.x) * t + (2 * p0.x - 5 * p1.x + 4 * p2.x - p3.x) * t2 + (-p0.x + 3 * p1.x - 3 * p2.x + p3.x) * t3), 0,
                                 0.5 * (2 * p1.z + (-p0.z + p2.z) * t + (2 * p0.z - 5 * p1.z + 4 * p2.z - p3.z) * t2 + (-p0.z + 3 * p1.z - 3 * p2.z + p3.z) * t3)));
                }
            }
            o.Add(new P3(ctrl[n - 1].x, 0, ctrl[n - 1].z));
            double s = 0;
            for (int i = 0; i < o.Count; i++)
            {
                var p = o[i]; p.y = T.GetH(p.x, p.z);
                if (i > 0) { var q = o[i - 1]; s += GMath.Hypot(p.x - q.x, p.z - q.z, p.y - q.y); }
                p.s = s; o[i] = p;
            }
            return o;
        }
        static PisteAnalysis AnalyzePiste0(IList<P2> ctrl, double width)
        {
            var r = new PisteAnalysis { width = width > 0 ? width : 30 };
            if (ctrl.Count < 2) { r.err = "Placez au moins deux points : le départ en haut, l'arrivée en bas."; return r; }
            var pts = r.pts = Spline(ctrl);
            r.len = pts[pts.Count - 1].s; r.drop = pts[0].y - pts[pts.Count - 1].y; r.topAlt = pts[0].y;
            var slopes = new List<double>(); double minY = pts[0].y, rise = 0;
            for (int i = 1; i < pts.Count; i++)
            {
                double dh = GMath.Hypot(pts[i].x - pts[i - 1].x, pts[i].z - pts[i - 1].z); if (dh == 0) dh = 1;
                double along = (pts[i - 1].y - pts[i].y) / dh, gx, gz; T.Grad(pts[i].x, pts[i].z, out gx, out gz);
                slopes.Add(Math.Max(Math.Abs(along), 0.26 * GMath.Hypot(gx, gz)));
                if (pts[i].y > minY + 5) rise = Math.Max(rise, pts[i].y - minY); minY = Math.Min(minY, pts[i].y);
                if (!Relief.Inside(pts[i].x, pts[i].z)) { r.err = "La piste sort des limites du domaine."; return r; }
            }
            var sorted = slopes.OrderBy(x => x).ToList();
            r.avg = slopes.Sum() / slopes.Count; r.p90 = sorted[(int)Math.Floor(sorted.Count * 0.9)]; r.max = sorted[sorted.Count - 1];
            double m = 0.55 * r.p90 + 0.45 * r.avg;
            r.diff = m < 0.14 ? 0 : m < 0.26 ? 1 : m < 0.42 ? 2 : 3;
            r.trees = T.TreesNearPath(pts.Select(p => p.Xz).ToList(), r.width / 2 + 5, false);
            r.cost = R((4000 + r.len * 28 * (r.width / 30) * (1 + r.diff * 0.25) + r.trees * 120) / 100) * 100;
            if (r.len < 80) { r.err = "Piste trop courte (minimum 80 m)."; return r; }
            if (r.drop < 8) { r.err = "L'arrivée doit être plus basse que le départ : une piste se descend."; return r; }
            if (rise > 0) { r.err = "La piste remonte de " + R(rise) + " m : les skieurs ne remontent que par les remontées."; return r; }
            if (r.p90 > 1.15) { r.err = "Pente trop forte (" + R(r.p90 * 100) + " %) : impraticable."; return r; }
            foreach (var l in G.lifts) if (Catalog.Lift(l.type).kind == "tow" && CrossesLine(pts, l.a.Xz, l.b.Xz, 45)) { r.err = "Traversée du " + l.name + " interdite : une piste ne coupe pas un téléski."; return r; }
            if (!CanPay(r.cost)) r.warn = "Fonds insuffisants.";
            r.ok = true; return r;
        }
        // JONCTIONS : une piste ne chevauche jamais une autre. Elle peut partir d'une piste existante (embranchement) et,
        // dès qu'elle en touche une autre, elle s'arrête dessus et la rejoint ; le réseau les relie à cet endroit.
        public static List<Piste> PistePool;
        public static string AutoDest = "any";                 // pistes prises en compte (null = celles du joueur)
        public sealed class Junction { public int i; public Piste piste; public double s, apart; public P2 at; }
        public static Junction FindJunction(IList<P3> pts, double width, IList<Piste> pool = null)
        {
            var list = (pool ?? PistePool ?? G.pistes).Where(o => o.pts != null && o.pts.Count > 1).ToList();
            if (list.Count == 0 || pts.Count < 3) return null;
            double s0; var start = new HashSet<Piste>(list.Where(o => ClosestOnPiste(o, pts[0].x, pts[0].z, out s0) < (width + o.width) / 2 + 6));
            bool left = start.Count == 0; double apart = 0, walked = 0;
            for (int i = 1; i < pts.Count; i++)
            {
                var p = pts[i]; double step = GMath.Hypot(p.x - pts[i - 1].x, p.z - pts[i - 1].z); walked += step; double ss;
                if (!left && !start.Any(o => ClosestOnPiste(o, p.x, p.z, out ss) < (width + o.width) / 2 * 0.9)) left = true;
                if (left) apart += step;
                foreach (var o in list)
                {
                    double lim = (width + o.width) / 2 * 0.95, cs; double d = ClosestOnPiste(o, p.x, p.z, out cs); if (d >= lim) continue;
                    if (start.Contains(o) && walked < 170) continue;
                    var j = PisteAt(o, cs);
                    return new Junction { i = i, piste = o, s = walked, at = new P2(j.x, j.z), apart = apart };
                }
            }
            return null;
        }
        // RACCORD AUX REMONTÉES : départ près de l'arrivée d'une remontée → depuis la gare ; fin près du départ d'une remontée plus basse → à la gare.
        public static List<Lift> LiftPool;                   // remontées prises en compte (null = celles du joueur)
        public static List<P2> AttachPiste(IList<P2> ctrl, out Lift startLift, out Lift endLift)
        {
            var lifts = LiftPool ?? G.lifts; var o = ctrl.ToList(); startLift = null; endLift = null;
            if (o.Count < 2) return o;
            P2 f = o[0]; double hf = T.GetH(f.x, f.z), bd = 140;
            foreach (var l in lifts) { double d = GMath.Hypot(l.b.x - f.x, l.b.z - f.z); if (d < bd && l.b.y >= hf - 15) { bd = d; startLift = l; } }
            if (startLift != null) { var st = new P2(R(startLift.b.x), R(startLift.b.z)); if (bd < 35 || o.Count > 2) o[0] = st; else o.Insert(0, st); }
            P2 e = o[o.Count - 1]; double he = T.GetH(e.x, e.z); bd = 160;
            foreach (var l in lifts) { if (l == startLift) continue; double d = GMath.Hypot(l.a.x - e.x, l.a.z - e.z); if (d < bd && l.a.y <= he + 5) { bd = d; endLift = l; } }
            if (endLift == null && startLift != null) { var l = startLift; double d = GMath.Hypot(l.a.x - e.x, l.a.z - e.z); if (d < 160 && l.a.y <= he + 5) { endLift = l; bd = d; } }
            if (endLift != null) { var st = new P2(R(endLift.a.x), R(endLift.a.z)); if (bd < 35) o[o.Count - 1] = st; else o.Add(st); }
            return o;
        }
        public static PisteAnalysis AnalyzePiste(IList<P2> ctrl, double width = 30)
        {
            Lift sl, el; var c2 = AttachPiste(ctrl, out sl, out el);
            var r = AnalyzePisteJ(c2, width); r.startLift = sl != null ? sl.name : null; r.endLift = r.joins == null && el != null ? el.name : null;
            return r;
        }
        static PisteAnalysis AnalyzePisteJ(IList<P2> ctrl, double width)
        {
            if (width <= 0) width = 30;
            if (ctrl.Count < 2) { var r0 = AnalyzePiste0(ctrl, width); r0.ctrl = ctrl.ToList(); return r0; }
            var pts0 = Spline(ctrl);
            var j = FindJunction(pts0, width);
            if (j == null) { var r1 = AnalyzePiste0(ctrl, width); r1.ctrl = ctrl.ToList(); return r1; }
            var hs = new double[pts0.Count]; for (int i = 1; i < pts0.Count; i++) hs[i] = hs[i - 1] + GMath.Hypot(pts0[i].x - pts0[i - 1].x, pts0[i].z - pts0[i - 1].z);
            var keep = new List<P2> { ctrl[0] };
            for (int k = 1; k < ctrl.Count; k++)
            {
                double best = 1e9, bs = 0; for (int i = 0; i < pts0.Count; i++) { double d = GMath.Hypot(pts0[i].x - ctrl[k].x, pts0[i].z - ctrl[k].z); if (d < best) { best = d; bs = hs[i]; } }
                if (bs < j.s - 25) keep.Add(ctrl[k]); else break;
            }
            keep.Add(new P2(R(j.at.x), R(j.at.z)));
            var r = AnalyzePiste0(keep, width); r.ctrl = keep; r.joins = j.piste.name; r.joinPiste = j.piste;
            if (r.ok && j.apart < 60) { r.ok = false; r.err = "Ce tracé suit la piste " + j.piste.name + " : écartez-le pour créer une vraie nouvelle piste (elle pourra la rejoindre plus bas)."; }
            if (r.ok && r.len < 80) { r.ok = false; r.err = "La piste rejoint " + j.piste.name + " trop tôt : écartez le tracé de la piste existante."; }
            return r;
        }
        static bool CrossesLine(IList<P3> pts, P2 la, P2 lb, double margin)
        {
            double dx = lb.x - la.x, dz = lb.z - la.z, len = GMath.Hypot(dx, dz), ux = dx / len, uz = dz / len;
            var a = new P2(la.x + ux * margin, la.z + uz * margin); var b = new P2(lb.x - ux * margin, lb.z - uz * margin);
            if (len < margin * 2.2) return false;
            for (int i = 0; i < pts.Count - 1; i++) if (GMath.SegCross(pts[i].Xz, pts[i + 1].Xz, a, b)) return true;
            return false;
        }
        public static Piste CommitPiste(IList<P2> ctrl, PisteAnalysis an, Piste loaded = null)
        {
            if (loaded == null && !Spend(an.cost, "Construction")) return null;
            if (loaded == null && an.ctrl != null) ctrl = an.ctrl;
            var used = new HashSet<string>(G.pistes.Select(x => x.name));
            var p = loaded ?? new Piste
            {
                id = G.nextId++, name = Catalog.PISTE_NAMES.FirstOrDefault(n => !used.Contains(n)) ?? "Piste " + G.nextId,
                ctrl = ctrl.Select(c => new P2(R(c.x), R(c.z))).ToList(), width = an.width, depth = G.snowBase + (an.topAlt - 1650) * 0.03
            };
            p.pts = an.pts; p.len = an.len; p.drop = an.drop; p.avg = an.avg; p.p90 = an.p90; p.diff = an.diff; p.topAlt = an.topAlt; p.on = 0; p.edges = new List<Edge>();
            var xz = p.pts.Select(q => q.Xz).ToList();
            T.TreesNearPath(xz, p.width / 2 + 5, true);
            T.SnowAlong(p.pts, p.width / 2 + Relief.CELL);
            if (loaded == null) { G.pistes.Add(p); Net.Rebuild(); Emit("piste:add", p); Emit("world"); }
            return p;
        }
        public struct PistePos { public double x, z, dx, dz, slope; }
        public static PistePos PisteAt(Piste p, double s)
        {
            var pts = p.pts; int lo = 0, hi = pts.Count - 1;
            while (hi - lo > 1) { int m = (lo + hi) >> 1; if (pts[m].s <= s) lo = m; else hi = m; }
            P3 a = pts[lo], b = pts[hi]; double ds = b.s - a.s; if (ds == 0) ds = 1; double t = GMath.Clamp((s - a.s) / ds, 0, 1);
            return new PistePos { x = a.x + (b.x - a.x) * t, z = a.z + (b.z - a.z) * t, dx = b.x - a.x, dz = b.z - a.z, slope = (a.y - b.y) / ds };
        }
        public static double ClosestOnPiste(Piste p, double x, double z, out double s)
        {
            double best = 1e9; s = 0;
            for (int i = 0; i < p.pts.Count - 1; i++)
            {
                P3 a = p.pts[i], b = p.pts[i + 1]; double t; double d = GMath.SegDist(x, z, a.x, a.z, b.x, b.z, out t);
                if (d < best) { best = d; s = a.s + (b.s - a.s) * t; }
            }
            return best;
        }

        // ---------- remontées ----------
        public static string ShortName(string kind) { return kind == "tow" ? "TK" : kind == "chair" ? "TS" : kind == "gondola" ? "TC" : kind == "cablecar" ? "TPH" : kind == "funi" ? "Funi" : "Tapis"; }
        public static LiftAnalysis AnalyzeLift(string defId, P2 a, P2 b, bool force = false)
        {
            var def = Catalog.Lift(defId); var r = new LiftAnalysis { def = def };
            double ay = T.GetH(a.x, a.z), by = T.GetH(b.x, b.z), hl = GMath.Hypot(b.x - a.x, b.z - a.z);
            r.vert = by - ay; r.hlen = hl; r.len = GMath.Hypot(hl, r.vert); r.maxLen = MaxLen(def);
            r.cost = R(def.cost * (0.45 + 0.55 * Math.Min(1.3, r.len / def.maxLen)) / 1000) * 1000;
            double speed = def.speed * Const.MOTION * (Researched("handsfree") ? 1.12 : 1);
            int n = def.kind == "cablecar" || def.kind == "funi" ? 2 : Math.Max(2, (int)R(2 * r.len / def.spacing));
            r.vehicles = n; r.capacity = R(def.seats * 3600 / ((2 * r.len / n) / speed)); r.rideMin = r.len / speed / 60;
            r.maint = R(r.cost * 0.0022 + 220);
            if (!Relief.Inside(a.x, a.z) || !Relief.Inside(b.x, b.z)) { r.err = "Station hors des limites du domaine."; if (!force) return r; }
            if (r.len < 60) { r.err = "Ligne trop courte (minimum 60 m)."; if (!force) return r; }
            if (r.len > r.maxLen) { r.err = "Portée dépassée : " + R(r.len) + " m pour " + r.maxLen + " m maximum. Rapprochez la gare ou choisissez un autre modèle."; if (!force) return r; }
            if (r.vert < (def.kind == "carpet" ? 2 : 10)) { r.err = "La gare d'arrivée doit être plus haute que la gare de départ."; if (!force) return r; }
            double stLim = def.kind == "funi" ? 1.6 : 0.9;
            if (T.Slope(a.x, a.z) > stLim || T.Slope(b.x, b.z) > stLim) { r.err = "Terrain trop raide pour une gare."; if (!force) return r; }
            // FUNICULAIRE SOUTERRAIN : tunnel en ligne droite sous le relief, quelle que soit la pente ; seules les gares sont visibles
            if (def.kind == "funi")
            {
                double ay1 = ay + 1.2, by1 = by + 1.2; int nseg = Math.Max(1, (int)Math.Ceiling(hl / 120));
                var tp = new List<CablePt> { new CablePt { x = a.x, z = a.z, y = ay1, g = ay } };
                for (int i = 1; i < nseg; i++) { double t = (double)i / nseg, x = GMath.Lerp(a.x, b.x, t), z = GMath.Lerp(a.z, b.z, t), g = T.GetH(x, z); tp.Add(new CablePt { x = x, z = z, y = Math.Min(GMath.Lerp(ay1, by1, t), g - 30), g = g }); }
                tp.Add(new CablePt { x = b.x, z = b.z, y = by1, g = by });
                double ts = 0; tp[0].s = 0;
                for (int i = 1; i < tp.Count; i++) { ts += GMath.Hypot(tp[i].x - tp[i - 1].x, tp[i].y - tp[i - 1].y, tp[i].z - tp[i - 1].z); tp[i].s = ts; }
                r.path = tp; r.L = ts; r.a = new P3(a.x, ay, a.z); r.b = new P3(b.x, by, b.z);
                r.trees = T.TreesNearPath(new List<P2> { a, a }, 14, false) + T.TreesNearPath(new List<P2> { b, b }, 14, false); r.cost += r.trees * 120;
                if (!CanPay(r.cost)) r.warn = "Fonds insuffisants.";
                r.ok = r.err == null; return r;
            }
            int steps = Math.Max(4, (int)Math.Ceiling(hl / 20)); var prof = new double[steps + 1];
            for (int i = 0; i <= steps; i++) { double t = (double)i / steps; prof[i] = T.GetH(GMath.Lerp(a.x, b.x, t), GMath.Lerp(a.z, b.z, t)); }
            if (def.kind == "tow" || def.kind == "carpet" || def.kind == "funi")
            {
                double lim = def.kind == "tow" ? 0.6 : def.kind == "funi" ? 0.75 : 0.24, hi = prof[0];
                for (int i = 1; i < prof.Length; i++)
                {
                    double sl = (prof[i] - prof[i - 1]) / (hl / steps);
                    if (sl > lim) { r.err = "Pente trop forte sous la ligne (" + R(sl * 100) + " %, max " + R(lim * 100) + " %)."; if (!force) return r; }
                    if (prof[i] < hi - 8) { r.err = "Le tracé redescend : un " + (def.kind == "tow" ? "téléski" : def.kind == "funi" ? "funiculaire" : "tapis") + " doit monter en continu."; if (!force) return r; }
                    hi = Math.Max(hi, prof[i]);
                }
                if (def.kind == "tow") foreach (var p in G.pistes) if (CrossesLine(p.pts, a, b, 45)) { r.err = "Ce téléski couperait la piste " + p.name + " : traversée interdite."; if (!force) return r; }
            }
            int pyl = def.kind == "funi" ? Math.Max(1, (int)R(hl / 30)) : def.kind == "carpet" ? 0 : def.kind == "cablecar" ? Math.Max(1, (int)Math.Floor(hl / 1400)) : Math.Max(1, (int)R(hl / (def.big ? 420 : def.kind == "gondola" ? 125 : def.kind == "tow" ? 70 : 95)) - 1);
            double sh = def.kind == "funi" ? 1.2 : def.kind == "carpet" ? 0.6 : def.kind == "tow" ? 5 : 6;
            var path = new List<CablePt> { new CablePt { x = a.x, z = a.z, y = ay + sh, g = ay } };
            for (int i = 1; i <= pyl; i++)
            {
                double t = (double)i / (pyl + 1), x = GMath.Lerp(a.x, b.x, t), z = GMath.Lerp(a.z, b.z, t), g = T.GetH(x, z);
                path.Add(def.kind == "funi" ? new CablePt { x = x, z = z, y = g + 1.2, g = g } : new CablePt { x = x, z = z, y = g + def.pylonH, g = g, pylon = true });
            }
            path.Add(new CablePt { x = b.x, z = b.z, y = by + sh, g = by });
            double need = def.kind == "tow" ? 2.5 : def.kind == "carpet" || def.kind == "funi" ? -50 : 7;
            for (int it = 0; it < 9; it++)
            {
                bool bad = false;
                for (int i = 0; i < path.Count - 1; i++)
                {
                    var p = path[i]; var q = path[i + 1]; double d = GMath.Hypot(q.x - p.x, q.z - p.z); int n2 = Math.Max(2, (int)Math.Ceiling(d / 20)); double def2 = 0;
                    for (int k = 1; k < n2; k++) { double t = (double)k / n2; def2 = Math.Max(def2, T.GetH(GMath.Lerp(p.x, q.x, t), GMath.Lerp(p.z, q.z, t)) + need - GMath.Lerp(p.y, q.y, t)); }
                    if (def2 > 0.3) { bad = true; if (p.pylon) p.y += def2; if (q.pylon) q.y += def2; if (!p.pylon && !q.pylon) { r.err = "Le câble toucherait le relief."; if (!force) return r; } }
                }
                if (!bad) break;
                if (it == 8) { r.err = "Le relief coupe la ligne : le câble ne passe pas. Déplacez une gare."; if (!force) return r; }
            }
            foreach (var p in path) if (p.pylon && p.y - p.g > (def.kind == "cablecar" || def.big ? 110 : def.kind == "gondola" ? 70 : 45)) { r.err = "Pylône trop haut nécessaire (" + R(p.y - p.g) + " m). Contournez la crête ou choisissez un modèle à plus grande portée."; if (!force) return r; }
            double s = 0; path[0].s = 0;
            for (int i = 1; i < path.Count; i++) { s += GMath.Hypot(path[i].x - path[i - 1].x, path[i].y - path[i - 1].y, path[i].z - path[i - 1].z); path[i].s = s; }
            r.path = path; r.L = s; r.a = new P3(a.x, ay, a.z); r.b = new P3(b.x, by, b.z);
            r.trees = T.TreesNearPath(new List<P2> { a, b }, 9, false); r.cost += r.trees * 120;
            if (!CanPay(r.cost)) r.warn = "Fonds insuffisants.";
            r.ok = r.err == null; return r;
        }
        public static Lift CommitLift(LiftAnalysis an, Lift loaded = null)
        {
            if (loaded == null && !Spend(an.cost, "Construction")) return null;
            var def = an.def; var used = G.lifts.Select(x => x.name).ToList(); string sh = ShortName(def.kind);
            var l = loaded ?? new Lift { id = G.nextId++, type = def.id, name = sh + " " + (Catalog.LIFT_NAMES.FirstOrDefault(n => !used.Any(u => u.EndsWith(" " + n))) ?? G.nextId.ToString()), built = G.day, cost = an.cost };
            l.a = an.a; l.b = an.b; l.path = an.path; l.L = an.L; l.len = an.len; l.vert = an.vert; l.capacity = an.capacity; l.maint = an.maint;
            l.n = an.vehicles; l.D = 2 * l.L / l.n; l.phase = 0; l.veh = new List<List<Visitor>>(); for (int i = 0; i < l.n; i++) l.veh.Add(new List<Visitor>());
            l.queue = new List<Visitor>(); l.brokenUntil = 0; l.windStop = false; l.rides = 0;
            if (def.kind == "funi") { T.TreesNearPath(new List<P2> { l.a.Xz, l.a.Xz }, 14, true); T.TreesNearPath(new List<P2> { l.b.Xz, l.b.Xz }, 14, true); } else T.TreesNearPath(new List<P2> { l.a.Xz, l.b.Xz }, 9, true);
            foreach (var st in new[] { l.a, l.b }) { T.Flatten(st.x, st.z, 12, st.y, 14); if (loaded == null) G.flats.Add(new Flat { x = st.x, z = st.z, r = 12, h = st.y, blend = 14 }); }
            if (loaded == null) { G.lifts.Add(l); ReattachPistes(l); Net.Rebuild(); Emit("lift:add", l); Emit("world"); }
            return l;
        }
        /// <summary>Une nouvelle remontée raccorde d'elle-même les pistes voisines (fins vers sa gare de départ, départs vers son arrivée).</summary>
        public static int ReattachPistes(Lift l, List<Piste> pistes = null)
        {
            var list = pistes ?? G.pistes; int n = 0;
            foreach (var p in list.ToList())
            {
                P3 a = p.pts[0], z = p.pts[p.pts.Count - 1];
                double de = GMath.Hypot(l.a.x - z.x, l.a.z - z.z), ds = GMath.Hypot(l.b.x - a.x, l.b.z - a.z);
                bool nearEnd = de < 160 && de > 8 && l.a.y <= z.y + 5, nearStart = p.on == 0 && ds < 140 && ds > 8 && l.b.y >= a.y - 15;
                if (!nearEnd && !nearStart) continue;
                var prev = PistePool; PistePool = list.Where(o => o != p).ToList();
                var an = AnalyzePiste(p.ctrl, p.width); PistePool = prev;
                if (!an.ok || an.pts == null) continue;
                p.ctrl = an.ctrl.Select(c => new P2(R(c.x), R(c.z))).ToList(); p.pts = an.pts; p.len = an.len; p.drop = an.drop; p.avg = an.avg; p.p90 = an.p90; p.diff = an.diff; p.topAlt = an.topAlt;
                T.TreesNearPath(p.pts.Select(q => q.Xz).ToList(), p.width / 2 + 5, true); T.SnowAlong(p.pts, p.width / 2 + Relief.CELL);
                Emit("piste:refresh", p); n++;
            }
            return n;
        }
        /// <summary>Remplace le modèle sur la même ligne (gares inchangées) ; remontée vide uniquement. dry = devis seul.</summary>
        public static string Modernize(Lift l, string type, bool dry, out double cost, out LiftAnalysis an)
        {
            var others = G.lifts; G.lifts = others.Where(x => x != l).ToList();
            an = AnalyzeLift(type, l.a.Xz, l.b.Xz); G.lifts = others; cost = 0;
            if (!an.ok) return an.err;
            cost = Math.Max(R(an.cost * 0.1), an.cost - R(l.cost * 0.3));
            if (dry) return null;
            if (l.queue.Count > 0 || l.veh.Any(v => v.Count > 0)) return "Des visiteurs utilisent encore cette remontée : fermez-la et attendez qu'elle se vide.";
            if (!Spend(cost, "Construction")) return "Fonds insuffisants.";
            string old = l.name.Split(' ')[0]; l.type = type; l.cost = an.cost; l.cond = 1;
            CommitLift(an, l);
            string sh = ShortName(an.def.kind);
            if (new[] { "TK", "TS", "TC", "TPH", "Funi", "Tapis" }.Contains(old)) l.name = sh + l.name.Substring(old.Length);
            Net.Rebuild(); Emit("lift:refresh", l); Emit("world"); return null;
        }
        public static P3 LiftPos(Lift l, double s)
        {
            var p = l.path; int i = 0; while (i < p.Count - 2 && p[i + 1].s < s) i++;
            var a = p[i]; var b = p[i + 1]; double t = GMath.Clamp((s - a.s) / (b.s - a.s), 0, 1);
            return new P3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
        }
        public static bool LiftSeason(Lift l) { if (!G.summer) return true; var k = Catalog.Lift(l.type).kind; return k != "tow" && k != "carpet"; }
        public static double PassNow() { return G.summer ? G.summerPass : G.passPrice; }
        public static bool NightOn() { return !G.summer && Researched("night") && G.pistes.Any(p => p.lit) && G.lifts.Any(l => l.night); }
        public static bool NightActive() { return G.time >= Const.NIGHT_OPEN && G.time < Const.NIGHT_CLOSE && Researched("night"); }
        static bool NightLift(Lift l) { return l.night && Researched("night"); }
        public static bool LiftRunning(Lift l) { return l.open && LiftSeason(l) && !l.windStop && G.time >= l.brokenUntil && ((G.time >= Const.LIFT_OPEN - 900 && G.time < Const.LIFT_CLOSE + 2400) || (NightLift(l) && G.time >= Const.NIGHT_OPEN - 900 && G.time < Const.NIGHT_CLOSE + 1800)); }
        public static bool LiftBoarding(Lift l) { return l.open && LiftSeason(l) && !l.windStop && G.time >= l.brokenUntil && ((G.time >= Const.LIFT_OPEN && G.time < Const.LIFT_CLOSE) || (NightLift(l) && NightActive())); }
        public static bool PisteOpen(Piste p) { return p.open && !p.snowClosed && !(p.avaUntil > G.time) && (G.time < Const.LIFT_CLOSE + 2700 || G.time >= Const.NIGHT_CLOSE + 600 || p.lit); }

        // ---------- tracé assisté (A*) ----------
        public static AutoPisteResult AutoPiste(P2 start, P2? goal, double width, int ease = 3)
        {
            double thr = new[] { 0.085, 0.14, 0.24, 0.17 }[GMath.Clamp(ease, 0, 3)], wt = ease == 0 ? 30 : ease == 1 ? 22 : 14;
            const int S = 2; int N = Relief.N, M = (int)Math.Ceiling((double)N / S); double C = Relief.CELL;
            Func<int, P2> to = i => new P2(-Relief.HALF + (i % M) * S * C, -Relief.HALF + (i / M) * S * C);
            P2 gx0 = goal ?? Relief.HUB;
            // sans arrivée imposée : front de neige ou départ d'une remontée plus basse (AutoDest : "any", "self", "hub")
            var dests = new List<Tuple<P2, double, string, Lift>>();
            if (!goal.HasValue)
            {
                dests.Add(Tuple.Create(Relief.HUB, 110.0, "le front de neige", (Lift)null));
                if (AutoDest != "hub")
                {
                    double h0 = T.GetH(start.x, start.z); var pool = LiftPool ?? G.lifts; var own = pool.FirstOrDefault(l => GMath.Hypot(l.b.x - start.x, l.b.z - start.z) < 80);
                    foreach (var l in pool)
                    {
                        if (AutoDest == "self" ? l != own : l == own) continue;
                        if (l.a.y < h0 - 40 && GMath.Hypot(l.a.x - start.x, l.a.z - start.z) > 250) dests.Add(Tuple.Create(l.a.Xz, 45.0, l.name, l));
                    }
                    if (AutoDest == "self" && dests.Count > 1) dests.RemoveAt(0);
                }
            }
            Func<double, double, int> ci = (x, z) => (int)R((z + Relief.HALF) / C / S) * M + (int)R((x + Relief.HALF) / C / S);
            Func<int, double> hAt = i => { var p = to(i); return T.GetH(p.x, p.z); };
            int si = ci(start.x, start.z), gi = ci(gx0.x, gx0.z); double stepL = S * C;
            if (hAt(si) < (goal.HasValue ? T.GetH(gx0.x, gx0.z) : dests.Min(d => T.GetH(d.Item1.x, d.Item1.z))) + 8) return new AutoPisteResult { err = "Le départ doit être nettement plus haut que l'arrivée." };
            var heap = new MinHeap(); heap.Push(0, si);
            var cost = new Dictionary<int, double> { { si, 0 } }; var prev = new Dictionary<int, int>(); var done = new HashSet<int>();
            int[] nbx = { 1, -1, 0, 0, 1, -1, 1, -1 }, nbz = { 0, 0, 1, -1, 1, 1, -1, -1 };
            var hg = to(gi); int iter = 0, reached = -1; Tuple<P2, double, string, Lift> hitDest = null;
            var corr = new HashSet<int>(); foreach (var o in PistePool ?? G.pistes) if (o.pts != null) foreach (var q in o.pts) corr.Add(ci(q.x, q.z));
            var tows = new List<P2[]>();
            foreach (var l in G.lifts) if (Catalog.Lift(l.type).kind == "tow") { double dx = l.b.x - l.a.x, dz = l.b.z - l.a.z, n = GMath.Hypot(dx, dz); if (n == 0) n = 1; tows.Add(new[] { new P2(l.a.x + dx / n * 45, l.a.z + dz / n * 45), new P2(l.b.x - dx / n * 45, l.b.z - dz / n * 45) }); }
            while (heap.Count > 0 && iter++ < 60000)
            {
                int i = heap.Pop(); if (done.Contains(i)) continue; done.Add(i);
                var p = to(i);
                if (goal.HasValue ? GMath.Hypot(p.x - hg.x, p.z - hg.z) < 60 : (hitDest = dests.FirstOrDefault(d => GMath.Hypot(p.x - d.Item1.x, p.z - d.Item1.z) < d.Item2)) != null) { reached = i; break; }
                double hi = hAt(i);
                for (int k = 0; k < 8; k++)
                {
                    int nx = (i % M) + nbx[k], nz = (i / M) + nbz[k]; if (nx < 1 || nz < 1 || nx >= M - 1 || nz >= M - 1) continue;
                    int j = nz * M + nx; if (done.Contains(j)) continue;
                    var q = to(j); if (!Relief.Inside(q.x, q.z, 80)) continue;
                    bool cross = false; foreach (var t in tows) if (GMath.SegCross(p, q, t[0], t[1])) { cross = true; break; }
                    if (cross) continue;
                    double hj = T.GetH(q.x, q.z), len = stepL * (nbx[k] != 0 && nbz[k] != 0 ? 1.414 : 1), along = (hi - hj) / len;
                    if (along < -0.003) continue;
                    double gx, gz; T.Grad(q.x, q.z, out gx, out gz); double crs = GMath.Hypot(gx, gz), eff = Math.Max(along, 0.26 * crs);
                    double pen = 1 + wt * Math.Max(0, eff - thr) + 3 * Math.Max(0, along < 0.03 ? 0.03 - along : 0) * 10 + 4 * Math.Max(0, crs - 0.5);
                    if (T.Slope(q.x, q.z) > 1.0) pen += 30;
                    if (corr.Contains(j) && GMath.Hypot(q.x - start.x, q.z - start.z) > 200) pen += 2.5;   // ne pas longer une piste existante
                    double nc = cost[i] + len * pen, oc; if (nc >= (cost.TryGetValue(j, out oc) ? oc : 1e18)) continue;
                    cost[j] = nc; prev[j] = i; heap.Push(nc + (goal.HasValue ? GMath.Hypot(q.x - hg.x, q.z - hg.z) : dests.Min(d => GMath.Hypot(q.x - d.Item1.x, q.z - d.Item1.z) - d.Item2)), j);
                }
            }
            if (reached < 0) return new AutoPisteResult { err = goal.HasValue ? "Aucune descente continue ne mène à ce point depuis le départ." : "Aucune descente continue ne mène au front de neige depuis ce point. Choisissez un départ plus bas, ou activez la carte des zones desservables pour repérer les emplacements possibles." };
            var pts = new List<P2>(); for (int i = reached; ; ) { pts.Insert(0, to(i)); int pi; if (!prev.TryGetValue(i, out pi)) break; i = pi; }
            pts[0] = start; if (goal.HasValue) pts.Add(gx0); else if (hitDest != null && hitDest.Item4 != null) pts.Add(hitDest.Item1);
            { double m = T.GetH(start.x, start.z); var o = new List<P2> { pts[0] };
              for (int i = 1; i < pts.Count; i++) { double h = T.GetH(pts[i].x, pts[i].z); if (h <= m + 0.4) { o.Add(pts[i]); m = Math.Min(m, h); } }
              if (o.Count < 2) return new AutoPisteResult { err = "Départ trop plat pour tracer une descente." }; pts = o; }
            AutoPisteResult best = null;
            foreach (double tol in new double[] { 90, 60, 40, 28, 18, 10 })
            {
                var ctrl = DP(pts, tol); if (ctrl.Count > 26) continue;
                var an = AnalyzePiste(ctrl, width > 0 ? width : 30);
                if (an.ok) { best = new AutoPisteResult { ctrl = ctrl, an = an }; break; }
                if (best == null) best = new AutoPisteResult { ctrl = ctrl, an = an, bad = true };
            }
            if (best != null) best.dest = hitDest != null ? hitDest.Item3 : null;
            return best ?? new AutoPisteResult { err = "Tracé trop complexe : choisissez un départ plus proche." };
        }
        static List<P2> DP(List<P2> a, double tol)       // simplification de Douglas-Peucker
        {
            if (a.Count < 3) return a;
            double md = 0; int mi = 0;
            for (int i = 1; i < a.Count - 1; i++) { double d = GMath.SegDist(a[i].x, a[i].z, a[0].x, a[0].z, a[a.Count - 1].x, a[a.Count - 1].z); if (d > md) { md = d; mi = i; } }
            if (md < tol) return new List<P2> { a[0], a[a.Count - 1] };
            var left = DP(a.GetRange(0, mi + 1), tol); left.RemoveAt(left.Count - 1);
            left.AddRange(DP(a.GetRange(mi, a.Count - mi), tol)); return left;
        }

        // ---------- bâtiments ----------
        public static bool InVillage(double x, double z) { return GMath.Hypot(x - Relief.VILLAGE.x, z - 2720) < 620; }
        public static bool NearRoad(double x, double z, double d)
        {
            foreach (var r in G.roads) for (int i = 0; i < r.pts.Count - 1; i++) if (GMath.SegDist(x, z, r.pts[i].x, r.pts[i].z, r.pts[i + 1].x, r.pts[i + 1].z) < d) return true;
            return false;
        }
        public static BuildingAnalysis AnalyzeBuilding(string defId, double x, double z)
        {
            var def = Catalog.Building(defId); var r = new BuildingAnalysis { def = def, x = x, z = z, y = T.GetH(x, z) };
            r.slope = T.Slope(x, z); r.trees = T.TreesIn((tx, tz) => GMath.Hypot(tx - x, tz - z) < def.r + 4, x - def.r - 4, z - def.r - 4, x + def.r + 4, z + def.r + 4, false);
            r.earth = R(r.slope * def.r * def.r * 9 / 100) * 100;
            r.cost = def.cost + r.earth + r.trees * 120;
            if (!Relief.Inside(x, z, 100)) { r.err = "Hors des limites du domaine."; return r; }
            if (r.slope > (def.r > 25 ? 0.4 : 0.65)) { r.err = "Terrain trop raide ici (" + R(r.slope * 100) + " %)."; return r; }
            foreach (var d in Const.DECOR) if (GMath.Hypot(d.x - x, d.z - z) < d.r + def.r) { r.err = "Emplacement occupé par une maison du village."; return r; }
            foreach (var b in G.buildings) if (GMath.Hypot(b.x - x, b.z - z) < b.Def.r + def.r) { r.err = "Emplacement occupé par : " + b.Def.name + "."; return r; }
            foreach (var l in G.lifts)
            {
                bool funi = Catalog.Lift(l.type).kind == "funi";
                if (funi ? Math.Min(GMath.Hypot(x - l.a.x, z - l.a.z), GMath.Hypot(x - l.b.x, z - l.b.z)) < def.r + 16 : GMath.SegDist(x, z, l.a.x, l.a.z, l.b.x, l.b.z) < def.r + 8)
                { r.err = (funi ? "Trop près d'une gare du " : "Trop près de la ligne du ") + l.name + "."; return r; }
            }
            foreach (var p in G.pistes) { double s; if (def.look != "snowpark" && def.look != "stadium" && ClosestOnPiste(p, x, z, out s) < def.r + p.width / 2 - 4) { r.err = "Sur la piste " + p.name + " : décalez le bâtiment sur le bord."; return r; } }
            if (def.road && !InVillage(x, z) && !NearRoad(x, z, 130)) { r.err = "Doit être desservi par une route (à moins de 130 m) ou placé au village."; return r; }
            double nd; var nn = Net.NearestNode(x, z, out nd);
            if (!def.road && def.type != "garage" && def.type != "heli" && !(nn != null && nd < 240) && !InVillage(x, z)) r.warn = "Non relié au domaine : placez-le près d'une gare ou d'une extrémité de piste pour que les visiteurs y accèdent.";
            if (!CanPay(r.cost)) r.warn = "Fonds insuffisants.";
            r.ok = true; return r;
        }
        public static Building CommitBuilding(BuildingAnalysis an, double rot)
        {
            if (!Spend(an.cost, "Construction")) return null;
            var def = an.def;
            T.TreesIn((tx, tz) => GMath.Hypot(tx - an.x, tz - an.z) < def.r + 4, an.x - def.r - 4, an.z - def.r - 4, an.x + def.r + 4, an.z + def.r + 4, true);
            T.Flatten(an.x, an.z, def.r, an.y); G.flats.Add(new Flat { x = an.x, z = an.z, r = def.r, h = an.y });
            var b = MakeBuilding(def, an.x, an.z, rot);
            G.buildings.Add(b); Net.Rebuild(); Emit("building:add", b); Emit("world");
            return b;
        }
        public static int Bcap(Building b)
        {
            var def = b.Def;
            if (def.type == "garage") return b.machines > 0 ? b.machines : (int)def.cap;
            if (def.type == "rescue") return b.patrol > 0 ? b.patrol : (int)def.cap;
            return (int)R(def.cap * (1 + 0.5 * (b.level - 1)) * Mgmt.CapMul(b));
        }
        public static double UpgradeCost(Building b) { return R(b.Def.cost * 0.6 * b.level); }

        // ---------- routes ----------
        /// <summary>Une route qui en rencontre une autre s'y raccorde (elle s'arrête au carrefour) au lieu de la chevaucher.</summary>
        public static List<P2> RoadJunction(IList<P2> pts)
        {
            double walked = 0;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                P2 a = pts[i], b = pts[i + 1]; double d = GMath.Hypot(b.x - a.x, b.z - a.z); int n = Math.Max(1, (int)Math.Ceiling(d / 8));
                for (int k = 1; k <= n; k++)
                {
                    double x = GMath.Lerp(a.x, b.x, (double)k / n), z = GMath.Lerp(a.z, b.z, (double)k / n), w = walked + d * k / n; if (w < 60) continue;
                    foreach (var rd in G.roads) for (int m = 0; m < rd.pts.Count - 1; m++)
                        {
                            double t; double dd = GMath.SegDist(x, z, rd.pts[m].x, rd.pts[m].z, rd.pts[m + 1].x, rd.pts[m + 1].z, out t); if (dd > 9) continue;
                            var o = pts.Take(i + 1).ToList(); o.Add(new P2(R(GMath.Lerp(rd.pts[m].x, rd.pts[m + 1].x, t)), R(GMath.Lerp(rd.pts[m].z, rd.pts[m + 1].z, t)))); return o;
                        }
                }
                walked += d;
            }
            return null;
        }
        public static RoadAnalysis AnalyzeRoad(IList<P2> pts)
        {
            var r = new RoadAnalysis();
            if (pts.Count < 2) { r.err = "Placez au moins deux points."; return r; }
            var jn = RoadJunction(pts); if (jn != null) { pts = jn; r.joins = true; }
            r.pts = pts.ToList();
            if (!InVillage(pts[0].x, pts[0].z) && !NearRoad(pts[0].x, pts[0].z, 90)) { r.err = "Une route doit partir du village ou d'une route existante."; return r; }
            for (int i = 0; i < pts.Count - 1; i++)
            {
                P2 a = pts[i], b = pts[i + 1]; double d = GMath.Hypot(b.x - a.x, b.z - a.z); int n = Math.Max(1, (int)Math.Ceiling(d / 25));
                for (int k = 0; k < n; k++)
                {
                    double h0 = T.GetH(GMath.Lerp(a.x, b.x, (double)k / n), GMath.Lerp(a.z, b.z, (double)k / n)), h1 = T.GetH(GMath.Lerp(a.x, b.x, (double)(k + 1) / n), GMath.Lerp(a.z, b.z, (double)(k + 1) / n));
                    r.maxSlope = Math.Max(r.maxSlope, Math.Abs(h1 - h0) / (d / n));
                }
                r.len += d;
                if (!Relief.Inside(b.x, b.z)) { r.err = "Hors des limites."; return r; }
            }
            r.trees = T.TreesNearPath(pts, 8, false);
            r.cost = R((r.len * 130 + r.trees * 120) / 100) * 100;
            if (r.maxSlope > 0.16) { r.err = "Pente trop forte pour une route (" + R(r.maxSlope * 100) + " %, max 16 %). Tracez des lacets."; return r; }
            if (!CanPay(r.cost)) r.warn = "Fonds insuffisants.";
            r.ok = true; return r;
        }
        public static Road CommitRoad(IList<P2> pts, RoadAnalysis an)
        {
            if (!Spend(an.cost, "Construction")) return null;
            if (an.pts != null) pts = an.pts;
            T.TreesNearPath(pts, 8, true);
            var r = new Road { id = G.nextId++, pts = pts.Select(p => new P2(R(p.x), R(p.z))).ToList() };
            G.roads.Add(r); Emit("world"); return r;
        }

        // ---------- démolition (jamais avec des visiteurs engagés) ----------
        public static string Demolish(object obj)
        {
            var l = obj as Lift; var p = obj as Piste; var b = obj as Building; var rd = obj as Road;
            if (l != null)
            {
                if (l.queue.Count > 0 || l.veh.Any(v => v.Count > 0)) return "Des visiteurs utilisent encore cette remontée : fermez-la et attendez qu'elle se vide.";
                G.lifts.Remove(l); Earn(R(l.cost * 0.25), "Revente");
            }
            else if (p != null) { if (p.on > 0) return "Des skieurs sont encore sur cette piste : fermez-la d'abord."; G.pistes.Remove(p); }
            else if (b != null)
            {
                if (b.occ > 0 || b.queue.Count > 0) return "Bâtiment occupé : fermez-le et attendez la sortie des clients.";
                if (b.type == "ticket" && G.buildings.Count(x => x.type == "ticket") < 2) return "Il faut conserver au moins une billetterie.";
                G.buildings.Remove(b); Earn(R(b.Def.cost * 0.25), "Revente");
            }
            else if (rd != null) { if (rd.@fixed) return "La route d'accès ne peut pas être supprimée."; G.roads.Remove(rd); }
            Net.Rebuild(); Emit("removed", obj); Emit("world");
            return null;
        }

        // ---------- statistiques ----------
        public static Stats Stats()
        {
            var s = new Stats { lifts = G.lifts.Count, bestDay = G.bestDay, sat = G.sat, prestige = G.prestige };
            int hub = Net.Hub.id;
            foreach (var p in G.pistes)
            {
                s.km += p.len / 1000; if (p.open) { if (p.diff <= 1) s.easy++; if (p.diff == 2) s.red++; if (p.diff == 3) s.black++; }
                if (Net.From(p.nb.id, 3, Network.PLAN).dist[hub] < double.PositiveInfinity && Net.From(hub, 3, Network.PLAN).dist[p.na.id] < double.PositiveInfinity) s.connected++;
            }
            double lo = 1e9;
            foreach (var l in G.lifts) { s.topAlt = Math.Max(s.topAlt, l.b.y); lo = Math.Min(lo, l.a.y); }
            s.vert = G.lifts.Count > 0 ? s.topAlt - lo : 0;
            foreach (var b in G.buildings)
            {
                var def = b.Def; s.AddCount(def.type, 1); if (def.id != def.type) s.AddCount(def.id, 1);
                if (def.type == "lodging") s.beds += Bcap(b); if (def.type == "parking") s.access += Bcap(b);
                if (def.type == "lodging" && def.seg == 2) s.luxBeds += Bcap(b);
            }
            s.eventsHosted = G.eventsHosted.Values.Sum(); int h; s.worldCup = G.eventsHosted.TryGetValue("worldcup", out h) ? h : 0; s.olympics = G.eventsHosted.TryGetValue("olympics", out h) ? h : 0;
            var hs = G.history.Skip(Math.Max(0, G.history.Count - 7)).ToList(); s.satAvg = hs.Count > 0 ? hs.Average(x => x.sat) : G.sat;
            s.restaurants = G.buildings.Count(b => b.Def.type == "food");
            s.activities = G.buildings.Where(b => b.Def.type == "activity").Select(b => b.type).Distinct().Count();
            s.premiumLift = G.lifts.Any(l => l.type == "funicular" || l.type == "cablecar" || l.type == "three_s");
            s.stars = PrestigeStars(G.prestige);
            s.fame = G.fame; s.debt = G.loan; s.machines = Mgmt.GroomerCount(); s.patrol = Mgmt.PatrolCount(); s.todayV = G.visitorsToday;
            s.capacity = G.lifts.Sum(l => l.capacity); s.minCond = G.lifts.Count > 0 ? G.lifts.Min(l => l.cond) : 1;
            s.profit7 = hs.Count > 0 ? hs.Average(x => x.inc - x.exp) : -1;
            foreach (var e in Catalog.EVENTS) s.SetHosted(e.id, G.eventsHosted.TryGetValue(e.id, out h) ? h : 0);
            s.medals = G.medals.Values.Sum(); s.research_olympic = Researched("olympic");
            s.bidOk = (G.bid != null && (G.bid.phase == "prep" || G.bid.phase == "games" || G.bid.phase == "done")) || G.OlympicsOpened; s.olyDay1 = G.OlympicsOpened;
            s.money = G.money; s.total = G.totalVisitors; s.year = G.year; s.loan = G.loan; s.research = G.research.done.Count;
            double night; s.night = G.ledger0.TryGetValue("Forfaits nocturnes", out night) && night > 0;
            return s;
        }

        // PRESTIGE (0-100, en étoiles) : le standing. Sans luxe, sans loisirs variés ni remontées d'exception, il reste plafonné.
        public static Stats UpdatePrestige()
        {
            var s = Stats(); var parts = new Dictionary<string, double>(); var caps = new List<KeyValuePair<double, string>>();
            int tech = 0; var kinds = new HashSet<string>();
            foreach (var l in G.lifts) { var d = Catalog.Lift(l.type); tech = Math.Max(tech, d.tier); kinds.Add(d.id); }
            double prem = (kinds.Contains("funicular") ? 4 : 0) + (kinds.Contains("cablecar") ? 4 : 0) + (kinds.Contains("three_s") ? 3 : 0) + (kinds.Contains("gondola_8") || kinds.Contains("gondola_10") ? 2 : 0) + (kinds.Contains("bubble_8") ? 1.5 : 0) + (kinds.Contains("chair_det_6") ? 1 : 0);
            int colors = new[] { 0, 1, 2, 3 }.Count(d => G.pistes.Any(p => p.diff == d));
            parts["domaine"] = Math.Min(28, Math.Min(12, s.km * 0.4) + Math.Min(6, s.vert / 300) + colors + Math.Min(6, Math.Max(0, (s.topAlt - 2000) / 250)));
            parts["remontees"] = Math.Min(14, tech * 1.1 + Math.Min(7, prem));
            double lux = 0, nights = 0, priceSum = 0, luxRating = 0; int luxN = 0;
            foreach (var b in G.buildings) { var d = b.Def; if (d.type != "lodging") continue; double c = Bcap(b); priceSum += b.price * c; nights += c; if (d.seg == 2) { lux += c; luxRating += b.rating; luxN++; } }
            double avgNight = nights > 0 ? priceSum / nights : 0;
            parts["hebergement"] = Math.Min(24, Math.Min(14, lux / 15) + Math.Min(6, Math.Max(0, (avgNight - 80) / 25)) + (luxN > 0 && luxRating / luxN >= 4 ? 4 : 0));
            int acts = G.buildings.Where(b => b.Def.type == "activity").Select(b => b.type).Distinct().Count();
            int premiumFood = G.buildings.Count(b => b.Def.type == "food" && (b.range == 2 || b.type == "gastro" || b.type == "alt_restaurant"));
            parts["loisirs"] = Math.Min(20, acts * 2 + (s.gastro > 0 ? 4 : 0) + (s.spa > 0 ? 3 : 0) + Math.Min(3, premiumFood));
            double ev = 0; foreach (var kv in G.eventsHosted) { var e = Catalog.Event(kv.Key); if (e != null) ev += e.prestige * Math.Min(2, kv.Value) * 0.35; }
            parts["evenements"] = Math.Min(8, ev);
            var rated = G.buildings.Where(b => Mgmt.IsService(b) || b.Def.type == "lodging").ToList(); double avgR = rated.Count > 0 ? rated.Average(b => b.rating) : 3;
            parts["service"] = GMath.Clamp((avgR - 3) * 3, 0, 6);
            double pt = parts.Values.Sum();
            if (lux == 0) caps.Add(new KeyValuePair<double, string>(55, "aucun hébergement de luxe"));
            if (acts < 3) caps.Add(new KeyValuePair<double, string>(60, "moins de 3 loisirs différents"));
            if (prem < 2) caps.Add(new KeyValuePair<double, string>(72, "aucune remontée d'exception (télécabine, téléphérique, 3S, funiculaire)"));
            if (s.gastro == 0 && s.spa == 0) caps.Add(new KeyValuePair<double, string>(80, "ni table gastronomique ni spa"));
            foreach (var c in caps) pt = Math.Min(pt, c.Key);
            G.prestige = GMath.Clamp(R(pt), 0, 100); G.prestigeParts = parts; G.prestigeCaps = caps.Select(c => c.Value + " (plafond " + c.Key + ")").ToList();
            return s;
        }
        public static double PrestigeStars(double p) { return R(GMath.Clamp(p, 0, 100) / 10) / 2; }

        public struct Attractivity { public double a, km; public int[] kinds; public Stats s; }
        public static Attractivity Attract()
        {
            var s = Stats(); double km = 0; var kinds = new int[4]; int hub = Net.Hub.id;
            foreach (var p in G.pistes) if (p.open && !p.snowClosed && p.edges.Count > 0 && Net.From(p.nb.id, 3, Network.PLAN).dist[hub] < double.PositiveInfinity && Net.From(hub, 3, Network.PLAN).dist[p.na.id] < double.PositiveInfinity) { km += p.len / 1000; kinds[p.diff]++; }
            double a = Math.Min(1, km / 22) * 0.55 + kinds.Count(k => k > 0) * 0.05 + Math.Min(0.1, s.vert / 12000);
            foreach (var b in G.buildings) a += b.Def.attract + (G.summer && b.Def.summer > 0 ? 0.04 : 0);
            return new Attractivity { a = Math.Min(1.25, a), km = km, kinds = kinds, s = s };
        }
    }

    /// <summary>Tas binaire (priorité, valeur) pour l'A*.</summary>
    public sealed class MinHeap
    {
        readonly List<double> k = new List<double>(); readonly List<int> v = new List<int>();
        public int Count { get { return v.Count; } }
        public void Push(double key, int val)
        {
            k.Add(key); v.Add(val); int i = v.Count - 1;
            while (i > 0) { int p = (i - 1) >> 1; if (k[p] <= k[i]) break; Swap(p, i); i = p; }
        }
        public int Pop()
        {
            int top = v[0], last = v.Count - 1; k[0] = k[last]; v[0] = v[last]; k.RemoveAt(last); v.RemoveAt(last);
            int i = 0;
            for (; ; ) { int a = 2 * i + 1, b = a + 1, m = i; if (a < v.Count && k[a] < k[m]) m = a; if (b < v.Count && k[b] < k[m]) m = b; if (m == i) break; Swap(m, i); i = m; }
            return top;
        }
        void Swap(int a, int b) { double tk = k[a]; k[a] = k[b]; k[b] = tk; int tv = v[a]; v[a] = v[b]; v[b] = tv; }
    }

    /// <summary>Réseau du domaine (GPS) : nœuds aux gares et extrémités de pistes, arêtes « remontée » et « piste ».</summary>
    public sealed class Network
    {
        public const int LIVE = 0, DOWN = 1, PLAN = 2;
        public List<Node> nodes = new List<Node>(); public List<Edge> edges = new List<Edge>(); public Node Hub; public int version, epoch;
        public sealed class Paths { public double[] dist; public Edge[] prev; }
        readonly Dictionary<long, Paths> cache = new Dictionary<long, Paths>();
        GameState G { get { return Game.G; } }

        public void Rebuild()
        {
            var old = nodes; nodes = new List<Node>(); edges = new List<Edge>();
            Func<double, double, Node> node = (x, z) =>
            {
                foreach (var nn in nodes) if (GMath.Hypot(nn.x - x, nn.z - z) < (nn.hub ? Relief.HUB_R : 75)) return nn;
                var n = new Node { id = nodes.Count, x = x, z = z, y = Game.T.GetH(x, z) }; nodes.Add(n); return n;
            };
            Hub = node(Relief.HUB.x, Relief.HUB.z); Hub.hub = true;
            foreach (var l in G.lifts) { l.na = node(l.a.x, l.a.z); l.nb = node(l.b.x, l.b.z); }
            foreach (var p in G.pistes) { var a = p.pts[0]; var b = p.pts[p.pts.Count - 1]; p.na = node(a.x, a.z); p.nb = node(b.x, b.z); }
            Func<Edge, Edge> E = e => { e.id = edges.Count; edges.Add(e); e.from.@out.Add(e); return e; };
            foreach (var l in G.lifts) { l.edge = null; if (l.na != l.nb) l.edge = E(new Edge { kind = "lift", lift = l, from = l.na, to = l.nb, len = l.L }); }
            foreach (var p in G.pistes)
            {
                var stops = new List<KeyValuePair<double, Node>> { new KeyValuePair<double, Node>(0, p.na) };
                foreach (var n in nodes) { if (n == p.na || n == p.nb) continue; double s; double d = Game.ClosestOnPiste(p, n.x, n.z, out s); if (d < 55 && s > 50 && s < p.len - 50) stops.Add(new KeyValuePair<double, Node>(s, n)); }
                stops.Sort((a, b) => a.Key.CompareTo(b.Key)); stops.Add(new KeyValuePair<double, Node>(p.len, p.nb));
                p.edges = new List<Edge>();
                for (int i = 0; i < stops.Count - 1; i++) if (stops[i].Value != stops[i + 1].Value) p.edges.Add(E(new Edge { kind = "piste", piste = p, from = stops[i].Value, to = stops[i + 1].Value, s0 = stops[i].Key, s1 = stops[i + 1].Key, len = stops[i + 1].Key - stops[i].Key }));
            }
            foreach (var b in G.buildings)
            {
                Node best = null; double bd = 260;
                foreach (var n in nodes) { double d = GMath.Hypot(n.x - b.x, n.z - b.z); if (d < bd) { bd = d; best = n; } }
                if (Game.InVillage(b.x, b.z)) best = Hub;
                b.node = best != null ? best.id : -1; if (best != null) best.services.Add(b);
            }
            version++; cache.Clear();
            Game.Emit("network", old);
        }
        public void Touch() { epoch++; cache.Clear(); }
        public Node NearestNode(double x, double z, out double dist)
        {
            Node best = null; dist = 1e9;
            foreach (var n in nodes) { double d = GMath.Hypot(n.x - x, n.z - z); if (d < dist) { dist = d; best = n; } }
            return best;
        }
        public bool EdgeUsable(Edge e, int maxDiff) { return e.kind == "lift" ? Game.LiftBoarding(e.lift) : Game.PisteOpen(e.piste) && e.piste.diff <= maxDiff; }
        public double EdgeTime(Edge e)
        {
            if (e.kind == "lift") { var l = e.lift; return l.L / (Catalog.Lift(l.type).speed * Const.MOTION) + l.queue.Count / Math.Max(1, l.capacity) * 3600 + 60; }
            return e.len / (9 * Const.MOTION);
        }
        /// <summary>Dijkstra depuis src. mode : LIVE (horaires et ouvertures), DOWN (pistes seules), PLAN (structure, pour les statistiques).</summary>
        public Paths From(int src, int maxDiff, int mode = LIVE)
        {
            long key = (long)src * 64 + maxDiff * 4 + mode;
            Paths r; if (cache.TryGetValue(key, out r)) return r;
            int n = nodes.Count; var dist = new double[n]; var prev = new Edge[n]; var done = new bool[n];
            for (int i = 0; i < n; i++) dist[i] = double.PositiveInfinity;
            dist[src] = 0;
            for (; ; )
            {
                int u = -1; double bd = double.PositiveInfinity;
                for (int i = 0; i < n; i++) if (!done[i] && dist[i] < bd) { bd = dist[i]; u = i; }
                if (u < 0) break; done[u] = true;
                foreach (var e in nodes[u].@out)
                {
                    if (mode == PLAN) { if (!(e.kind == "lift" ? e.lift.open && Game.LiftSeason(e.lift) : e.piste.open && !e.piste.snowClosed && e.piste.diff <= maxDiff)) continue; }
                    else if (mode == DOWN) { if (e.kind == "lift" || !(e.piste.diff <= maxDiff && e.piste.open)) continue; }
                    else if (!EdgeUsable(e, maxDiff)) continue;
                    double d = bd + EdgeTime(e);
                    if (d < dist[e.to.id]) { dist[e.to.id] = d; prev[e.to.id] = e; }
                }
            }
            r = new Paths { dist = dist, prev = prev }; cache[key] = r; return r;
        }
        public List<Edge> Path(int src, int dst, int maxDiff, int mode = LIVE)
        {
            var r = From(src, maxDiff, mode); if (double.IsPositiveInfinity(r.dist[dst])) return null;
            var path = new List<Edge>(); int n = dst;
            while (n != src) { var e = r.prev[n]; if (e == null) return null; path.Insert(0, e); n = e.from.id; }
            return path;
        }
        public double Time(int src, int dst, int maxDiff, int mode = LIVE) { return From(src, maxDiff, mode).dist[dst]; }
    }
}
