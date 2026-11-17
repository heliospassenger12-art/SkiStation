// SkiStation — Gestion : scénarios (étapes, délais, échec, victoire), NOTORIÉTÉ, critères X Games / Jeux Olympiques,
// candidature olympique, station préconstruite. Portage de Web/js/scenario.js.
using System;
using System.Collections.Generic;
using System.Linq;

namespace SkiStation.Gestion
{
    public sealed class Crit { public string text; public Func<Stats, bool> test; public Func<Stats, string> val; public Crit(string t, Func<Stats, bool> f, Func<Stats, string> v) { text = t; test = f; val = v; } }
    public sealed class CritState { public string text, val; public bool ok; }
    public sealed class BidCheck { public bool ok; public List<string> why = new List<string>(); }

    public static class Scenarios
    {
        static GameState G { get { return Game.G; } }
        public const double BID_COST = 600000; public const int EVAL_DAYS = 25, PREP_DAYS = 100;
        /// <summary>Condition de victoire réutilisable par le mode compétition Alpine Empires : le jour 1 des Jeux.</summary>
        public const string VICTORY = "olympics_day1";
        /// <summary>Records par scénario (étoiles, jours) : à brancher sur PlayerPrefs côté Unity.</summary>
        public static Func<string, string> LoadRecords = k => null; public static Action<string, string> SaveRecords = (k, v) => { };

        public static ScenarioDef Cur { get { return G != null && G.scn != null ? Catalog.Scenario(G.scn.id) : null; } }
        public static Stats Stats() { return Game.Stats(); }

        // ---------- NOTORIÉTÉ : humeur des visiteurs et rapport qualité/prix ----------
        public static double ValueForMoney()
        {
            double fair = Sim.Today != null && Sim.Today.fair > 0 ? Sim.Today.fair : 26;
            double v = 55 + 70 * (1 - Game.PassNow() / fair), w = 0, r = 0, q = 0;
            foreach (var b in G.buildings)
            {
                var d = b.Def; if (!(Mgmt.IsService(b) || d.type == "lodging") || b.price == 0) continue;
                double k = Math.Max(1, b.visits + b.guests), rf = Mgmt.RefPrice(b); w += k; r += k * b.price / (rf != 0 ? rf : b.price); q += k * (b.rating - 3);
            }
            if (w > 0) v += 30 * (1 - r / w) + 8 * (q / w);
            return GMath.Clamp(v, 0, 100);
        }
        public static Dictionary<string, double> FameParts()
        {
            var s = Game.Stats(); var parts = new Dictionary<string, double>();
            parts["humeur"] = 0.5 * s.satAvg;
            parts["qualitePrix"] = 0.2 * ValueForMoney();
            parts["frequentation"] = 0.15 * Math.Min(100, 100 * Math.Log10(1 + G.bestDay) / Math.Log10(1501));
            parts["prestige"] = 0.1 * G.prestige;
            double ev = 0; foreach (var kv in G.eventsHosted) { var e = Catalog.Event(kv.Key); if (e != null) ev += Math.Min(2, kv.Value) * e.fameGain * 0.5; }
            parts["evenements"] = Math.Min(15, ev);
            parts["marketing"] = (G.marketing.tv > 0 ? 5 : 0) + (G.marketing.social > 0 ? 3 : 0);
            parts["fidelite"] = Loyalty.FamePart();
            return parts;
        }
        public static double FameTarget() { double b = FameParts().Values.Sum(); return GMath.Clamp(b * GMath.Clamp(0.25 + G.totalVisitors / 2500.0, 0.25, 1), 0, 100); }
        public static void DailyFame() { double t = FameTarget(); G.fame = GMath.Clamp(G.fame + (t - G.fame) * (t > G.fame ? 0.05 : 0.04), 0, 100); }

