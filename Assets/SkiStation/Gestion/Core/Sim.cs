// SkiStation — Gestion : simulation. Horloge unique, visiteurs (besoins + décisions SKIER / MANGER / REPOS / RENTRER),
// familles et groupes, remontées physiques, services, cours de ski, secours, damage, fin de journée. Portage de Web/js/sim.js.
using System;
using System.Collections.Generic;
using System.Linq;

namespace SkiStation.Gestion
{
    public sealed class Group
    {
        public int id; public bool fam; public string last, name, mode; public List<Visitor> members = new List<Visitor>();
        public Piste target; public double targetT = -1e9, eatT = -1e9, arrive, leave; public int eatNode = -1, maxDiff = 3;
    }
    public sealed class Visitor
    {
        public int id; public string name; public ProfileDef prof; public int skill, seg; public bool child; public int color;
        public Building homeB; public P2 home; public bool lodger; public double arriveT, leaveT;
        public double hunger, energy, sat, spent, km, patience, spend; public int runs;
        public bool needRental, hasGear, hasPass, lesson, lessonDone, dined, noFood, noDinner, leaving, night, nightDone, carried, balkedOnce;
        public double lessonAt, lessonPrice; public string lessonKind;
        public string state = "pending", decision = "Arrive", reason = "", mode; public double sf = 1;
        public int node; public double x, z, y, hx, hz = 1, lat;
        public List<string> thoughts = new List<string>(); public HashSet<int> did = new HashSet<int>(); public bool didBar, didShop;
        public Group grp;
        // déplacements / états
        public double tx, tz, wspeed = 1; public string then; public Building svc; public double until, waitStart, svcWait, restGain, skipFood, balked, qPen, remain;
        public string state0 = ""; public List<Edge> path; public Piste target, lastPiste; public Lift lift; public int veh; public Edge edge; public double s, fallAt = -1;
        public int dn, waits, apres; public bool pOnDone;
    }
    public sealed class Rescue { public string kind; public double x, z, y, speed, t0; public Visitor v; public string phase; public Building post, src; public bool slow; public double sx, sz; }
    public sealed class Groomer { public double x, z, y, s, hx, hz = 1; public Building garage; public Piste piste; public string state; }
    public sealed class DemandInfo { public double total, day, lodgers, fair, demand, access, lost; public Game.Attractivity at; public string why; }
    public sealed class Obstacle { public double x, z, r; }

    public static class Sim
    {
        public static List<Visitor> Visitors = new List<Visitor>(), Arrivals = new List<Visitor>();
        public static List<Rescue> Rescues = new List<Rescue>(); public static List<Groomer> Groomers = new List<Groomer>();
        public static int Present, NextV = 1, Expected; public static double LastTouch;
        public static Dictionary<string, int> Fb = new Dictionary<string, int>();
        public static DemandInfo Today; public static bool Grooming; public static List<Piste> GroomList, GroomPlanned;
        public static List<Obstacle> Obstacles = new List<Obstacle>();
        static readonly HashSet<string> Flags = new HashSet<string>();
        const double M = Const.MOTION, WALK = 1.0; static readonly double[] SKI_V = { 5.5, 9, 12.5, 16 };
        static readonly int[] COLORS = { 0xe53935, 0x1e88e5, 0xfdd835, 0x43a047, 0xfb8c00, 0x8e24aa, 0x00acc1, 0xd81b60, 0x3949ab, 0x7cb342, 0xf4511e, 0x212121, 0xffffff, 0x6d4c41 };
        static GameState G { get { return Game.G; } }
        static Network Net { get { return Game.Net; } }
        static double Rnd() { return Rand.Next(); }

        static Sim()
        {
            Game.Bus += (ev, a, b) => { if (ev == "network") { RebuildObstacles(); OnNetwork(a as List<Node>); } };
        }

        // ---------------- demande et arrivées ----------------
        public static DemandInfo Demand()
        {
            var at = Game.Attract(); var s = at.s; double fair = FairPrice(at);
            if (at.km <= 0) return new DemandInfo { fair = fair, at = at, why = G.summer ? "Aucun sentier desservi : l'été, seuls télésièges, télécabines, téléphériques et funiculaires fonctionnent." : "Aucune piste reliée au front de neige par une remontée." };
            double bs = (G.summer ? 30 + 650 * Math.Pow(at.a, 1.1) : 40 + 1500 * Math.Pow(at.a, 1.1)) * (G.mode == "career" ? new[] { 1.15, 1, 0.88 }[GMath.Clamp(G.level, 0, 2)] : 1);
            double rep = 0.55 + G.reputation / 100 * 0.9;
            double cal = (Game.IsWeekend(G.day) ? 1.35 : 1) * (G.summer ? 1.15 : Game.IsHoliday(G.day) ? 1.3 : 1);
            double priceF = GMath.Clamp(1.6 - 0.6 * Game.PassNow() / fair, 0.2, 1.35);
            double mk = 1 + (G.marketing.social > 0 ? 0.15 : 0) + (G.marketing.tv > 0 ? 0.35 : 0);
            double ev = G.@event != null && G.@event.day <= G.day ? Catalog.Event(G.@event.id).boost : 1;
            double rental = s.rental > 0 || G.summer ? 1 : 0.74;
            double demand = bs * rep * cal * Weather.Def.demand * priceF * mk * ev * rental;
            double lodgers = Mgmt.AllocateLodging(demand, cal, ev);
            double day = Game.R(Math.Min(demand, s.access));
            double total = Math.Min(1500, day + lodgers);
            return new DemandInfo { total = total, day = Math.Max(0, total - lodgers), lodgers = Math.Min(lodgers, total), fair = fair, at = at, demand = Game.R(demand), access = s.access, lost = Math.Max(0, Game.R(demand - s.access)) };
        }
        public static double YieldMul() { return Game.Researched("yield") && (Game.IsWeekend(G.day) || G.summer || Game.IsHoliday(G.day)) ? 1.12 : 1; }
        public static double FairPrice(Game.Attractivity at) { return G.summer ? 9 + at.km * 0.45 + at.s.vert * 0.002 + at.a * 8 : 22 + at.km * 1.4 + at.s.vert * 0.004 + at.a * 12; }

        static ProfileDef PickProfile(bool comp)
        {
            var ws = Catalog.PROFILES.Select(p => p.w * (comp && p.id == "sportif" ? 2 : 1)).ToArray();
            double r = Rnd() * ws.Sum();
            for (int i = 0; i < ws.Length; i++) { r -= ws[i]; if (r <= 0) return Catalog.PROFILES[i]; }
            return Catalog.PROFILES[0];
        }
        static ProfileDef PF(string id) { return Catalog.PROFILES.First(p => p.id == id); }
        static double Tri(double a, double c, double b) { double u = Rnd(), f = (c - a) / (b - a); return u < f ? a + Math.Sqrt(u * (b - a) * (c - a)) : b - Math.Sqrt((1 - u) * (b - a) * (b - c)); }
        static int SegOf(Building b) { return b == null || b.Def.seg < 0 ? 0 : b.Def.seg; }

