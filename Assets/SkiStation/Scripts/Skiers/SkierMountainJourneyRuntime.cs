using System;
using UnityEngine;

namespace SkiStation
{
    public enum SkierMountainJourneyState
    {
        None,
        LiftBottom,
        Queue,
        Boarding,
        Riding,
        LiftTop,
        SlopeAccess,
        Skiing,
        SlopeBottom,
        ChoosingNextLift,
        Completed
    }

    [Serializable]
    public struct SkierMountainJourneySnapshot
    {
        public string skierId;
        public string liftId;
        public string slopeId;
        public SkierMountainJourneyState state;
        public float stateTime;
        public Vector3 position;
    }

    /// <summary>
    /// 1.01: explicit state machine for the real management skier's mountain loop.
    /// It consumes the existing lift/queue/disembark/transfer systems and never creates
    /// visitors or moves the player avatar.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkierMountainJourneyRuntime : MonoBehaviour
    {
        public SkierMountainJourneyState State { get; private set; }
        public SkierMountainJourneySnapshot Snapshot { get; private set; }
        public LiftRuntime CurrentLift { get; private set; }
        public SlopeRuntime CurrentSlope { get; private set; }
        public event Action Changed;

        private SkierRuntime skier;
        private SkierRouteAgent routeAgent;
        private float stateTime;
        private SkierState lastSkierState;
        private Transform lastParent;
        private LiftRuntime lastObservedLift;

        private void Awake()
        {
            skier = GetComponent<SkierRuntime>();
            routeAgent = GetComponent<SkierRouteAgent>();
            if (skier != null) lastSkierState = skier.State;
            SetState(SkierMountainJourneyState.LiftBottom, true);
        }

        private void Update()
        {
            if (skier == null) return;
            stateTime += Time.deltaTime;

            Transform parent = skier.transform.parent;
            var vehicle = parent != null ? parent.GetComponent<LiftVehicleRuntime>() : null;
            var lift = vehicle != null ? vehicle.GetComponentInParent<LiftRuntime>() : null;

            if (vehicle != null && lift != null)
            {
                CurrentLift = lift;
                lastObservedLift = lift;
                if (vehicle.GoingUp)
                    SetState(SkierMountainJourneyState.Riding);
                else
                    SetState(SkierMountainJourneyState.Riding);
            }
            else if (lastParent != null && parent == null && lastObservedLift != null)
            {
                CurrentLift = lastObservedLift;
                SetState(SkierMountainJourneyState.LiftTop);
            }

            if (skier.State == SkierState.WaitingForLift)
            {
                if (CurrentLift != null && CurrentLift.StationLogic != null && CurrentLift.StationLogic.QueueSize > 0)
                    SetState(SkierMountainJourneyState.Queue);
                else
                    SetState(SkierMountainJourneyState.LiftBottom);
            }
            else if (skier.State == SkierState.Skiing)
            {
                var agent = routeAgent != null ? routeAgent : GetComponent<SkierRouteAgent>();
                if (agent != null && agent.CurrentSlope != null)
                {
                    CurrentSlope = agent.CurrentSlope;
                    var transfer = GetComponent<SkierMountainTransferRuntime>();
                    bool inTransfer = transfer != null && transfer.enabled && transfer.State == SkierTransferState.Travelling;
                    SetState(inTransfer ? SkierMountainJourneyState.SlopeAccess : SkierMountainJourneyState.Skiing);
                }
                else if (CurrentLift != null)
                {
                    SetState(SkierMountainJourneyState.LiftTop);
                }
                else
                {
                    SetState(SkierMountainJourneyState.ChoosingNextLift);
                }
            }

            if (lastSkierState != skier.State || lastParent != parent)
            {
                lastSkierState = skier.State;
                lastParent = parent;
                Changed?.Invoke();
            }

            Snapshot = new SkierMountainJourneySnapshot
            {
                skierId = skier.GetInstanceID().ToString(),
                liftId = CurrentLift != null ? CurrentLift.LiftId : string.Empty,
                slopeId = CurrentSlope != null ? CurrentSlope.name : string.Empty,
                state = State,
                stateTime = stateTime,
                position = skier.transform.position
            };
        }

        public void BindLift(LiftRuntime lift)
        {
            CurrentLift = lift;
            lastObservedLift = lift;
            SetState(SkierMountainJourneyState.LiftBottom, true);
        }

        public void BindSlope(SlopeRuntime slope)
        {
            CurrentSlope = slope;
            SetState(SkierMountainJourneyState.Skiing, true);
        }

        private void SetState(SkierMountainJourneyState value, bool force = false)
        {
            if (!force && State == value) return;
            State = value;
            stateTime = 0f;
            Changed?.Invoke();
        }
    }
}
