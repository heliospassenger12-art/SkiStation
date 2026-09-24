using System;
using UnityEngine;

namespace SkiStation
{
    public enum LiftPassengerDisembarkState { WaitingForArrival, Unloading, Exiting, Departed }

    [Serializable]
    public struct LiftPassengerDisembarkSnapshot
    {
        public string skierId;
        public string liftId;
        public LiftPassengerDisembarkState state;
        public LiftStationSlotType slotType;
        public Vector3 position;
        public float progress01;
    }

    /// <summary>0.83: real top-station handoff. The skier only chooses the next destination after leaving the station flow.</summary>
    public sealed class LiftPassengerDisembarkRuntime : MonoBehaviour
    {
        public LiftPassengerDisembarkSnapshot Snapshot { get; private set; }
        public bool IsDisembarking => enabled && skier != null && !handedOff;
        public event Action Changed;
        private SkierRuntime skier;
        private LiftRuntime lift;
        private LiftStationSlotRuntime unloadSlot;
        private LiftStationSlotRuntime exitSlot;
        private float progress;
        private bool handedOff;
        private Vector3 startPosition;
        private Vector3 exitPosition;

        public void Begin(SkierRuntime target, LiftRuntime sourceLift, LiftStationSlotRuntime unload = null, LiftStationSlotRuntime exit = null)
        {
            // Ignore duplicate arrival callbacks while this passenger is already
            // walking out: resetting progress would snap them back to the unload point.
            if (IsDisembarking && target == skier && sourceLift == lift) return;
            enabled = true;
            skier = target;
            if (skier != null) skier.StopMoving();
            lift = sourceLift;
            // A real vehicle has just unloaded this passenger. Record that fact
            // before post-arrival routing can request the first slope.
            if (skier != null && lift != null)
            {
                var routeAgent = skier.GetComponent<SkierRouteAgent>();
                if (routeAgent != null) routeAgent.ConfirmCompletedLiftRide(lift);
            }
            unloadSlot = unload;
            exitSlot = exit;
            progress = 0f;
            handedOff = false;
            startPosition = skier != null ? skier.transform.position : transform.position;
            exitPosition = exitSlot != null ? exitSlot.transform.position : startPosition + (lift != null ? lift.transform.forward * 2f : Vector3.forward * 2f);
            Snapshot = new LiftPassengerDisembarkSnapshot {
                skierId = skier != null ? skier.GetInstanceID().ToString() : string.Empty,
                liftId = lift != null ? lift.LiftId : string.Empty,
                state = LiftPassengerDisembarkState.Unloading,
                slotType = LiftStationSlotType.Unloading,
                position = skier != null ? skier.transform.position : transform.position,
                progress01 = 0f
            };
            Changed?.Invoke();
        }

        private void Awake() { if (skier == null) skier = GetComponent<SkierRuntime>(); }

        private void Update()
        {
            if (skier == null || handedOff) return;
            progress = Mathf.Clamp01(progress + Time.deltaTime / 1.0f);
            float motionT = Mathf.SmoothStep(0f, 1f, progress);
            skier.transform.position = Vector3.Lerp(startPosition, exitPosition, motionT);
            var state = progress < .65f ? LiftPassengerDisembarkState.Unloading : LiftPassengerDisembarkState.Exiting;
            Snapshot = new LiftPassengerDisembarkSnapshot {
                skierId = skier.GetInstanceID().ToString(), liftId = lift != null ? lift.LiftId : string.Empty,
                state = state, slotType = state == LiftPassengerDisembarkState.Unloading ? LiftStationSlotType.Unloading : LiftStationSlotType.Exit,
                position = skier.transform.position, progress01 = progress
            };
            if (progress >= 1f)
            {
                var completedSnapshot = Snapshot; completedSnapshot.state = LiftPassengerDisembarkState.Departed; Snapshot = completedSnapshot;
                var journey = skier.GetComponent<SkierMountainJourneyRuntime>() ?? skier.gameObject.AddComponent<SkierMountainJourneyRuntime>();
                journey.BindLift(lift);
                unloadSlot?.Release(skier);
                exitSlot?.Release(skier);
                handedOff = true;
                var coordinator = skier.GetComponent<LiftPostArrivalRoutingRuntime>() ?? skier.gameObject.AddComponent<LiftPostArrivalRoutingRuntime>();
                coordinator.Begin(lift);
            }
            Changed?.Invoke();
            if (handedOff) enabled = false;
        }
    }
}