        public static void StartDay()
        {
            var d = Today = Demand(); var s = d.at.s;
            Arrivals = new List<Visitor>(); Visitors = new List<Visitor>(); Rescues = new List<Rescue>(); Groomers = new List<Groomer>(); Present = 0; Fb = new Dictionary<string, int>(); Grooming = false; GroomPlanned = null;
            Flags.Clear(); G.satToday = new List<double>(); G.visitorsToday = 0; G.incidentsToday = 0;
            foreach (var b in G.buildings) { b.visits = 0; b.revenue = 0; b.cogs = 0; b.occ = 0; b.queue = new List<Visitor>(); b.session = new Dictionary<double, int>(); b.busy = 0; }
            Mgmt.SecurePistes();
            foreach (var l in G.lifts) { l.queue = new List<Visitor>(); l.rides = 0; l.waitSum = 0; foreach (var v in l.veh) v.Clear(); }
            foreach (var p in G.pistes) p.on = 0;
            bool comp = G.@event != null && G.@event.day <= G.day && Catalog.Event(G.@event.id).comp;
            var parkings = G.buildings.Where(b => b.Def.type == "parking" && b.open).ToList();
            bool hasKids = s.kids > 0, hasSchool = s.school > 0;
            Action<Building, P2, bool, ProfileDef, Group> mk = (homeB, home, lodger, pr, grp) =>
            {
                pr = pr ?? PickProfile(comp); double r = Rnd(); int skill = 0;
                for (int i = 0; i < 4; i++) { r -= pr.skill[i]; if (r <= 0) { skill = i; break; } }
                double arrive = grp != null && grp.arrive > 0 ? grp.arrive + Rnd() * 240 : lodger ? Tri(8.4, 9.0, 10.2) * 3600 : Tri(8.0, 9.2, 11.6) * 3600;
                var v = new Visitor
                {
                    id = NextV++, name = Rand.Pick(Catalog.FIRST) + " " + (grp != null && grp.last != null ? grp.last : Rand.Pick(Catalog.LAST)), prof = pr, skill = skill, child = pr.child, color = Rand.Pick(COLORS),
                    homeB = homeB, home = home, lodger = lodger, arriveT = arrive, leaveT = (lodger ? Rand.Range(15.6, 16.9) : Math.Min(17.0, Math.Max(arrive / 3600 + 3.5, Rand.Range(14.6, 17.0)))) * 3600,
                    hunger = Rand.Range(0.08, 0.35), energy = Rand.Range(0.85, 1), sat = 0.62, patience = Rand.Range(0.7, 1.4),
                    spend = lodger ? new[] { 0.72, 1.05, 1.85 }[SegOf(homeB)] * Rand.Range(0.85, 1.2) : pr.spend * Rand.Range(0.7, 1.4),
                    needRental = s.rental > 0 && Rnd() < 0.32,
                    lesson = G.summer ? false : pr.child ? (hasKids && Rnd() < 0.8) : (hasSchool && (skill == 0 ? Rnd() < 0.5 : skill == 1 && Rnd() < 0.1)),
                    node = Net.Hub.id, x = home.x, z = home.z, lat = Rnd() * 6.28
                };
                v.seg = v.spend < 0.85 ? 0 : v.spend < 1.4 ? 1 : 2;
                bool hb = homeB != null && homeB.opts != null && homeB.opts.hb;
                if (lodger && Rnd() < new[] { 0.5, 0.3, 0.06 }[SegOf(homeB)] && !hb) v.dined = true;
                if (lodger && homeB.opts != null && homeB.opts.shuttle) { v.x = Relief.HUB.x + (Rnd() - 0.5) * 30; v.z = Relief.HUB.z + 40 + Rnd() * 20; v.sat += 0.02; }
                if (v.lesson) v.lessonAt = (arrive < 9.4 * 3600 ? 9.75 : 14) * 3600;
                if (G.summer)
                {
                    bool bike = !v.child && Rnd() < (pr.id == "sportif" ? 0.85 : pr.id == "fetard" ? 0.6 : 0.3) && !(grp != null && grp.fam);
                    v.mode = grp != null && grp.mode != null ? grp.mode : bike ? "vtt" : "rando"; if (grp != null && grp.mode == null) grp.mode = v.mode;
                    if (v.mode == "rando") { v.sf = 0.2; v.skill = 3; v.needRental = false; } else { v.sf = 0.85; v.needRental = s.rental > 0 && Rnd() < 0.45; }
                }
                if (grp != null) { if (grp.arrive == 0) { grp.arrive = arrive; grp.leave = v.leaveT; } else v.leaveT = grp.leave + Rnd() * 300; v.grp = grp; grp.members.Add(v); grp.maxDiff = Math.Min(grp.maxDiff, Math.Min(3, skill + 1)); }
                Arrivals.Add(v);
            };
            Func<Building, P2, bool, double, int> party = (homeB, home, lodger, max) =>
            {
                var pr = PickProfile(comp); bool fam = pr.id == "famille" || pr.id == "enfant";
                int n = Rnd() < (fam ? 0.15 : 0.45) ? 1 : 2 + (int)Math.Floor(Rnd() * (fam ? 3 : 4)); n = (int)Math.Min(n, max);
                if (n < 2) { mk(homeB, home, lodger, pr.id == "enfant" ? PF("famille") : pr, null); return 1; }
                string last = Rand.Pick(Catalog.LAST);
                var grp = new Group { id = NextV, fam = fam, last = fam ? last : null, name = fam ? "Famille " + last : "Groupe d'amis" };
                int adults = fam ? Math.Min(n, Rnd() < 0.3 ? 1 : 2) : n;
                for (int i = 0; i < n; i++) mk(homeB, home, lodger, fam ? PF(i < adults ? "famille" : "enfant") : pr, grp);
                return n;
            };
            int k = 0;
            for (int i = 0; i < d.day;)
            {
                Building hb = null; P2 home = Const.ARRIVAL;
                if (parkings.Count > 0) { hb = parkings[k % parkings.Count]; home = new P2(hb.x, hb.z); k++; }
                i += party(hb, home, false, d.day - i);
            }
            double left = d.lodgers;
            foreach (var b in G.buildings) if (b.Def.type == "lodging" && b.open) for (int i = 0; i < b.guests && left > 0;) { int n = party(b, new P2(b.x, b.z), true, Math.Min(b.guests - i, left)); i += n; left -= n; }
            Arrivals = Arrivals.OrderBy(v => v.arriveT).ToList();
            Expected = Arrivals.Count;
        }

        // ---------------- pensées ----------------
        public static void Think(Visitor v, string text, string cat, double delta = 0)
        {
            if (delta != 0) v.sat = GMath.Clamp(v.sat + delta, 0, 1);
            v.thoughts.Insert(0, text); if (v.thoughts.Count > 5) v.thoughts.RemoveAt(5);
            if (cat != null) { int c; Fb.TryGetValue(cat, out c); Fb[cat] = c + 1; }
        }
        static void WalkTo(Visitor v, double x, double z, string then, double speed = WALK) { v.state = "walk"; v.tx = x; v.tz = z; v.then = then; v.wspeed = speed; }
        static double Jitter(double r) { return (Rnd() - 0.5) * 2 * r; }
        static Node NodeOf(Visitor v) { return v.node >= 0 && v.node < Net.nodes.Count ? Net.nodes[v.node] : Net.Hub; }

