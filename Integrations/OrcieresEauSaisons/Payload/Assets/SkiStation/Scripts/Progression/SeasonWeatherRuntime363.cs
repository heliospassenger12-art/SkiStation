using System;
using UnityEngine;

namespace SkiStation
{
    [Serializable]
    public struct SeasonWeatherDay363
    {
        public int day;
        public MountainWeatherType weather;
        public string weatherLabel;
        public float temperatureC;
        public float windKmh;
        public float visibility01;
        public float predictedSnowQuality01;
        public string snowLabel;
        public float demandMultiplier;
        public string impactLabel;
    }

    /// <summary>
    /// 3.82 - Deterministic 7-day mountain forecast synchronized with live weather.
    /// The forecast is authoritative for daily weather values so displayed temperature/wind/visibility match gameplay exactly.
    /// SlopeConditionRuntime remains authoritative for live piste evolution from those weather inputs.
    /// </summary>
    public sealed class SeasonWeatherRuntime363 : MonoBehaviour
    {
        public static SeasonWeatherRuntime363 I { get; private set; }
        private int appliedDay = -1;

        private bool appliedSeasonalDomain;
        public int CurrentDay => GameClock.I != null ? GameClock.I.CurrentDay : (StationRuntime.I != null ? StationRuntime.I.State.day : 1);
        public SeasonWeatherDay363 Today => GetForecast(CurrentDay);
        public float TodayDemandMultiplier => Today.demandMultiplier;
        public string PlanningHint => BuildPlanningHint();
        public event Action Changed;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy() { if (I == this) I = null; }

        private void Start() => ApplyTodayIfNeeded();

        private void Update()
        {
            if (SkiStation.Arcade.ArcadeSessionTime2611.BlocksManagement(this)) return;
            ApplyTodayIfNeeded();
        }

        public SeasonWeatherDay363[] GetUpcoming(int count)
        {
            count = Mathf.Clamp(count, 1, 14);
            var result = new SeasonWeatherDay363[count];
            for (int i = 0; i < count; i++) result[i] = GetForecast(CurrentDay + i);
            return result;
        }

        public SeasonWeatherDay363 GetForecast(int absoluteDay)
        {
            int day = Mathf.Max(1, absoluteDay);
            float r = Hash01(day * 19 + 363);
            float coldWave = Hash01(day * 31 + 91);

            MountainWeatherType type = r < .34f ? MountainWeatherType.Clear
                : r < .58f ? MountainWeatherType.Cloudy
                : r < .80f ? MountainWeatherType.Snow
                : r < .94f ? MountainWeatherType.HeavySnow
                : MountainWeatherType.Storm;

            float temp;
            float wind;
            float visibility;
            float snow;
            float demand;
            string impact;

            switch (type)
            {
                case MountainWeatherType.Clear:
                    temp = Mathf.Lerp(-7f, 2f, coldWave);
                    wind = Mathf.Lerp(3f, 18f, Hash01(day * 13 + 7));
                    visibility = 1f;
                    snow = Mathf.Lerp(.68f, .88f, Hash01(day * 17 + 3));
                    demand = temp > 1f ? .98f : 1.05f;
                    impact = temp > 1f ? "Soleil doux : neige plus fragile en bas du domaine." : "Belle journée : confort visiteurs élevé.";
                    break;
                case MountainWeatherType.Cloudy:
                    temp = Mathf.Lerp(-8f, 1f, coldWave);
                    wind = Mathf.Lerp(6f, 25f, Hash01(day * 13 + 7));
                    visibility = .86f;
                    snow = Mathf.Lerp(.70f, .90f, Hash01(day * 17 + 3));
                    demand = 1.00f;
                    impact = "Conditions stables : impact limité sur la fréquentation.";
                    break;
                case MountainWeatherType.Snow:
                    temp = Mathf.Lerp(-9f, -1f, coldWave);
                    wind = Mathf.Lerp(8f, 32f, Hash01(day * 13 + 7));
                    visibility = .70f;
                    snow = Mathf.Lerp(.82f, .97f, Hash01(day * 17 + 3));
                    demand = 1.03f;
                    impact = "Neige fraîche : qualité des pistes en hausse, visibilité réduite.";
                    break;
                case MountainWeatherType.HeavySnow:
                    temp = Mathf.Lerp(-11f, -2f, coldWave);
                    wind = Mathf.Lerp(16f, 45f, Hash01(day * 13 + 7));
                    visibility = .48f;
                    snow = Mathf.Lerp(.88f, 1f, Hash01(day * 17 + 3));
                    demand = .92f;
                    impact = "Fortes chutes : excellente neige mais confort et débit pénalisés.";
                    break;
                default:
                    temp = Mathf.Lerp(-9f, 0f, coldWave);
                    wind = Mathf.Lerp(38f, 68f, Hash01(day * 13 + 7));
                    visibility = .32f;
                    snow = Mathf.Lerp(.72f, .90f, Hash01(day * 17 + 3));
                    demand = .78f;
                    impact = "Tempête : fréquentation nettement réduite et remontées ralenties.";
                    break;
            }

            if (TerrainGeneration.RealTerrainWater.SeasonalDomain &&
                TerrainGeneration.RealTerrainWater.SeasonFor(day) != TerrainGeneration.AlpineSeason.Winter)
            {
                var season = TerrainGeneration.RealTerrainWater.SeasonFor(day);
                if (season == TerrainGeneration.AlpineSeason.Summer)
                {
                    type = r < .65f ? MountainWeatherType.Clear : MountainWeatherType.Cloudy;
                    temp = Mathf.Lerp(13f, 24f, coldWave);
                    snow = .12f;
                    visibility = type == MountainWeatherType.Clear ? 1f : .87f;
                    demand = 1f;
                    impact = "Été : conditions favorables aux activités de montagne.";
                }
                else
                {
                    temp += season == TerrainGeneration.AlpineSeason.Spring ? 8f : 5f;
                    if (temp > 2f && (type == MountainWeatherType.Snow || type == MountainWeatherType.HeavySnow))
                    {
                        type = MountainWeatherType.Cloudy;
                        visibility = .86f;
                        snow = Mathf.Min(snow,.49f);
                        impact = "Redoux : surveillez la fonte de la neige et des surfaces gelées.";
                    }
                }
            }
            // Small, bounded snow-quality attractiveness effect; weather stays secondary to calendar/economy.
            demand *= Mathf.Lerp(.97f, 1.04f, snow);
            demand = Mathf.Clamp(demand, .75f, 1.08f);

            return new SeasonWeatherDay363
            {
                day = day,
                weather = type,
                weatherLabel = MountainWeatherRuntime.Label(type),
                temperatureC = temp,
                windKmh = wind,
                visibility01 = visibility,
                predictedSnowQuality01 = snow,
                snowLabel = SnowLabel(snow),
                demandMultiplier = demand,
                impactLabel = impact
            };
        }

