using UnityEngine;
using UnityEngine.Rendering;

namespace SkiStation
{
    /// <summary>
    /// 2.64 visual-only alpine lighting pass for management mode.
    /// Improves sun, shadows, ambient sky and distance depth without touching gameplay,
    /// construction geometry, colliders, pistes, lifts, visitors or Arcade.
    /// </summary>
    public sealed class AlpineAtmosphere264 : MonoBehaviour
    {
        static AlpineAtmosphere264 instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive || instance != null) return;
            var go = new GameObject("SkiStation_264_AtmosphereAlpine_VISUEL");
            instance = go.AddComponent<AlpineAtmosphere264>();
        }

        void Start()
        {
            Invoke(nameof(Apply), 1.05f);
            Invoke(nameof(Apply), 2.25f); // wins over older presentation bootstraps after startup only
        }

        void Apply()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.63f, .75f, .88f, 1f);
            RenderSettings.ambientEquatorColor = new Color(.48f, .56f, .64f, 1f);
            RenderSettings.ambientGroundColor = new Color(.30f, .34f, .37f, 1f);
            RenderSettings.ambientIntensity = .88f;

            // Very light distance haze: enough to separate distant ridges, but not enough
            // to hide construction previews or make the playable domain milky.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.69f, .79f, .88f, 1f);
            RenderSettings.fogStartDistance = 1750f;
            RenderSettings.fogEndDistance = 7200f;

            Light sun = FindSun();
            if (sun == null)
            {
                var s = new GameObject("SkiStation_264_SoleilAlpin");
                sun = s.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.enabled = true;
            sun.color = new Color(1f, .935f, .82f, 1f);
            sun.intensity = 1.28f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = .82f;
            sun.shadowBias = .045f;
            sun.shadowNormalBias = .35f;
            sun.transform.rotation = Quaternion.Euler(39f, -31f, 0f);

            Camera cam = Camera.main;
            if (cam == null) cam = FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                cam.backgroundColor = new Color(.50f, .70f, .87f, 1f);
                cam.farClipPlane = Mathf.Max(cam.farClipPlane, 9000f);
            }

            Debug.Log("SkiStation 2.64: atmosphere alpine activee (soleil chaud, ombres douces, ciel froid et brume lointaine legere). Gameplay inchange.");
        }

        static Light FindSun()
        {
            Light fallback = null;
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l == null || l.type != LightType.Directional) continue;
                if (fallback == null) fallback = l;
                string n = l.name.ToLowerInvariant();
                if (n.Contains("sun") || n.Contains("soleil")) return l;
            }
            return fallback;
        }

        void OnDestroy() { if (instance == this) instance = null; }
    }
}
