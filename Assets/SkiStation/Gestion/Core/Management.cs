// SkiStation — Gestion : services (gamme, personnel, horaires, note), hébergements (segments), banque (prêts échéancés),
// recherche (laboratoire, file d'attente), pisteurs, dameuses, comptes de saison. Portage de Web/js/manage.js.
using System;
using System.Collections.Generic;
using System.Linq;

namespace SkiStation.Gestion
{
    public sealed class DayReport
    {
        public List<string> notes = new List<string>(); public double capital, opex, inc, exp, sat, lost; public int day, visitors, incidents; public string date, why, weather;
        public Ledger ledger; public Dictionary<string, int> fb = new Dictionary<string, int>();
        public bool failed, victory; public string failTitle, failText, victoryText; public int stars, elapsed;
    }
    public sealed class LodgingInfo { public double[] pools = new double[3], served = new double[3], beds = new double[3]; }
    public sealed class PatrolInfo { public int patrol, posts, unsecured; public double needKm, capKm; }

    public static class Mgmt
    {
        static readonly HashSet<string> SVC = new HashSet<string> { "food", "drink", "shop", "rental", "activity" };
        static GameState G { get { return Game.G; } }
        public static LodgingInfo LodgingInfo; public static PatrolInfo PatrolInfo;
        public const double MachineCost = 85000;

        public static Building InitBuilding(Building b)
        {
            var def = Catalog.Building(b.type);
            if (b.hours == null) b.hours = def.eve ? "eve" : "day";
            if (b.opts == null) b.opts = new HotelOptions { hb = def.hb };
            if (def.type == "garage" && b.machines == 0) b.machines = (int)def.cap;
            if (def.type == "rescue" && b.patrol == 0) b.patrol = (int)def.cap;
            return b;
        }
        public static void Migrate(GameState g)
        {
            foreach (var b in g.buildings) InitBuilding(b);
            if (g.elapsed == 0) g.elapsed = (g.year - 1) * Const.SEASON_DAYS + g.day;
            SyncLoan();
        }

        // ---------- services ----------
        public static bool IsService(Building b) { return SVC.Contains(b.Def.type); }
        public static double RefPrice(Building b) { var d = b.Def; return d.type == "lodging" ? d.price : Game.R(d.price * new[] { 0.75, 1, 1.4 }[b.range]); }
        public static double Quality(Building b)
        {
            var def = b.Def;
            double q = (def.quality > 0 ? def.quality : 0.5) + (b.range - 1) * 0.13 + (b.staffLvl - 1) * 0.07 + (G.wage - 1) * 0.04;
            if (Game.Researched("training")) q += 0.06;
            if (def.type == "lodging" && b.opts.spa) q += 0.1;
            return GMath.Clamp(q, 0.1, 1.25);
        }
        public static double Dur(Building b) { var d = b.Def; return (d.dur > 0 ? d.dur : 8) * new[] { 1.25, 1, 0.82 }[b.staffLvl] * (d.type == "food" && Game.Researched("kitchen") ? 0.88 : 1); }
        public static double CapMul(Building b) { var d = b.Def; return (d.type == "food" && Game.Researched("kitchen") ? 1.15 : 1) * (SVC.Contains(d.type) ? new[] { 0.88, 1, 1.1 }[b.staffLvl] : 1); }
        public static bool IsOpen(Building b)
        {
            if (!b.open) return false;
            var t = b.Def.type;
            if ((t == "food" || t == "drink") && b.hours != "eve" && G.time > 17.5 * 3600) return false;
            return true;
        }
        public static double CogsRate(Building b)
        {
            var t = b.Def.type; if (!(t == "food" || t == "drink" || t == "shop" || t == "rental")) return 0;
            double bs = t == "rental" ? 0.12 : t == "shop" ? 0.5 : new[] { 0.27, 0.33, 0.4 }[b.range];
            return bs * (Game.Researched("purchasing") ? 0.85 : 1);
        }
        /// <summary>Charges fixes du jour : [entretien, salaires].</summary>
        public static double[] DailyCost(Building b)
        {
            var def = b.Def;
            double lvl = 1 + 0.4 * (b.level - 1), wage = new[] { 0.88, 1, 1.15 }[G.wage];
            double up = def.upkeep * lvl * 0.45, sal = def.upkeep * lvl * 0.55 * new[] { 0.8, 1, 1.28 }[b.staffLvl] * wage;
            if (!b.open) return new[] { def.upkeep * 0.3, 0 };
            if ((def.type == "food" || def.type == "drink") && b.hours == "eve" && !def.eve) sal *= 1.25;
            if (def.type == "school") sal += b.staff * 190 * wage;
            if (def.type == "rescue") sal += Math.Max(0, b.patrol - def.cap) * 150 * wage;
            if (def.type == "garage") { sal += Math.Max(0, b.machines - def.cap) * 160 * wage; up += Math.Max(0, b.machines - def.cap) * 90; if (G.groomNight) sal += b.machines * 170 * wage; }
            if (def.type == "lodging") { if (b.opts.spa) up += 320; if (b.opts.shuttle) up += 190; }
            return new[] { up, sal };
        }
        public static void Rate(Building b, double score) { b.rating = GMath.Clamp(b.rating + (score - b.rating) * 0.04, 0, 5); }
        public static void SetRange(Building b, int r) { double rp = RefPrice(b); double k = b.price / (rp != 0 ? rp : 1); b.range = r; b.price = Math.Max(1, Game.R(RefPrice(b) * GMath.Clamp(k, 0.6, 1.6))); }