        // ---------------- services ----------------
        static Building ServicesAt(Node node, string type, Visitor v)
        {
            Building best = null; double bs = -1e9;
            foreach (var b in node.services)
            {
                var def = b.Def;
                if (def.type != type || !Mgmt.IsOpen(b)) continue;
                double rf = Mgmt.RefPrice(b);
                if (b.price > rf * (1.2 + 0.5 * v.spend)) continue;
                if (type == "food" && b.range == 2 && v.seg == 0 && Rnd() < 0.8) continue;
                if (!string.IsNullOrEmpty(def.tags) && type == "activity" && v.did.Contains(b.id)) continue;
                double sc = Mgmt.Quality(b) * (0.6 + 0.4 * v.spend) + b.rating * 0.06 - b.queue.Count / Math.Max(4.0, Game.Bcap(b)) - (b.price / (rf != 0 ? rf : 1)) * 0.2 - Math.Abs(b.range - v.seg) * 0.12 + Rnd() * 0.3;
                if (sc > bs) { bs = sc; best = b; }
            }
            return best;
        }
        static void GoService(Visitor v, Building b, string label, string why = "")
        {
            v.svc = b; v.decision = label; v.reason = why ?? "";
            var def = b.Def; WalkTo(v, b.x + Jitter(def.r * 0.5), b.z + def.r * 0.7 + Jitter(3), "service");
        }
        static void TryEnter(Visitor v)
        {
            var b = v.svc;
            if (b == null || !G.buildings.Contains(b) || !Mgmt.IsOpen(b)) { v.svc = null; BackToNode(v); return; }
            var def = b.Def;
            if (b.occ < Game.Bcap(b)) { Enter(v, b); return; }
            if (b.queue.Count > Game.Bcap(b) * 0.8 * v.patience + 4 && def.type != "ticket" && def.type != "rental")
            {
                if (!v.did.Contains(b.id)) Think(v, "Trop de monde : " + def.name, "Attente aux services", -0.04); v.did.Add(b.id); v.svc = null; v.skipFood = G.time + 1800; BackToNode(v); return;
            }
            b.queue.Add(v); v.state = "queueS"; v.state0 = "q"; v.waitStart = G.time;
        }
        static void Enter(Visitor v, Building b)
        {
            var def = b.Def;
            b.occ++; v.state = "inS"; v.svcWait = v.waitStart > 0 && v.state0 == "q" ? G.time - v.waitStart : 0; v.state0 = "";
            v.until = G.time + (def.type == "ticket" ? 2 : Mgmt.Dur(b)) * 60 * Rand.Range(0.8, 1.2);
        }
        static void FinishService(Visitor v)
        {
            var b = v.svc; var def = b.Def; b.occ--; b.visits++;
            double price = b.price; string cat = def.cat; double q = Mgmt.Quality(b), rf = Mgmt.RefPrice(b);
            if (def.type == "ticket")
            {
                price = Game.PassNow() * (v.child ? 0.6 : v.lodger ? 0.88 : 1) * YieldMul(); cat = "Forfaits"; v.hasPass = true;
                double fair = Today.fair; v.sat += GMath.Clamp((fair - Game.PassNow()) / fair * 0.18, -0.2, 0.08);
                if (Game.PassNow() > fair * 1.25) Think(v, "Le forfait est cher pour ce domaine", "Prix du forfait");
            }
            else if (def.type == "rental") { v.hasGear = true; cat = "Location"; }
            else if (def.type == "food")
            {
                cat = "Restauration"; v.hunger = def.id == "snack" ? Math.Max(0, v.hunger - 0.65) : 0; v.energy = Math.Min(1, v.energy + 0.25);
                Think(v, q > 0.8 ? "Excellent repas !" : q < 0.42 ? "Repas décevant" : "Repas correct", q < 0.42 ? "Qualité des restaurants" : null, (q - 0.5) * 0.16 + (b.node != Net.Hub.id ? 0.03 : 0));
                if (v.decision == "Dîner") v.dined = true;
            }
            else if (def.type == "drink") { cat = "Restauration"; v.energy = Math.Min(1, v.energy + 0.3); v.hunger = Math.Max(0, v.hunger - 0.15); v.sat += 0.04; }
            else if (def.type == "shop") { cat = "Boutiques"; v.sat += 0.02; }
            else if (def.type == "activity") { cat = "Activités"; Think(v, "Super moment : " + def.name, null, (def.fun > 0 ? def.fun : 0.05) * (0.6 + q * 0.5)); v.energy -= 0.06; }
            v.did.Add(b.id);
            if (price > 0)
            {
                Game.Earn(price, cat); b.revenue += price; v.spent += price; v.sat -= Math.Max(0, price / (rf != 0 ? rf : price) - 1) * 0.08;
                double cg = price * Mgmt.CogsRate(b); if (cg > 0) { Game.Charge(cg, "Achats"); b.cogs += cg; }
            }
            if (Mgmt.IsService(b)) Mgmt.Rate(b, GMath.Clamp(q * 5.2 - v.svcWait / 600 - Math.Max(0, b.price / (rf != 0 ? rf : 1) - 1) * 3 + Rnd() * 0.6 - 0.3, 0, 5));
            while (b.queue.Count > 0 && b.occ < Game.Bcap(b))
            {
                var w0 = b.queue[0]; b.queue.RemoveAt(0);
                if (w0.state == "queueS") { double w = G.time - w0.waitStart; if (w > 900) Think(w0, "Longue attente : " + def.name, "Attente aux services", -Math.Min(0.1, w / 36000)); Enter(w0, b); }
            }
            v.svc = null; BackToNode(v);
        }
        static void BackToNode(Visitor v) { var n = NodeOf(v); WalkTo(v, n.x + Jitter(12), n.z + Jitter(12), "node"); }

        // ---------------- décisions ----------------
        static void GoHome(Visitor v, string why) { v.decision = "Rentrer"; v.reason = why; v.leaving = true; WalkTo(v, v.home.x + Jitter(8), v.home.z + Jitter(8), "exit"); }
        static void HeadToHub(Visitor v, string why)
        {
            int hub = Net.Hub.id, md = Math.Min(3, v.skill + 1);
            v.decision = "Rentrer"; v.reason = why; v.target = null;
            var path = G.time < Const.LIFT_CLOSE - 300 ? (Net.Path(v.node, hub, md) ?? Net.Path(v.node, hub, 3)) : null;
            if (path == null) path = Net.Path(v.node, hub, 3, Network.DOWN);
            if (path == null)
            {
                Think(v, "Bloqué en altitude : retour à pied !", "Bloqués en altitude", -0.3);
                v.node = hub; WalkTo(v, Relief.HUB.x + Jitter(15), Relief.HUB.z + Jitter(15), "node", WALK * 1.6); return;
            }
            v.path = path; var e = v.path[0]; v.path.RemoveAt(0); TakeEdge(v, e);
        }
        static Node FindFoodNode(Visitor v, double remain)
        {
            int md = Math.Min(3, v.skill + 1), hub = Net.Hub.id; var from = Net.From(v.node, md);
            Node best = null; double bt = double.PositiveInfinity;
            foreach (var n in Net.nodes)
            {
                if (n.id == v.node || double.IsPositiveInfinity(from.dist[n.id])) continue;
                if (!n.services.Any(b => b.Def.type == "food" && b.open)) continue;
                double back = n.hub ? 0 : Net.Time(n.id, hub, md), t = from.dist[n.id] + back;
                if (t + 2400 < remain && from.dist[n.id] < bt) { bt = from.dist[n.id]; best = n; }
            }
            return best;
        }
        static Edge Shift(List<Edge> l) { var e = l[0]; l.RemoveAt(0); return e; }

