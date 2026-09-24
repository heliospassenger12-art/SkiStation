using System;
using UnityEngine;

namespace SkiStation
{
    public enum LiftStationOperatingState { Closed, Ready, Boarding, Running, Blocked, Faulted }

    [Serializable]
    public struct LiftStationOperationsSnapshot
    {
        public string liftId;
        public string liftName;
        public LiftStationOperatingState state;
        public int queue;
        public int passengersOnLine;
        public int capacityPerVehicle;
        public float effectiveSpeedKmh;
        public float travelMinutes;
        public bool bottomReady;
        public bool topReady;
    }

    /// <summary>
    /// 0.78: unified operational view of the two ends of a real lift.
    /// It observes LiftRuntime/LiftStationLogic/LiftVehicleRuntime and does not spawn passengers.
    /// </summary>
    public sealed class LiftStationOperationsRuntime : MonoBehaviour
    {
        public static LiftStationOperationsRuntime Find(LiftRuntime lift)
            => lift == null ? null : lift.GetComponent<LiftStationOperationsRuntime>();

        public LiftRuntime Lift { get; private set; }
        public LiftStationEndpointRuntime BottomEndpoint { get; private set; }
        public LiftStationEndpointRuntime TopEndpoint { get; private set; }
        public LiftStationOperationsSnapshot Snapshot { get; private set; }
        public event Action Changed;
        [SerializeField] private float refreshSeconds = .5f;
        private float timer;

        public void Bind(LiftRuntime lift)
        {
            Lift = lift;
            if (lift == null) return;
            BottomEndpoint = EnsureEndpoint(LiftStationEndpointKind.Bottom, lift.Bottom);
            TopEndpoint = EnsureEndpoint(LiftStationEndpointKind.Top, lift.Top);
            Rebuild();
        }

        private LiftStationEndpointRuntime EnsureEndpoint(LiftStationEndpointKind kind, Vector3 position)
        {
            string name = kind == LiftStationEndpointKind.Bottom ? "StationBottom" : "StationTop";
            Transform existing = transform.Find(name);
            GameObject go = existing != null ? existing.gameObject : new GameObject(name);
            go.transform.SetParent(transform, false);
            var endpoint = go.GetComponent<LiftStationEndpointRuntime>() ?? go.AddComponent<LiftStationEndpointRuntime>();
            endpoint.Configure(Lift, kind, position);
            return endpoint;
        }

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < Mathf.Max(.1f, refreshSeconds)) return;
            timer = 0f;
            Rebuild();
        }

        public void Rebuild()
        {
            if (Lift == null) return;
            var logic = Lift.StationLogic;
            int passengers = 0;
            var vehicles = Lift.GetComponentsInChildren<LiftVehicleRuntime>();
            for (int i = 0; i < vehicles.Length; i++)
                if (vehicles[i] != null) passengers += vehicles[i].OccupantCount;

            bool blocked = LiftMaintenanceIntegrationRuntime.Instance != null &&
                           LiftMaintenanceIntegrationRuntime.Instance.IsUnderMaintenance(Lift.LiftId);
            var state = blocked ? LiftStationOperatingState.Blocked :
                        Lift.Faulted ? LiftStationOperatingState.Faulted :
                        !Lift.Open ? LiftStationOperatingState.Closed :
                        (logic != null && logic.QueueSize > 0 ? LiftStationOperatingState.Boarding : LiftStationOperatingState.Ready);

            Snapshot = new LiftStationOperationsSnapshot {
                liftId = Lift.LiftId,
                liftName = Lift.Definition != null ? Lift.Definition.displayName : Lift.name,
                state = state,
                queue = logic != null ? logic.QueueSize : 0,
                passengersOnLine = passengers,
                capacityPerVehicle = Lift.EffectiveVehicleCapacity,
                effectiveSpeedKmh = Lift.EffectiveSpeedKmh,
                travelMinutes = float.IsInfinity(Lift.TravelSeconds) ? 0f : Lift.TravelSeconds / 60f,
                bottomReady = BottomEndpoint != null,
                topReady = TopEndpoint != null
            };
            Changed?.Invoke();
        }
    }
}
