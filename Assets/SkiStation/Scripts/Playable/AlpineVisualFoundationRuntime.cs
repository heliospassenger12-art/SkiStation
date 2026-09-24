using UnityEngine;

namespace SkiStation
{
    /// <summary>2.17 visual-only alpine presentation. Does not create slopes, lifts, visitors or gameplay data.</summary>
    public sealed class AlpineVisualFoundationRuntime : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (FindFirstObjectByType<AlpineVisualFoundationRuntime>() == null)
                new GameObject("SkiStation_2.15_VisualFoundation").AddComponent<AlpineVisualFoundationRuntime>();
        }

        private void Start()
        {
            ApplyAtmosphere();
            Invoke(nameof(ApplyAtmosphere), .5f);
        }

        private void ApplyAtmosphere()
        {
            RenderSettings.ambientLight = new Color(.50f,.56f,.64f,1f);
            RenderSettings.fog = false; // 2.17: construction readability first; no gameplay fog.

            Light sun = null;
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l != null && l.type == LightType.Directional) { sun = l; break; }
            if (sun == null)
            {
                var go = new GameObject("SkiStation_AlpineSun");
                sun = go.AddComponent<Light>(); sun.type = LightType.Directional;
            }
            sun.enabled = true;
            sun.intensity = 1.35f;
            sun.color = new Color(1f,.94f,.84f,1f);
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(42f,-28f,0f);

            if (Camera.main != null)
            {
                Camera.main.backgroundColor = new Color(.49f,.70f,.88f,1f);
                Camera.main.farClipPlane = Mathf.Max(Camera.main.farClipPlane, 9000f);
            }
        }
    }
}