        public static void Decide(Visitor v)
        {
            if (++v.dn > 5) { v.state = "rest"; v.until = G.time + 300; return; }
            var n = NodeOf(v); int hub = Net.Hub.id; double now = G.time; bool atHub = n.id == hub; int md = Math.Min(3, v.skill + 1);
            v.path = null; v.target = null;
            if (v.leaving) { if (atHub) GoHome(v, v.reason); else HeadToHub(v, v.reason); return; }
            if (!v.hasPass) { var b = ServicesAt(Net.Hub, "ticket", v) ?? G.buildings.FirstOrDefault(x => x.type == "ticket"); if (b != null) GoService(v, b, "Achète son forfait"); else GoHome(v, "Pas de billetterie"); return; }
            if (v.needRental && !v.hasGear)
            {
                var b = ServicesAt(Net.Hub, "rental", v);
                if (b != null) { GoService(v, b, "Loue son matériel"); return; }
                Think(v, "Location trop chère ou fermée", "Location", -0.25); GoHome(v, "Pas de matériel"); return;
            }
            if (v.lesson && !v.lessonDone && atHub)
            {
                if (now > v.lessonAt + 1500) { if (v.lessonAt < 12 * 3600 && now < 14.2 * 3600) v.lessonAt = 14 * 3600; else v.lesson = false; }
                if (v.lesson && now > v.lessonAt - 3 * 3600)
                {
                    var b = G.buildings.FirstOrDefault(x => x.open && x.Def.type == (v.child ? "kids" : "school"));
                    if (b != null && now >= v.lessonAt - 1500) { JoinLesson(v, b); return; }
                    if (b != null && v.lessonAt - now < 4500) { v.state = "rest"; v.until = v.lessonAt - 1200; v.decision = "Attend son cours"; v.reason = "Cours à " + GMath.HHMM(v.lessonAt); return; }
                    if (b == null) v.lesson = false;
                }
            }
            bool inNight = v.night && Game.NightActive() && now < Const.NIGHT_CLOSE - 600;
            if (v.night && !inNight && now < Const.NIGHT_OPEN && now >= Const.LIFT_CLOSE - 600)
            {
                if (!atHub) { HeadToHub(v, "Pause avant la nocturne"); return; }
                var b = v.hunger > 0.45 ? ServicesAt(n, "food", v) : !v.didBar ? ServicesAt(n, "drink", v) : null;
                if (b != null) { if (b.Def.type == "drink") v.didBar = true; GoService(v, b, "Attend la nocturne", b.Def.name); return; }
                v.state = "rest"; v.until = Const.NIGHT_OPEN + Rnd() * 300; v.decision = "Attend la nocturne"; v.reason = "Ouverture à 18:30"; return;
            }
            bool liftsOpen = (now >= Const.LIFT_OPEN && now < Const.LIFT_CLOSE - 600) || inNight;
            double tHome = atHub ? 0 : Math.Min(Net.Time(v.node, hub, md), 5400);
            double remain = v.leaveT - now - tHome - 600; v.remain = remain;
            if ((now >= Const.LIFT_CLOSE - 600 && !inNight) || remain <= 0)
            {
                if (v.night && now >= Const.NIGHT_OPEN) v.night = false;
                if (!atHub) { HeadToHub(v, remain <= 0 ? "Heure de départ" : "Fermeture des remontées"); return; }
                ApresSki(v); return;
            }
            // MANGER
            bool lunch = now > 11.5 * 3600 && now < 14 * 3600;
            bool gEat = v.grp != null && now - v.grp.eatT < 1500 && v.grp.eatNode == n.id && v.hunger > 0.28;
            if ((v.hunger > 0.78 || (lunch && v.hunger > 0.5) || gEat) && !(v.skipFood > now))
            {
                var b = ServicesAt(n, "food", v);
                if (b != null) { if (v.grp != null && !gEat) { v.grp.eatT = now; v.grp.eatNode = n.id; } GoService(v, b, "Manger", gEat ? "Déjeune avec " + v.grp.name.ToLowerInvariant() : "Faim " + Game.R(v.hunger * 100) + " %"); return; }
                var fn = FindFoodNode(v, remain);
                if (fn != null) { v.path = Net.Path(v.node, fn.id, md); if (v.path != null && v.path.Count > 0) { v.decision = "Manger"; v.reason = "Restaurant le plus proche via le domaine"; TakeEdge(v, Shift(v.path)); return; } }
                if (!v.noFood) { v.noFood = true; Think(v, "Nulle part où manger !", "Manque de restauration", -0.12); }
                if (v.hunger > 0.97 && atHub) { GoHome(v, "Trop faim, rien à manger"); return; }
                v.skipFood = now + 2400;
            }
            // REPOS
            if (v.energy < 0.24)
            {
                var b = ServicesAt(n, "drink", v) ?? (v.hunger > 0.3 ? ServicesAt(n, "food", v) : null);
                if (b != null) { GoService(v, b, "Repos", "Énergie " + Game.R(v.energy * 100) + " %"); return; }
                if (v.energy < 0.1 && remain < 5400) { if (atHub) ApresSki(v); else HeadToHub(v, "Épuisé"); return; }
                v.state = "rest"; v.until = now + 600; v.restGain = 0.2; v.decision = "Repos"; v.reason = "Pause de 10 min, énergie " + Game.R(v.energy * 100) + " %"; return;
            }
            if (!liftsOpen) { v.state = "rest"; v.until = Math.Max(now + 300, Const.LIFT_OPEN + Rnd() * 120); v.decision = "Attend l'ouverture"; v.reason = "Remontées ouvertes à 09:00"; return; }
            if (Rnd() < 0.12) { var b = ActivityAt(n, v); if (b != null) { GoService(v, b, "Activité", b.Def.name); return; } }
            // SKIER : piste cible, budget temps vérifié sur le trajet complet + retour
            var from = Net.From(v.node, md); var grp = v.grp;
            Piste best = null; double bsc = -1e9; bool together = false;
            if (grp != null && grp.target != null && now - grp.targetT < 1800 && G.pistes.Contains(grp.target))
            {
                var p = grp.target; double t1 = from.dist[p.na.id], t2 = p.nb.id == hub ? 0 : Net.Time(p.nb.id, hub, md);
                if (Game.PisteOpen(p) && p.diff <= md && p.edges.Count > 0 && t1 < 2400 && !double.IsPositiveInfinity(t2) && t1 + p.len / (SKI_V[v.skill] * M * v.sf) + t2 <= remain) { best = p; together = true; }
            }
            if (best == null) foreach (var p in G.pistes)
                {
                    if (!Game.PisteOpen(p) || p.diff > md || p.edges.Count == 0) continue;
                    double t1 = from.dist[p.na.id]; if (double.IsPositiveInfinity(t1)) continue;
                    double t2 = p.nb.id == hub ? 0 : Net.Time(p.nb.id, hub, md); if (double.IsPositiveInfinity(t2)) continue;
                    if (t1 + p.len / (SKI_V[v.skill] * M * v.sf) + t2 > remain) continue;
                    int dd = p.diff - v.skill;
                    double sc = (dd == 0 ? 1 : dd == -1 ? 0.7 : dd == 1 ? 0.35 : 0.3) + Weather.Quality(p, v.skill) * 0.6 - Math.Min(1, p.on / (p.len * p.width / 500)) * 0.5 - t1 / 3600 * 0.7 + Rnd() * 0.55;
                    if (p == v.lastPiste) sc -= 0.25;
                    if (grp != null && p.diff > grp.maxDiff) sc -= 0.9;
                    if (v.prof.id == "contemplatif" || v.prof.id == "sportif") sc += (p.topAlt - 1650) / 2500 * 0.3;
                    if (sc > bsc) { bsc = sc; best = p; }
                }
            if (best == null)
            {
                if (!atHub) { HeadToHub(v, "Plus assez de temps pour une descente"); return; }
                var stopped = G.lifts.FirstOrDefault(l => l.open && Game.LiftSeason(l) && l.na == Net.Hub && (now < l.brokenUntil || l.windStop));
                if (stopped != null && now < Const.LIFT_CLOSE - 5400 && ++v.waits <= 10)
                {
                    if (v.waits == 1) Think(v, stopped.name + " à l'arrêt, j'attends", "Pannes", -0.05);
                    v.state = "rest"; v.until = now + 900; v.decision = "Attend la remise en service"; v.reason = stopped.name + " à l'arrêt"; return;
                }
                if (now < Const.LIFT_CLOSE - 3600 && v.runs == 0) { Think(v, "Aucune piste accessible pour mon niveau", "Pistes inadaptées au niveau", -0.3); GoHome(v, "Rien à skier"); return; }
                ApresSki(v); return;
            }
            if (grp != null && !together) { grp.target = best; grp.targetT = now; }
            if (together) v.sat = Math.Min(1, v.sat + 0.004);
            v.target = best; v.decision = "Skier"; v.reason = (together ? "Avec " + grp.name.ToLowerInvariant() + " : " : "Objectif : ") + best.name + " (" + Catalog.DIFF_NAMES[best.diff].ToLowerInvariant() + ")";
            v.path = Net.Path(v.node, best.na.id, md) ?? new List<Edge>();
            Advance(v);
        }
        static Building ActivityAt(Node n, Visitor v)
        {
            Building pick = null;
            foreach (var b in n.services) { var def = b.Def; if (def.type == "activity" && b.open && !v.did.Contains(b.id) && b.price <= def.price * (1.2 + 0.5 * v.spend + (def.price == 0 ? 1 : 0)) && (def.tags == v.prof.tags || Rnd() < 0.3)) pick = b; }
            return pick;
        }
        public static bool NightReachable()
        {
            if (!Game.NightOn()) return false;
            return G.pistes.Any(p => p.lit && p.open && !p.snowClosed && G.lifts.Any(l => l.night && l.open && l.na == Net.Hub && l.nb == p.na));
        }
        static void ApresSki(Visitor v)
        {
            if (!v.nightDone && G.time < Const.NIGHT_CLOSE - 3600 && NightReachable() && v.energy > 0.3 && v.skill > 0)
            {
                v.nightDone = true;
                double want = (v.lodger ? 0.35 : 0.12) + (v.prof.id == "fetard" || v.prof.id == "sportif" ? 0.25 : 0) - Math.Max(0, G.nightPrice - 14) * 0.02;
                if (Rnd() < want)
                {
                    v.night = true; v.leaveT = Const.NIGHT_CLOSE - 300 + Rnd() * 300;
                    double price = G.nightPrice * (v.lodger ? 0.85 : 1); Game.Earn(price, "Forfaits nocturnes"); v.spent += price;
                    Think(v, "Je prolonge avec le ski nocturne !", null, 0.03); v.dn = 0; Decide(v); return;
                }
            }
            bool hb = v.homeB != null && v.homeB.opts != null && v.homeB.opts.hb;
            if (v.lodger && !v.dined && !hb && !v.noDinner)
            {
                var evening = Net.Hub.services.Where(b => b.Def.type == "food" && b.open && b.hours == "eve").ToList();
                if (evening.Count == 0) v.noDinner = true;
                else if (G.time < 18.4 * 3600) { if (v.apres >= 1) { v.state = "rest"; v.until = 18.4 * 3600 + Rnd() * 1500; v.decision = "Attend le dîner"; v.reason = "Les restaurants servent le soir à partir de 18 h 30"; return; } }
                else if (G.time < 21.5 * 3600) { var b = ServicesAt(Net.Hub, "food", v); if (b != null) { GoService(v, b, "Dîner", b.Def.name); return; } v.noDinner = true; }
            }
            var hubN = Net.Hub; int n = ++v.apres;
            if (G.time < (v.nightDone && G.time > Const.NIGHT_OPEN ? 21.5 : 19.5) * 3600 && n <= (v.lodger ? 3 : 2) && Rnd() < (v.prof.id == "fetard" ? 0.85 : 0.55))
            {
                var b = ActivityAt(hubN, v) ?? (v.didBar ? null : ServicesAt(hubN, "drink", v)) ?? (Rnd() < 0.4 && !v.didShop ? ServicesAt(hubN, "shop", v) : null) ?? (v.hunger > 0.6 ? ServicesAt(hubN, "food", v) : null);
                if (b != null) { var t = b.Def.type; if (t == "drink") v.didBar = true; if (t == "shop") v.didShop = true; GoService(v, b, "Après-ski", b.Def.name); return; }
            }
            GoHome(v, "Journée terminée");
        }
        static void Advance(Visitor v)
        {
            var n = NodeOf(v);
            if (v.path != null && v.path.Count > 0) { TakeEdge(v, Shift(v.path)); return; }
            if (v.target != null)
            {
                foreach (var e in n.@out) if (e.kind == "piste" && e.piste == v.target) { TakeEdge(v, e); return; }
                v.lastPiste = v.target; v.target = null;
            }
            Decide(v);
        }
        static void TakeEdge(Visitor v, Edge e)
        {
            if (e == null || e.id >= Net.edges.Count || Net.edges[e.id] != e) { v.path = null; v.target = null; Decide(v); return; }
            bool forced = v.decision == "Rentrer";
            if (e.kind == "lift")
            {
                var l = e.lift;
                if (!Game.LiftBoarding(l)) { Net.Touch(); v.path = null; v.target = null; Decide(v); return; }
                double wait = l.queue.Count / Math.Max(1, l.capacity) * 3600;
                if (wait > 1500 * v.patience && !forced && v.balked == 0) { v.balked = G.time; Think(v, "File interminable au " + l.name, "Files aux remontées", -0.06); }
                l.queue.Add(v); v.state = "queueL"; v.lift = l; v.waitStart = G.time;
            }
            else
            {
                var p = e.piste;
                if (!Game.PisteOpen(p) && !forced) { Net.Touch(); v.path = null; v.target = null; Decide(v); return; }
                v.state = "ski"; v.edge = e; v.s = e.s0; p.on++;
                int dd = p.diff - v.skill;
                double risk = Mgmt.RiskFactor(p) * (v.mode == "rando" ? 0.1 : 1) * 0.0024 * (1 + 2.2 * Math.Max(0, dd)) * (1 + 2.5 * p.ice) * (1 + Math.Min(1.5, p.on / (p.len * p.width / 400))) * (Game.Researched("padding") ? 0.7 : 1) * (p.groomed < 0.3 ? 1.3 : 1) * (G.casual ? 0.5 : 1);
                v.fallAt = Rnd() < risk * e.len / 1000 ? Rand.Range(e.s0, e.s1) : -1;
            }
        }
        static void ArriveNode(Visitor v, Node node) { v.node = node.id; v.x = node.x + Jitter(8); v.z = node.z + Jitter(8); Advance(v); }

