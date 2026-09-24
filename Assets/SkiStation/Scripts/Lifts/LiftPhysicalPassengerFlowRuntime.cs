using System;
using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// 0.81: makes the real management skiers physically occupy queue and boarding slots.
    /// It never creates passengers and never replaces the authoritative LiftQueueRuntime.
    /// </summary>
    public sealed class LiftPhysicalPassengerFlowRuntime : MonoBehaviour
    {
        public event Action Changed;
        public LiftRuntime Lift { get; private set; }
        public LiftStationSlotRuntime[] QueueSlots { get; private set; } = Array.Empty<LiftStationSlotRuntime>();
        public LiftStationSlotRuntime[] BoardingSlots { get; private set; } = Array.Empty<LiftStationSlotRuntime>();

        private LiftQueueRuntime queue;
        private LiftStationSlotBuilder builder;
        private float refreshTimer;

        public void Bind(LiftRuntime lift)
        {
            Lift = lift;
            queue = lift != null ? lift.StationLogic != null ? lift.StationLogic.GetComponent<LiftQueueRuntime>() : null : null;
            builder = lift != null ? lift.GetComponent<LiftStationSlotBuilder>() : null;
            RefreshSlots();
        }

        private void Awake()
        {
            if (Lift == null) Lift = GetComponent<LiftRuntime>();
            if (Lift != null) Bind(Lift);
        }

        private void Update()
        {
            if (Lift == null) return;
            refreshTimer -= Time.deltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = .15f;
            RefreshSlots();
            PositionQueuedSkiers();
        }

        private void RefreshSlots()
        {
            if (builder == null && Lift != null) builder = Lift.GetComponent<LiftStationSlotBuilder>();
            if (builder != null)
            {
                QueueSlots = builder.GetSlots(LiftStationSlotType.Queue, LiftStationEndpointKind.Bottom);
                BoardingSlots = builder.GetSlots(LiftStationSlotType.Boarding, LiftStationEndpointKind.Bottom);
            }
            if (queue == null && Lift != null && Lift.StationLogic != null)
                queue = Lift.StationLogic.GetComponent<LiftQueueRuntime>();
        }

        private void PositionQueuedSkiers()
        {
            if (queue == null || QueueSlots == null) return;
            foreach (var slot in QueueSlots) if (slot != null) slot.Release();
            int count = Mathf.Min(queue.Count, QueueSlots.Length);
            for (int i = 0; i < count; i++)
            {
                var skier = queue.GetAt(i);
                var slot = QueueSlots[i];
                if (skier == null || slot == null) continue;
                slot.TryReserve(skier);
                skier.MoveTo(slot.transform.position);
            }
            Changed?.Invoke();
        }

        public bool PromoteHeadToBoarding()
        {
            if (queue == null || BoardingSlots == null || BoardingSlots.Length == 0) return false;
            var skier = queue.Peek();
            if (skier == null) return false;
            for (int i = 0; i < BoardingSlots.Length; i++)
            {
                var slot = BoardingSlots[i];
                if (slot != null && !slot.IsOccupied)
                {
                    slot.TryReserve(skier);
                    skier.MoveTo(slot.transform.position);
                    return true;
                }
            }
            return false;
        }

        public void Release(SkierRuntime skier)
        {
            if (skier == null) return;
            ReleaseFrom(QueueSlots, skier);
            ReleaseFrom(BoardingSlots, skier);
            Changed?.Invoke();
        }

        private static void ReleaseFrom(LiftStationSlotRuntime[] slots, SkierRuntime skier)
        {
            if (slots == null) return;
            for (int i = 0; i < slots.Length; i++) slots[i]?.Release(skier);
        }
    }
}
