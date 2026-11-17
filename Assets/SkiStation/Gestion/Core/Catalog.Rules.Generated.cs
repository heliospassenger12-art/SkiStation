// FICHIER GÉNÉRÉ depuis Web/js/data.js (script gen_rules.js) : événements, succès et scénarios.
using System;
using System.Collections.Generic;
using System.Linq;

namespace SkiStation.Gestion
{
    public sealed class EventDef { public string id, name, needTxt; public double cost, boost, prestige, fame, fameGain; public int days; public bool comp, bid; public Func<Stats, bool> need; }
    public sealed class AchDef { public string id, name, desc; public Func<Stats, GameState, bool> test; }
    public sealed class Goal { public string text; public Func<Stats, bool> test; public Func<Stats, string> val; }
    public sealed class StageDef { public string title; public double reward, fame; public Goal[] goals; }
    public sealed class ScenarioStart { public double money, fame = 5, rep = -1, sat = -1; public int seed; public string prebuilt; public string[] research = new string[0]; }
    public sealed class ScenarioDef { public string id, name, tag, brief, victory; public int diff, deadline; public bool seedLock, bid; public double bankrupt; public ScenarioStart start; public StageDef[] stages; }

    public static partial class Catalog
    {
        public static readonly EventDef[] EVENTS = {
            new EventDef { id = "fete", name = "Fête du village", cost = 15000, boost = 1.25, prestige = 1, fame = 0, fameGain = 0.8, days = 1, comp = false, bid = false, needTxt = "Aucun prérequis", need = s => true },
            new EventDef { id = "concert", name = "Festival de concerts", cost = 60000, boost = 1.4, prestige = 2, fame = 10, fameGain = 1.8, days = 2, comp = false, bid = false, needTxt = "Scène de concerts · notoriété 10 %", need = s => s.stage >= 1 && s.fame >= 10 },
            new EventDef { id = "regional", name = "Course régionale", cost = 40000, boost = 1.3, prestige = 3, fame = 18, fameGain = 3, days = 1, comp = true, bid = false, needTxt = "Stade de slalom, piste rouge · notoriété 18 %", need = s => s.stadium >= 1 && s.red >= 1 && s.fame >= 18 },
            new EventDef { id = "europe", name = "Coupe d'Europe", cost = 150000, boost = 1.5, prestige = 6, fame = 35, fameGain = 5, days = 2, comp = true, bid = false, needTxt = "Stade, piste noire, 250 lits · notoriété 35 %, prestige 30", need = s => s.stadium >= 1 && s.black >= 1 && s.beds >= 250 && s.fame >= 35 && s.prestige >= 30 },
            new EventDef { id = "xgames", name = "X Games d'hiver", cost = 450000, boost = 1.9, prestige = 9, fame = 60, fameGain = 9, days = 3, comp = true, bid = false, needTxt = "Tous les critères X Games (voir Carrière) · notoriété 60 %, prestige 45", need = s => Scenarios.CritOk("xgames") },
            new EventDef { id = "worldcup", name = "Coupe du monde", cost = 600000, boost = 1.8, prestige = 10, fame = 68, fameGain = 10, days = 3, comp = true, bid = false, needTxt = "Stade, 2 pistes noires, 600 lits, une Coupe d'Europe, satisfaction 72 % · notoriété 68 %, prestige 50", need = s => s.stadium >= 1 && s.black >= 2 && s.beds >= 600 && s.h_europe >= 1 && s.fame >= 68 && s.prestige >= 50 && s.satAvg >= 72 },
            new EventDef { id = "olympics", name = "Jeux Olympiques d'hiver", cost = 3000000, boost = 2.2, prestige = 25, fame = 80, fameGain = 20, days = 6, comp = true, bid = true, needTxt = "Uniquement via la candidature olympique", need = s => false }
        };
        public static readonly AchDef[] ACH = {
            new AchDef { id = "first", name = "Premier client", desc = "Accueillir un premier visiteur.", test = (s, G) => s.total >= 1 },
            new AchDef { id = "v10k", name = "Dix mille sourires", desc = "10 000 visiteurs cumulés.", test = (s, G) => s.total >= 10000 },
            new AchDef { id = "v100k", name = "Destination incontournable", desc = "100 000 visiteurs cumulés.", test = (s, G) => s.total >= 100000 },
            new AchDef { id = "full", name = "Complet !", desc = "1 000 visiteurs en une journée.", test = (s, G) => s.bestDay >= 1000 },
            new AchDef { id = "happy", name = "Station du bonheur", desc = "Satisfaction de 80 % avec au moins 300 visiteurs.", test = (s, G) => s.sat >= 80 && G.visitorsToday >= 300 },
            new AchDef { id = "rainbow", name = "Les quatre couleurs", desc = "Une piste ouverte de chaque difficulté.", test = (s, G) => new[] { 0, 1, 2, 3 }.All(d => G.pistes.Any(p => p.diff == d && p.open)) },
            new AchDef { id = "km20", name = "Grand domaine", desc = "20 km de pistes.", test = (s, G) => s.km >= 20 },
            new AchDef { id = "alt3500", name = "Toit du domaine", desc = "Une remontée arrivant au-dessus de 3 500 m.", test = (s, G) => s.topAlt >= 3500 },
            new AchDef { id = "vert2000", name = "Deux mille de dénivelé", desc = "2 000 m entre le point bas et le point haut desservis.", test = (s, G) => s.vert >= 2000 },
            new AchDef { id = "million", name = "Premier million", desc = "1 000 000 € en caisse.", test = (s, G) => s.money >= 1000000 },
            new AchDef { id = "debtfree", name = "Sans dette", desc = "N'avoir aucun emprunt en cours après la première saison.", test = (s, G) => s.year >= 2 && s.loan <= 0 },
            new AchDef { id = "medals", name = "Pépinière de champions", desc = "500 médailles délivrées par l'école de ski.", test = (s, G) => s.medals >= 500 },
            new AchDef { id = "nightowl", name = "Oiseaux de nuit", desc = "Vendre des forfaits nocturnes.", test = (s, G) => s.night },
            new AchDef { id = "science", name = "Bureau d'études", desc = "Terminer 10 recherches.", test = (s, G) => s.research >= 10 },
            new AchDef { id = "host", name = "Terre d'événements", desc = "Organiser 5 événements.", test = (s, G) => s.eventsHosted >= 5 },
            new AchDef { id = "stars5", name = "Cinq étoiles", desc = "Prestige ★★★★★ (95 ou plus).", test = (s, G) => s.prestige >= 95 },
            new AchDef { id = "season", name = "Une saison au compteur", desc = "Terminer une saison d'hiver.", test = (s, G) => s.year >= 2 },
            new AchDef { id = "olympic", name = "Flamme olympique", desc = "Organiser les Jeux Olympiques d'hiver.", test = (s, G) => s.olympics >= 1 }
        };
        public static readonly ScenarioDef[] SCENARIOS = {
            new ScenarioDef { id = "first", name = "Premiers pas", tag = "Facile", diff = 1, seedLock = false, bid = false, deadline = 70, bankrupt = 200000, brief = "Une montagne vierge, 420 000 € en poche et 70 jours pour prouver que votre station peut attirer du monde. Objectif : faire venir 250 visiteurs en une seule journée.", victory = "Plus de 250 visiteurs en une journée : votre station est sur la carte des Alpes !",
                start = new ScenarioStart { money = 420000, seed = 1337, fame = 6, research = new string[] {  } },
                stages = new[] {
                    new StageDef { title = "Ouvrir la station", reward = 40000, fame = 0, goals = new[] {
                        new Goal { text = "Construire une remontée", test = s => s.lifts >= 1, val = s => GMath.Num(s.lifts) },
                        new Goal { text = "Relier une piste au front de neige", test = s => s.connected >= 1, val = s => GMath.Num(s.connected) },
                        new Goal { text = "Accueillir 60 visiteurs en une journée", test = s => s.bestDay >= 60, val = s => s.bestDay + " / 60" } } },
                    new StageDef { title = "250 visiteurs", reward = 120000, fame = 0, goals = new[] {
                        new Goal { text = "250 visiteurs en une seule journée", test = s => s.bestDay >= 250, val = s => s.bestDay + " / 250" },
                        new Goal { text = "Satisfaction d'au moins 55 %", test = s => s.sat >= 55, val = s => s.sat + " %" } } } } },
            new ScenarioDef { id = "rescue", name = "Station en perdition", tag = "Difficile", diff = 3, seedLock = true, bid = false, deadline = 320, bankrupt = 200000, brief = "Val-Mort vieillit mal : remontées usées, forfait trop cher, clients mécontents et 470 000 € de dettes. Remettez les comptes à flot, modernisez, puis faites de cette station une destination incontournable avant 320 jours.", victory = "Val-Mort est redevenue incontournable : la presse parle d'un miracle alpin !",
                start = new ScenarioStart { money = 45000, seed = 4242, fame = 12, rep = 24, sat = 38, prebuilt = "decay", research = new string[] { "tier1" } },
                stages = new[] {
                    new StageDef { title = "Éviter la faillite", reward = 60000, fame = 0, goals = new[] {
                        new Goal { text = "Satisfaction d'au moins 55 %", test = s => s.sat >= 55, val = s => s.sat + " %" },
                        new Goal { text = "Résultat positif en moyenne sur 7 jours", test = s => s.profit7 > 0, val = s => (s.profit7 >= 0 ? "+" : "−") + GMath.Num(Math.Abs(s.profit7)) + " € / jour" },
                        new Goal { text = "Un poste de secours", test = s => s.rescue >= 1, val = s => GMath.Num(s.rescue) } } },
                    new StageDef { title = "Remise à niveau", reward = 150000, fame = 0, goals = new[] {
                        new Goal { text = "Toutes les remontées en bon état (≥ 80 %)", test = s => s.minCond >= 0.8, val = s => Math.Round(s.minCond * 100).ToString("0") + " %" },
                        new Goal { text = "Au moins 2 dameuses", test = s => s.machines >= 2, val = s => GMath.Num(s.machines) },
                        new Goal { text = "Dette ramenée à 250 000 € ou moins", test = s => s.debt <= 250000, val = s => GMath.Num(s.debt) + " €" },
                        new Goal { text = "Satisfaction d'au moins 68 %", test = s => s.sat >= 68, val = s => s.sat + " %" },
                        new Goal { text = "10 km de pistes", test = s => s.km >= 10, val = s => s.km.ToString("0.0") + " km" } } },
                    new StageDef { title = "Incontournable", reward = 400000, fame = 0, goals = new[] {
                        new Goal { text = "Notoriété d'au moins 55 %", test = s => s.fame >= 55, val = s => Math.Round(s.fame).ToString("0") + " %" },
                        new Goal { text = "600 visiteurs en une journée", test = s => s.bestDay >= 600, val = s => s.bestDay + " / 600" },
                        new Goal { text = "Satisfaction d'au moins 78 %", test = s => s.sat >= 78, val = s => s.sat + " %" },
                        new Goal { text = "Prestige d'au moins 45", test = s => s.prestige >= 45, val = s => GMath.Num(s.prestige) },
                        new Goal { text = "Organiser une course régionale", test = s => s.h_regional >= 1, val = s => GMath.Num(s.h_regional) },
                        new Goal { text = "Dette inférieure à 100 000 €", test = s => s.debt < 100000, val = s => GMath.Num(s.debt) + " €" } } } } },
            new ScenarioDef { id = "family", name = "Capitale des familles", tag = "Moyen", diff = 2, seedLock = false, bid = false, deadline = 240, bankrupt = 200000, brief = "Faites de votre station la référence des vacances en famille : Piou-Piou, école de ski, pistes faciles, hébergements et activités pour toutes les générations. Vous avez deux saisons.", victory = "Les familles du monde entier réservent chez vous : Capitale des familles !",
                start = new ScenarioStart { money = 520000, seed = 9001, fame = 8, research = new string[] { "tier1" } },
                stages = new[] {
                    new StageDef { title = "Accueillir les petits", reward = 80000, fame = 0, goals = new[] {
                        new Goal { text = "Jardin des Piou-Piou", test = s => s.kids >= 1, val = s => GMath.Num(s.kids) },
                        new Goal { text = "École de ski", test = s => s.school >= 1, val = s => GMath.Num(s.school) },
                        new Goal { text = "2 pistes vertes ou bleues ouvertes", test = s => s.easy >= 2, val = s => GMath.Num(s.easy) },
                        new Goal { text = "Un restaurant", test = s => s.food >= 1, val = s => GMath.Num(s.food) } } },
                    new StageDef { title = "Station familiale", reward = 250000, fame = 0, goals = new[] {
                        new Goal { text = "350 visiteurs en une journée", test = s => s.bestDay >= 350, val = s => s.bestDay + " / 350" },
                        new Goal { text = "150 lits touristiques", test = s => s.beds >= 150, val = s => GMath.Num(s.beds) + " / 150" },
                        new Goal { text = "Satisfaction d'au moins 72 %", test = s => s.sat >= 72, val = s => s.sat + " %" },
                        new Goal { text = "200 médailles délivrées par l'école", test = s => s.medals >= 200, val = s => s.medals + " / 200" },
                        new Goal { text = "Une luge ou une patinoire", test = s => s.luge + s.rink >= 1, val = s => (s.luge + s.rink) + "" } } } } },
            new ScenarioDef { id = "luxe", name = "Station de prestige", tag = "Difficile", diff = 3, seedLock = false, bid = false, deadline = 300, bankrupt = 200000, brief = "Une clientèle fortunée, des palaces et des tables étoilées. Avec 1,4 M€ et la recherche hôtelière acquise, bâtissez l'adresse la plus prisée des Alpes en moins de 300 jours.", victory = "Votre palace affiche complet : vous êtes l'adresse des Alpes !",
                start = new ScenarioStart { money = 1400000, seed = 1337, fame = 10, research = new string[] { "tier1", "tier2", "luxury" } },
                stages = new[] {
                    new StageDef { title = "Les premières étoiles", reward = 200000, fame = 0, goals = new[] {
                        new Goal { text = "Un hôtel ★★★★ ou un palace", test = s => s.hotel4 + s.palace >= 1, val = s => (s.hotel4 + s.palace) + "" },
                        new Goal { text = "Une table gastronomique", test = s => s.gastro >= 1, val = s => GMath.Num(s.gastro) },
                        new Goal { text = "Prestige d'au moins 40", test = s => s.prestige >= 40, val = s => GMath.Num(s.prestige) },
                        new Goal { text = "Satisfaction d'au moins 70 %", test = s => s.sat >= 70, val = s => s.sat + " %" } } },
                    new StageDef { title = "L'adresse des Alpes", reward = 500000, fame = 0, goals = new[] {
                        new Goal { text = "Un palace ★★★★★", test = s => s.palace >= 1, val = s => GMath.Num(s.palace) },
                        new Goal { text = "2 chalets de prestige", test = s => s.luxchalet >= 2, val = s => GMath.Num(s.luxchalet) },
                        new Goal { text = "Notoriété d'au moins 55 %", test = s => s.fame >= 55, val = s => Math.Round(s.fame).ToString("0") + " %" },
                        new Goal { text = "Prestige d'au moins 65", test = s => s.prestige >= 65, val = s => GMath.Num(s.prestige) },
                        new Goal { text = "Organiser 2 événements", test = s => s.eventsHosted >= 2, val = s => GMath.Num(s.eventsHosted) },
                        new Goal { text = "Satisfaction d'au moins 80 %", test = s => s.sat >= 80, val = s => s.sat + " %" } } } } },
            new ScenarioDef { id = "olympic", name = "La route des Jeux", tag = "Campagne", diff = 2, seedLock = false, bid = true, deadline = 0, bankrupt = 200000, brief = "La grande campagne, longue et exigeante : de la première remontée aux Jeux Olympiques d'hiver. Il faudra exceller partout (domaine, prestige, notoriété, sécurité, hébergement), atteindre le niveau X Games, organiser une Coupe du monde, puis battre les villes rivales devant le CIO. L'ouverture des Jeux marque la victoire. Pas de limite de temps.", victory = "La flamme olympique est allumée chez vous : les Jeux Olympiques d'hiver s'ouvrent et votre station entre dans la légende. Partie gagnée !",
                start = new ScenarioStart { money = 650000, seed = 1337, fame = 5, research = new string[] {  } },
                stages = new[] {
                    new StageDef { title = "Premiers flocons", reward = 60000, fame = 0, goals = new[] {
                        new Goal { text = "Construire une remontée", test = s => s.lifts >= 1, val = s => GMath.Num(s.lifts) },
                        new Goal { text = "Relier une piste au front de neige", test = s => s.connected >= 1, val = s => GMath.Num(s.connected) },
                        new Goal { text = "40 visiteurs en une journée", test = s => s.bestDay >= 40, val = s => s.bestDay + " / 40" } } },
                    new StageDef { title = "Station familiale", reward = 120000, fame = 0, goals = new[] {
                        new Goal { text = "2 pistes vertes ou bleues", test = s => s.easy >= 2, val = s => GMath.Num(s.easy) },
                        new Goal { text = "Un restaurant et une location de matériel", test = s => s.food >= 1 && s.rental >= 1, val = s => (s.food >= 1 ? "✔" : "✘") + " / " + (s.rental >= 1 ? "✔" : "✘") },
                        new Goal { text = "150 visiteurs en une journée", test = s => s.bestDay >= 150, val = s => s.bestDay + " / 150" },
                        new Goal { text = "Satisfaction d'au moins 60 %", test = s => s.sat >= 60, val = s => s.sat + " %" } } },
                    new StageDef { title = "Station reconnue", reward = 250000, fame = 20, goals = new[] {
                        new Goal { text = "École de ski et Jardin des Piou-Piou", test = s => s.school >= 1 && s.kids >= 1, val = s => (s.school >= 1 ? "✔" : "✘") + " / " + (s.kids >= 1 ? "✔" : "✘") },
                        new Goal { text = "6 km de pistes", test = s => s.km >= 6, val = s => s.km.ToString("0.0") + " km" },
                        new Goal { text = "Garage à dameuses et poste de secours", test = s => s.garage >= 1 && s.rescue >= 1, val = s => (s.garage >= 1 ? "✔" : "✘") + " / " + (s.rescue >= 1 ? "✔" : "✘") },
                        new Goal { text = "100 lits touristiques", test = s => s.beds >= 100, val = s => GMath.Num(s.beds) + " / 100" },
                        new Goal { text = "400 visiteurs en une journée", test = s => s.bestDay >= 400, val = s => s.bestDay + " / 400" },
                        new Goal { text = "Notoriété de 20 %", test = s => s.fame >= 20, val = s => Math.Round(s.fame).ToString("0") + " %" } } },
                    new StageDef { title = "Terre de compétition", reward = 400000, fame = 35, goals = new[] {
                        new Goal { text = "Une piste rouge et une piste noire", test = s => s.red >= 1 && s.black >= 1, val = s => s.red + " rouge · " + s.black + " noire" },
                        new Goal { text = "Un stade de slalom", test = s => s.stadium >= 1, val = s => GMath.Num(s.stadium) },
                        new Goal { text = "Organiser une course régionale", test = s => s.h_regional >= 1, val = s => GMath.Num(s.h_regional) },
                        new Goal { text = "600 visiteurs en une journée", test = s => s.bestDay >= 600, val = s => s.bestDay + " / 600" },
                        new Goal { text = "Notoriété de 35 %", test = s => s.fame >= 35, val = s => Math.Round(s.fame).ToString("0") + " %" } } },
                    new StageDef { title = "Niveau international", reward = 600000, fame = 45, goals = new[] {
                        new Goal { text = "Organiser une Coupe d'Europe", test = s => s.h_europe >= 1, val = s => GMath.Num(s.h_europe) },
                        new Goal { text = "Prestige ★★ (40)", test = s => s.prestige >= 40, val = s => s.prestige + " / 40" },
                        new Goal { text = "Notoriété de 50 %", test = s => s.fame >= 50, val = s => Math.Round(s.fame).ToString("0") + " %" },
                        new Goal { text = "20 km de pistes", test = s => s.km >= 20, val = s => s.km.ToString("0.0") + " km" },
                        new Goal { text = "Satisfaction moyenne de 70 %", test = s => s.satAvg >= 70, val = s => Math.Round(s.satAvg).ToString("0") + " %" } } },
                    new StageDef { title = "Le niveau X Games", reward = 900000, fame = 60, goals = new[] {
                        new Goal { text = "Remplir tous les critères X Games (les organiser reste facultatif)", test = s => Scenarios.CritOk("xgames"), val = s => Math.Round(Scenarios.CritShare("xgames") * 100).ToString("0") + " %" } } },
                    new StageDef { title = "Coupe du monde", reward = 1200000, fame = 68, goals = new[] {
                        new Goal { text = "Organiser une Coupe du monde", test = s => s.h_worldcup >= 1, val = s => GMath.Num(s.h_worldcup) },
                        new Goal { text = "Prestige ★★★ (60)", test = s => s.prestige >= 60, val = s => s.prestige + " / 60" },
                        new Goal { text = "Notoriété de 75 %", test = s => s.fame >= 75, val = s => Math.Round(s.fame).ToString("0") + " %" },
                        new Goal { text = "Dossier « Jeux Olympiques » étudié", test = s => s.research_olympic, val = s => s.research_olympic ? "✔" : "✘" } } },
                    new StageDef { title = "Candidature olympique", reward = 2000000, fame = 75, goals = new[] {
                        new Goal { text = "Candidature retenue par le CIO face aux villes rivales", test = s => s.bidOk, val = s => s.bidOk ? "✔" : "✘" } } },
                    new StageDef { title = "Les Jeux Olympiques", reward = 0, fame = 82, goals = new[] {
                        new Goal { text = "Réussir l'inspection finale et ouvrir les Jeux (jour 1 = victoire)", test = s => s.olyDay1, val = s => s.olyDay1 ? "✔" : "✘" } } } } }
        };
    }
}
