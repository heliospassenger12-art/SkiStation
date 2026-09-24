using UnityEngine;

namespace SkiStation
{
    // Decorative terrain dressing only. Does not edit the mountain mesh, roads, colliders or arcade.
    public sealed class StationFrontSnowLandscape247 : MonoBehaviour
    {
        private static StationFrontSnowLandscape247 instance;
        private GameObject scenery;
        private Material snow, rock, pine, trunk;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive || instance != null) return;
            var go = new GameObject("SkiStation_247_Paysage_FrontDeNeige");
            instance = go.AddComponent<StationFrontSnowLandscape247>();
        }
        private static Material Make(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader); mat.color = color; return mat;
        }
        private void Start()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) { Destroy(gameObject); return; }
            snow = Make(new Color(.87f, .91f, .94f));
            rock = Make(new Color(.39f, .34f, .31f));
            pine = Make(new Color(.12f, .27f, .23f));
            trunk = Make(new Color(.30f, .21f, .15f));
            scenery = new GameObject("Talus_bosquets_et_rochers_DECORATIFS");
            scenery.transform.SetParent(transform, false);
            // Same fallback anchor as the visible village. Keep the central road and building plots free.
            Vector3 anchor = StationGatewayRuntime.I != null && StationGatewayRuntime.I.GatewayArrivalPoint != null
                ? StationGatewayRuntime.I.GatewayArrivalPoint.position
                : new Vector3(0f, 0f, -2600f);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int row = 0; row < 7; row++)
                {
                    float z = anchor.z + 24f + row * 43f;
                    float x = anchor.x + side * (155f + 18f * Mathf.Sin(row * 1.7f));
                    float y = SkiStationGuaranteedMountainVisual.Height(x, z);
                    PrimitiveType shape = PrimitiveType.Sphere;
                    Add("Talus_neige", shape, new Vector3(x, y + 1.5f, z),
                        new Vector3(58f + row % 3 * 9f, 3.2f, 32f), snow);
                    if (row % 2 == 0)
                    {
                        float rx = x + side * 22f, rz = z + 11f;
                        float ry = SkiStationGuaranteedMountainVisual.Height(rx, rz);
                        Add("Affleurement_rocheux", PrimitiveType.Sphere, new Vector3(rx, ry + 1.1f, rz),
                            new Vector3(12f, 4f, 8f), rock);
                    }
                    for (int j = 0; j < 3; j++)
                    {
                        float tx = x + side * (8f + j * 12f), tz = z - 11f + j * 11f;
                        float ty = SkiStationGuaranteedMountainVisual.Height(tx, tz);
                        Add("Tronc_sapin", PrimitiveType.Cylinder, new Vector3(tx, ty + 2.5f, tz),
                            new Vector3(.8f, 2.5f, .8f), trunk);
                        var crown = Add("Sapin", PrimitiveType.Cylinder, new Vector3(tx, ty + 9f, tz),
                            new Vector3(4f + j, 7f, 4f + j), pine);
                        // Cylinder is a low-poly stand-in; no collider can obstruct pedestrians or vehicles.
                        crown.transform.rotation = Quaternion.identity;
                    }
                }
            }
            Debug.Log("SkiStation 2.47: paysage decoratif genere autour du village, sans modifier le terrain.");
        }
        private GameObject Add(string label, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = label; go.transform.SetParent(scenery.transform, false);
            go.transform.position = pos; go.transform.localScale = scale;
            var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }
        private void OnDestroy()
        {
            if (instance == this) instance = null;
            if (scenery != null) Destroy(scenery);
            foreach (var mat in new[] { snow, rock, pine, trunk }) if (mat != null) Destroy(mat);
        }
    }
}
