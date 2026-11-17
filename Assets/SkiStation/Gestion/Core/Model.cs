// SkiStation — Gestion : modèle de données (portage de la version web). Champs en minuscules, identiques au JavaScript,
// pour suivre le portage ligne à ligne. Les champs marqués « exécution » ne sont pas sauvegardés : ils sont recalculés.
using System;
using System.Collections.Generic;

namespace SkiStation.Gestion
{
    public static class Const
    {
        public const double CLOCK_RATE = 30;      // secondes de jeu par seconde réelle à x1
        public const double MOTION = 0.2;         // vitesse simulée = vitesse réelle × MOTION (par seconde de jeu)
        public const double DAY_START = 7.5 * 3600, DAY_END = 24 * 3600 - 60;
        public const double LIFT_OPEN = 9 * 3600, LIFT_CLOSE = 16.75 * 3600;
        public const double NIGHT_OPEN = 18.5 * 3600, NIGHT_CLOSE = 21 * 3600;
        public const int SEASON_DAYS = 140, SUMMER_DAYS = 30;
        public static readonly P2 ARRIVAL = new P2(60, 2990);
        public static readonly Decor[] DECOR = {
            new Decor(-150, 2800, 16, "church"), new Decor(-90, 2870, 11, "house"), new Decor(-210, 2870, 11, "house"), new Decor(-250, 2790, 11, "house"),
            new Decor(-40, 2790, 11, "house"), new Decor(-130, 2930, 11, "house"), new Decor(230, 2830, 11, "house"), new Decor(290, 2890, 11, "house"), new Decor(-310, 2880, 11, "house") };
    }
    public struct Decor { public double x, z, r; public string k; public Decor(double x, double z, double r, string k) { this.x = x; this.z = z; this.r = r; this.k = k; } }

    public sealed class CablePt { public double x, y, z, g, s; public bool pylon; }

    public sealed class Piste
    {
        public int id; public string name; public List<P2> ctrl = new List<P2>(); public double width = 30;
        public bool open = true, cannons, lit, snowClosed, groomedTonight, noGroom, secured;
        public double depth, groomed = 0.8, ice, slush, powder, avaUntil, postDist; public int passes, prio = 1;
        // exécution
        [NonSerialized] public List<P3> pts; public double len, drop, avg, p90, topAlt; public int diff;
        [NonSerialized] public int on; [NonSerialized] public List<Edge> edges = new List<Edge>(); [NonSerialized] public Node na, nb;
    }

    public sealed class Lift
    {
        public int id; public string type, name; public bool open = true, night; public double cond = 1, cost, built;
        public P3 a, b;
        // exécution
        [NonSerialized] public List<CablePt> path; public double L, len, vert, capacity, maint; public int n; public double D, phase;
        [NonSerialized] public List<List<Visitor>> veh = new List<List<Visitor>>(); [NonSerialized] public List<Visitor> queue = new List<Visitor>();
        public double brokenUntil; public bool windStop; public int rides; public double waitSum; public int riders; public bool moving;
        [NonSerialized] public Node na, nb; [NonSerialized] public Edge edge;
    }

    public sealed class HotelOptions { public bool hb, spa, shuttle; }

    public sealed class Building
    {
        public int id; public string type; public double x, z, y, rot; public int level = 1; public double price; public bool open = true, starter;
        public int staff, range = 1, staffLvl = 1; public string hours = "day"; public double rating = 3.5; public HotelOptions opts;
        public int machines, patrol; public double sRev, sCost, sVisits, guests;
        // exécution
        [NonSerialized] public int occ; [NonSerialized] public List<Visitor> queue = new List<Visitor>();
        public int visits; public double revenue, cogs, lastRev, lastCost, lastVisits; public int busy; public int node = -1;
        [NonSerialized] public Dictionary<double, int> session = new Dictionary<double, int>();
        [NonSerialized] public double _pf, _w;
        public BuildingDef Def { get { return Catalog.Building(type); } }
    }

    public sealed class Road { public int id; public bool @fixed; public List<P2> pts = new List<P2>(); }

    public sealed class Node { public int id; public double x, z, y; public bool hub; public List<Edge> @out = new List<Edge>(); public List<Building> services = new List<Building>(); }
    public sealed class Edge { public int id; public string kind; public Lift lift; public Piste piste; public Node from, to; public double len, s0, s1; }

