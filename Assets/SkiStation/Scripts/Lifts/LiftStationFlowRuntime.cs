using System;
using UnityEngine;

namespace SkiStation
{
    public enum LiftStationFlowState { Approaching, Docked, Boarding, Releasing, Departing }

    [Serializable]
    public struct LiftStationFlowSnapshot
    {
        public string liftId;
        public string vehicleId;
        public LiftStationEndpointKind endpoint;
        public LiftStationFlowState state;
        public int occupantCount;
        public int capacity;
        public int freeSeats;
        public float progress01;
        public Vector3 dockingPosition;
        public Vector3 boardingPosition;
        public Vector3 unloadingPosition;
    }

    /// <summary>0.79: explicit docking/boarding/unloading state for every real lift vehicle.</summary>
    public sealed class LiftStationFlowRuntime : MonoBehaviour
    {
        public LiftStationFlowSnapshot Snapshot { get; private set; }
        public LiftStationFlowState State { get; private set; }
        public event Action Changed;
        private LiftVehicleRuntime vehicle;
        private LiftRuntime lift;
        private float progress;
        private int lastEndpoint = -1;

        public void Bind(LiftVehicleRuntime target)
        {
            vehicle = target;
            lift = target != null ? target.GetComponentInParent<LiftRuntime>() : null;
            Rebuild(true);
        }

        private void Awake() { if (vehicle == null) vehicle = GetComponent<LiftVehicleRuntime>(); }

        private void Update()
        {
            if (vehicle == null) return;
            bool bottom = vehicle.NormalizedPosition <= .0025f;
            bool top = vehicle.NormalizedPosition >= .9975f;
            int endpoint = bottom ? 0 : top ? 1 : -1;
            if (endpoint >= 0 && endpoint != lastEndpoint)
            {
                lastEndpoint = endpoint;
                progress = 0f;
                State = LiftStationFlowState.Approaching;
            }
            else if (endpoint < 0)
            {
                lastEndpoint = -1;
                State = LiftStationFlowState.Departing;
                progress = Mathf.Clamp01(progress + Time.deltaTime * .5f);
            }
            else
            {
                progress = Mathf.Clamp01(progress + Time.deltaTime / Mathf.Max(.1f, GetDockingSeconds()));
                State = progress < .25f ? LiftStationFlowState.Approaching :
                        vehicle.GoingUp ? LiftStationFlowState.Boarding : LiftStationFlowState.Releasing;
                if (progress >= 1f) State = vehicle.GoingUp ? LiftStationFlowState.Boarding : LiftStationFlowState.Releasing;
            }
            Rebuild(false);
        }

        private float GetDockingSeconds()
        {
            var def = lift != null ? lift.Definition : null;
            string n = def != null && def.displayName != null ? def.displayName.ToLowerInvariant() : string.Empty;
            if (n.Contains("funiculaire") || n.Contains("funicular")) return 2f;
            if (n.Contains("télécabine") || n.Contains("gondole") || n.Contains("cabin")) return 1.5f;
            if (n.Contains("téléski") || n.Contains("tow")) return .5f;
            if (n.Contains("tapis") || n.Contains("carpet")) return .25f;
            return 1f;
        }

        private void Rebuild(bool force)
        {
            if (vehicle == null) return;
            bool bottom = vehicle.NormalizedPosition <= .01f;
            bool top = vehicle.NormalizedPosition >= .99f;
            Vector3 pos = vehicle.transform.position;
            Vector3 fwd = vehicle.transform.forward;
            Vector3 board = pos + fwd * 1.2f;
            Vector3 unload = pos + fwd * 1.8f + Vector3.up * .05f;
            Snapshot = new LiftStationFlowSnapshot {
                liftId = lift != null ? lift.LiftId : string.Empty,
                vehicleId = vehicle.GetInstanceID().ToString(),
                endpoint = bottom ? LiftStationEndpointKind.Bottom : LiftStationEndpointKind.Top,
                state = State,
                occupantCount = vehicle.OccupantCount,
                capacity = vehicle.Capacity,
                freeSeats = Mathf.Max(0, vehicle.Capacity - vehicle.OccupantCount),
                progress01 = progress,
                dockingPosition = pos,
                boardingPosition = board,
                unloadingPosition = unload
            };
            if (force) Changed?.Invoke();
        }
    }
}
