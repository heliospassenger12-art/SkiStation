// SkiStation — Gestion : FIDÉLITÉ. Les visiteurs contents deviennent des habitués qui reviennent plusieurs fois dans la
// saison (surtout le week-end et pendant les vacances) et d'une saison à l'autre ; les plus fidèles achètent le forfait
// saison ; les déçus espacent leurs venues puis disparaissent. Portage de Web/js/loyalty.js.
using System;
using System.Collections.Generic;
using System.Linq;

namespace SkiStation.Gestion
{
    public sealed class ClientMember { public string f, p; public int s, c, col; }
    public sealed class Client
    {
        public int id; public string last, fav; public bool fam, group; public List<ClientMember> members = new List<ClientMember>();
        public double loy, sat; public int visits, vs, first, last_, since, pass, seg;
    }
    public sealed class LoyaltyStats { public int clients, people, loyal, pass, today; public double avgVisits; public List<Client> top; }

    public static class Loyalty
    {
        const int CAP = 1500;
        static GameState G { get { return Game.G; } }
        public static double PassPrice() { return G.seasonPassPrice > 0 ? G.seasonPassPrice : Game.R(G.passPrice * 11); }

        static double Chance(Client c)
        {
            int since = G.elapsed - c.last_;
            double p = 0.04 * c.loy + 0.17 * Math.Pow(c.loy, 1.4);
            p *= Game.IsWeekend(G.day) ? 1.8 : Game.IsHoliday(G.day) ? 1.6 : 0.75;
            if (since < 3) p *= 0.3; else if (since < 7) p *= 0.7;
            if (c.pass == G.year) p *= 2.4;
            p *= Weather.Def.demand; if (G.summer) p *= 0.3;
            return GMath.Clamp(p, 0, 0.75);
        }
        public static List<Client> PickToday(double slots)
        {
            var o = new List<Client>(); int n = 0, max = (int)Math.Floor(slots * 0.85);
            foreach (var c in G.clients) { if (n + c.members.Count > max) continue; if (Rand.Next() < Chance(c)) { o.Add(c); n += c.members.Count; } }
            return o;
        }
        public static double DemandMul()
        {
            int n = 0; double s = 0; foreach (var c in G.clients) if (c.loy >= 0.5) { n++; s += c.sat; }
            if (n == 0) return 1;
            return GMath.Clamp(1 + Math.Min(0.18, n / 1500.0 * 0.18) * (s / n - 0.55) / 0.25, 0.9, 1.2);
        }
        public static double FamePart() { return Math.Min(6, G.clients.Count(c => c.loy >= 0.5) / 60.0); }

