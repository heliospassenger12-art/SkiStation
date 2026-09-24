using UnityEngine;

namespace SkiStation
{
    public enum StationGatewayVisualStyle { SignOnly, GatewayArch, GatewayArchWithTunnel }

    [CreateAssetMenu(menuName = "SkiStation/Infrastructure/Station Gateway Definition")]
    public class StationGatewayDefinition : ScriptableObject
    {
        public string gatewayId = "main_gateway";
        public string stationName = "Ma Station";
        public StationGatewayVisualStyle visualStyle = StationGatewayVisualStyle.GatewayArchWithTunnel;
        [Min(1)] public int arrivalCapacityPerHour = 240;
        public bool acceptsCars = true;
        public bool acceptsBuses = true;
        public bool acceptsPedestrians = true;
        public bool useExternalSpawn = true;
        [Min(1f)] public float externalSpawnDistance = 80f;
        [Min(1f)] public float gatewayDepth = 18f;
        [Min(0.5f)] public float arrivalDetectionRadius = 5f;
        [Header("Dépose-minute") ]
        [Min(1)] public int dropOffVehicleCapacity = 4;
        [Min(1)] public int dropOffQueueCapacity = 20;
        [Min(1f)] public float carDropOffDwellSeconds = 18f;
        [Min(1f)] public float busDropOffDwellSeconds = 45f;
        [Min(0.5f)] public float dropOffSlotRadius = 2.5f;
    }
}
