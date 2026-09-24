using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    [Serializable]
    public sealed class PiouPiouZoneData
    {
        public string name;
        public List<Vector3> points = new();
        public GameObject root;
    }

    /// <summary>Dedicated physical child-learning zones. Adults are never accepted here.</summary>
    public sealed class PiouPiouZoneRuntime : MonoBehaviour
    {
        public static PiouPiouZoneRuntime I { get; private set; }
        public readonly List<PiouPiouZoneData> zones = new();
        public event Action Changed;
        [SerializeField] private float postSpacing = 3f;
        [SerializeField] private float fenceHeight = 1.1f;

        private void Awake() { if (I != null && I != this) { Destroy(gameObject); return; } I = this; DontDestroyOnLoad(gameObject); }

        public bool AddZone(IReadOnlyList<Vector3> rawPoints, string customName = null)
        {
            if (rawPoints == null || rawPoints.Count < 3) return false;
            var z = new PiouPiouZoneData { name = string.IsNullOrWhiteSpace(customName) ? "Jardin des Piou-Piou" : customName };
            foreach (var p in rawPoints) z.points.Add(ProjectToGround(p));
            z.root = BuildBoundary(z);
            zones.Add(z); Changed?.Invoke(); return true;
        }

        public bool IsPointInside(Vector3 point)
        {
            foreach (var z in zones) if (PointInPolygonXZ(point, z.points)) return true;
            return false;
        }

        public bool IsAdultAllowed(Vector3 point, SkierRuntime skier)
        {
            if (!IsPointInside(point)) return true;
            return skier != null && skier.PopulationProfile != null && skier.PopulationProfile.IsChild;
        }

        public int ZoneCount => zones.Count;

        private GameObject BuildBoundary(PiouPiouZoneData zone)
        {
            var root = new GameObject(zone.name + "_Limites");
            for (int i = 0; i < zone.points.Count; i++)
            {
                Vector3 a = zone.points[i], b = zone.points[(i + 1) % zone.points.Count];
                float d = Vector3.Distance(a, b);
                int count = Mathf.Max(1, Mathf.CeilToInt(d / Mathf.Max(1f, postSpacing)));
                for (int n = 0; n < count; n++)
                {
                    var p = Vector3.Lerp(a, b, n / (float)count);
                    var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    post.name = "Piquet_PiouPiou"; post.transform.SetParent(root.transform);
                    post.transform.position = p + Vector3.up * fenceHeight * .5f;
                    post.transform.localScale = new Vector3(.07f, fenceHeight * .5f, .07f);
                }
                var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.name = "Barriere_PiouPiou"; rail.transform.SetParent(root.transform);
                Vector3 delta = b - a; delta.y = 0f;
                rail.transform.position = (a + b) * .5f + Vector3.up * (fenceHeight * .62f);
                if (delta.sqrMagnitude > .01f) rail.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
                rail.transform.localScale = new Vector3(.08f, .08f, delta.magnitude * .5f);
            }
            return root;
        }

        private static Vector3 ProjectToGround(Vector3 p)
        {
            if (Physics.Raycast(p + Vector3.up * 250f, Vector3.down, out var hit, 600f, ~0, QueryTriggerInteraction.Ignore)) return hit.point + Vector3.up * .03f;
            return p;
        }

        private static bool PointInPolygonXZ(Vector3 p, List<Vector3> polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                Vector3 a = polygon[i], b = polygon[j];
                bool intersect = ((a.z > p.z) != (b.z > p.z)) && (p.x < (b.x - a.x) * (p.z - a.z) / Mathf.Max(.0001f, b.z - a.z) + a.x);
                if (intersect) inside = !inside;
            }
            return inside;
        }
    }
}
