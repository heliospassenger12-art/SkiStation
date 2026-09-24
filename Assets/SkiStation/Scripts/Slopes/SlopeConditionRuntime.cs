using System;
using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// Live piste condition: snowfall improves the snow base, traffic degrades it,
    /// grooming restores it. It is deliberately lightweight and independent from
    /// the authoritative skier movement system.
    /// </summary>
    public sealed class SlopeConditionRuntime : MonoBehaviour
    {
        public static SlopeConditionRuntime Get(SlopeRuntime slope)
            => slope != null ? slope.GetComponent<SlopeConditionRuntime>() : null;

        public float SnowQuality01 { get; private set; } = .82f;
        public float SnowDepthCm { get; private set; } = 55f;
        public float GroomingProgress01 { get; private set; } = .85f;
        [Range(0f, 1f)] public float morningSunExposure01 = .65f;
        [Range(0f, 1f)] public float dailySunExposure01 = .70f;
        public float IceRisk01 { get; private set; }
        public float SoupRisk01 { get; private set; }
        public string SoupLabel => SoupRisk01 >= .78f ? "Neige en soupe" : SoupRisk01 >= .45f ? "Neige lourde" : "Neige correcte";
        public float SurfaceQualityFactor => Mathf.Clamp01(Mathf.Lerp(1f, .68f, SoupRisk01) * Mathf.Lerp(1f, .78f, IceRisk01));
        public string IceLabel => IceRisk01 >= .78f ? "Verglacée" : IceRisk01 >= .45f ? "Risque de verglas" : "Peu de verglas";
        public bool RequiresGrooming => SnowQuality01 < .55f || GroomingProgress01 < .40f;
        public string ConditionLabel
        {
            get
            {
                if (SnowQuality01 >= .82f) return "Excellente";
                if (SnowQuality01 >= .68f) return "Bonne";
                if (SnowQuality01 >= .52f) return "Correcte";
                if (SnowQuality01 >= .35f) return "Dégradée";
                return "Mauvaise";
            }
        }

        public float VisitorQualityFactor
        {
            get
            {
                float baseFactor = Mathf.Lerp(.55f, 1.08f, SnowQuality01) * Mathf.Lerp(.82f, 1f, GroomingProgress01);
                if (SurfaceConditionOptionsRuntime.I == null || !SurfaceConditionOptionsRuntime.I.AdvancedSurfaceConditions) return baseFactor;
                return baseFactor * SurfaceQualityFactor;
            }
        }
        public event Action Changed;

        private SlopeRuntime slope;
        private float tick;

        private void Awake()
        {
            slope = GetComponent<SlopeRuntime>();
            if (slope != null) slope.SetGroomed(true);
        }

        private void Update()
        {
            if (slope == null || GameClock.I == null) return;
            tick += Time.unscaledDeltaTime;
            if (tick < 1f) return;
            float dt = tick;
            tick = 0f;

            float weatherSnow = 0f;
            float weatherQuality = 1f;
            if (MountainWeatherRuntime.I != null)
            {
                switch (MountainWeatherRuntime.I.Current)
                {
                    case MountainWeatherType.Snow: weatherSnow = .10f; weatherQuality = 1.02f; break;
                    case MountainWeatherType.HeavySnow: weatherSnow = .28f; weatherQuality = 1.04f; break;
                    case MountainWeatherType.Storm: weatherSnow = .08f; weatherQuality = .92f; break;
                    case MountainWeatherType.Clear: weatherQuality = .997f; break;
                    default: weatherQuality = .999f; break;
                }
            }

            SnowDepthCm = Mathf.Clamp(SnowDepthCm + weatherSnow * dt - .015f * dt, 0f, 220f);
            SnowQuality01 = Mathf.Clamp01(SnowQuality01 * Mathf.Pow(weatherQuality, dt) + weatherSnow * .004f * dt);

            bool advanced = SurfaceConditionOptionsRuntime.I == null || SurfaceConditionOptionsRuntime.I.AdvancedSurfaceConditions;

            // Morning shade can leave a hard/icy surface when temperatures are at or below 0°C.
            // It fades naturally once the piste receives enough sun.
            float hour = GameClock.I.CurrentHour;
            if (advanced && hour >= 6f && hour < 11f && MountainWeatherRuntime.I != null)
            {
                float cold = Mathf.Clamp01((1f - MountainWeatherRuntime.I.TemperatureC) / 7f);
                float shade = 1f - Mathf.Clamp01(morningSunExposure01);
                IceRisk01 = Mathf.Clamp01(IceRisk01 + cold * shade * .035f * dt);
                IceRisk01 = Mathf.Clamp01(IceRisk01 - Mathf.Clamp01(morningSunExposure01) * .012f * dt);
            }
            else if (advanced && hour >= 11f)
                IceRisk01 = Mathf.Clamp01(IceRisk01 - .02f * dt);
            else if (!advanced)
                IceRisk01 = 0f;

            float trafficStress = Mathf.Clamp01(slope.TrafficDensity * 750f);
            SnowQuality01 = Mathf.Clamp01(SnowQuality01 - trafficStress * .006f * dt);
            GroomingProgress01 = Mathf.Clamp01(GroomingProgress01 - trafficStress * .004f * dt);

            // Advanced surface model: low altitude + all-day sun + heavy traffic can turn
            // the piste into soft, wet "soupe" later in the day. The model is intentionally
            // gradual and is disabled in simplified mode so players can ignore this layer.
            if (advanced && MountainWeatherRuntime.I != null)
            {
                float altitudeFactor = GetLowAltitudeFactor();
                float sunFactor = Mathf.Clamp01(dailySunExposure01);
                float warmFactor = Mathf.Clamp01((MountainWeatherRuntime.I.TemperatureC + 1f) / 8f);
                float heatWindow = hour >= 10f && hour <= 17.5f ? 1f : .15f;
                float soupPressure = altitudeFactor * sunFactor * trafficStress * warmFactor * heatWindow;
                SoupRisk01 = Mathf.Clamp01(SoupRisk01 + soupPressure * .018f * dt);
                SoupRisk01 = Mathf.Clamp01(SoupRisk01 - Mathf.Clamp01(1f - sunFactor) * .003f * dt);
            }
            else
                SoupRisk01 = 0f;

            bool shouldGroom = !RequiresGrooming && GroomingProgress01 > .65f;
            slope.SetGroomed(shouldGroom);

            if (MountainWeatherRuntime.I != null && MountainWeatherRuntime.I.Visibility01 < .22f)
                slope.SetOpen(false);
            else if (SnowQuality01 > .34f && (!advanced || (IceRisk01 < .92f && SoupRisk01 < .92f)))
                slope.SetOpen(true);

            Changed?.Invoke();
        }

        public void ApplyGroomingResult(float intensity = 1f)
        {
            bool advanced = SurfaceConditionOptionsRuntime.I == null || SurfaceConditionOptionsRuntime.I.AdvancedSurfaceConditions;
            intensity = Mathf.Clamp(intensity, .5f, 2f);
            SnowQuality01 = Mathf.Clamp01(SnowQuality01 + .22f * intensity);
            IceRisk01 = Mathf.Clamp01(IceRisk01 - .55f * intensity);
            SoupRisk01 = Mathf.Clamp01(SoupRisk01 - .45f * intensity);
            GroomingProgress01 = Mathf.Clamp01(GroomingProgress01 + .65f * intensity);
            slope.SetGroomed(true);
            if (SnowQuality01 > .34f && (!advanced || (IceRisk01 < .92f && SoupRisk01 < .92f))) slope.SetOpen(true);
            Changed?.Invoke();
        }

        public void RefreshGameplayImpact()
        {
            bool advanced = SurfaceConditionOptionsRuntime.I == null || SurfaceConditionOptionsRuntime.I.AdvancedSurfaceConditions;
            if (!advanced) { IceRisk01 = 0f; SoupRisk01 = 0f; }
            Changed?.Invoke();
        }

        private float GetLowAltitudeFactor()
        {
            var slopes = FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None);
            if (slopes == null || slopes.Length < 2) return 0.5f;
            float min = float.MaxValue, max = float.MinValue, value = 0f; int count = 0;
            foreach (var other in slopes)
            {
                if (other == null || other.Points == null || other.Points.Count == 0) continue;
                float avg = 0f; foreach (var point in other.Points) avg += point.y; avg /= other.Points.Count;
                min = Mathf.Min(min, avg); max = Mathf.Max(max, avg);
                if (other == slope) value = avg;
                count++;
            }
            if (count < 2 || max - min < .01f) return 0.5f;
            return 1f - Mathf.Clamp01((value - min) / (max - min));
        }

        public bool Groom(float intensity = 1f)
        {
            if (StationRuntime.I == null) return false;
            float cost = Mathf.Max(150f, slope.Length * .45f * Mathf.Clamp(intensity, .5f, 2f));
            if (!StationRuntime.I.Spend(cost, FinanceCategory.Maintenance, slope.name, "Damage de piste")) return false;
            SnowQuality01 = Mathf.Clamp01(SnowQuality01 + .22f * Mathf.Clamp(intensity, .5f, 2f));
            IceRisk01 = Mathf.Clamp01(IceRisk01 - .55f * Mathf.Clamp(intensity, .5f, 2f));
            GroomingProgress01 = Mathf.Clamp01(GroomingProgress01 + .65f * Mathf.Clamp(intensity, .5f, 2f));
            SoupRisk01 = Mathf.Clamp01(SoupRisk01 - .45f * Mathf.Clamp(intensity, .5f, 2f));
            slope.SetGroomed(true);
            slope.SetOpen(true);
            Changed?.Invoke();
            return true;
        }
    }
}
