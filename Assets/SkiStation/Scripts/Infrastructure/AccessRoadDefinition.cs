using UnityEngine;

namespace SkiStation
{
    public enum AccessRoadType { CarRoad, ServiceRoad, PedestrianPath }
    public enum RoadSurfaceType { Asphalt, Gravel, Paved, SnowPacked }

    [CreateAssetMenu(menuName = "SkiStation/Infrastructure/Access Road Definition")]
    public class AccessRoadDefinition : ScriptableObject
    {
        public AccessRoadType type = AccessRoadType.CarRoad;
        public RoadSurfaceType surface = RoadSurfaceType.Asphalt;
        [Min(1f)] public float width = 6f;
        [Min(0f)] public float sidewalkWidth = 1.5f;
        public bool hasSidewalk = true;
        public bool allowTwoWayTraffic = true;
        public AccessTrafficDirection trafficDirection = AccessTrafficDirection.TwoWay;
        public bool winterCompatible = true;
        public bool summerCompatible = true;
        [Min(0f)] public float constructionCostPerMeter = 180f;
    }
}
