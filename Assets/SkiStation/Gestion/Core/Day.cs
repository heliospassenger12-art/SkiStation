// SkiStation — Gestion : fin de journée (bilan, charges, réputation, événements, recherche, banque, nuit, notoriété,
// candidature, carrière, succès), saisons hiver / été, événements du matin et actions du joueur. Portage de sim.js / events.js / ui.js.
using System;
using System.Collections.Generic;
using System.Linq;

namespace SkiStation.Gestion
{
    public static partial class Game
    {
        /// <summary>Sauvegarde automatique (branchée par la couche Unity sur un fichier).</summary>
        public static Action AutoSave = () => { };

        public static void EndDay()
        {
            Sim.FinishVisitorsAtEndOfDay();
            double up = 0, sal = 0;
            foreach (var b in G.buildings) { var c = Mgmt.DailyCost(b); up += c[0]; sal += c[1]; b.sRev += b.revenue; b.sCost += c[0] + c[1] + b.cogs; b.sVisits += b.visits; b.lastRev = b.revenue; b.lastCost = c[0] + c[1] + b.cogs; b.lastVisits = b.visits; }
            double lm = 0;
            foreach (var l in G.lifts) { lm += l.maint * new[] { 0.55, 1, 1.6 }[G.maint] * (l.open ? 1 : 0.4) * (Researched("energy") ? 0.85 : 1); l.cond = GMath.Clamp(l.cond - 0.012 + new[] { 0.004, 0.012, 0.022 }[G.maint], 0.2, 1); l.brokenUntil = 0; }
            Charge(R(up), "Entretien bâtiments"); Charge(R(sal), "Salaires"); Charge(R(lm), "Remontées");
            if (G.marketing.social > 0) G.marketing.social--; if (G.marketing.tv > 0) G.marketing.tv--;
            int n = G.satToday.Count; double avg = n > 0 ? G.satToday.Average() * 100 : G.sat;
            if (n > 0) { G.sat = R(avg); G.reputation = GMath.Clamp(G.reputation + (avg - G.reputation) * Math.Min(0.22, 0.04 + n / 2500.0) * (avg > G.reputation && Researched("loyalty") ? 1.3 : 1) + (G.wage - 1) * 0.12, 0, 100); }
            G.bestDay = Math.Max(G.bestDay, G.visitorsToday);
            double inc = Mgmt.Sum(G.ledger.inc), exp = Mgmt.Sum(G.ledger.exp), opex = Mgmt.Opex(G.ledger.exp);
            var report = new DayReport { opex = opex, day = G.day, date = DateStr(G.day), visitors = G.visitorsToday, sat = G.sat, inc = inc, exp = exp, ledger = G.ledger, fb = Sim.Fb, incidents = G.incidentsToday, lost = Sim.Today != null ? Sim.Today.lost : 0, why = Sim.Today != null ? Sim.Today.why : null, weather = G.weather.type };
            G.history.Add(new HistoryEntry { su = G.summer, day = G.day, year = G.year, v = G.visitorsToday, sat = G.sat, inc = R(inc), exp = R(opex), money = R(G.money), rep = R(G.reputation) });
            if (G.history.Count > 150) G.history.RemoveAt(0);
            if (G.@event != null && G.@event.day <= G.day)
            {
                var ev = Catalog.Event(G.@event.id); G.@event.left--;
                if (G.@event.left <= 0)
                {
                    bool ok = G.sat >= 50; int h; G.eventsHosted.TryGetValue(ev.id, out h); G.eventsHosted[ev.id] = h + 1;
                    G.reputation = GMath.Clamp(G.reputation + (ok ? ev.prestige * 0.6 : -3), 0, 100); G.fame = GMath.Clamp(G.fame + (ok ? ev.fameGain : -ev.fameGain * 0.6), 0, 100);
                    report.notes.Add(ok ? "🏆 " + ev.name + " : un succès ! Réputation en hausse." : "😕 " + ev.name + " : organisation critiquée (satisfaction trop basse).");
                    G.@event = null;
                }
            }
            Mgmt.DailyResearch(report);
            Mgmt.DailyLoans(report);
            var planned = Sim.TakeGroomPlanned();
            if (!G.summer && planned != null)
            {
                var missed = planned.Where(p => !p.groomedTonight && G.pistes.Contains(p)).ToList();
                G.groomReport = new GroomReport { done = planned.Count - missed.Count, missed = missed.Select(p => p.name).ToList(), km = planned.Where(p => p.groomedTonight).Sum(p => p.len) / 1000 };
                if (missed.Count > 0 && Mgmt.GroomerCount() > 0) report.notes.Add("🚜 Damage incomplet : " + missed.Count + " piste(s) non préparée(s) faute de capacité (" + string.Join(", ", missed.Take(3).Select(p => p.name)) + (missed.Count > 3 ? "…" : "") + ").");
            }
            Mgmt.CloseDay(G.ledger, G.visitorsToday);
            G.ledger0 = G.ledger.inc; G.ledger = new Ledger();
            Weather.Night();
            UpdatePrestige(); G.elapsed++; Scenarios.DailyFame(); Scenarios.DailyBid(report);
            Scenarios.CheckCareer(report);
            CheckAch(report);
            Loyalty.EndDay(Sim.Visitors, report);
            G.day++; Weather.Next();
            if (G.summer) { if (G.day >= Const.SUMMER_DAYS) EndSummer(report); }
            else if (G.day >= Const.SEASON_DAYS) { if (Researched("summer")) StartSummer(report); else SeasonEnd(report); }
            G.time = Const.DAY_START; Sim.ResetClock();
            foreach (var m in report.notes) { G.log.Insert(0, new LogEntry { t = Const.DAY_START, d = G.day, msg = m, kind = "good" }); if (G.log.Count > 60) G.log.RemoveAt(G.log.Count - 1); }
            Sim.StartDay();
            Scenarios.DayStart(report);
            MorningEvents.Morning();
            AutoSave();
            Emit("day:end", report);
        }
        static void WinterReset(DayReport report)
        {
            G.day = 0; G.year++; G.snowBase = 45; Mgmt.NewSeason(); Loyalty.NewSeason(report);
            foreach (var p in G.pistes) { p.depth = 45 + (p.topAlt - 1650) * 0.03; p.ice = 0; p.slush = 0; p.groomed = 0.8; p.snowClosed = false; p.avaUntil = 0; }
        }
        static void StartSummer(DayReport report)
        {
            G.summer = true; G.day = 0; if (G.@event != null && G.@event.day > 0) G.@event = null;
            foreach (var p in G.pistes) { p.snowClosed = false; p.avaUntil = 0; p.ice = 0; p.slush = 0; p.powder = 0; }
            report.notes.Add("🌞 Fin de la saison d'hiver. La saison d'été commence : 30 jours de VTT et de randonnée. Téléskis et tapis sont à l'arrêt ; réglez le forfait été dans Finances.");
            Weather.Init(); Net.Touch(); Emit("season");
        }
        static void EndSummer(DayReport report)
        {
            G.summer = false; WinterReset(report);
            report.notes.Add("❄️ Fin de l'été : les premières neiges tombent. Saison d'hiver " + G.year + " !");
            Weather.Init(); Net.Touch(); Emit("season");
        }
        /// <summary>Passer l'été sans le jouer : recettes forfaitaires réduites pour les jours restants.</summary>
        public static bool SkipSummer()
        {
            if (!G.summer || Sim.Present > 0) return false;
            var hs = G.history.Where(h => h.su && h.year == G.year).ToList(); double avg = hs.Count > 0 ? hs.Average(h => h.inc - h.exp) : 0;
            double gain = R(Math.Max(0, avg) * 0.6 * (Const.SUMMER_DAYS - G.day)); G.money += gain;
            var report = new DayReport { day = G.day, date = DateStr(G.day), sat = G.sat, ledger = G.ledger }; report.notes.Add("Été abrégé : " + GMath.Eur(gain) + " de recettes estimées pour les jours restants.");
            EndSummer(report); G.time = Const.DAY_START; Sim.ResetClock(); Sim.StartDay(); AutoSave(); Emit("day:end", report); return true;
        }
        static void SeasonEnd(DayReport report)
        {
            double summer = 0;
            if (Researched("summer")) { foreach (var b in G.buildings) { var d = b.Def; summer += d.summer * 90 * 9 + (d.type == "lodging" ? Bcap(b) * b.price * 0.25 * 70 : 0); } foreach (var l in G.lifts) if (Catalog.Lift(l.type).kind != "tow") summer += 2500; }
            G.money += summer; G.day = 0; G.year++; G.snowBase = 45; Mgmt.NewSeason(); Loyalty.NewSeason(report);
            foreach (var p in G.pistes) { p.depth = 45 + (p.topAlt - 1650) * 0.03; p.ice = 0; p.slush = 0; p.groomed = 0.8; p.snowClosed = false; }
            report.notes.Add("🌞 Fin de saison " + (G.year - 1) + ". " + (summer > 0 ? "Recettes d'été (VTT, randonnée, hébergement) : " + GMath.Eur(summer) + "." : "Recherchez « Saison d'été » pour générer des recettes hors saison.") + " Nouvelle saison d'hiver !");
            Weather.Init();
        }
        public static void CheckAch(DayReport report)
        {
            var s = Stats();
            foreach (var a in Catalog.ACH) if (!G.ach.ContainsKey(a.id) && a.test(s, G))
                {
                    G.ach[a.id] = G.day + 1 + (G.year - 1) * Const.SEASON_DAYS; string msg = "🏅 Succès débloqué : " + a.name + ".";
                    if (report != null) report.notes.Add(msg); else Log(msg, "good");
                }
        }
    }