        // ---------------- cours de ski ----------------
        static void JoinLesson(Visitor v, Building b)
        {
            var def = b.Def; double key = v.lessonAt; int n; b.session.TryGetValue(key, out n);
            int cap = def.type == "kids" ? Game.Bcap(b) : b.staff * 8;
            if (n >= cap) { Think(v, "Cours complet", "Cours de ski complets", -0.12); v.lesson = false; Decide(v); return; }
            double price = b.price; int seats = 1; v.lessonKind = "collectif";
            if (def.type == "school" && v.spend > 1.25 && n + 8 <= cap) { price = Game.R(b.price * 2.4); seats = 8; v.lessonKind = "individuel"; }
            b.session[key] = n + seats;
            v.svc = b; v.decision = "Cours de ski"; v.reason = "Cours " + v.lessonKind + " à " + GMath.HHMM(v.lessonAt);
            v.lessonPrice = price; WalkTo(v, b.x + Jitter(10), b.z + Jitter(10), "lesson");
        }
        static void EndLesson(Visitor v)
        {
            var b = v.svc; v.lessonDone = true; v.svc = null;
            Game.Earn(v.lessonPrice, "Cours de ski"); if (b != null) { b.revenue += v.lessonPrice; b.visits++; } v.spent += v.lessonPrice;
            string medal;
            if (v.child) medal = Catalog.MEDALS[Math.Min(5, (int)Math.Floor(Rnd() * 3.2 + Rnd() * 3))];
            else if (v.skill == 0 && Rnd() < 0.45) { v.skill = 1; medal = "1re Étoile"; }
            else medal = v.skill == 0 ? "Flocon" : v.skill == 1 ? "Étoile de Bronze" : Rnd() < 0.5 ? "Flèche" : "Chamois";
            int c; G.medals.TryGetValue(medal, out c); G.medals[medal] = c + 1;
            Think(v, "Médaille obtenue : " + medal + " !", null, 0.13); v.energy -= 0.2; v.hunger += 0.1;
            BackToNode(v);
        }

