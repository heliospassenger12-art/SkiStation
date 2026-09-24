using UnityEngine;

namespace SkiStation
{
    // 2.47 additive management-only scenery. Does not move existing gameplay gateways or visitors.
    public sealed class StationFrontRoadVisual247 : MonoBehaviour
    {
        private static StationFrontRoadVisual247 instance;
        private GameObject scenery;
        private StationRoadNetwork247 roadNetwork;
        private StationGatewayRuntime attachedGateway;
        private float nextCheck;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;
            if (instance != null) return;
            var go = new GameObject("SkiStation_247_Route_FrontDeNeige_VISUELLE");
            instance = go.AddComponent<StationFrontRoadVisual247>();
        }


        // The optional gameplay gateway is not instantiated in a fresh management scene.
        // Keep visual scenery available independently, without creating visitors or changing arcade.
        private static Vector3 VisualAnchor()
        {
            var point = StationGatewayRuntime.I != null ? StationGatewayRuntime.I.GatewayArrivalPoint : null;
            if (point != null) return point.position;
            const float x = 0f, z = -2600f;
            return new Vector3(x, SkiStationGuaranteedMountainVisual.Height(x, z), z);
        }

        private void Update()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) { if (scenery != null) Destroy(scenery); Destroy(gameObject); return; }
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 1.5f;
            var gateway = StationGatewayRuntime.I;
            if (gateway == attachedGateway && scenery != null) return;
            if (scenery != null) Destroy(scenery);
            attachedGateway = gateway;
            Build(VisualAnchor());
        }

        private static float Ground(float x, float z) => SkiStationGuaranteedMountainVisual.Height(x, z);

        private void Build(Vector3 arrival)
        {
            scenery = new GameObject("Route_d_acces_et_trottoirs_VISUELS");
            if (roadNetwork == null) roadNetwork = GetComponent<StationRoadNetwork247>();
            if (roadNetwork == null) roadNetwork = gameObject.AddComponent<StationRoadNetwork247>();
            scenery.transform.SetParent(transform, false);
            // The mountain's southern map edge is z=-3400. Keep the final road aligned with the actual gateway.
            float edgeZ = AlpineMassifGeneratorV2.MinZ;
            float endZ = Mathf.Clamp(arrival.z, edgeZ + 30f, -950f);
            float endX = Mathf.Clamp(arrival.x, -3500f, 3500f);
            const int segments = 64;
            Vector3[] centers = new Vector3[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float smooth = t * t * (3f - 2f * t);
                float x = Mathf.Lerp(endX - 160f, endX, smooth);
                float z = Mathf.Lerp(edgeZ, endZ, t);
                centers[i] = new Vector3(x, Ground(x, z), z);
            }
            roadNetwork.RegisterPermanentRoad(centers);
            Strip("Chaussee_asphalte", centers, -8f, 8f, 0.23f, new Color(.16f,.18f,.21f));
            Strip("Trottoir_gauche", centers, -11.8f, -8.2f, .36f, new Color(.65f,.66f,.65f));
            Strip("Trottoir_droit", centers, 8.2f, 11.8f, .36f, new Color(.65f,.66f,.65f));
            Strip("Bordure_gauche", centers, -8.3f, -8f, .43f, Color.white);
            Strip("Bordure_droite", centers, 8f, 8.3f, .43f, Color.white);
            Strip("Ligne_bord_gauche", centers, -7.4f, -7.27f, .28f, new Color(.94f,.92f,.8f));
            Strip("Ligne_bord_droite", centers, 7.27f, 7.4f, .28f, new Color(.94f,.92f,.8f));
            for (int i = 2; i < segments - 1; i += 4)
            {
                Vector3 a = centers[i], b = centers[Mathf.Min(i + 2, segments)];
                Vector3 direction = (b - a).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
                Vector3 p = (a + b) * .5f + Vector3.up * .35f;
                Box("Pointille_central", p, Quaternion.LookRotation(direction, Vector3.up), new Vector3(.16f,.035f,Vector3.Distance(a,b)*.7f), new Color(.95f,.89f,.65f));
            }
            // A simple pedestrian crossing at the station entrance.
            Vector3 last = centers[segments - 3];
            Vector3 tangent = (centers[segments] - centers[segments - 6]).normalized;
            Vector3 lateral = Vector3.Cross(Vector3.up, tangent).normalized;
            for (int i = -4; i <= 4; i++)
            {
                Vector3 p = last + lateral * (i * 1.65f) + Vector3.up * .36f;
                Box("Passage_pieton", p, Quaternion.LookRotation(tangent, Vector3.up), new Vector3(.85f,.035f,3f), Color.white);
            }
            Debug.Log("SkiStation 2.47: route et trottoirs visuels crees vers le portail existant. Aucun spawn modifie.");
        }

        private void Strip(string label, Vector3[] path, float left, float right, float elevation, Color color)
        {
            var go = new GameObject(label);
            go.transform.SetParent(scenery.transform, false);
            var vertices = new Vector3[path.Length * 2];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[(path.Length - 1) * 6];
            for (int i = 0; i < path.Length; i++)
            {
                Vector3 forward = (path[Mathf.Min(i + 1, path.Length - 1)] - path[Mathf.Max(0, i - 1)]).normalized;
                Vector3 lateral = Vector3.Cross(Vector3.up, forward).normalized;
                Vector3 l = path[i] + lateral * left, r = path[i] + lateral * right;
                vertices[2*i] = new Vector3(l.x, Ground(l.x,l.z) + elevation, l.z);
                vertices[2*i+1] = new Vector3(r.x, Ground(r.x,r.z) + elevation, r.z);
                uv[2*i] = new Vector2(0,i); uv[2*i+1] = new Vector2(1,i);
                if (i == path.Length - 1) continue;
                int v = 2*i, t = 6*i;
                triangles[t] = v; triangles[t+1] = v+2; triangles[t+2] = v+1;
                triangles[t+3] = v+1; triangles[t+4] = v+2; triangles[t+5] = v+3;
            }
            var mesh = new Mesh { name = label, vertices = vertices, triangles = triangles, uv = uv };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = Material(color);
        }

        private void Box(string label, Vector3 position, Quaternion rotation, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = label; go.transform.SetParent(scenery.transform, false);
            go.transform.SetPositionAndRotation(position, rotation); go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = Material(color);
            var collider = go.GetComponent<Collider>(); if (collider != null) Destroy(collider);
        }

        private static Material Material(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var material = new Material(shader); material.color = color;
            return material;
        }
        private void OnDestroy() { if (instance == this) instance = null; }
    }
}