        // ---------- création d'une partie de scénario ----------
        public static void Setup(GameState g, NewGameOptions o)
        {
            var sc = Catalog.Scenario(o.scn); if (sc == null) return; var st = sc.start;
            g.scn = new ScnState { id = sc.id }; g.mode = "career"; g.money = st.money; g.fame = st.fame;
            if (st.rep >= 0) g.reputation = st.rep; if (st.sat >= 0) g.sat = st.sat;
            g.research.done = st.research.ToList(); g.tier = 0;
            foreach (var id in g.research.done) { var r = Catalog.Research(id); if (r != null && r.tier > 0) g.tier = Math.Max(g.tier, r.tier); }
            if (st.prebuilt == "decay") { double m = g.money; g.money = 1e9; BuildDecay(); g.money = m; g.ledger = new Ledger(); g.season = new Season(); g.history.Clear(); }
        }
        static Lift PlaceLift(string type, Rng rng, double min, double max, double spread = 1.8)
        {
            var m = Game.T.ReachMask();
            for (int i = 0; i < 900; i++)
            {
                var a = new P2(Relief.HUB.x + (rng.Next() - 0.5) * 240, Relief.HUB.z + (rng.Next() - 0.3) * 110);
                double ang = (rng.Next() - 0.5) * spread, len = min + rng.Next() * (max - min); var b = new P2(a.x + Math.Sin(ang) * len, a.z - Math.Cos(ang) * len);
                if (!Game.T.InReach(m, b.x, b.z)) continue;
                if (G.lifts.Any(l => GMath.Hypot(l.b.x - b.x, l.b.z - b.z) < 260)) continue;
                var an = Game.AnalyzeLift(type, a, b); if (an.ok) return Game.CommitLift(an);
            }
            return null;
        }
        static Piste PlacePiste(Lift l, int ease) { var r = Game.AutoPiste(l.b.Xz, null, 30, ease); return r.err != null || r.bad ? null : Game.CommitPiste(r.ctrl, r.an); }
        static Building PlaceBuilding(string type, Rng rng, double z)
        {
            for (int i = 0; i < 200; i++) { double x = -520 + rng.Next() * 1000; var an = Game.AnalyzeBuilding(type, x, z + (rng.Next() - 0.5) * 120); if (an.ok) return Game.CommitBuilding(an, 0); }
            return null;
        }
        /// <summary>Station vieillissante : remontées usées, pistes mal entretenues, peu de services, dettes, forfait trop cher.</summary>
        static void BuildDecay()
        {
            var rng = new Rng((uint)(G.seed * 3 + 11));
            var l1 = PlaceLift("tow_classic", rng, 380, 700); var l2 = PlaceLift("chair_fixed_2", rng, 900, 1300); var l3 = PlaceLift("tow_classic", rng, 650, 900);
            var plan = new[] { Tuple.Create(l1, 0, 0.32), Tuple.Create(l2, 2, 0.48), Tuple.Create(l3, 1, 0.4) };
            foreach (var t in plan) { if (t.Item1 == null) continue; t.Item1.cond = t.Item3; var p = PlacePiste(t.Item1, t.Item2); if (p != null) { p.groomed = 0.25; p.depth = 40; if (t.Item2 == 2) p.ice = 0.45; } }
            var sn = PlaceBuilding("snack", rng, 2600); var rt = PlaceBuilding("rental", rng, 2640); var gt = PlaceBuilding("gite", rng, 2900); var gg = PlaceBuilding("garage", rng, 2760);
            if (sn != null) { sn.range = 0; sn.staffLvl = 0; sn.rating = 1.7; }
            if (rt != null) { rt.range = 0; rt.rating = 2; }
            if (gt != null) gt.rating = 2.1;
            if (gg != null) gg.machines = 1;
            G.loans = new List<Loan> { new Loan { id = G.nextId++, name = "Prêt d'équipement (ancien)", left = 380000, pay = 3200, rate = 0.0009, daysLeft = 118 }, new Loan { id = G.nextId++, name = "Crédit de trésorerie", left = 90000, pay = 2500, rate = 0.0013, daysLeft = 36 } };
            Mgmt.SyncLoan(); G.passPrice = 38; G.maint = 0; G.wage = 0; G.bestDay = 60; G.snowBase = 40;
            foreach (var p in G.pistes) p.cannons = false;
            Game.Net.Rebuild(); Game.UpdatePrestige();
        }