    public sealed class WeatherState { public string type = "sun"; public double temp, wind, snow; public WeatherDef Def { get { return Catalog.Weather(type); } } }
    public sealed class ResearchJob { public string id; public int left, total; }
    public sealed class ResearchState { public List<string> done = new List<string>(); public List<ResearchJob> active = new List<ResearchJob>(); public List<string> queue = new List<string>(); }
    public sealed class Loan { public int id; public string name; public double left, pay, rate; public int daysLeft; }
    public sealed class EventRun { public string id; public int day, left; }
    public sealed class Marketing { public int social, tv; }
    public sealed class LogEntry { public double t; public int d; public string msg, kind; }
    public sealed class HistoryEntry { public bool su; public int day, year, v; public double sat, inc, exp, money, rep; }
    public sealed class Ledger { public Dictionary<string, double> inc = new Dictionary<string, double>(), exp = new Dictionary<string, double>(); }
    public sealed class Season { public Dictionary<string, double> inc = new Dictionary<string, double>(), exp = new Dictionary<string, double>(); public int days; public double visitors; }
    public sealed class Safety { public int incidents, severe; public double respSum; public int respN; }
    public sealed class GroomReport { public int done; public List<string> missed = new List<string>(); public double km; }
    public sealed class ScnState { public string id; public int stage, stars; public bool done, failed; }
    public sealed class Rival { public string name; public double score; }
    public sealed class Bid { public string phase; public int due, mid, retry, tries; public double score = -1; public bool late; public List<Rival> rivals = new List<Rival>(); }
    public sealed class Flat { public double x, z, r, h, blend; }

    public sealed class GameState
    {
        public int v = 1; public string mode = "career", name = "Mont-Hélios"; public bool unlimited, allUnlocked, casual; public int seed = 1337, level = 1;
        public int day, year = 1; public double time = Const.DAY_START, speed = 1; public bool paused = true;
        public double money, loan; public Dictionary<string, int> ach = new Dictionary<string, int>();
        public Ledger ledger = new Ledger(); public Dictionary<string, double> ledger0 = new Dictionary<string, double>(); public List<HistoryEntry> history = new List<HistoryEntry>();
        public List<Piste> pistes = new List<Piste>(); public List<Lift> lifts = new List<Lift>(); public List<Building> buildings = new List<Building>(); public List<Road> roads = new List<Road>();
        public int nextId = 1; public List<Flat> flats = new List<Flat>();
        public ResearchState research = new ResearchState(); public int tier, lab = 1;
        public double passPrice = 26, nightPrice = 14, summerPass = 12; public bool summer; public int maint = 1, wage = 1; public Marketing marketing = new Marketing();
        public double reputation = 50, prestige, sat = 60, fame = 8; public Dictionary<string, double> prestigeParts = new Dictionary<string, double>(); public List<string> prestigeCaps = new List<string>();
        [NonSerialized] public List<double> satToday = new List<double>();
        public WeatherState weather = new WeatherState(); public List<WeatherState> forecast = new List<WeatherState>(); public double snowBase = 55;
        public ScnState scn; public int elapsed; public Bid bid; public EventRun @event; public Dictionary<string, int> eventsHosted = new Dictionary<string, int>(); public int bestDay;
        public Dictionary<string, int> medals = new Dictionary<string, int>(); public int incidentsToday, visitorsToday, totalVisitors; public List<LogEntry> log = new List<LogEntry>();
        public List<Loan> loans = new List<Loan>(); public Season season = new Season(), lastSeason; public Safety safety = new Safety(); public bool groomNight; public GroomReport groomReport; public int negDays;
        public string pending, lastEvent; public int olympicsDay1 = -1;
        public List<int> removedTrees = new List<int>();
        public bool OlympicsOpened { get { return olympicsDay1 >= 0; } }
    }

    // ---------- résultats d'analyse (devis avant validation) ----------
    public class Analysis { public bool ok; public string err, warn; public double cost; public int trees; }
    public sealed class PisteAnalysis : Analysis { public double width = 30, len, drop, topAlt, avg, p90, max; public int diff; public List<P3> pts; public List<P2> ctrl; public string joins, startLift, endLift; public Piste joinPiste; }
    public sealed class LiftAnalysis : Analysis { public LiftDef def; public double vert, hlen, len, maxLen, capacity, rideMin, maint, L; public int vehicles; public List<CablePt> path; public P3 a, b; }
    public sealed class BuildingAnalysis : Analysis { public BuildingDef def; public double x, z, y, slope, earth; }
    public sealed class RoadAnalysis : Analysis { public double len, maxSlope; public List<P2> pts; public bool joins; }
    public sealed class AutoPisteResult { public string err, dest; public List<P2> ctrl; public PisteAnalysis an; public bool bad; }
}
