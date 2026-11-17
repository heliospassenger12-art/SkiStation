// SkiStation — Gestion : météo, enneigement, neige de culture. Portage de Web/js/weather.js.
using System;
using System.Collections.Generic;

namespace SkiStation.Gestion
{
    public static class Weather
    {
        public static double WindNow;
        static GameState G { get { return Game.G; } }

        static WeatherState Roll(int day)
        {
            double late = GMath.Smooth(85, 140, day), r = Rand.Next();
            var table = G.summer
                ? new[] { T("sun", 0.46), T("cloud", 0.30), T("rain", 0.17), T("storm", 0.07) }
                : new[] { T("sun", 0.30), T("cloud", 0.24), T("snow", 0.23 - late * 0.09), T("heavy", 0.07 - late * 0.03), T("storm", 0.05), T("thaw", 0.04 + late * 0.14), T("frost", 0.11 - late * 0.09) };
            double tot = 0; foreach (var t in table) tot += t.Value;
            double x = r * tot;
            foreach (var t in table) { x -= t.Value; if (x <= 0) return Make(t.Key, day); }
            return Make("sun", day);
        }
        static KeyValuePair<string, double> T(string k, double v) { return new KeyValuePair<string, double>(k, v); }

        public static WeatherState Make(string type, int day)
        {
            var def = Catalog.Weather(type);
            if (G.summer) return new WeatherState { type = type, temp = Game.R(19 + Rand.Range(-3, 5) - (type == "rain" || type == "storm" ? 6 : 0)), wind = Game.R(def.wind * 0.7 + Rand.Range(3, 22)), snow = 0 };
            double b = -4 + 9 * GMath.Smooth(60, 140, day) + (day < 15 ? 1.5 : 0);
            return new WeatherState { type = type, temp = Game.R(b + def.warm + Rand.Range(-2, 2) + (type == "sun" ? 1.5 : 0)), wind = Game.R(def.wind + Rand.Range(5, type == "storm" ? 45 : 28)), snow = Game.R(def.snow * Rand.Range(0.6, 1.4)) };
        }
        public static void Init()
        {
            G.weather = Make("sun", 0); G.weather.wind = 10;
            G.forecast = new List<WeatherState> { Make("cloud", 1), Roll(2), Roll(3), Roll(4) };
            WindNow = G.weather.wind;
        }
        public static void Next()
        {
            G.weather = G.forecast[0]; G.forecast.RemoveAt(0); G.forecast.Add(Roll(G.day + 4));
            WindNow = G.weather.wind;
        }
        public static WeatherDef Def { get { return Catalog.Weather(G.weather.type); } }
        public static double TempAt(double alt, bool night)
        {
            return G.weather.temp - (alt - 1650) * 0.0065 - (night ? 5 : 0) + (G.time > 12 * 3600 && G.time < 16 * 3600 && !night ? 2 : 0);
        }
        /// <summary>Qualité de neige perçue 0..1 (le niveau module l'effet de la poudreuse).</summary>
        public static double Quality(Piste p, int skill)
        {
            if (G.summer) return G.weather.type == "rain" ? 0.45 : 0.78;
            double q = 0.5 + 0.3 * p.groomed + p.powder * (skill >= 2 ? 0.25 : -0.1) - 0.5 * p.ice - 0.35 * p.slush;
            if (p.depth < 30) q -= 0.2;
            return GMath.Clamp(q, 0, 1);
        }
        public static string State(Piste p)
        {
            if (G.summer) return G.weather.type == "rain" ? "Sentier boueux" : "Sentier sec";
            if (p.snowClosed) return "Neige insuffisante";
            if (p.ice > 0.45) return "Verglas";
            if (p.slush > 0.45) return "Neige soupe";
            if (p.powder > 0.5) return "Poudreuse";
            if (p.groomed > 0.7) return "Damée";
            if (p.groomed > 0.35) return "Tracée";
            return "Bosselée";
        }
        /// <summary>Vent : varie dans la journée, arrête les remontées exposées (les passagers engagés terminent).</summary>
        public static void UpdateWind()
        {
            WindNow = Math.Max(0, G.weather.wind * (0.75 + 0.5 * Math.Sin(G.time / 2700 + G.day)) + Rand.Range(-4, 4));
            bool changed = false;
            foreach (var l in G.lifts)
            {
                var def = Catalog.Lift(l.type); double w = WindNow * (0.65 + (l.b.y - 1650) / 2500 * 0.75);
                bool stop = w > def.wind || (l.windStop && w > def.wind - 8);
                if (stop != l.windStop) { l.windStop = stop; changed = true; Game.Log(stop ? "🌬️ " + l.name + " arrêté : vent " + Game.R(w) + " km/h en crête." : l.name + " : le vent faiblit, réouverture.", stop ? "warn" : "good"); }
            }
            if (changed) Game.Net.Touch();
        }
        /// <summary>Effets en cours de journée (toutes les 10 min de jeu).</summary>
        public static void DayTick()
        {
            if (G.casual || G.summer) return;
            foreach (var p in G.pistes)
            {
                double mid = p.topAlt - p.drop / 2, t = TempAt(mid, false);
                if (G.time > 12.5 * 3600 && t > 1.5) p.slush = Math.Min(1, p.slush + (t - 1.5) * 0.012);
                if (t > 3) p.ice = Math.Max(0, p.ice - 0.03);
            }
        }
        /// <summary>Passage de la nuit : chutes, fonte, regel, canons. Le damage est fait avant par les dameuses.</summary>
        public static void Night()
        {
            var w = G.weather; double cannonCost = 0; var closed = new List<string>();
            if (G.summer) { foreach (var p in G.pistes) { p.passes = 0; p.snowClosed = false; p.avaUntil = 0; } return; }
            bool sm2 = Game.Researched("snowmaking2");
            foreach (var p in G.pistes)
            {
                double mid = p.topAlt - p.drop / 2, tn = TempAt(mid, true), td = TempAt(mid, false);
                double fall = w.snow * (0.6 + (mid - 1650) / 1000 * 0.5);
                if (td > 2) p.depth -= (td - 2) * 1.3;
                p.depth -= 0.25 + p.passes * 0.001;
                if (p.slush > 0.25 && tn < -1) p.ice = Math.Max(p.ice, p.slush * (p.groomedTonight ? 0.2 : 0.9));
                p.slush = 0;
                if (fall > 2) { p.depth += fall * 0.6; p.powder = GMath.Clamp(fall / 22, 0, 1); p.ice *= 0.2; if (!p.groomedTonight) p.groomed = Math.Min(p.groomed, 0.45); }
                else p.powder *= 0.4;
                if (p.cannons && tn < (sm2 ? -1 : -2) && p.depth < 110) { p.depth += 9; cannonCost += p.len * 0.9 * (sm2 ? 0.6 : 1); }
                p.depth = GMath.Clamp(p.depth, 0, 400);
                if (G.casual) { p.depth = Math.Max(p.depth, 60); p.ice = 0; p.slush = 0; }
                bool was = p.snowClosed; p.snowClosed = p.depth < 15;
                if (p.snowClosed && !was) closed.Add(p.name);
                p.avaUntil = 0;
                if (!Game.Researched("gazex") && w.snow >= 25 && (p.topAlt > 2700 || p.diff == 3)) p.avaUntil = 10.5 * 3600;
                p.passes = 0; p.groomedTonight = false;
            }
            G.snowBase = GMath.Clamp(G.snowBase + w.snow * 0.5 - Math.Max(0, w.temp - 1) * 1.2 - 0.3, 5, 300);
            if (cannonCost > 0) Game.Charge(Game.R(cannonCost), "Neige de culture");
            if (closed.Count > 0) Game.Log("⚠️ Neige insuffisante : " + string.Join(", ", closed) + " fermée(s). Installez des enneigeurs.", "warn");
        }
    }
}