        // ---------- hébergements : trois segments ----------
        static int Seg(BuildingDef d) { return d.seg < 0 ? 0 : d.seg; }
        public static double DinnerPrice(Building b) { return new double[] { 16, 24, 46 }[Seg(b.Def)]; }
        public static double SpaCost(Building b) { return Game.R(b.Def.cost * 0.12 / 1000) * 1000 + 20000; }
        /// <summary>Répartit la demande de nuitées entre les hébergements ; renvoie le nombre de résidents.</summary>
        public static double AllocateLodging(double demand, double cal, double ev)
        {
            double N = demand * 0.8 * (Game.Researched("booking") ? 1.15 : 1) * (cal > 1 ? 1.1 : 1) * (ev > 1 ? 1.25 : 1) * (G.summer ? 0.7 : 1);
            var pools = new[] { N * 0.44, N * 0.38, N * 0.18 * GMath.Clamp(0.2 + G.prestige / 60, 0.2, 1.6) };
            var info = new LodgingInfo(); for (int i = 0; i < 3; i++) info.pools[i] = Game.R(pools[i]);
            double spill = 0, total = 0;
            for (int seg = 2; seg >= 0; seg--)
            {
                var list = G.buildings.Where(b => b.Def.type == "lodging" && Seg(b.Def) == seg).ToList();
                double pool = pools[seg] + spill, wsum = 0;
                foreach (var b in list)
                {
                    var def = b.Def; double beds = Game.Bcap(b); info.beds[seg] += beds;
                    b._pf = b.open ? GMath.Clamp(1.7 - 0.7 * b.price / def.price, 0.05, 1.3) : 0;
                    b._w = beds * b._pf * (0.5 + Quality(b) * 0.5 + b.rating / 5 * 0.4) * (b.opts.hb ? 1.08 : 1);
                    wsum += b._w;
                }
                double servedSeg = 0;
                foreach (var b in list)
                {
                    double beds = Game.Bcap(b), want = wsum > 0 ? pool * b._w / wsum : 0;
                    double target = Math.Min(beds * Math.Min(1, b._pf + 0.1), want);
                    b.guests = Game.R(b.guests > 0 ? b.guests * 0.6 + target * 0.4 : target * 0.7);
                    b.guests = Math.Min(b.guests, beds); if (!b.open) b.guests = 0;
                    servedSeg += b.guests;
                }
                info.served[seg] = servedSeg; total += servedSeg;
                spill = Math.Max(0, pool - servedSeg) * 0.55;
            }
            LodgingInfo = info; return total;
        }

