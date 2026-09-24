using System;
using UnityEngine;

namespace SkiStation
{
    public enum LiftStationSlotType { Queue, Boarding, Unloading, Exit }

    [Serializable]
    public struct LiftStationSlotSnapshot
    {
        public string slotId;
        public LiftStationSlotType type;
        public LiftStationEndpointKind endpoint;
        public bool occupied;
        public Vector3 position;
        public int index;
    }

    /// <summary>0.80: physical/logical station slot. Slots are fixed anchors; visitors remain real SkierRuntime objects.</summary>
    public sealed class LiftStationSlotRuntime : MonoBehaviour
    {
        public string SlotId { get; private set; }
        public LiftStationSlotType SlotType { get; private set; }
        public LiftStationEndpointKind Endpoint { get; private set; }
        public int Index { get; private set; }
        public SkierRuntime Occupant { get; private set; }
        public bool IsOccupied => Occupant != null;
        public LiftStationSlotSnapshot Snapshot => new LiftStationSlotSnapshot {
            slotId = SlotId, type = SlotType, endpoint = Endpoint, occupied = IsOccupied,
            position = transform.position, index = Index
        };

        public void Configure(string id, LiftStationSlotType type, LiftStationEndpointKind endpoint, int index)
        {
            SlotId = id; SlotType = type; Endpoint = endpoint; Index = index;
        }

        public bool TryReserve(SkierRuntime skier)
        {
            if (skier == null || IsOccupied) return false;
            Occupant = skier;
            return true;
        }

        public void Release(SkierRuntime expected = null)
        {
            if (expected == null || Occupant == expected) Occupant = null;
        }
    }
}
