using UnityEngine;

namespace SkiStation
{
    /// <summary>2.79 visual-only mid/long-distance alpine depth. No gameplay colliders.</summary>
    public sealed class AlpinePanoramaDepth279 : MonoBehaviour
    {
        static AlpinePanoramaDepth279 instance;
        GameObject root; float retry;
        Material snowFar, rockFar, forestFar, forestShadow;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive || instance != null) return;
            var g = new GameObject("SkiStation_279_Profondeur_Panorama_VISUEL");
            instance = g.AddComponent<AlpinePanoramaDepth279>();
        }

        static Material Mat(Color c, float smooth)
        {
            var s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Sprites/Default");
            var m = new Material(s); m.color = c;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
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

        GameObject P(string n, PrimitiveType t, Vector3 p, Vector3 sc, Material m, Quaternion r)
        {
            var g = GameObject.CreatePrimitive(t); g.name = n; g.transform.SetParent(root.transform, false);
            g.transform.position = p; g.transform.localScale = sc; g.transform.rotation = r;
            var c = g.GetComponent<Collider>(); if (c != null) Destroy(c);
            var rr = g.GetComponent<Renderer>(); if (rr != null) rr.sharedMaterial = m;
            return g;
        }

        void Build(Vector3 a)
        {
            root = new GameObject("Panorama_alpin_profondeur_279_VISUEL"); root.transform.SetParent(transform, false);
            snowFar = Mat(new Color(.82f, .88f, .92f), .28f);
            rockFar = Mat(new Color(.34f, .35f, .35f), .08f);
            forestFar = Mat(new Color(.045f, .12f, .085f), .10f);
            forestShadow = Mat(new Color(.028f, .075f, .055f), .08f);

            // Large, sparse background rock/snow masses. They sit on the generated mountain and only enrich silhouettes.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 5; i++)
                {
                    float z = a.z + 620f + i * 145f;
                    float x = a.x + side * (250f + i * 68f);
                    float y = H(x, z);
                    float h = 26f + (i % 3) * 10f;
                    P("Masse_roche_lointaine", PrimitiveType.Cube,
                        new Vector3(x, y + h * .38f, z),
                        new Vector3(48f + i * 8f, h, 35f + (i % 2) * 12f), rockFar,
                        Quaternion.Euler(8f + i * 3f, side * (18f + i * 9f), side * (12f + i * 2f)));
                    P("Couronne_neige_lointaine", PrimitiveType.Sphere,
                        new Vector3(x - side * 3f, y + h * .78f, z - 2f),
                        new Vector3(43f + i * 6f, 2.1f, 30f + (i % 2) * 9f), snowFar, Quaternion.identity);
                }
            }

            // Forest masses are deliberately grouped instead of thousands of individual trees: readable and inexpensive at distance.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 9; i++)
                {
                    float z = a.z + 420f + i * 92f;
                    float x = a.x + side * (190f + 35f * Mathf.Sin(i * 1.41f));
                    float y = H(x, z);
                    float h = 18f + (i % 4) * 3f;
                    var mat = i % 2 == 0 ? forestFar : forestShadow;
                    P("Bosquet_lointain", PrimitiveType.Cylinder,
                        new Vector3(x, y + h * .38f, z), new Vector3(18f + i % 3 * 4f, h * .38f, 18f + i % 3 * 4f), mat, Quaternion.identity);
                    P("Bosquet_lointain_cime", PrimitiveType.Cylinder,
                        new Vector3(x, y + h * .68f, z), new Vector3(12f + i % 2 * 3f, h * .28f, 12f + i % 2 * 3f), mat, Quaternion.identity);
                }
            }

            Debug.Log("SkiStation 2.79: profondeur panoramique, masses forestieres et silhouettes roche/neige lointaines installees; gameplay inchange.");
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
            if (root != null) Destroy(root);
            foreach (var m in new[] { snowFar, rockFar, forestFar, forestShadow }) if (m != null) Destroy(m);
        }
    }
}