    /// <summary>Événements aléatoires du matin : une situation, deux choix, des conséquences visibles.</summary>
    public static class MorningEvents
    {
        public sealed class Choice { public string label; public Func<GameState, string> apply; public Choice(string l, Func<GameState, string> a) { label = l; apply = a; } }
        public sealed class Def { public string id, title, text; public Func<GameState, bool> ok; public Choice[] choices; }
        static IEnumerable<Piste> P(GameState G, Func<Piste, bool> f) { return G.pistes.Where(f); }
        static void Touch() { Game.Net.Touch(); }
        public static readonly Def[] LIST = {
            new Def { id = "influencer", ok = G => G.lifts.Count > 0, title = "📱 Une influenceuse ski", text = "Une créatrice suivie par 800 000 personnes propose de filmer sa journée chez vous contre des forfaits gratuits pour son équipe.",
                choices = new[] { new Choice("Accepter (forfaits offerts)", G => { G.reputation = Math.Min(100, G.reputation + 3); G.marketing.social = Math.Max(G.marketing.social, 3); return "Vidéo publiée : réputation +3 et effet « réseaux sociaux » pendant 3 jours."; }),
                                  new Choice("Refuser poliment", G => "Elle ira tourner chez la concurrence.") } },
            new Def { id = "strike", ok = G => G.lifts.Count >= 2, title = "✊ Préavis de grève", text = "Les conducteurs de remontées réclament une prime de froid. Sans accord, ils menacent d'arrêter les installations jusqu'à 11 h.",
                choices = new[] { new Choice("Verser la prime (12 000 €)", G => { Game.Charge(12000, "Salaires"); return "Accord trouvé, les remontées tournent normalement."; }),
                                  new Choice("Refuser", G => { foreach (var l in G.lifts) l.brokenUntil = Math.Max(l.brokenUntil, 11 * 3600); G.reputation = Math.Max(0, G.reputation - 2); Touch(); return "Grève : toutes les remontées arrêtées jusqu'à 11 h. Réputation −2."; }) } },
            new Def { id = "avalanche", ok = G => P(G, p => p.topAlt > 2400).Any() && G.weather.snow > 5, title = "⚠️ Risque d'avalanche", text = "Le chef pisteur signale une plaque instable au-dessus des pistes hautes après la nuit de neige.",
                choices = new[] { new Choice("Fermer les pistes hautes jusqu'à 12 h", G => { foreach (var p in P(G, p => p.topAlt > 2400)) p.avaUntil = 12 * 3600; Touch(); return "Pistes hautes fermées ce matin, aucun risque pris."; }),
                                  new Choice("Garder ouvert", G => { if (Rand.Next() < 0.35) { G.reputation = Math.Max(0, G.reputation - 8); var p = P(G, x => x.topAlt > 2400).First(); p.avaUntil = 15 * 3600; Touch(); return "❄️ Une coulée a touché " + p.name + " : pas de victime, mais la presse s'en empare. Réputation −8."; } return "La plaque a tenu. Journée normale."; }) } },
            new Def { id = "subsidy", ok = G => G.mode == "career" && G.passPrice > 18, title = "🏛️ Aide de la région", text = "La région offre une subvention si vous baissez le forfait journée de 3 € pour soutenir l'accès au ski.",
                choices = new[] { new Choice("Accepter (+90 000 €, forfait −3 €)", G => { G.money += 90000; G.passPrice -= 3; return "Subvention versée. Forfait ramené à " + G.passPrice + " €."; }),
                                  new Choice("Décliner", G => "Vous gardez la main sur vos tarifs.") } },
            new Def { id = "team", ok = G => P(G, p => p.diff >= 2).Any(), title = "🎿 Équipe nationale", text = "L'équipe de France de slalom voudrait privatiser une de vos pistes rouges ou noires pour s'entraîner aujourd'hui.",
                choices = new[] { new Choice("Privatiser la piste (+35 000 €)", G => { var p = P(G, x => x.diff >= 2).OrderByDescending(x => x.diff).First(); p.avaUntil = 16.75 * 3600; Touch(); G.money += 35000; G.reputation = Math.Min(100, G.reputation + 1); return p.name + " réservée aux athlètes aujourd'hui. +35 000 €, réputation +1."; }),
                                  new Choice("Refuser", G => "Les athlètes iront ailleurs.") } },
            new Def { id = "power", ok = G => G.lifts.Count > 0, title = "⚡ Panne électrique", text = "Une ligne haute tension est tombée cette nuit. Le distributeur annonce un retour du courant vers 11 h 30.",
                choices = new[] { new Choice("Louer des groupes électrogènes (18 000 €)", G => { Game.Charge(18000, "Remontées"); return "Groupes installés : ouverture à l'heure."; }),
                                  new Choice("Attendre le courant", G => { foreach (var l in G.lifts) l.brokenUntil = Math.Max(l.brokenUntil, 11.5 * 3600); Touch(); return "Remontées arrêtées jusqu'à 11 h 30."; }) } },
            new Def { id = "film", ok = G => G.pistes.Count >= 2, title = "🎬 Tournage de film", text = "Une production cherche un décor alpin pour une scène de poursuite et propose de louer le domaine pour la matinée.",
                choices = new[] { new Choice("Accepter (+45 000 €)", G => { G.money += 45000; G.reputation = Math.Min(100, G.reputation + 1); foreach (var l in G.lifts) l.brokenUntil = Math.Max(l.brokenUntil, 10 * 3600); Touch(); return "Tournage jusqu'à 10 h : ouverture retardée, +45 000 €."; }),
                                  new Choice("Refuser", G => "Vos skieurs profitent de la première trace.") } },
            new Def { id = "inspection", ok = G => G.lifts.Any(l => l.cond < 0.8), title = "🔎 Contrôle technique", text = "Le service des remontées mécaniques annonce une inspection surprise. Certaines installations sont fatiguées.",
                choices = new[] { new Choice("Remise à niveau immédiate", G => { double c = 0; foreach (var l in G.lifts) { c += Game.R((1 - l.cond) * l.cost * 0.08); l.cond = 1; } Game.Charge(c, "Remontées"); return "Toutes les remontées remises à neuf pour " + GMath.Eur(c) + "."; }),
                                  new Choice("Passer le contrôle tel quel", G => { var bad = G.lifts.Where(l => l.cond < 0.75).ToList(); foreach (var l in bad) l.brokenUntil = 13 * 3600; Touch(); return bad.Count > 0 ? bad.Count + " remontée(s) arrêtée(s) jusqu'à 13 h par l'inspecteur." : "Contrôle réussi de justesse."; }) } },
            new Def { id = "chef", ok = G => G.buildings.Any(b => b.Def.type == "food"), title = "👨‍🍳 Chef étoilé", text = "Un chef réputé propose une résidence d'une semaine dans votre restaurant.",
                choices = new[] { new Choice("L'accueillir (25 000 €)", G => { if (!Game.Spend(25000, "Marketing")) return "Fonds insuffisants : le chef décline."; G.reputation = Math.Min(100, G.reputation + 4); return "La presse gastronomique en parle : réputation +4."; }),
                                  new Choice("Décliner", G => "Le restaurant garde sa carte habituelle.") } }
        };
        /// <summary>Chaque matin : environ une chance sur quatre, jamais deux fois de suite le même.</summary>
        public static void Morning()
        {
            var G = Game.G; if (G.pending != null || G.day < 2 || Rand.Next() > 0.27) return;
            var pool = LIST.Where(e => e.id != G.lastEvent && e.ok(G)).ToArray(); if (pool.Length == 0) return;
            var e2 = Rand.Pick(pool); G.pending = e2.id; G.lastEvent = e2.id;
            Game.Emit("morning", e2);
        }
        public static Def Current { get { var G = Game.G; return G != null && G.pending != null ? LIST.FirstOrDefault(e => e.id == G.pending) : null; } }
        public static void Choose(int i)
        {
            var G = Game.G; var e = Current; if (e == null) return;
            string msg = e.choices[i].apply(G); G.pending = null;
            Game.Log(e.title + " — " + msg, "info");
        }
    }

