using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// 0.95: lightweight autonomous road movement for real arriving visitors.
    /// It reuses the existing access network; no lanes, extra traffic population or new road rules are added.
    /// </summary>
    public sealed class RoadVehicleAgentRuntime : MonoBehaviour
    {
        private readonly List<Vector3> path = new List<Vector3>();
        private int pathIndex;
        private SkierRuntime owner;
        private StationArrivalTransportType transportType;
        private float speed = 13f;
        private bool waitingForManagementFlow;
        private float reachDistance = 1.8f;

        public SkierRuntime Owner => owner;
        public bool IsDriving => !waitingForManagementFlow && owner != null;

        public void Initialize(SkierRuntime skier, StationArrivalTransportType type)
        {
            owner = skier;
            transportType = type;
            speed = type == StationArrivalTransportType.CoachBus ? 10f : 13f;
            BuildArrivalPath();
            if (path.Count < 2) waitingForManagementFlow = true;
        }

        public void Tick(float dt)
        {
            if (owner == null) return;
            if (owner.State == SkierState.Leaving ||
                (StationArrivalFlowRuntime.I != null && StationArrivalFlowRuntime.I.IsPostDropOff(owner)))
            {
                Destroy(gameObject);
                return;
            }

            if (waitingForManagementFlow)
            {
                // The management simulation remains authoritative. Once it has moved
                // the visitor beyond the external-road phase, the visual vehicle can retire.
                if (StationArrivalFlowRuntime.I != null && StationArrivalFlowRuntime.I.IsPostDropOff(owner))
                    Destroy(gameObject);
                return;
            }

            if (pathIndex >= path.Count)
            {
                waitingForManagementFlow = true;
                return;
            }

            Vector3 target = path[pathIndex];
            Vector3 delta = target - transform.position;
            delta.y = 0f;
            if (delta.magnitude <= reachDistance)
            {
                pathIndex++;
                if (pathIndex >= path.Count)
                {
                    waitingForManagementFlow = true;
                    return;
                }
                target = path[pathIndex];
                delta = target - transform.position;
                delta.y = 0f;
            }

            float targetSpeed = speed;
            StationRoadVehicleSnapshot snapshot;
            if (StationRoadTrafficRuntime.I != null && StationRoadTrafficRuntime.I.TryGet(owner.GetInstanceID(), out snapshot))
                targetSpeed = Mathf.Max(0.5f, snapshot.targetSpeedMetersPerSecond);

            float step = targetSpeed * Mathf.Max(0f, dt);
            Vector3 next = Vector3.MoveTowards(transform.position, target, step);
            Vector3 forward = next - transform.position;
            forward.y = 0f;
            transform.position = next;
            if (forward.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        private void BuildArrivalPath()
        {
            path.Clear();
            pathIndex = 0;
            if (owner == null) return;

            var gateway = StationGatewayRuntime.I;
            Vector3 destination = gateway != null && gateway.GatewayArrivalPoint != null
                ? gateway.GatewayArrivalPoint.position
                : owner.transform.position;

            var router = FindObjectOfType<SkierAccessPathRuntime>();
            bool built = router != null && router.TryBuildPath(owner, owner.transform.position, destination, path);
            if (!built)
            {
                // No network = no fake route. The visitor simulation continues normally.
                path.Clear();
                return;
            }
            if (path.Count > 1) pathIndex = 1;
        }
    }
}
