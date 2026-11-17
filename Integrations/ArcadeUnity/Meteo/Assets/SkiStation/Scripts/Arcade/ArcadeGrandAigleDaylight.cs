using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SkiStation.Arcade
{
    // Presentation clock for Grand Aigle in isolation. Never advances GameClock or station simulation.
    internal sealed class ArcadeGrandAigleDaylight : IDisposable
    {
        readonly struct Palette
        {
            public readonly float Hour;
            public readonly Color Zenith, Horizon, Ambient;

            public Palette(float hour, Color zenith, Color horizon, Color ambient)
            {
                Hour = hour;
                Zenith = zenith;
                Horizon = horizon;
                Ambient = ambient;
            }
        }

        static Color Rgb(uint hex) => new Color32((byte)(hex >> 16), (byte)(hex >> 8), (byte)hex, 255);

        static readonly Palette Night = new Palette(0f, Rgb(0x070E26), Rgb(0x233A61), Rgb(0x253650));
        static readonly Palette[] Palettes =
        {
            Night,
            new Palette(4.5f, Night.Zenith, Night.Horizon, Night.Ambient),
            new Palette(5.5f, Rgb(0x293F69), Rgb(0xBA8B8A), Rgb(0x4B6281)),
            new Palette(6.3f, Rgb(0x55739B), Rgb(0xF4B597), Rgb(0x617C9B)),
            new Palette(8f, Rgb(0x406FAD), Rgb(0xBACCE2), Rgb(0x879CB5)),
            new Palette(12f, Rgb(0x397BB9), Rgb(0xC0D5E7), Rgb(0x9CACBD)),
            new Palette(16f, Rgb(0x4272A7), Rgb(0xD0CBCE), Rgb(0x8997AB)),
            new Palette(18f, Rgb(0x22295D), Rgb(0xB35583), Rgb(0x5A639C)),
            new Palette(19.3f, Rgb(0x111D4B), Rgb(0x70396C), Rgb(0x404F85)),
            new Palette(20.7f, Night.Zenith, Night.Horizon, Night.Ambient),
            new Palette(24f, Night.Zenith, Night.Horizon, Night.Ambient)
        };

        static readonly int ZenithId = Shader.PropertyToID("_Zenith");
        static readonly int HorizonId = Shader.PropertyToID("_Horizon");
        static readonly int CloudTimeId = Shader.PropertyToID("_Clock");
        static readonly int SunDirectionId = Shader.PropertyToID("_SunDir");
        static readonly int SunTintId = Shader.PropertyToID("_SunTint");
        static readonly int SunStrengthId = Shader.PropertyToID("_SunStrength");
        static readonly int MoonDirectionId = Shader.PropertyToID("_MoonDir");
        static readonly int TwilightTintId = Shader.PropertyToID("_TwilightTint");
        static readonly int TwilightStrengthId = Shader.PropertyToID("_TwilightStrength");
        static readonly Color DawnScattering = Rgb(0xFFB86C);
        static readonly Color DuskScattering = Rgb(0xFF514D);
        static readonly int WeatherVeilId = Shader.PropertyToID("_WeatherVeil");
        static readonly int CloudsId = Shader.PropertyToID("_Clouds");
        static readonly int NightId = Shader.PropertyToID("_Night");
        static readonly Color MoonColor = new Color(.59f, .73f, 1f);
        static readonly Color DawnSun = new Color(1f, .68f, .40f);
        static readonly Color DuskSun = new Color(1f, .36f, .31f);

        readonly ArcadeGrandAigleWeather weather;
        readonly Scene scene;
        readonly Material sky, originalSky;
        readonly Light sun, moon, originalSun;
        readonly AmbientMode originalAmbientMode;
        readonly SphericalHarmonicsL2 originalProbe;
        readonly bool originalFog;
        readonly FogMode originalFogMode;
        readonly Color originalFogColor;
        readonly float originalFogDensity;

        bool disposed;
        double clockHour;
        float cloudSeconds;

        public static ArcadeGrandAigleDaylight TryCreate(Transform root, float startHour)
        {
            // The legacy renderer still owns lighting in Built-in. Leave other domains to their native clock.
            if (!(GraphicsSettings.currentRenderPipeline is UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)
                || root.gameObject.scene != SceneManager.GetActiveScene())
                return null;

            var source = Resources.Load<Material>("ArcadeA257/AlpineSky");
            if (source == null || source.shader == null || !source.shader.isSupported || !source.HasProperty(NightId))
                return null;
            return new ArcadeGrandAigleDaylight(root, source, startHour);
        }

        ArcadeGrandAigleDaylight(Transform root, Material source, float startHour)
        {
            scene = root.gameObject.scene;
            originalSky = RenderSettings.skybox;
            originalSun = RenderSettings.sun;
            originalAmbientMode = RenderSettings.ambientMode;
            originalProbe = RenderSettings.ambientProbe;
            originalFog = RenderSettings.fog;
            originalFogMode = RenderSettings.fogMode;
            originalFogColor = RenderSettings.fogColor;
            originalFogDensity = RenderSettings.fogDensity;

            weather = root.gameObject.AddComponent<ArcadeGrandAigleWeather>();
            sky = new Material(source) { name = "Grand Aigle · ciel de session", hideFlags = HideFlags.DontSave };
            sun = CreateLight(root, "Soleil Arcade", LightShadows.Soft);
            moon = CreateLight(root, "Lune Arcade", LightShadows.None);
            moon.color = MoonColor;
            RenderSettings.skybox = sky;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            SetHour(startHour);
        }

        static Light CreateLight(Transform root, string name, LightShadows shadows)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(root, false);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = shadows;
            return light;
        }

        static float Smooth(float from, float to, float value)
            => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, value));

        static float Altitude(float hour) => 56f * Mathf.Sin((hour - 6f) * Mathf.PI / 12f);
        static Quaternion Orbit(float hour) => Quaternion.Euler(Altitude(hour), (hour - 12f) * 15f, 0f);

        public void Tick(float deltaSeconds, float minutesPerDay)
        {
            if (disposed || scene != SceneManager.GetActiveScene() || deltaSeconds <= 0f)
                return;
            cloudSeconds += deltaSeconds * (1f + weather.Current.Wind * .45f);
            if (!weather.HoldTime)
                clockHour = (clockHour + deltaSeconds * (24d / (60d * Mathf.Max(.25f, minutesPerDay)))) % 24d;
            weather.Advance((float)clockHour, deltaSeconds, ArcadeSkiSession.ActiveSession?.playerCamera);
            ApplyHour((float)clockHour);
        }

        public void SetHour(float hour)
        {
            if (disposed || scene != SceneManager.GetActiveScene() || float.IsNaN(hour) || float.IsInfinity(hour))
                return;
            clockHour = Mathf.Repeat(hour, 24f);
            weather.Seek((float)clockHour);
            ApplyHour((float)clockHour);
        }

        void ApplyHour(float hour)
        {
            for (int i = 1; i < Palettes.Length; i++)
            {
                var next = Palettes[i];
                if (hour > next.Hour) continue;
                var previous = Palettes[i - 1];
                float blend = Smooth(previous.Hour, next.Hour, hour);
                Apply(hour, Color.Lerp(previous.Zenith, next.Zenith, blend),
                      Color.Lerp(previous.Horizon, next.Horizon, blend),
                      Color.Lerp(previous.Ambient, next.Ambient, blend));
                return;
            }
        }

        void Apply(float hour, Color zenith, Color horizon, Color ambient)
        {
            float altitude = Altitude(hour);
            float daylight = Smooth(-2f, 36f, altitude);
            float night = 1f - Smooth(-17f, -3f, altitude);
            float dawn = Smooth(4.7f, 6.2f, hour) * (1f - Smooth(6.6f, 8.6f, hour));
            float dusk = Smooth(16.9f, 18.2f, hour) * (1f - Smooth(18.75f, 20.55f, hour));
            float twilight = Mathf.Max(dawn, dusk);
            float dawnSide = 1f - Smooth(10f, 14f, hour);

            sun.transform.rotation = Orbit(hour);
            sun.color = Color.Lerp(Color.Lerp(DuskSun, DawnSun, dawnSide), new Color(1f, .96f, .90f), Smooth(0f, 31f, altitude));
            var conditions = weather.Current;
            sun.intensity = (1.18f * daylight + .36f * dawn * Smooth(-1f, 6f, altitude)) * conditions.Light;
            sun.shadowStrength = .76f * Smooth(.4f, 7f, altitude) * conditions.Light;
            moon.transform.rotation = Orbit(hour + 12f);
            moon.intensity = .14f * night * conditions.Light;

            Color haze = Color.Lerp(Rgb(0x9AAEBE), Rgb(0x263954), night);
            zenith = Color.Lerp(zenith, haze, conditions.Veil * .78f);
            horizon = Color.Lerp(horizon, haze, conditions.Veil);
            ambient = Color.Lerp(ambient, haze, conditions.Veil * .25f);

            sky.SetColor(ZenithId, zenith);
            sky.SetColor(HorizonId, horizon);
            sky.SetColor(SunTintId, sun.color);
            sky.SetVector(SunDirectionId, -sun.transform.forward);
            sky.SetVector(MoonDirectionId, -moon.transform.forward);
            sky.SetFloat(SunStrengthId, Smooth(-8f, 3f, altitude) * conditions.Light);
            sky.SetFloat(NightId, night);
            sky.SetFloat(CloudsId, conditions.CloudCover);
            sky.SetFloat(WeatherVeilId, conditions.Veil);
            sky.SetColor(TwilightTintId, Color.Lerp(DuskScattering, DawnScattering, dawnSide));
            sky.SetFloat(TwilightStrengthId, twilight * (1f - conditions.Veil));
            sky.SetFloat(CloudTimeId, cloudSeconds);
            RenderSettings.fogColor = Color.Lerp(horizon, ambient, twilight * .66f);
            RenderSettings.fogDensity = conditions.FogDensity;

            // Keep diffuse visibility on URP materials without recapturing the environment every frame.
            var probe = new SphericalHarmonicsL2();
            probe.AddAmbientLight(ambient);
            RenderSettings.ambientProbe = probe;
            weather.SetLighting(ambient);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (scene == SceneManager.GetActiveScene() && RenderSettings.skybox == sky)
            {
                RenderSettings.skybox = originalSky;
                RenderSettings.sun = originalSun;
                RenderSettings.fog = originalFog;
                RenderSettings.fogMode = originalFogMode;
                RenderSettings.fogColor = originalFogColor;
                RenderSettings.fogDensity = originalFogDensity;
                RenderSettings.ambientMode = originalAmbientMode;
                RenderSettings.ambientProbe = originalProbe;
            }
            if (weather != null)
            {
                weather.Shutdown();
                UnityEngine.Object.Destroy(weather);
            }
            UnityEngine.Object.Destroy(sky);
            if (sun != null) UnityEngine.Object.Destroy(sun.gameObject);
            if (moon != null) UnityEngine.Object.Destroy(moon.gameObject);
        }
    }
}