    /// <summary>Actions du joueur (panneaux de gestion). Renvoient null en cas de succès, sinon un message.</summary>
    public static class Actions
    {
        static GameState G { get { return Game.G; } }
        public static void ToggleOpen(object o) { var l = o as Lift; var p = o as Piste; var b = o as Building; if (l != null) l.open = !l.open; if (p != null) p.open = !p.open; if (b != null) b.open = !b.open; Game.Net.Touch(); Game.Emit("world"); }
        public static string Cannons(Piste p) { double c = Game.R(p.len * 55); if (!Game.Spend(c, "Construction")) return "Fonds insuffisants."; p.cannons = true; Game.Emit("piste:refresh", p); return null; }
        public static string Light(Piste p) { double c = Game.R(p.len * 45); if (!Game.Spend(c, "Construction")) return "Fonds insuffisants."; p.lit = true; Game.Emit("piste:refresh", p); return null; }
        public static void NightLift(Lift l) { l.night = !l.night; Game.Net.Touch(); }
        public static void GroomPrio(Piste p, int prio) { p.prio = prio; p.noGroom = prio == 0; }
        public static string Upgrade(Building b) { if (b.level >= 3) return "Niveau maximum atteint."; if (!Game.Spend(Game.UpgradeCost(b), "Construction")) return "Fonds insuffisants."; b.level++; return null; }
        public static void Staff(Building b, int d) { b.staff = GMath.Clamp(b.staff + d, 1, 14); }
        public static void Maint(int lvl) { G.maint = GMath.Clamp(lvl, 0, 2); }
        public static void Wage(int lvl) { G.wage = GMath.Clamp(lvl, 0, 2); }
        public static string Marketing(string kind)
        {
            double c = kind == "tv" ? 45000 : 8000; if ((kind == "tv" ? G.marketing.tv : G.marketing.social) > 0) return "Campagne déjà en cours.";
            if (!Game.Spend(c, "Marketing")) return "Fonds insuffisants.";
            if (kind == "tv") G.marketing.tv = 7; else G.marketing.social = 5; return null;
        }
        public static string ScheduleEvent(string id)
        {
            var ev = Catalog.Event(id); if (G.@event != null) return "Un événement est déjà programmé.";
            if (ev.bid) return "Les Jeux Olympiques s'obtiennent uniquement par la candidature.";
            if (!ev.need(Game.Stats())) return "Prérequis manquants : " + ev.needTxt + ".";
            if (!Game.Spend(ev.cost, "Événements")) return "Fonds insuffisants.";
            G.@event = new EventRun { id = ev.id, day = G.day + 3, left = ev.days }; return null;
        }
        public static string LabUpgrade() { if (G.lab >= 3) return "Laboratoire au niveau maximum."; if (!Game.Spend(Mgmt.LabCost(), "Recherche")) return "Fonds insuffisants."; G.lab++; return null; }
        public static void GroomNight() { G.groomNight = !G.groomNight; }
        public static void SetRange(Building b, int r) { Mgmt.SetRange(b, GMath.Clamp(r, 0, 2)); }
        public static void StaffLevel(Building b, int lvl) { b.staffLvl = GMath.Clamp(lvl, 0, 2); }
        public static void Hours(Building b, string h) { b.hours = h; }
        public static string HotelOption(Building b, string opt)
        {
            var o = b.opts;
            if (opt == "spa") { if (o.spa) return null; if (!Game.Spend(Mgmt.SpaCost(b), "Construction")) return "Fonds insuffisants."; o.spa = true; return null; }
            if (opt == "hb") o.hb = !o.hb; else if (opt == "shuttle") o.shuttle = !o.shuttle; return null;
        }
        public static string Machines(Building b, int d)
        {
            if (d > 0) { if (b.machines >= 6) return "Garage complet (6 dameuses)."; if (!Game.Spend(Mgmt.MachineCost, "Construction")) return "Fonds insuffisants."; b.machines++; return null; }
            if (b.machines <= 1) return null; b.machines--; Game.Earn(30000, "Revente"); return null;
        }
        public static void Patrol(Building b, int d) { b.patrol = GMath.Clamp(b.patrol + d, 2, 12); Mgmt.SecurePistes(); }
        public static void Price(string which, double v) { if (which == "pass") G.passPrice = v; else if (which == "night") G.nightPrice = v; else if (which == "summer") G.summerPass = v; }
        public static void BuildingPrice(Building b, double v) { b.price = Math.Max(0, v); }
        public static void Rename(object o, string name)
        {
            name = (name ?? "").Trim(); if (name.Length == 0) return; if (name.Length > 28) name = name.Substring(0, 28);
            var l = o as Lift; var p = o as Piste; if (l != null) l.name = name; else if (p != null) p.name = name; else if (o == null) G.name = name;
        }
        public static void Speed(double s) { if (s <= 0) G.paused = true; else { G.paused = false; G.speed = s; } }
    }
}