        // ---------- banque ----------
        public static void SyncLoan() { G.loan = Game.R(G.loans.Sum(l => l.left)); }
        public static double CreditLimit() { return Game.R(400000 + G.prestige * 45000 + G.reputation * 6000); }
        public static string TakeLoan(string id)
        {
            var p = Catalog.LOANS.First(l => l.id == id);
            if (G.loan + p.amount > CreditLimit()) return "La banque refuse : votre plafond d'endettement est de " + GMath.Eur(CreditLimit()) + ". Il augmente avec le prestige et la réputation.";
            G.loans.Add(new Loan { id = G.nextId++, name = p.name, left = p.amount, pay = Game.R(p.amount / p.days), rate = p.rate, daysLeft = p.days });
            G.money += p.amount; SyncLoan(); return null;
        }
        public static string RepayLoan(int lid)
        {
            var l = G.loans.FirstOrDefault(x => x.id == lid); if (l == null) return "Prêt introuvable.";
            if (G.money < l.left && !G.unlimited) return "Trésorerie insuffisante pour solder ce prêt (" + GMath.Eur(l.left) + ").";
            if (!G.unlimited) G.money -= l.left; G.loans.Remove(l); SyncLoan(); return null;
        }
        /// <summary>Échéances du jour : intérêts (charge) + capital (sortie de trésorerie) ; crédit d'urgence après 3 jours de découvert.</summary>
        public static void DailyLoans(DayReport report)
        {
            double interest = 0, capital = 0;
            foreach (var l in G.loans.ToList())
            {
                interest += l.left * l.rate; double pay = Math.Min(l.pay, l.left); l.left -= pay; capital += pay; l.daysLeft--;
                if (l.left <= 1 || l.daysLeft <= 0) { capital += Math.Max(0, l.left); G.loans.Remove(l); report.notes.Add("🏦 " + l.name + " entièrement remboursé."); }
            }
            if (interest > 0) Game.Charge(Game.R(interest), "Intérêts");
            if (capital > 0 && !G.unlimited) G.money -= Game.R(capital);
            report.capital = Game.R(capital);
            if (!G.unlimited && G.money < -30000)
            {
                G.negDays++;
                if (G.negDays >= 3)
                {
                    double amt = Math.Ceiling((-G.money + 40000) / 10000) * 10000;
                    G.loans.Add(new Loan { id = G.nextId++, name = "Crédit d'urgence", left = amt, pay = Game.R(amt / 45), rate = 0.0026, daysLeft = 45 });
                    G.money += amt; G.negDays = 0; G.reputation = Math.Max(0, G.reputation - 1.5);
                    report.notes.Add("🏦 Découvert prolongé : la banque impose un crédit d'urgence de " + GMath.Eur(amt) + " au taux fort. Réputation −1,5.");
                }
            }
            else G.negDays = 0;
            SyncLoan();
        }

        // ---------- recherche ----------
        public static double LabCost() { return G.lab == 1 ? 120000 : G.lab == 2 ? 320000 : 0; }
        public static int RDays(ResearchDef r) { return Math.Max(1, (int)Game.R(r.days * new[] { 1, 1, 0.85, 0.7 }[GMath.Clamp(G.lab, 0, 3)])); }
        public static string RState(ResearchDef r)
        {
            var R = G.research;
            if (R.done.Contains(r.id)) return "done";
            if (R.active.Any(a => a.id == r.id)) return "active";
            if (R.queue.Contains(r.id)) return "queued";
            if (!string.IsNullOrEmpty(r.req) && !R.done.Contains(r.req) && !R.active.Any(a => a.id == r.req) && !R.queue.Contains(r.req)) return "locked";
            return "open";
        }
        static void Begin(ResearchDef r) { int d = RDays(r); G.research.active.Add(new ResearchJob { id = r.id, left = d, total = d }); }
        /// <summary>null = lancée, « queued » = mise en file, sinon message d'erreur.</summary>
        public static string StartResearch(string id)
        {
            var r = Catalog.Research(id); if (RState(r) != "open") return "Indisponible.";
            bool reqDone = string.IsNullOrEmpty(r.req) || G.research.done.Contains(r.req);
            if (G.research.active.Count < G.lab && reqDone) { if (!Game.Spend(r.cost, "Recherche")) return "Fonds insuffisants."; Begin(r); return null; }
            G.research.queue.Add(id); return "queued";
        }
        public static void CancelQueued(string id) { G.research.queue.Remove(id); }
        public static void DailyResearch(DayReport report)
        {
            var R = G.research;
            foreach (var a in R.active.ToList())
            {
                a.left--;
                if (a.left <= 0) { var r = Catalog.Research(a.id); R.done.Add(r.id); if (r.tier > 0) G.tier = Math.Max(G.tier, r.tier); R.active.Remove(a); report.notes.Add("🔬 Recherche terminée : " + r.name + "."); }
            }
            for (int i = 0; i < R.queue.Count && R.active.Count < G.lab;)
            {
                var r = Catalog.Research(R.queue[i]);
                if (!string.IsNullOrEmpty(r.req) && !R.done.Contains(r.req)) { i++; continue; }
                if (!Game.Spend(r.cost, "Recherche")) { i++; continue; }
                R.queue.RemoveAt(i); Begin(r); report.notes.Add("🔬 Recherche lancée depuis la file : " + r.name + ".");
            }
        }

