using System;
using UnityEngine;

namespace SkiStation
{
    public enum MountainWeatherType { Clear, Cloudy, Snow, HeavySnow, Storm }

    /// <summary>
    /// Lightweight weather simulation for the playable domain.
    /// Wind can influence comfort and lift performance, but NEVER causes damage.
    /// </summary>
    public sealed class MountainWeatherRuntime : MonoBehaviour
    {
        public static MountainWeatherRuntime I { get; private set; }
        public MountainWeatherType Current { get; private set; } = MountainWeatherType.Clear;
        public float TemperatureC { get; private set; } = -3f;
        public float WindKmh { get; private set; } = 8f;
        public float Visibility01 { get; private set; } = 1f;
        public float SnowQuality01 { get; private set; } = .82f;
        public int ForecastSeed { get; private set; }
        public event Action Changed;

        private float timer;
        [SerializeField] private float changeEveryGameHours = 3f;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
            ForecastSeed = UnityEngine.Random.Range(1000, 999999);
            RollWeather();
        }

        private void Update()
        {
            if (GameClock.I == null) return;
            timer += Time.unscaledDeltaTime;
            float interval = Mathf.Max(20f, changeEveryGameHours * 10f);
            if (timer >= interval) { timer = 0f; RollWeather(); }
        }

        public float LiftSpeedMultiplier
        {
            get
            {
                switch (Current)
                {
                    case MountainWeatherType.Snow: return .96f;
                    case MountainWeatherType.HeavySnow: return .88f;
                    case MountainWeatherType.Storm: return .78f;
                    default: return 1f;
                }
            }
        }

        public float VisitorComfortPenalty01
        {
            get
            {
                float weather = Current == MountainWeatherType.Clear ? 0f : Current == MountainWeatherType.Cloudy ? .03f : Current == MountainWeatherType.Snow ? .06f : Current == MountainWeatherType.HeavySnow ? .13f : .20f;
                return Mathf.Clamp01(weather + Mathf.Clamp01(WindKmh / 80f) * .08f + (1f - Visibility01) * .12f);
            }
        }

        public string ShortDescription => string.Format("{0} · {1:0}°C · vent {2:0} km/h · visibilité {3:0}%", Label(Current), TemperatureC, WindKmh, Visibility01 * 100f);

        public void ForceWeather(MountainWeatherType type)
        {
            Current = type;
            ApplyValues(type);
            Changed?.Invoke();
            RefreshLifts();
        }

        private void RollWeather()
        {
            float r = UnityEngine.Random.value;
            MountainWeatherType next = r < .38f ? MountainWeatherType.Clear : r < .62f ? MountainWeatherType.Cloudy : r < .84f ? MountainWeatherType.Snow : r < .96f ? MountainWeatherType.HeavySnow : MountainWeatherType.Storm;
            ForceWeather(next);
        }

        private void ApplyValues(MountainWeatherType type)
        {
            switch (type)
            {
                case MountainWeatherType.Clear: TemperatureC = UnityEngine.Random.Range(-7f, 1f); WindKmh = UnityEngine.Random.Range(2f, 16f); Visibility01 = 1f; SnowQuality01 = Mathf.Clamp01(SnowQuality01 + .02f); break;
                case MountainWeatherType.Cloudy: TemperatureC = UnityEngine.Random.Range(-8f, 2f); WindKmh = UnityEngine.Random.Range(5f, 24f); Visibility01 = .86f; SnowQuality01 = Mathf.Clamp01(SnowQuality01 + .01f); break;
                case MountainWeatherType.Snow: TemperatureC = UnityEngine.Random.Range(-9f, -1f); WindKmh = UnityEngine.Random.Range(8f, 32f); Visibility01 = .70f; SnowQuality01 = Mathf.Clamp01(SnowQuality01 + .08f); break;
                case MountainWeatherType.HeavySnow: TemperatureC = UnityEngine.Random.Range(-10f, -2f); WindKmh = UnityEngine.Random.Range(15f, 45f); Visibility01 = .48f; SnowQuality01 = Mathf.Clamp01(SnowQuality01 + .13f); break;
                default: TemperatureC = UnityEngine.Random.Range(-8f, 0f); WindKmh = UnityEngine.Random.Range(35f, 70f); Visibility01 = .32f; SnowQuality01 = Mathf.Clamp01(SnowQuality01 + .04f); break;
            }
        }

        private void RefreshLifts()
        {
            foreach (var lift in FindObjectsOfType<LiftRuntime>()) if (lift != null) lift.RefreshPerformance();
        }

        public static string Label(MountainWeatherType type)
        {
            switch (type) { case MountainWeatherType.Clear: return "Grand soleil"; case MountainWeatherType.Cloudy: return "Nuageux"; case MountainWeatherType.Snow: return "Neige"; case MountainWeatherType.HeavySnow: return "Fortes chutes de neige"; default: return "Tempête"; }
        }
    }
}
