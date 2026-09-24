using UnityEngine;

namespace SkiStation
{
    public enum AccessTrafficDirection { TwoWay, ForwardOnly, ReverseOnly }

    public static class AccessRoadDirectionUtility
    {
        public static bool Allows(AccessRoadRuntime road, AccessNetworkNode from, AccessNetworkNode to, float tolerance = 10f)
        {
            if (road == null || road.definition == null || road.definition.allowTwoWayTraffic) return true;
            if (road.ControlPoints == null || road.ControlPoints.Count < 2 || from == null || to == null) return true;
            int a = NearestIndex(road, from.transform.position);
            int b = NearestIndex(road, to.transform.position);
            if (a == b) return true;
            bool forward = a < b;
            switch (road.definition.trafficDirection)
            {
                case AccessTrafficDirection.ForwardOnly: return forward;
                case AccessTrafficDirection.ReverseOnly: return !forward;
                default: return true;
            }
        }

        private static int NearestIndex(AccessRoadRuntime road, Vector3 p)
        {
            int best = 0; float bestSq = float.MaxValue;
            for (int i = 0; i < road.ControlPoints.Count; i++)
            {
                var t = road.ControlPoints[i]; if (t == null) continue;
                float sq = (t.position - p).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = i; }
            }
            return best;
        }
    }
}