        // ---------------- accidents et secours ----------------
        static void Fall(Visitor v)
        {
            double r = Rnd(); var p = v.edge.piste; v.fallAt = -1;
            if (r < 0.72) { v.state = "down"; v.until = G.time + 150; Think(v, "Chute sans gravité sur " + p.name, null, -0.05); return; }
            bool severe = r > 0.95;
            v.state = "hurt"; G.incidentsToday++; G.safety.incidents++; if (severe) G.safety.severe++; v.decision = "Blessé"; v.reason = "Attend les secours";
            Building post = null; double bd = 1e9;
            var heli = severe ? G.buildings.FirstOrDefault(b => b.type == "heliport" && b.open) : null;
            foreach (var b in G.buildings) if (b.type == "rescue" && b.open && b.busy < Game.Bcap(b)) { double d = GMath.Hypot(b.x - v.x, b.z - v.z); if (d < bd) { bd = d; post = b; } }
            var src = heli ?? post;
            if (src == null)
            {
                Think(v, "Blessé, et aucun secours sur le domaine !", "Sécurité", -0.5); G.reputation = Math.Max(0, G.reputation - (severe ? 2 : 0.8));
                Game.Log("🚑 Accident sur " + p.name + " sans poste de secours : évacuation très lente.", "bad");
                Rescues.Add(new Rescue { kind = "patrol", x = Relief.HUB.x, z = Relief.HUB.z, v = v, phase = "go", speed = 2.2, slow = true, t0 = G.time - 3000 });
            }
            else
            {
                if (post != null && heli == null) post.busy++;
                if (severe && heli == null) { G.reputation = Math.Max(0, G.reputation - 0.8); Game.Log("🚑 Accident grave sur " + p.name + " : sans héliport, l'évacuation en traîneau est longue.", "bad"); }
                else if (severe) Game.Log("🚁 Accident grave sur " + p.name + " : l'hélicoptère décolle.", "warn");
                Rescues.Add(new Rescue { kind = heli != null ? "heli" : "patrol", x = src.x, z = src.z, y = src.y, v = v, phase = "go", speed = heli != null ? 14 : (severe ? 2.6 : 4.5) * Mgmt.PatrolSpeed(), post = heli != null ? null : post, src = src, t0 = G.time });
            }
        }
        static void UpdateRescues(double dt)
        {
            for (int i = Rescues.Count - 1; i >= 0; i--)
            {
                var r = Rescues[i]; var v = r.v;
                double tx = r.phase == "go" ? v.x : r.kind == "heli" ? r.src.x : Relief.HUB.x, tz = r.phase == "go" ? v.z : r.kind == "heli" ? r.src.z : Relief.HUB.z;
                double d = GMath.Hypot(tx - r.x, tz - r.z), stp = r.speed * dt;
                if (d <= stp + 4)
                {
                    if (r.phase == "go")
                    {
                        r.phase = "back"; double w = G.time - r.t0; G.safety.respSum += w; G.safety.respN++;
                        Think(v, w < 900 ? "Secours arrivés très vite, bravo aux pisteurs" : "Secours trop lents…", w < 900 ? null : "Sécurité", w < 900 ? -0.1 : -0.3);
                    }
                    else
                    {
                        if (r.post != null) r.post.busy = Math.Max(0, r.post.busy - 1);
                        if (!v.pOnDone) { v.edge.piste.on--; v.pOnDone = true; }
                        Finalize(v); Rescues.RemoveAt(i);
                    }
                    continue;
                }
                r.x += (tx - r.x) / d * stp; r.z += (tz - r.z) / d * stp;
                r.y = Game.T.GetH(r.x, r.z) + (r.kind == "heli" ? 90 * Math.Min(1, d / 200) + 6 : 0);
                if (r.phase == "back") { v.x = r.x + 2; v.z = r.z; v.y = r.y - (r.kind == "heli" ? 4 : 0); v.carried = true; }
            }
        }

        // ---------------- remontées : véhicules physiques, embarquement réel ----------------
        static void UpdateLifts(double dt)
        {
            double hf = Game.Researched("handsfree") ? 1.12 : 1;
            foreach (var l in G.lifts)
            {
                var def = Catalog.Lift(l.type); bool boarding = Game.LiftBoarding(l), broken = G.time < l.brokenUntil;
                int riders = 0; foreach (var vh in l.veh) riders += vh.Count; l.riders = riders;
                if (boarding && Rnd() < dt / 3600 * 0.012 * (1.7 - l.cond) * new[] { 2.2, 1, 0.45 }[G.maint])
                {
                    l.brokenUntil = G.time + Rand.Range(900, 3000); Net.Touch();
                    Game.Log("🔧 Panne sur " + l.name + " : arrêt d'environ " + Game.R((l.brokenUntil - G.time) / 60) + " min.", "warn");
                    foreach (var vh in l.veh) foreach (var v in vh) Think(v, "Coincé sur " + l.name + " en panne", "Pannes", -0.08);
                }
                l.moving = !broken && (boarding || riders > 0 || (Game.LiftRunning(l) && G.time < Const.LIFT_CLOSE));
                if (!boarding && l.queue.Count > 0 && G.time >= Const.LIFT_OPEN && (!broken || G.time - l.queue[0].waitStart > 2400))
                {
                    var q = l.queue; l.queue = new List<Visitor>();
                    foreach (var v in q) { if (l.windStop || broken) Think(v, l.name + " arrêté", "Pannes", -0.04); v.dn = 0; Decide(v); }
                }
                if (!l.moving) continue;
                double adv = def.speed * M * hf * dt, L2 = 2 * l.L, old = l.phase;
                l.phase = (old + adv) % L2;
                for (int i = 0; i < l.n; i++)
                {
                    double s0 = (old + i * l.D) % L2, s1 = s0 + adv; var vh = l.veh[i];
                    if (s0 < l.L && s1 >= l.L && vh.Count > 0)
                    {
                        var list = vh.ToList(); vh.Clear();
                        foreach (var v in list) { v.lift = null; v.dn = 0; l.rides++; ArriveNode(v, l.nb); }
                    }
                    if (s1 >= L2 && boarding && l.queue.Count > 0)
                    {
                        for (int k = 0; k < def.seats && l.queue.Count > 0; k++)
                        {
                            var v = l.queue[0]; l.queue.RemoveAt(0); double w = G.time - v.waitStart;
                            l.waitSum += w;
                            if (w > 720) { double pen = Math.Min(Math.Min(0.1, (w - 720) / 3600 * 0.25), 0.3 - v.qPen); v.qPen += Math.Max(0, pen); Think(v, Game.R(w / 60) + " min de queue au " + l.name, "Files aux remontées", -Math.Max(0, pen)); }
                            v.sat += (def.comfort - 0.5) * 0.012;
                            v.state = "ride"; v.veh = i; vh.Add(v);
                        }
                    }
                }
            }
        }
        public static double LiftWait(Lift l) { return l.queue.Count / Math.Max(1, l.capacity) * 60; }

