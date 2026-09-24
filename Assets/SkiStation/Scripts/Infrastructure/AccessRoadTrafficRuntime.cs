using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>0.87: observes real management skiers around constructed access roads.</summary>
    public sealed class AccessRoadTrafficRuntime : MonoBehaviour
    {
        public static AccessRoadTrafficRuntime I { get; private set; }
        [SerializeField] private float sampleRadius = 4f;
        [SerializeField] private float refreshSeconds = 0.5f;
        [SerializeField] private float saturationCount = 8f;
        private readonly Dictionary<AccessRoadRuntime, int> counts = new Dictionary<AccessRoadRuntime, int>();
        private float nextRefresh;
        public IReadOnlyDictionary<AccessRoadRuntime, int> Counts => counts;

        private void Awake() { I = this; }
        private void OnDestroy() { if (I == this) I = null; }
        private void Update()
        {
            if (Time.time < nextRefresh) return;
            nextRefresh = Time.time + refreshSeconds;
            Refresh();
        }

        public void Refresh()
        {
            counts.Clear();
            var roads = FindObjectsOfType<AccessRoadRuntime>();
            var skiers = FindObjectsOfType<SkierRuntime>();
            foreach (var road in roads)
            {
                if (road == null || !road.IsPedestrianAccessible) continue;
                int c = 0;
                foreach (var skier in skiers)
                {
                    if (skier == null) continue;
                    if (DistanceToRoad(road, skier.transform.position) <= sampleRadius) c++;
                }
                counts[road] = c;
            }
        }

        public int GetCount(AccessRoadRuntime road)
        {
            if (road == null) return 0;
            int count; return counts.TryGetValue(road, out count) ? count : 0;
        }

        public float GetPressure01(AccessRoadRuntime road)
        {
            return Mathf.Clamp01(GetCount(road) / Mathf.Max(1f, saturationCount));
        }

        public float EstimateDelayMultiplier(AccessRoadRuntime road)
        {
            // 1x when empty, up to 2.5x under severe crowding.
            return 1f + GetPressure01(road) * 1.5f;
        }

        private float DistanceToRoad(AccessRoadRuntime road, Vector3 point)
        {
            var cp = road.ControlPoints;
            float best = float.MaxValue;
            for (int i = 1; i < cp.Count; i++)
            {
                if (cp[i - 1] == null || cp[i] == null) continue;
                best = Mathf.Min(best, DistancePointSegment(point, cp[i - 1].position, cp[i].position));
            }
            return best;
        }

        private float DistancePointSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float t = ab.sqrMagnitude < 0.0001f ? 0f : Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector3.Distance(p, Vector3.Lerp(a, b, t));
        }
    }
}