        private void ApplyTodayIfNeeded()
        {
            int day = CurrentDay;
            bool seasonalDomain = TerrainGeneration.RealTerrainWater.SeasonalDomain;
            if (appliedDay == day && appliedSeasonalDomain == seasonalDomain) return;
            var today = GetForecast(day);
            if (MountainWeatherRuntime.I == null) return;

            MountainWeatherRuntime.I.ApplyForecastWeather(
                today.weather,
                today.temperatureC,
                today.windKmh,
                today.visibility01,
                today.predictedSnowQuality01);
            appliedSeasonalDomain = seasonalDomain;
            appliedDay = day;
            Changed?.Invoke();
        }

        private string BuildPlanningHint()
        {
            var days = GetUpcoming(7);
            for (int i = 0; i < days.Length; i++)
            {
                if (days[i].weather == MountainWeatherType.Storm)
                    return i == 0 ? "Tempête aujourd'hui : privilégiez sécurité et capacité de repli." : "Tempête prévue dans " + i + " jour" + (i > 1 ? "s" : "") + " : anticipez une fréquentation plus faible.";
                if (days[i].weather == MountainWeatherType.HeavySnow && i <= 2)
                    return i == 0 ? "Fortes chutes aujourd'hui : neige excellente mais remontées plus lentes." : "Fortes chutes prévues dans " + i + " jour" + (i > 1 ? "s" : "") + ".";
            }
            return "Météo globalement exploitable sur les prochains jours.";
        }

        private static string SnowLabel(float q)
        {
            if (q >= .90f) return "Excellente";
            if (q >= .78f) return "Très bonne";
            if (q >= .65f) return "Bonne";
            if (q >= .50f) return "Correcte";
            return "Dégradée";
        }

        private static float Hash01(int n)
        {
            float x = Mathf.Sin(n * 12.9898f + 78.233f) * 43758.5453f;
            return x - Mathf.Floor(x);
        }
    }

    public static class SeasonWeatherBootstrap363
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;
            if (SeasonWeatherRuntime363.I != null) return;
            new GameObject("Season Weather 3.63").AddComponent<SeasonWeatherRuntime363>();
        }
    }
}