        // ---------------- damage en soirée ----------------
        static void StartGrooming()
        {
            Grooming = true; Groomers = new List<Groomer>();
            if (G.summer) return;
            GroomList = Mgmt.GroomPlan(); GroomPlanned = GroomList.ToList();
            foreach (var b in G.buildings) if (b.type == "garage" && b.open) for (int i = 0; i < Game.Bcap(b); i++) Groomers.Add(new Groomer { x = b.x + i * 6, z = b.z, y = b.y, garage = b, state = "idle" });
            if (Groomers.Count == 0 && G.pistes.Count > 0) Game.Log("Aucune dameuse : les pistes ne seront pas préparées cette nuit.", "warn");
        }
        static void UpdateGroomers(double dt)
        {
            double sp = Mgmt.GroomSpeed();
            foreach (var g in Groomers)
            {
                if (g.state == "idle")
                {
                    g.piste = null;
                    int i = GroomList.FindIndex(p => !(p.lit && Game.NightOn() && G.time < Const.NIGHT_CLOSE + 600));
                    if (i < 0) { if (GroomList.Count == 0) g.state = "home"; continue; }
                    g.piste = GroomList[i]; GroomList.RemoveAt(i); g.state = "travel"; g.s = g.piste.len;
                }
                if (g.state == "travel" || g.state == "home")
                {
                    double tgx, tgz; if (g.state == "home") { tgx = g.garage.x; tgz = g.garage.z; } else { var e = g.piste.pts[g.piste.pts.Count - 1]; tgx = e.x; tgz = e.z; }
                    double d = GMath.Hypot(tgx - g.x, tgz - g.z), stp = 5 * dt;
                    if (d < stp + 2) { g.state = g.state == "travel" ? "groom" : "parked"; continue; }
                    g.hx = (tgx - g.x) / d; g.hz = (tgz - g.z) / d; g.x += g.hx * stp; g.z += g.hz * stp; g.y = Game.T.GetH(g.x, g.z);
                }
                else if (g.state == "groom")
                {
                    g.s -= sp * dt; var p = g.piste;
                    if (g.s <= 0) { p.groomed = 1; p.ice *= 0.15; p.powder *= 0.5; p.groomedTonight = true; Game.Charge(Game.R(p.len * 0.4), "Damage"); g.state = "idle"; continue; }
                    var t = Game.PisteAt(p, g.s); g.x = t.x; g.z = t.z; g.y = Game.T.GetH(g.x, g.z); double dd = GMath.Hypot(t.dx, t.dz); if (dd == 0) dd = 1; g.hx = -t.dx / dd; g.hz = -t.dz / dd;
                }
            }
        }

        // ---------------- un visiteur ----------------
        static void Finalize(Visitor v)
        {
            v.state = "gone"; Present--;
            if (v.lodger && v.homeB != null && v.homeB.price > 0)
            {
                var b = v.homeB; var def = b.Def; double q = Mgmt.Quality(b);
                Game.Earn(b.price, "Hébergement"); b.revenue += b.price; b.visits++;
                if (b.opts.hb && !v.dined) { double dp = Mgmt.DinnerPrice(b); Game.Earn(dp, "Restauration"); b.revenue += dp; Game.Charge(dp * 0.38, "Achats"); b.cogs += dp * 0.38; v.dined = true; v.sat += 0.02; }
                if (!v.dined && !G.summer) { v.sat -= 0.03; int c; Fb.TryGetValue("Rien pour dîner", out c); Fb["Rien pour dîner"] = c + 1; }
                v.sat += (q - 0.5) * 0.14 - Math.Max(0, b.price / def.price - 1) * 0.1;
                Mgmt.Rate(b, GMath.Clamp(v.sat * 4 + q * 1.6 - Math.Max(0, b.price / def.price - 1) * 2, 0, 5));
            }
            if (v.runs == 0 && !v.lessonDone && !v.carried) v.sat -= 0.15;
            v.sat = GMath.Clamp(v.sat, 0, 1); G.satToday.Add(v.sat);
        }
        static void UpdateVisitor(Visitor v, double dt)
        {
            v.dn = 0;
            v.hunger += dt / 16200; v.energy -= dt / 90000;
            if (v.hunger > 0.9) v.sat -= dt * 0.000012;
            switch (v.state)
            {
                case "walk":
                    {
                        double dx = v.tx - v.x, dz = v.tz - v.z, d = GMath.Hypot(dx, dz), stp = v.wspeed * dt;
                        if (d <= stp)
                        {
                            v.x = v.tx; v.z = v.tz;
                            if (v.then == "service") TryEnter(v);
                            else if (v.then == "exit") Finalize(v);
                            else if (v.then == "lesson") { v.state = "lesson"; v.until = Math.Max(G.time + 3600, v.lessonAt + 7200); }
                            else Decide(v);
                        }
                        else
                        {
                            v.hx = dx / d; v.hz = dz / d; double nx = v.x + v.hx * stp, nz = v.z + v.hz * stp;
                            if (d > 6)
                                foreach (var o in Obstacles)
                                {
                                    double ox = nx - o.x, oz = nz - o.z, od = GMath.Hypot(ox, oz);
                                    if (od >= o.r || od < 0.01 || GMath.Hypot(v.tx - o.x, v.tz - o.z) < o.r + 3) continue;
                                    if (GMath.Hypot(v.x - o.x, v.z - o.z) < o.r - 0.5 || (v.x - o.x) * v.hx + (v.z - o.z) * v.hz > 0) continue;
                                    double px = v.x - o.x, pz = v.z - o.z, pd = GMath.Hypot(px, pz); if (pd == 0) pd = 1; double ttx = v.tx - o.x, ttz = v.tz - o.z;
                                    double ccw = px * ttz - pz * ttx > 0 ? 1 : -1, R = Math.Max(o.r, pd);
                                    nx = o.x + px / pd * R + (-pz / pd) * ccw * stp; nz = o.z + pz / pd * R + (px / pd) * ccw * stp;
                                }
                            v.x = nx; v.z = nz;
                        }
                        v.y = Game.T.GetH(v.x, v.z); break;
                    }
                case "ski":
                    {
                        var e = v.edge; var p = e.piste; var t = Game.PisteAt(p, v.s);
                        double sp = v.sf * SKI_V[v.skill] * GMath.Clamp(0.7 + t.slope * 1.3, 0.6, 1.35) * (1 - 0.25 * p.slush) * (1 - 0.12 * p.powder) * M;
                        v.s += sp * dt; v.lat += sp * dt * 0.035;
                        double d = GMath.Hypot(t.dx, t.dz); if (d == 0) d = 1; double amp = (p.width / 2 - 3) * (0.5 + 0.4 * Math.Sin(v.id));
                        double off = Math.Sin(v.lat) * amp, c = Math.Cos(v.lat) * 0.6;
                        v.hx = t.dx / d - (-t.dz / d) * -c; v.hz = t.dz / d - (t.dx / d) * -c;
                        v.x = t.x + (-t.dz / d) * off; v.z = t.z + (t.dx / d) * off; v.y = Game.T.GetH(v.x, v.z);
                        if (v.fallAt > 0 && v.s >= v.fallAt) { Fall(v); break; }
                        if (v.s >= e.s1)
                        {
                            p.on--; p.passes++; p.groomed = Math.Max(0, p.groomed - 0.25 / (p.len / 1000 * 320)); p.powder = Math.Max(0, p.powder - 0.0008);
                            int dd = p.diff - v.skill; double q = Weather.Quality(p, v.skill), crowd = Math.Min(1, p.on / (p.len * p.width / 450));
                            double fun = (dd == 0 ? 1 : dd == -1 ? 0.6 : dd == 1 ? 0.2 : dd < -1 ? 0.3 : -0.6) * (0.4 + 0.6 * q) - crowd * 0.5;
                            v.sat += fun * e.len / 1000 * 0.032;
                            if (q < 0.3 && Rnd() < 0.3) Think(v, Weather.State(p) + " sur " + p.name, "Qualité de neige", -0.03);
                            if (crowd > 0.8 && Rnd() < 0.2) Think(v, "Trop de monde sur " + p.name, "Pistes surchargées", -0.02);
                            if (dd > 1) Think(v, p.name + " est trop difficile pour moi", "Pistes inadaptées au niveau", -0.06);
                            v.energy -= e.len / 1000 * (0.03 + 0.016 * p.diff + (dd > 0 ? 0.03 : 0)); v.km += e.len / 1000;
                            if (e.to == p.nb) v.runs++;
                            ArriveNode(v, e.to);
                        }
                        break;
                    }
                case "queueS":
                    if (G.time - v.waitStart > 1500 * v.patience && v.svc.Def.type != "ticket")
                    {
                        var b = v.svc; b.queue.Remove(v);
                        if (!v.did.Contains(b.id)) Think(v, "J'abandonne la file : " + b.Def.name, "Attente aux services", -0.05); v.did.Add(b.id); v.svc = null; v.skipFood = G.time + 1800; BackToNode(v);
                    }
                    break;
                case "inS": if (G.time >= v.until) FinishService(v); break;
                case "rest": if (G.time >= v.until) { if (v.restGain > 0) { v.energy = Math.Min(1, v.energy + v.restGain); v.restGain = 0; } Decide(v); } break;
                case "lesson": if (G.time >= v.until) EndLesson(v); break;
                case "down": if (G.time >= v.until) v.state = "ski"; break;
            }
        }