        public static void EndDay(List<Visitor> visitors, DayReport report)
        {
            var byClient = new Dictionary<int, List<Visitor>>(); var parties = new Dictionary<string, List<Visitor>>();
            foreach (var v in visitors)
            {
                if (v.state != "gone") continue;
                if (v.clientId > 0) { List<Visitor> l; if (!byClient.TryGetValue(v.clientId, out l)) byClient[v.clientId] = l = new List<Visitor>(); l.Add(v); continue; }
                string key = v.grp != null ? "g" + v.grp.id : "v" + v.id; List<Visitor> pl; if (!parties.TryGetValue(key, out pl)) parties[key] = pl = new List<Visitor>(); pl.Add(v);
            }
            foreach (var kv in byClient)
            {
                var c = G.clients.FirstOrDefault(x => x.id == kv.Key); if (c == null) continue; var vs = kv.Value;
                double s = vs.Average(v => v.sat);
                c.visits++; c.vs++; c.last_ = G.elapsed; c.sat = c.sat * 0.7 + s * 0.3;
                c.loy = GMath.Clamp(c.loy + (s - 0.62) * 0.5 - (s < 0.45 ? 0.12 : 0), 0, 1);
                var fav = Fav(vs); if (fav != null) c.fav = fav;
                for (int i = 0; i < vs.Count && i < c.members.Count; i++) c.members[i].s = Math.Max(c.members[i].s, vs[i].skill);
            }
            int added = 0;
            foreach (var vs in parties.Values)
            {
                double s = vs.Average(v => v.sat); if (s < 0.62 || vs.All(v => v.runs == 0 && !v.lessonDone)) continue;
                if (Rand.Next() > 0.25 + (s - 0.62)) continue;
                var g = vs[0].grp; string lastName = g != null && g.fam ? g.last : string.Join(" ", vs[0].name.Split(' ').Skip(1));
                G.clients.Add(new Client
                {
                    id = G.loyalSeq++, last = lastName, fam = g != null && g.fam, group = g != null,
                    members = vs.Select(v => new ClientMember { f = v.name.Split(' ')[0], p = v.prof.id, s = v.skill, c = v.child ? 1 : 0, col = v.color }).ToList(),
                    loy = GMath.Clamp(0.25 + (s - 0.62) * 0.8, 0.1, 0.6), visits = 1, vs = 1, first = G.elapsed, last_ = G.elapsed, sat = s, since = G.year, seg = vs[0].seg, fav = Fav(vs)
                });
                added++;
            }
            int lost = 0;
            for (int i = G.clients.Count - 1; i >= 0; i--)
            {
                var c = G.clients[i]; if (c.last_ == G.elapsed) continue;
                c.loy = Math.Max(0, c.loy - 0.0006);
                if (c.loy < 0.08 && G.elapsed - c.last_ > 30) { G.clients.RemoveAt(i); lost++; }
            }
            if (G.clients.Count > CAP) G.clients = G.clients.OrderByDescending(c => c.loy).Take(CAP).ToList();
            if (report != null && byClient.Count >= 10 && G.elapsed % 7 == 0) report.notes.Add("🔁 " + byClient.Count + " groupe(s) d'habitués sont revenus aujourd'hui.");
        }
        static string Fav(List<Visitor> vs)
        {
            var runs = new Dictionary<string, int>();
            foreach (var v in vs) foreach (var kv in v.pc) { int n; runs.TryGetValue(kv.Key, out n); runs[kv.Key] = n + kv.Value; }
            return runs.Count > 0 ? runs.OrderByDescending(x => x.Value).First().Key : null;
        }
        public static void NewSeason(DayReport report)
        {
            double price = PassPrice(), fair = Sim.Today != null && Sim.Today.fair > 0 ? Sim.Today.fair : 26, accept = GMath.Clamp(1.6 - 0.6 * price / (fair * 11), 0.1, 1.2);
            int sold = 0, people = 0;
            foreach (var c in G.clients)
            {
                double want = c.loy >= 0.65 && c.vs >= 4 ? 0.55 * accept : c.loy >= 0.8 ? 0.35 * accept : 0;
                if (want > 0 && Rand.Next() < want) { c.pass = G.year; sold++; people += c.members.Count; }
                c.vs = 0;
            }
            if (sold > 0)
            {
                double rev = Game.R(people * price * 0.8); Game.Earn(rev, "Forfaits saison");
                if (report != null) report.notes.Add("🎫 " + sold + " famille(s) et groupe(s) d'habitués achètent le forfait saison (" + people + " personnes) : " + GMath.Eur(rev) + " encaissés d'avance.");
            }
        }
        public static LoyaltyStats Stats()
        {
            return new LoyaltyStats
            {
                clients = G.clients.Count, people = G.clients.Sum(c => c.members.Count), loyal = G.clients.Count(c => c.loy >= 0.5), pass = G.clients.Count(c => c.pass == G.year),
                today = Sim.Visitors.Count(v => v.clientId > 0), avgVisits = G.clients.Count > 0 ? G.clients.Average(c => c.visits) : 0, top = G.clients.OrderByDescending(c => c.visits).Take(8).ToList()
            };
        }
        public static string Label(Client c) { return c.fam ? "Famille " + c.last : c.group ? "Groupe de " + c.members[0].f + " " + c.last : c.members[0].f + " " + c.last; }
    }
}
