using UnityEngine;
using UnityEngine.Rendering;

namespace SkiStation
{
    /// <summary>2.80 visual-only finishing pass: crisper management shadows, balanced alpine light and subtle snow highlights.</summary>
    public sealed class PremiumLightingSnow280 : MonoBehaviour
    {
        static PremiumLightingSnow280 instance;
        GameObject root; Material snowHighlight, snowShade; float retry;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive || instance != null) return;
            var g = new GameObject("SkiStation_280_Eclairage_Neige_Premium_VISUEL");
            instance = g.AddComponent<PremiumLightingSnow280>();
        }

        static Material Mat(Color c, float smooth)
        {
            var s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Sprites/Default");
            var m = new Material(s); m.color = c;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            return m;
        }

        static float H(float x, float z) => SkiStationGuaranteedMountainVisual.Height(x, z);

        void Update()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) { Destroy(gameObject); return; }
            if (root != null || Time.unscaledTime < retry) return;
            retry = Time.unscaledTime + 1f;
            var p = StationGatewayRuntime.I != null ? StationGatewayRuntime.I.GatewayArrivalPoint : null;
            Build(p != null ? p.position : new Vector3(0f, H(0f, -2600f), -2600f));
        }

        GameObject Patch(string n, Vector3 p, Vector3 sc, Material m, float yaw)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); g.name = n; g.transform.SetParent(root.transform, false);
            g.transform.position = p; g.transform.localScale = sc; g.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var c = g.GetComponent<Collider>(); if (c != null) Destroy(c);
            var r = g.GetComponent<Renderer>(); if (r != null) { r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true; }
            return g;
        }

        void Build(Vector3 a)
        {
            root = new GameObject("Finition_eclairage_neige_280_VISUEL"); root.transform.SetParent(transform, false);
            snowHighlight = Mat(new Color(.94f, .97f, 1f, 1f), .38f);
            snowShade = Mat(new Color(.72f, .80f, .87f, 1f), .18f);

            // Management-mode presentation quality. Does not alter camera controls or gameplay geometry.
            QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, 420f);
            QualitySettings.shadowCascades = Mathf.Max(QualitySettings.shadowCascades, 2);
            RenderSettings.ambientIntensity = .91f;

            Light sun = null;
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l != null && l.type == LightType.Directional) { sun = l; if (l.name.ToLowerInvariant().Contains("soleil") || l.name.ToLowerInvariant().Contains("sun")) break; }
            if (sun != null)
            {
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = .86f;
                sun.shadowBias = .035f;
                sun.shadowNormalBias = .28f;
            }

            // Sparse flattened patches create readable sun/shade variation without covering construction areas.
            for (int i = 0; i < 14; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float z = a.z + 180f + i * 78f;
                float x = a.x + side * (105f + (i % 4) * 42f);
                float y = H(x, z) + .10f;
                Patch(i % 3 == 0 ? "Neige_ombre_douce" : "Neige_reflet_doux", new Vector3(x, y, z),
                    new Vector3(26f + (i % 3) * 8f, .16f, 13f + (i % 4) * 5f), i % 3 == 0 ? snowShade : snowHighlight, i * 31f);
            }

            Debug.Log("SkiStation 2.80: finition eclairage/ombres/neige premium activee en gestion; camera et gameplay inchanges.");
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
            if (root != null) Destroy(root);
            if (snowHighlight != null) Destroy(snowHighlight);
            if (snowShade != null) Destroy(snowShade);
        }
    }
}
