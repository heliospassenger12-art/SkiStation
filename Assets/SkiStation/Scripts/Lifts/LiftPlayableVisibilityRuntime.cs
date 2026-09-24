using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>Prototype lift supports: each foundation rests on sampled terrain and
    /// the cable follows the same support points as the lift simulation.</summary>
    public sealed class LiftPlayableVisibilityRuntime : MonoBehaviour
    {
        readonly List<GameObject> made = new List<GameObject>();
        Material stationMat, towerMat, cableMat, foundationMat, rollerMat;
        [SerializeField, Range(0f, .015f)] float visualCableSagRatio = .003f;
        [SerializeField, Range(4, 32)] int cableSegmentsPerSpan = 12;
        [SerializeField, Min(0f)] float foundationOffset = 0.12f;
        [SerializeField, Min(0.5f)] float minimumTowerHeight = 4f;
        [SerializeField, Min(0f)] float minimumGroundClearance = 3f;
        [SerializeField, Range(2, 16)] int clearanceSamplesPerSpan = 8;
        [SerializeField, Min(0f)] float visualCableSpeedMetersPerSecond = 2.2f;

        public void Build(IReadOnlyList<Vector3> route)
        {
            if (route == null || route.Count < 2) return;
            foreach (var item in made) if (item != null) Destroy(item);
            made.Clear();
            stationMat = RuntimeMaterialFactory.Create(new Color(.82f, .34f, .05f, 1f));
            towerMat = RuntimeMaterialFactory.Create(new Color(.18f, .18f, .2f, 1f));
            cableMat = RuntimeMaterialFactory.Create(new Color(.10f, .10f, .12f, 1f));
            foundationMat = RuntimeMaterialFactory.Create(new Color(.52f, .53f, .54f, 1f));
            rollerMat = RuntimeMaterialFactory.Create(new Color(.08f, .09f, .11f, 1f));
            var terrain = FindObjectOfType<TerrainQuery>();
            MakeStation("Gare_Depart_VISIBLE", route[0]);
            MakeStation("Gare_Arrivee_VISIBLE", route[route.Count - 1]);
            MakeTerminalPulley("Poulie_gare_basse", route[0], route[1] - route[0]);
            MakeTerminalPulley("Poulie_gare_haute", route[route.Count - 1], route[route.Count - 1] - route[route.Count - 2]);

            // The incoming route contains the real cable support positions. Never
            // create an independent straight-line row of towers: that can leave
            // floating foundations or cables disconnected from the moving seats.
            var cable = new List<Vector3>(route.Count);
            for (int i = 0; i < route.Count; i++)
            {
                Vector3 support = route[i];
                if (i > 0 && i < route.Count - 1)
                {
                    float ground = SampleGround(terrain, support);
                    float height = Mathf.Max(minimumTowerHeight, support.y - ground);
                    support.y = ground + height;
                    MakePylon(new Vector3(support.x, ground + foundationOffset, support.z),
                        Mathf.Max(.1f, support.y - ground - foundationOffset), i,
                        (route[Mathf.Min(i + 1, route.Count - 1)] - route[i - 1]).normalized);
                }
                cable.Add(support);
            }
            for (int i = 1; i < cable.Count; i++)
            {
                // Warn rather than silently moving the simulated cable: changing
                // only its visual height would detach passengers from the line.
                for (int j = 1; j < clearanceSamplesPerSpan; j++)
                {
                    float t = j / (float)clearanceSamplesPerSpan;
                    Vector3 point = Vector3.Lerp(cable[i - 1], cable[i], t);
                    // Check the lowest part of the visible sagging span, not just its straight chord.
                    float spanHorizontal = Vector2.Distance(
                        new Vector2(cable[i - 1].x, cable[i - 1].z),
                        new Vector2(cable[i].x, cable[i].z));
                    float sag = Mathf.Min(1.5f, spanHorizontal * visualCableSagRatio);
                    point.y -= 4f * sag * t * (1f - t);
                    float ground = SampleGround(terrain, point);
                    if (point.y - ground < minimumGroundClearance)
                    {
                        Debug.LogWarning("SkiStation: portée " + i + " trop proche du relief (" +
                            (point.y - ground).ToString("F1") + " m). Adapter le tracé / les pylônes.", this);
                        break;
                    }
                }
                MakeCableSpan(cable[i - 1], cable[i], i);
            }
        }

        static float SampleGround(TerrainQuery terrain, Vector3 point)
        {
            if (terrain != null && terrain.TrySample(point, out var surface, out _, out _))
                return surface.y;
            // Fallback for imported terrain meshes, excluding buildings and pylons.
            float best = float.NegativeInfinity;
            var hits = Physics.RaycastAll(new Vector3(point.x, point.y + 2000f, point.z), Vector3.down, 4000f);
            foreach (var hit in hits)
                if ((hit.collider is MeshCollider || hit.collider.GetType().Name == "TerrainCollider"))
                    if (hit.point.y <= point.y + 100f && hit.point.y > best) best = hit.point.y;
            return float.IsNegativeInfinity(best) ? point.y - 8f : best;
        }

        void MakeStation(string name, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(transform);
            go.transform.position = position + Vector3.up * 5f;
            go.transform.localScale = new Vector3(18f, 10f, 14f);
            Set(go, stationMat);
        }
        void MakePylon(Vector3 foundation, float height, int index, Vector3 cableDirection)
        {
            // Procedural presentation only: the actual passenger route remains unchanged.
            var root = new GameObject("Pylone_Terrain_" + index.ToString("00"));
            root.transform.SetParent(transform);
            root.transform.position = foundation;
            made.Add(root);
            MakeBox(root.transform, "Semelle_beton", foundation - Vector3.up * .12f,
                new Vector3(2.7f, .38f, 2.7f), foundationMat);
            MakeBeam(foundation, foundation + Vector3.up * height, .32f, towerMat, "Fut_pylone");
            Vector3 head = foundation + Vector3.up * height;
            MakeBox(root.transform, "Traverse", head - Vector3.up * .3f,
                new Vector3(4.6f, .30f, .38f), towerMat);
            // The currently simulated line has one support point per pylon.
            // Centre the visual roller train on that exact point; do not draw
            // decorative parallel cable lanes which have no simulated route.
            Vector3 along = Vector3.ProjectOnPlane(cableDirection, Vector3.up).normalized;
            if (along.sqrMagnitude < .01f) along = Vector3.forward;
            Vector3 across = Vector3.Cross(Vector3.up, along).normalized;
            MakeBeam(foundation + Vector3.up * Mathf.Max(.5f, height - 2.8f),
                head - Vector3.up * .22f, .10f, towerMat, "Jambe_de_force");
            MakeBox(root.transform, "Balancier_galets", head - Vector3.up * .24f,
                new Vector3(.75f, .15f, .32f), towerMat);
            // Roller tread touches the support point; the wheel axis is across
            // the travel direction, so its visible rotation matches cable travel.
            for (int r = -1; r <= 1; r++)
            {
                Vector3 centre = head + along * (r * .38f) - Vector3.up * .18f;
                MakeRotatingWheel(root.transform, "Galet_roulement_" + r,
                    centre, across, .18f, .12f, visualCableSpeedMetersPerSecond);
            }
        }

        void MakeTerminalPulley(string name, Vector3 support, Vector3 travel)
        {
            Vector3 along = Vector3.ProjectOnPlane(travel, Vector3.up).normalized;
            if (along.sqrMagnitude < .01f) along = Vector3.forward;
            Vector3 axis = Vector3.Cross(Vector3.up, along).normalized;
            // A visible terminal wheel centred on the actual line endpoint.
            // This is a visual proxy, not a simulation of the return cable loop.
            MakeRotatingWheel(transform, name, support - Vector3.up * 1.1f,
                axis, 1.1f, .20f, visualCableSpeedMetersPerSecond);
        }

        void MakeRotatingWheel(Transform parent, string name, Vector3 centre,
            Vector3 axis, float radius, float width, float speed)
        {
            var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = name;
            wheel.transform.SetParent(parent);
            wheel.transform.position = centre;
            wheel.transform.rotation = Quaternion.FromToRotation(Vector3.up, axis);
            wheel.transform.localScale = new Vector3(radius, width * .5f, radius);
            Set(wheel, rollerMat);
            var spin = wheel.AddComponent<LiftVisualWheelSpin249>();
            spin.Configure(axis, radius, speed);
        }

        void MakeBox(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent);
            box.transform.position = position;
            box.transform.localScale = scale;
            Set(box, material);
        }

        void MakeCableSpan(Vector3 a, Vector3 b, int spanIndex)
        {
            // Approximate visual catenary; this must not be mistaken for the physical
            // passenger path. Sag is deliberately bounded to prevent large offsets.
            float horizontal = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
            float sag = Mathf.Min(1.5f, horizontal * visualCableSagRatio);
            var cableGo = new GameObject("Cable_visuel_portee_" + spanIndex);
            cableGo.transform.SetParent(transform);
            made.Add(cableGo);
            var line = cableGo.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = cableSegmentsPerSpan + 1;
            line.startWidth = .09f;
            line.endWidth = .09f;
            line.numCapVertices = 3;
            line.sharedMaterial = cableMat;
            for (int j = 0; j <= cableSegmentsPerSpan; j++)
            {
                float t = j / (float)cableSegmentsPerSpan;
                Vector3 point = Vector3.Lerp(a, b, t);
                point.y -= 4f * sag * t * (1f - t);
                line.SetPosition(j, point);
            }
        }

        void MakeBeam(Vector3 a, Vector3 b, float radius, Material material, string name)
        {
            Vector3 delta = b - a;
            if (delta.sqrMagnitude < .01f) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name; go.transform.SetParent(transform);
            go.transform.position = (a + b) * .5f;
            go.transform.up = delta.normalized;
            go.transform.localScale = new Vector3(radius, delta.magnitude * .5f, radius);
            Set(go, material);
        }
        void Set(GameObject go, Material material)
        {
            var collider = go.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
            made.Add(go);
        }
    }
}
