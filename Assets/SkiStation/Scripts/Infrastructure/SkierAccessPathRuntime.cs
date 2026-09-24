using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>0.87: weighted pedestrian routing with real traffic-aware costs.</summary>
    public sealed class SkierAccessPathRuntime : MonoBehaviour
    {
        [SerializeField] private float endpointTolerance = 10f;
        [SerializeField] private bool allowVehicleRoadsWithSidewalk = true;
        [SerializeField] private float congestionWeight = 2.5f;
        [SerializeField] private float qualityWeight = 1.0f;

        public bool TryBuildPath(Vector3 from, Vector3 to, List<Vector3> output) => TryBuildPath(null, from, to, output);

        public bool TryBuildPath(SkierRuntime skier, Vector3 from, Vector3 to, List<Vector3> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            output.Clear();
            var network = AccessNetworkRuntime.I;
            if (network == null || network.Nodes == null || network.Nodes.Count == 0) return false;
            var start = FindNearestAccessibleNode(network, from);
            var goal = FindNearestAccessibleNode(network, to);
            if (start == null || goal == null) return false;
            if (start == goal) { output.Add(from); output.Add(to); return true; }

            var dist = new Dictionary<AccessNetworkNode, float>();
            var previous = new Dictionary<AccessNetworkNode, AccessNetworkNode>();
            var viaRoad = new Dictionary<AccessNetworkNode, AccessRoadRuntime>();
            var open = new List<AccessNetworkNode>();
            foreach (var n in network.Nodes) if (n != null) { dist[n] = float.MaxValue; open.Add(n); }
            dist[start] = 0f;

            while (open.Count > 0)
            {
                AccessNetworkNode current = null; float best = float.MaxValue;
                foreach (var n in open) if (dist[n] < best) { best = dist[n]; current = n; }
                if (current == null || best == float.MaxValue) break;
                open.Remove(current);
                if (current == goal) break;
                foreach (var road in current.Connections)
                {
                    if (!IsPedestrianAccessible(road)) continue;
                    foreach (var next in GetConnectedNodes(network, road, current))
                    {
                        if (!AccessRoadDirectionUtility.Allows(road, current, next)) continue;
                        if (next == null || !dist.ContainsKey(next) || !open.Contains(next)) continue;
                        float cost = EdgeCost(road, skier);
                        float candidate = best + cost;
                        if (candidate < dist[next])
                        {
                            dist[next] = candidate;
                            previous[next] = current;
                            viaRoad[next] = road;
                        }
                    }
                }
            }
            if (!previous.ContainsKey(goal)) return false;

            var nodes = new List<AccessNetworkNode>();
            for (var n = goal; ; n = previous[n]) { nodes.Add(n); if (n == start) break; }
            nodes.Reverse();
            output.Add(from);
            for (int i = 1; i < nodes.Count; i++) AppendRoadPolyline(viaRoad[nodes[i]], nodes[i - 1], nodes[i], output);
            output.Add(to);
            RemoveNearDuplicates(output, 0.75f);
            return output.Count >= 2;
        }

        private float EdgeCost(AccessRoadRuntime road, SkierRuntime skier)
        {
            float distance = Mathf.Max(1f, road.LengthMeters);
            float congestion = AccessRoadTrafficRuntime.I != null ? AccessRoadTrafficRuntime.I.GetPressure01(road) : 0f;
            float qualityPenalty = 0f;
            if (road.definition != null)
            {
                switch (road.definition.surface)
                {
                    case RoadSurfaceType.Paved: qualityPenalty = 0f; break;
                    case RoadSurfaceType.Asphalt: qualityPenalty = 0.05f; break;
                    case RoadSurfaceType.Gravel: qualityPenalty = 0.18f; break;
                    case RoadSurfaceType.SnowPacked: qualityPenalty = 0.28f; break;
                }
            }
            float preference = 0f;
            var profile = skier != null ? skier.PopulationProfile : null;
            if (profile != null && road.HasSidewalk) preference -= 0.05f;
            return distance * (1f + congestion * congestionWeight + qualityPenalty * qualityWeight + preference);
        }

        private AccessNetworkNode FindNearestAccessibleNode(AccessNetworkRuntime network, Vector3 point)
        {
            AccessNetworkNode best = null; float bestSq = endpointTolerance * endpointTolerance;
            foreach (var node in network.Nodes)
            {
                if (node == null || !NodeHasAccessibleRoad(node)) continue;
                float sq = (node.transform.position - point).sqrMagnitude;
                if (sq <= bestSq) { bestSq = sq; best = node; }
            }
            return best;
        }
        private bool NodeHasAccessibleRoad(AccessNetworkNode node) { foreach (var road in node.Connections) if (IsPedestrianAccessible(road)) return true; return false; }
        private bool IsPedestrianAccessible(AccessRoadRuntime road) => road != null && (!road.IsVehicleRoad || (allowVehicleRoadsWithSidewalk && road.HasSidewalk));
        private IEnumerable<AccessNetworkNode> GetConnectedNodes(AccessNetworkRuntime network, AccessRoadRuntime road, AccessNetworkNode current)
        { foreach (var node in network.Nodes) if (node != null && node != current && NearAnyPoint(road, node.transform.position)) yield return node; }
        private bool NearAnyPoint(AccessRoadRuntime road, Vector3 point) { float sq = endpointTolerance * endpointTolerance; foreach (var c in road.ControlPoints) if (c != null && (c.position - point).sqrMagnitude <= sq) return true; return false; }
        private void AppendRoadPolyline(AccessRoadRuntime road, AccessNetworkNode from, AccessNetworkNode to, List<Vector3> output)
        { if (road == null || road.ControlPoints.Count == 0) { output.Add(from.transform.position); output.Add(to.transform.position); return; } int a=NearestControlPointIndex(road,from.transform.position), b=NearestControlPointIndex(road,to.transform.position), step=a<=b?1:-1; for(int i=a;;i+=step){var p=road.ControlPoints[i];if(p!=null)output.Add(p.position);if(i==b)break;} }
        private int NearestControlPointIndex(AccessRoadRuntime road, Vector3 point) { int best=0;float bestSq=float.MaxValue;for(int i=0;i<road.ControlPoints.Count;i++){var t=road.ControlPoints[i];if(t==null)continue;float sq=(t.position-point).sqrMagnitude;if(sq<bestSq){bestSq=sq;best=i;}}return best; }
        private void RemoveNearDuplicates(List<Vector3> points,float distance){float sq=distance*distance;for(int i=points.Count-1;i>0;i--)if((points[i]-points[i-1]).sqrMagnitude<=sq)points.RemoveAt(i);}
    }
}
