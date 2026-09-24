using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// 0.99: plans coherent routes through the real slope graph.
    /// It only reads existing slopes/connections; it never creates skiers or traffic.
    /// </summary>
    public sealed class SkiDomainRoutePlanner : MonoBehaviour
    {
        public static SkiDomainRoutePlanner I { get; private set; }
        [Min(0f)] public float distanceWeight = 1f;
        [Min(0f)] public float trafficWeight = 45f;
        [Min(0f)] public float turnPenalty = 8f;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        public bool TryBuildSlopeRoute(SlopeRuntime start, SlopeRuntime destination, List<SlopeRuntime> result)
        {
            if (result == null) return false;
            result.Clear();
            if (start == null || destination == null || SlopeNetwork.I == null) return false;
            if (start == destination) { result.Add(start); return true; }

            var open = new List<SlopeRuntime> { start };
            var cameFrom = new Dictionary<SlopeRuntime, SlopeRuntime>();
            var cost = new Dictionary<SlopeRuntime, float> { [start] = 0f };
            var visited = new HashSet<SlopeRuntime>();

            while (open.Count > 0)
            {
                int bestIndex = 0;
                float bestScore = float.MaxValue;
                for (int i = 0; i < open.Count; i++)
                {
                    var node = open[i];
                    float score = cost[node] + Heuristic(node, destination);
                    if (score < bestScore) { bestScore = score; bestIndex = i; }
                }

                var current = open[bestIndex];
                open.RemoveAt(bestIndex);
                if (!visited.Add(current)) continue;
                if (current == destination) return Reconstruct(start, destination, cameFrom, result);

                var connection = current.GetComponent<SlopeConnectionRuntime>();
                if (connection == null || !connection.Bottom.IsValid) continue;
                var next = connection.Bottom.slope;
                if (next == null || !next.Open || visited.Contains(next)) continue;

                float nextCost = cost[current] + EdgeCost(current, next);
                if (!cost.TryGetValue(next, out float oldCost) || nextCost < oldCost)
                {
                    cost[next] = nextCost;
                    cameFrom[next] = current;
                    if (!open.Contains(next)) open.Add(next);
                }
            }
            return false;
        }

        public IReadOnlyList<SlopeRuntime> GetAvailableNext(SlopeRuntime slope)
        {
            var result = new List<SlopeRuntime>();
            if (slope == null) return result;
            var c = slope.GetComponent<SlopeConnectionRuntime>();
            if (c != null && c.Bottom.IsValid && c.Bottom.slope != null && c.Bottom.slope.Open)
                result.Add(c.Bottom.slope);
            return result;
        }

        private float EdgeCost(SlopeRuntime from, SlopeRuntime to)
        {
            float traffic = from.TrafficDensity + to.TrafficDensity;
            float distance = Vector3.Distance(from.BottomPoint, to.TopPoint);
            return from.Length * distanceWeight + distance * 0.25f + traffic * trafficWeight + turnPenalty * TurnPenalty(from, to);
        }

        private float Heuristic(SlopeRuntime a, SlopeRuntime b)
        {
            return Vector3.Distance(a.BottomPoint, b.TopPoint) * 0.25f;
        }

        private static float TurnPenalty(SlopeRuntime from, SlopeRuntime to)
        {
            if (from.Points.Count < 2 || to.Points.Count < 2) return 0f;
            Vector3 a = (from.Points[from.Points.Count - 1] - from.Points[from.Points.Count - 2]).normalized;
            Vector3 b = (to.Points[1] - to.Points[0]).normalized;
            return Mathf.Clamp01((1f - Vector3.Dot(a, b)) * 0.5f);
        }

        private static bool Reconstruct(SlopeRuntime start, SlopeRuntime end, Dictionary<SlopeRuntime, SlopeRuntime> cameFrom, List<SlopeRuntime> result)
        {
            var reverse = new List<SlopeRuntime> { end };
            var current = end;
            while (current != start)
            {
                if (!cameFrom.TryGetValue(current, out current)) { result.Clear(); return false; }
                reverse.Add(current);
            }
            reverse.Reverse();
            result.AddRange(reverse);
            return true;
        }
    }
}