        // ---------- pisteurs-secouristes ----------
        public static int PatrolCount() { return G.buildings.Where(b => b.type == "rescue" && b.open).Sum(b => b.patrol); }
        public static double KmPerPatrol() { return 3 + (Game.Researched("signage") ? 1 : 0); }
        public static PatrolInfo SecurePistes()
        {
            var posts = G.buildings.Where(b => b.type == "rescue" && b.open).ToList();
            double budget = PatrolCount() * KmPerPatrol() * 1000, need = 0;
            var list = G.pistes.Where(p => p.open).Select(p => { var mid = p.pts[p.pts.Count >> 1]; double d = 1e9; foreach (var b in posts) d = Math.Min(d, GMath.Hypot(b.x - mid.x, b.z - mid.z)); return new KeyValuePair<Piste, double>(p, d); }).OrderBy(x => x.Value).ToList();
            foreach (var p in G.pistes) { p.secured = false; p.postDist = 0; }
            foreach (var it in list) { need += it.Key.len; it.Key.postDist = it.Value < 1e8 ? it.Value : -1; if (budget >= it.Key.len * 0.5 && posts.Count > 0) { it.Key.secured = true; budget -= it.Key.len; } }
            PatrolInfo = new PatrolInfo { patrol = PatrolCount(), posts = posts.Count, needKm = need / 1000, capKm = PatrolCount() * KmPerPatrol(), unsecured = G.pistes.Count(p => p.open && !p.secured) };
            return PatrolInfo;
        }
        public static double RiskFactor(Piste p) { return (p.secured ? 0.8 : 1.25) * (Game.Researched("signage") ? 0.85 : 1); }
        public static double PatrolSpeed() { return Game.Researched("patrol2") ? 1.5 : 1; }
        public static double? ResponseEta(Piste p) { if (!(p.postDist > 0)) return null; return (240 + p.postDist / (4.5 * PatrolSpeed())) / 60; }

        // ---------- dameuses ----------
        public static int GroomerCount() { return G.buildings.Where(b => b.type == "garage" && b.open).Sum(b => b.machines); }
        public static double GroomSpeed() { return 0.25 * (Game.Researched("winch") ? 1.4 : 1) * (Game.Researched("groomer2") ? 1.3 : 1) * (G.groomNight ? 1.5 : 1); }
        public static double GroomCapacityKm() { return GroomerCount() * (Const.DAY_END - 17.25 * 3600) * GroomSpeed() / 1000 * 0.9; }
        static double Need(Piste p) { return p.passes + p.ice * 500 + (1 - p.groomed) * 300; }
        public static List<Piste> GroomPlan()
        {
            bool winch = Game.Researched("winch");
            return G.pistes.Where(p => p.prio > 0 && (p.diff < 3 || winch)).OrderByDescending(p => p.prio).ThenByDescending(Need).ToList();
        }

        // ---------- comptes de saison ----------
        public static void CloseDay(Ledger ledger, double visitors)
        {
            var s = G.season;
            foreach (var kv in ledger.inc) Game.Add(s.inc, kv.Key, kv.Value);
            foreach (var kv in ledger.exp) Game.Add(s.exp, kv.Key, kv.Value);
            s.days++; s.visitors += visitors;
        }
        public static void NewSeason()
        {
            G.lastSeason = G.season; G.season = new Season();
            foreach (var b in G.buildings) { b.sRev = 0; b.sCost = 0; b.sVisits = 0; }
            G.safety = new Safety();
        }
        public static double Sum(Dictionary<string, double> o) { return o.Values.Sum(); }
        public static double Opex(Dictionary<string, double> o) { return o.Where(kv => kv.Key != "Construction" && kv.Key != "Recherche").Sum(kv => kv.Value); }
    }
}