        // ---------- étapes, échec, victoire ----------
        public static List<CritState> GoalState(StageDef stage) { var s = Stats(); return stage.goals.Select(g => new CritState { text = g.text, ok = g.test(s), val = g.val != null ? g.val(s) : "" }).ToList(); }
        public static string Hint()
        {
            var sc = Cur; if (sc == null || G.scn.done || G.scn.failed) return null;
            var st = sc.stages[G.scn.stage]; var todo = GoalState(st).FirstOrDefault(g => !g.ok);
            return todo != null ? "Objectif de l'étape « " + st.title + " » : " + todo.text + (todo.val != "" ? " (" + todo.val + ")" : "") + "." : null;
        }
        public static int? DaysLeft() { var sc = Cur; return sc != null && sc.deadline > 0 ? Math.Max(0, sc.deadline - G.elapsed) : (int?)null; }
        static void SaveRecord(string id, int stars, int days)
        {
            var key = "skistation_scn_" + id; var old = LoadRecords(key);
            if (old != null) { var p = old.Split('|'); int os = int.Parse(p[0]), od = int.Parse(p[1]); if (os > stars || (os == stars && od <= days)) return; }
            SaveRecords(key, stars + "|" + days);
        }
        public static void CheckCareer(DayReport report)
        {
            var sc = Cur; if (sc == null || G.scn.done || G.scn.failed) return;
            Action<string> note = m => { if (report != null) report.notes.Add(m); else Game.Log(m, "good"); };
            if (G.money < -(sc.bankrupt > 0 ? sc.bankrupt : 200000) && !G.unlimited)
            {
                G.scn.failed = true; if (report != null) { report.failed = true; report.failTitle = "Faillite"; report.failText = "Vos dettes dépassent ce que la banque tolère : la station est mise sous tutelle. Vous pouvez continuer en mode libre ou recommencer."; }
                Game.Emit("career"); return;
            }
            if (sc.deadline > 0 && G.elapsed >= sc.deadline)
            {
                G.scn.failed = true; if (report != null) { report.failed = true; report.failTitle = "Délai dépassé"; report.failText = "Le délai de " + sc.deadline + " jours est écoulé sans que tous les objectifs soient atteints. Vous pouvez continuer en mode libre."; }
                Game.Emit("career"); return;
            }
            var st = sc.stages[G.scn.stage]; var s = Stats();
            if (st.goals.All(g => g.test(s)))
            {
                G.money += st.reward; G.scn.stage++;
                note("🎖️ Étape « " + st.title + " » accomplie ! Prime : " + GMath.Eur(st.reward) + ".");
                if (G.scn.stage >= sc.stages.Length)
                {
                    G.scn.done = true; G.scn.stage = sc.stages.Length - 1;
                    double ratio = sc.deadline > 0 ? (double)G.elapsed / sc.deadline : 0.5; int stars = ratio <= 0.5 ? 3 : ratio <= 0.8 ? 2 : 1;
                    G.scn.stars = sc.deadline > 0 ? stars : 3; SaveRecord(sc.id, G.scn.stars, G.elapsed);
                    if (report != null) { report.victory = true; report.victoryText = sc.victory; report.stars = G.scn.stars; report.elapsed = G.elapsed; }
                }
                Game.Emit("career");
            }
        }

        // ---------- CRITÈRES DES GRANDS ÉVÉNEMENTS ----------
        // Les critères olympiques reprennent tous ceux des X Games avec des seuils plus élevés.
        static string N(double v) { return GMath.Num(v); }
        static string Ok(bool b) { return b ? "✔" : "✘"; }
        public static readonly Dictionary<string, Crit[]> CRIT = new Dictionary<string, Crit[]>
        {
            { "xgames", new[] {
                new Crit("Notoriété d'au moins 60 %", s => s.fame >= 60, s => Game.R(s.fame) + " %"),
                new Crit("Prestige ★★½ (45)", s => s.prestige >= 45, s => s.prestige + " / 45"),
                new Crit("Satisfaction moyenne (7 jours) d'au moins 72 %", s => s.satAvg >= 72, s => Game.R(s.satAvg) + " %"),
                new Crit("Snowpark et scène de concerts", s => s.snowpark >= 1 && s.stage >= 1, s => Ok(s.snowpark > 0) + " / " + Ok(s.stage > 0)),
                new Crit("800 lits touristiques", s => s.beds >= 800, s => N(s.beds) + " / 800"),
                new Crit("30 km de pistes dont 2 noires", s => s.km >= 30 && s.black >= 2, s => s.km.ToString("0") + " km · " + s.black + " noire(s)"),
                new Crit("Débit des remontées de 9 000 pers./h", s => s.capacity >= 9000, s => N(s.capacity) + " / 9 000"),
                new Crit("10 pisteurs répartis sur 2 postes de secours", s => s.patrol >= 10 && s.rescue >= 2, s => s.patrol + " pisteurs · " + s.rescue + " poste(s)"),
                new Crit("5 restaurants", s => s.restaurants >= 5, s => s.restaurants + " / 5"),
                new Crit("Une Coupe d'Europe déjà organisée", s => s.h_europe >= 1, s => N(s.h_europe)) } },
            { "olympics", new[] {
                new Crit("Notoriété d'au moins 82 %", s => s.fame >= 82, s => Game.R(s.fame) + " %"),
                new Crit("Prestige ★★★½ (72)", s => s.prestige >= 72, s => s.prestige + " / 72"),
                new Crit("Satisfaction moyenne (7 jours) d'au moins 78 %", s => s.satAvg >= 78, s => Game.R(s.satAvg) + " %"),
                new Crit("Snowpark, scène, stade de slalom et stade olympique", s => s.snowpark >= 1 && s.stage >= 1 && s.stadium >= 1 && s.olympic >= 1, s => string.Join(" ", new[] { s.snowpark, s.stage, s.stadium, s.olympic }.Select(x => Ok(x > 0)))),
                new Crit("1 400 lits dont 250 de luxe", s => s.beds >= 1400 && s.luxBeds >= 250, s => N(s.beds) + " lits · " + N(s.luxBeds) + " luxe"),
                new Crit("55 km de pistes dont 4 noires, sommet au-dessus de 3 000 m", s => s.km >= 55 && s.black >= 4 && s.topAlt >= 3000, s => s.km.ToString("0") + " km · " + s.black + " noires · " + N(s.topAlt) + " m"),
                new Crit("Débit de 16 000 pers./h et une remontée d'exception", s => s.capacity >= 16000 && s.premiumLift, s => N(s.capacity) + " / 16 000 · exception " + Ok(s.premiumLift)),
                new Crit("18 pisteurs, 3 postes de secours et un héliport", s => s.patrol >= 18 && s.rescue >= 3 && s.heli >= 1, s => s.patrol + " · " + s.rescue + " postes · héliport " + Ok(s.heli > 0)),
                new Crit("8 restaurants", s => s.restaurants >= 8, s => s.restaurants + " / 8"),
                new Crit("6 dameuses et la neige de culture", s => s.machines >= 6 && Game.Researched("snowmaking"), s => s.machines + " dameuses · neige " + Ok(Game.Researched("snowmaking"))),
                new Crit("Une Coupe d'Europe et une Coupe du monde organisées", s => s.h_europe >= 1 && s.h_worldcup >= 1, s => s.h_europe + " · " + s.h_worldcup),
                new Crit("Dette inférieure à 1 M€", s => s.debt < 1000000, s => N(s.debt) + " €") } }
        };
        public static List<CritState> CritStateOf(string id) { var s = Stats(); return CRIT[id].Select(c => new CritState { text = c.text, ok = c.test(s), val = c.val(s) }).ToList(); }
        public static bool CritOk(string id) { return CritStateOf(id).All(c => c.ok); }
        public static double CritShare(string id) { var st = CritStateOf(id); return (double)st.Count(c => c.ok) / st.Count; }

