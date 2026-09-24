using UnityEngine;

namespace SkiStation
{
    public class StationArrivalPortal : MonoBehaviour
    {
        [SerializeField] private StationGatewayRuntime gateway;
        [SerializeField] private float radius = 5f;

        public StationGatewayRuntime Gateway => gateway;

        public void Configure(StationGatewayRuntime target, float detectionRadius)
        {
            gateway = target;
            radius = Mathf.Max(0.5f, detectionRadius);
        }

        public bool IsInside(Vector3 worldPosition)
        {
            if (gateway == null || gateway.GatewayArrivalPoint == null) return false;
            return (worldPosition - gateway.GatewayArrivalPoint.position).sqrMagnitude <= radius * radius;
        }
    }
}
