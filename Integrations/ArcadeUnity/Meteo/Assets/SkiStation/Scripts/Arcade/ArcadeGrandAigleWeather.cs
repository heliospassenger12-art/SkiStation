using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace SkiStation.Arcade
{
    // Local presentation only. The Gameplay and Management weather models stay independent.
    [DisallowMultipleComponent]
    public sealed class ArcadeGrandAigleWeather : MonoBehaviour
    {
        public enum WeatherPreset { Forecast, Clear, Overcast, Fog, Snow }
        [SerializeField] WeatherPreset preset = WeatherPreset.Forecast;
        [SerializeField] bool holdTime;
        public bool HoldTime { get => holdTime; set => holdTime = value; }
        public WeatherPreset SelectedPreset => preset;
        [SerializeField, Range(1f, 120f)] float transitionSeconds = 25f;

        [Serializable]
        public struct Look
        {
            [Range(0f, 1f)] public float CloudCover;
            [Range(0f, .005f)] public float FogDensity;
            [Range(0f, 1.5f)] public float Light;
            [Range(0f, 1f)] public float Veil;
            [Range(0f, 1f)] public float Snow;
            [Range(0f, 8f)] public float Wind;

            internal Look(float cloudCover, float fogDensity, float light, float veil, float snow, float wind)
            {
                CloudCover = cloudCover;
                FogDensity = fogDensity;
                Light = light;
                Veil = veil;
                Snow = snow;
                Wind = wind;
            }

            internal static Look Blend(Look a, Look b, float t) => new Look(
                Mathf.Lerp(a.CloudCover, b.CloudCover, t), Mathf.Lerp(a.FogDensity, b.FogDensity, t),
                Mathf.Lerp(a.Light, b.Light, t), Mathf.Lerp(a.Veil, b.Veil, t),
                Mathf.Lerp(a.Snow, b.Snow, t), Mathf.Lerp(a.Wind, b.Wind, t));
        }

        [SerializeField] Look clear = new Look(.28f, .00012f, 1f, 0f, 0f, .45f);
        [SerializeField] Look overcast = new Look(.77f, .0004f, .58f, .52f, 0f, 1.20f);
        [SerializeField] Look fog = new Look(.69f, .0016f, .48f, .72f, 0f, .65f);
        [SerializeField] Look snow = new Look(.93f, .00085f, .52f, .65f, 1f, 2.25f);

        Look from, into;
        WeatherPreset target;
        float elapsed;
        bool initialized, triedSnowShader;
        ParticleSystem flakes;
        Material snowMaterial;

        public Look Current { get; private set; }

        public void SetPreset(WeatherPreset value)
        {
            if (!Enum.IsDefined(typeof(WeatherPreset), value))
                throw new ArgumentOutOfRangeException(nameof(value));
            preset = value;
        }

        [ContextMenu("Météo/Ciel clair")]
        void PreviewClear() => SetPreset(WeatherPreset.Clear);
        [ContextMenu("Météo/Couvert")]
        void PreviewOvercast() => SetPreset(WeatherPreset.Overcast);
        [ContextMenu("Météo/Neige")]
        void PreviewSnow() => SetPreset(WeatherPreset.Snow);
        [ContextMenu("Météo/Brouillard")]
        void PreviewFog() => SetPreset(WeatherPreset.Fog);

        WeatherPreset Resolve(float hour)
        {
            if (preset != WeatherPreset.Forecast) return preset;
            if (hour >= 9.5f && hour < 12f) return WeatherPreset.Overcast;
            if (hour >= 12f && hour < 15.5f) return WeatherPreset.Snow;
            if (hour >= 15.5f && hour < 17f) return WeatherPreset.Fog;
            return WeatherPreset.Clear;
        }

        Look Profile(WeatherPreset mode)
        {
            switch (mode)
            {
                case WeatherPreset.Overcast: return overcast;
                case WeatherPreset.Fog: return fog;
                case WeatherPreset.Snow: return snow;
                default: return clear;
            }
        }

        internal void Seek(float hour)
        {
            target = Resolve(hour);
            Current = from = into = Profile(target);
            elapsed = Mathf.Max(1f, transitionSeconds);
            initialized = true;
        }

        internal void Advance(float hour, float deltaSeconds, Camera view)
        {
            if (!isActiveAndEnabled) return;
            if (!initialized) Seek(hour);
            var next = Resolve(hour);
            if (next != target)
            {
                target = next;
                from = Current;
                into = Profile(next);
                elapsed = 0f;
            }
            float duration = Mathf.Max(1f, transitionSeconds);
            elapsed = Mathf.Min(elapsed + deltaSeconds, duration);
            Current = Look.Blend(from, into, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            UpdateSnow(view);
        }

        void UpdateSnow(Camera view)
        {
            if (Current.Snow <= .001f || view == null)
            {
                if (flakes != null && flakes.isPaused) flakes.Play(false);
                if (flakes != null && flakes.isEmitting)
                    flakes.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                return;
            }

            if (flakes == null && !triedSnowShader) CreateSnow();
            if (flakes == null) return;

            var direction = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized;
            flakes.transform.position = view.transform.position + direction * 8f + Vector3.up * 6f;
            var emission = flakes.emission;
            emission.rateOverTime = 165f * Current.Snow;
            var wind = flakes.velocityOverLifetime;
            wind.x = Current.Wind;
            if (flakes.isPaused || !flakes.isEmitting) flakes.Play(false);
        }

        void CreateSnow()
        {
            triedSnowShader = true;
            Shader shader = Resources.Load<Shader>("ArcadeA257/GrandAigleWeatherSnow");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("[Grand Aigle] Shader de précipitations indisponible.", this);
                return;
            }
            snowMaterial = new Material(shader) { name = "Flocons de Grand Aigle", hideFlags = HideFlags.DontSave };
            var snowObject = new GameObject("Neige météo Arcade");
            snowObject.SetActive(false);
            snowObject.transform.SetParent(transform, false);
            snowObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            flakes = snowObject.AddComponent<ParticleSystem>();

            var main = flakes.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.7f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.3f, 4.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(.035f, .10f);
            main.startColor = new Color(.92f, .96f, 1f, .73f);
            main.maxParticles = 750;

            var shape = flakes.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(44f, 44f, 19f);
            var wind = flakes.velocityOverLifetime;
            wind.enabled = true;
            wind.space = ParticleSystemSimulationSpace.World;
            wind.x = Current.Wind;
            wind.y = 0f;
            wind.z = 0f;

            var renderer = flakes.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = snowMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            snowObject.SetActive(true);
        }

        internal void SetLighting(Color ambient)
        {
            if (snowMaterial != null)
                snowMaterial.SetColor("_WeatherTint", ambient * 1.4f);
        }

        void OnDisable()
        {
            if (flakes != null)
                flakes.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        void Update()
        {
            if (flakes == null) return;
            var session = ArcadeSkiSession.ActiveSession;
            if (session == null || !session.IsPlaying)
            {
                if (!flakes.isStopped || flakes.particleCount > 0)
                    flakes.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            else if (session.IsPaused && !flakes.isPaused && !flakes.isStopped)
                flakes.Pause(false);
        }

        internal void Shutdown()
        {
            if (flakes != null) Destroy(flakes.gameObject);
            flakes = null;
            if (snowMaterial != null) Destroy(snowMaterial);
            snowMaterial = null;
        }

        void OnDestroy() => Shutdown();
    }
}