        // ---------- CANDIDATURE OLYMPIQUE : longue et sélective ----------
        static readonly string[] RIVALS = { "Val d'Argent", "Hochfeld", "Nordvik", "Montsorel", "Sankt Albrun", "Lago Bianco", "Kirovsk-Ouest", "Shiramine" };
        public static BidCheck BidCan()
        {
            var s = Stats(); var r = new BidCheck();
            if (G.bid != null && (G.bid.phase == "eval" || G.bid.phase == "prep" || G.bid.phase == "games" || G.bid.phase == "done")) { r.why.Add("Une candidature est déjà en cours."); return r; }
            if (G.bid != null && G.bid.phase == "refused" && G.elapsed < G.bid.retry) r.why.Add("Nouvelle candidature possible dans " + (G.bid.retry - G.elapsed) + " jours.");
            if (!Game.Researched("olympic")) r.why.Add("Recherche « Dossier olympique » requise.");
            if (!CritOk("xgames")) r.why.Add("Le niveau X Games doit être atteint (tous ses critères remplis).");
            if (!(s.h_worldcup >= 1)) r.why.Add("Il faut avoir organisé une Coupe du monde.");
            if (s.fame < 75) r.why.Add("Notoriété de 75 % requise (" + Game.R(s.fame) + " %).");
            if (s.prestige < 60) r.why.Add("Prestige de 60 requis (" + s.prestige + ").");
            if (!Game.CanPay(BID_COST)) r.why.Add("Il faut " + GMath.Eur(BID_COST) + " pour monter le dossier.");
            r.ok = r.why.Count == 0; return r;
        }
        /// <summary>0-100 : ce que voit la commission d'évaluation.</summary>
        public static double DossierScore()
        {
            var s = Stats();
            return GMath.Clamp(55 * CritShare("olympics") + 0.15 * s.fame + 0.12 * s.prestige + 0.1 * s.satAvg + 2 * Math.Min(4, s.h_worldcup + s.h_xgames + s.h_europe), 0, 100);
        }
        public static string BidStart()
        {
            var c = BidCan(); if (!c.ok) return c.why[0];
            if (!Game.Spend(BID_COST, "Événements")) return "Fonds insuffisants.";
            var pool = RIVALS.OrderBy(x => Rand.Next()).Take(3).ToList();
            G.bid = new Bid { phase = "eval", due = G.elapsed + EVAL_DAYS, rivals = pool.Select(n => new Rival { name = n, score = Game.R(74 + Rand.Next() * 16) }).ToList(), tries = (G.bid != null ? G.bid.tries : 0) + 1 };
            Game.Log("📁 Dossier de candidature olympique déposé. La commission du CIO évalue les 4 candidatures pendant " + EVAL_DAYS + " jours.", "info"); return null;
        }
        public static void DailyBid(DayReport report)
        {
            var b = G.bid; if (b == null) return;
            if (b.phase == "eval")
            {
                foreach (var r in b.rivals) r.score = GMath.Clamp(r.score + (Rand.Next() - 0.45) * 0.6, 60, 97);
                if (G.elapsed < b.due) return;
                double me = Game.R(DossierScore()); var best = b.rivals.Aggregate((a, r) => r.score > a.score ? r : a);
                b.score = me;
                if (me > best.score) { b.phase = "prep"; b.due = G.elapsed + PREP_DAYS; b.mid = G.elapsed + PREP_DAYS / 2; report.notes.Add("🏛️ Vote du CIO : votre dossier (" + me + " / 100) devance " + string.Join(", ", b.rivals.Select(r => r.name + " (" + Game.R(r.score) + ")")) + ". Les Jeux Olympiques vous sont attribués ! " + PREP_DAYS + " jours pour remplir tout le cahier des charges."); }
                else { b.phase = "refused"; b.retry = G.elapsed + 60; report.notes.Add("🏛️ Vote du CIO : " + best.name + " l'emporte (" + Game.R(best.score) + " contre " + me + " pour vous). Renforcez la station et recandidatez dans 60 jours."); }
            }
            else if (b.phase == "prep")
            {
                if (b.mid > 0 && G.elapsed >= b.mid)
                {
                    b.mid = 0; var miss = CritStateOf("olympics").Where(c => !c.ok).ToList();
                    report.notes.Add(miss.Count > 0 ? "🔍 Inspection de mi-parcours : " + miss.Count + " critère(s) encore non rempli(s) — " + string.Join(", ", miss.Take(3).Select(c => c.text)) + (miss.Count > 3 ? "…" : "") + "." : "🔍 Inspection de mi-parcours : tout est conforme, continuez ainsi.");
                }
                if (G.elapsed < b.due) return;
                if (G.summer) { b.late = true; return; }
                var miss2 = CritStateOf("olympics").Where(c => !c.ok).ToList();
                if (miss2.Count == 0) { b.phase = "games"; G.@event = new EventRun { id = "olympics", day = G.day + 1, left = 6 }; report.notes.Add("🔥 Inspection finale réussie : la flamme olympique arrive demain. Les Jeux Olympiques d'hiver commencent chez vous !"); }
                else { b.phase = "refused"; b.retry = G.elapsed + 80; G.reputation = Math.Max(0, G.reputation - 12); G.fame = Math.Max(0, G.fame - 18); report.notes.Add("😱 Inspection finale : " + miss2.Count + " critère(s) non rempli(s) (" + string.Join(", ", miss2.Take(3).Select(c => c.text)) + "…). Le CIO retire les Jeux. Réputation −12, notoriété −18."); }
            }
            else if (b.phase == "games" && G.@event == null) b.phase = "done";
        }
        /// <summary>Début de journée : le jour 1 des Jeux Olympiques marque la victoire de la partie.</summary>
        public static void DayStart(DayReport report)
        {
            if (G.@event != null && G.@event.id == "olympics" && G.@event.day == G.day && !G.OlympicsOpened)
            {
                G.olympicsDay1 = G.elapsed;
                report.notes.Add("🏅 Jour 1 des Jeux Olympiques d'hiver : la cérémonie d'ouverture se tient à " + G.name + ". Partie gagnée !");
                var sc = Cur;
                if (sc != null && sc.bid && !G.scn.done)
                {
                    G.scn.stage = sc.stages.Length - 1; G.scn.done = true; G.scn.stars = 3; SaveRecord(sc.id, 3, G.elapsed);
                    report.victory = true; report.victoryText = sc.victory; report.stars = 3; report.elapsed = G.elapsed; Game.Emit("career");
                }
                else if (sc != null) CheckCareer(report);
                if (G.scn == null) { report.victory = true; report.victoryText = "Les Jeux Olympiques d'hiver s'ouvrent chez vous : vous avez gagné la partie. Vous pouvez continuer à jouer."; }
            }
        }
        public static void Free() { G.scn = null; G.mode = "sandbox"; }
    }
}