        static void RebuildObstacles()
        {
            Obstacles = new List<Obstacle>(); if (G == null) return;
            foreach (var b in G.buildings) { var def = b.Def; var lk = def.look; if (lk == "parking" || lk == "bus" || lk == "garden" || lk == "snowpark" || lk == "luge" || lk == "rink") continue; Obstacles.Add(new Obstacle { x = b.x, z = b.z, r = Math.Min(def.r * 0.62, 16) }); }
            foreach (var d in Const.DECOR) Obstacles.Add(new Obstacle { x = d.x, z = d.z, r = d.r * 0.75 });
        }
        /// <summary>Remise en cohérence des visiteurs quand le réseau change en cours de journée.</summary>
        static void OnNetwork(List<Node> old)
        {
            if (old == null || old.Count == 0) return;
            foreach (var v in Visitors)
            {
                if (v.state == "gone") continue;
                if (v.node >= 0 && v.node < old.Count) { var o = old[v.node]; double dd; var nn = Net.NearestNode(o.x, o.z, out dd); v.node = nn != null ? nn.id : Net.Hub.id; } else v.node = Net.Hub.id;
                v.path = null; v.target = null;
                if ((v.state == "ski" || v.state == "down") && v.edge != null)
                {
                    var p = v.edge.piste; var e = p.edges.FirstOrDefault(x => v.s >= x.s0 - 1 && v.s < x.s1);
                    v.edge = e ?? new Edge { id = -1, piste = p, s0 = v.s, s1 = p.len, to = p.nb, from = p.na, len = p.len - v.s, kind = "piste" };
                }
            }
        }

        // ---------------- boucle principale ----------------
        static void Step(double dt)
        {
            G.time += dt;
            while (Arrivals.Count > 0 && Arrivals[0].arriveT <= G.time)
            {
                var v = Arrivals[0]; Arrivals.RemoveAt(0); v.state = "walk"; v.y = Game.T.GetH(v.x, v.z); v.dn = 0;
                if (v.lodger && !v.hasPass) { v.hasPass = true; double pr = Game.PassNow() * (v.child ? 0.6 : 0.88) * YieldMul(); Game.Earn(pr, "Forfaits"); v.spent += pr; }
                Visitors.Add(v); Present++; G.visitorsToday++; G.totalVisitors++; Decide(v);
            }
            if (G.time - LastTouch > 600 || G.time < LastTouch)
            {
                LastTouch = G.time; Net.Touch(); Weather.UpdateWind(); Weather.DayTick();
                if (Present > 0 && Rnd() < 0.5) Game.Emit("pulse");
            }
            if (!Flags.Contains("open") && G.time >= Const.LIFT_OPEN) { Flags.Add("open"); Net.Touch(); if (G.lifts.Count > 0) Game.Emit("toast", "🚡 09:00 — ouverture des remontées.", "info"); }
            if (!Flags.Contains("night") && G.time >= Const.NIGHT_OPEN && Game.NightOn()) { Flags.Add("night"); Net.Touch(); Game.Emit("toast", "🌙 18:30 — ouverture du ski nocturne sur les pistes éclairées.", "info"); }
            if (!Flags.Contains("nightEnd") && G.time >= Const.NIGHT_CLOSE && Game.NightOn()) { Flags.Add("nightEnd"); Net.Touch(); }
            if (!Flags.Contains("close") && G.time >= Const.LIFT_CLOSE) { Flags.Add("close"); Net.Touch(); if (G.lifts.Count > 0) Game.Emit("toast", "16:45 — fermeture des remontées, dernier retour station.", "info"); }
            UpdateLifts(dt);
            for (int i = 0; i < Visitors.Count; i++) if (Visitors[i].state != "gone") UpdateVisitor(Visitors[i], dt);
            if (Rescues.Count > 0) UpdateRescues(dt);
            if (!Grooming && G.time >= 17.25 * 3600) StartGrooming();
            if (Grooming) UpdateGroomers(dt);
            if (G.time >= Const.DAY_END || (G.time > 18 * 3600 && Present <= 0 && Arrivals.Count == 0 && Groomers.All(g => g.state == "parked"))) Game.EndDay();
        }
        /// <summary>À appeler à chaque image avec le temps réel écoulé (secondes).</summary>
        public static void Tick(double realDt)
        {
            if (G == null || G.paused) return;
            double dt = Math.Min(realDt, 0.1) * G.speed * Const.CLOCK_RATE;
            int n = Math.Min(10, (int)Math.Ceiling(dt / 3)); if (n < 1) return; dt /= n;
            for (int i = 0; i < n; i++) { Step(dt); if (G.paused) break; }
        }
        /// <summary>Simulation sans affichage, à pas fixe (tests, avance rapide).</summary>
        public static void StepFixed(double dt) { Step(dt); }
        /// <summary>Passe au lendemain quand la station est vide : le damage du soir est joué en accéléré.</summary>
        public static bool SkipToEnd()
        {
            if (Present > 0 || (Arrivals.Count > 0 && G.time < 17 * 3600)) return false;
            Arrivals.Clear();
            if (!Grooming) StartGrooming();
            for (double t = Math.Max(G.time, 17.25 * 3600); t < Const.DAY_END; t += 30) { G.time = t; UpdateGroomers(30); }
            G.time = Const.DAY_END; Game.EndDay(); return true;
        }
        internal static List<Piste> TakeGroomPlanned() { return GroomPlanned; }
        internal static void FinishVisitorsAtEndOfDay()
        {
            foreach (var v in Visitors) if (v.state != "gone") { Think(v, "Retour très tardif", "Bloqués en altitude", -0.15); if (v.state == "inS" && v.svc != null) v.svc.occ = 0; Finalize(v); }
        }
        internal static void ResetClock() { LastTouch = 0; }
    }
}
