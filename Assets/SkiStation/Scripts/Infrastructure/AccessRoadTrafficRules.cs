using UnityEngine;

namespace SkiStation
{
    public enum AccessIntersectionPriority { PriorityRoad, GiveWay, Stop, ServicePriority }

    public static class AccessRoadTrafficRules
    {
        public static AccessIntersectionPriority GetPriority(AccessRoadRuntime road)
        {
            if (road == null || road.definition == null) return AccessIntersectionPriority.GiveWay;
            if (road.definition.type == AccessRoadType.ServiceRoad) return AccessIntersectionPriority.ServicePriority;
            return AccessIntersectionPriority.PriorityRoad;
        }

        public static float ApproachSpeed(AccessRoadRuntime road, bool stop, bool giveWay)
        {
            float speed = road != null && road.definition != null && road.definition.type == AccessRoadType.ServiceRoad ? 8f : 12f;
            if (stop) return 0f;
            if (giveWay) return speed * 0.35f;
            return speed * 0.65f;
        }
    }
}
