using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>Creates deterministic queue/boarding/unloading anchors around a lift endpoint.</summary>
    public sealed class LiftStationSlotBuilder : MonoBehaviour
    {
        [Min(1)] public int queueSlots = 8;
        [Min(1)] public int boardingSlots = 2;
        [Min(1)] public int unloadingSlots = 2;
        [Min(.5f)] public float slotSpacing = 1.2f;
        public bool buildOnAwake;
        private readonly List<LiftStationSlotRuntime> slots = new List<LiftStationSlotRuntime>();
        public IReadOnlyList<LiftStationSlotRuntime> Slots => slots;

        private void Awake() { if (buildOnAwake) Build(); }

        public void Build()
        {
            Clear();
            BuildSet(LiftStationSlotType.Queue, LiftStationEndpointKind.Bottom, queueSlots, -3f);
            BuildSet(LiftStationSlotType.Boarding, LiftStationEndpointKind.Bottom, boardingSlots, 0f);
            BuildSet(LiftStationSlotType.Unloading, LiftStationEndpointKind.Top, unloadingSlots, 0f);
            BuildSet(LiftStationSlotType.Exit, LiftStationEndpointKind.Top, unloadingSlots, 2.2f);
        }

        public IEnumerable<LiftStationSlotRuntime> Get(LiftStationSlotType type, LiftStationEndpointKind endpoint)
        {
            for (int i = 0; i < slots.Count; i++)
                if (slots[i].SlotType == type && slots[i].Endpoint == endpoint) yield return slots[i];
        }

        public LiftStationSlotRuntime[] GetSlots(LiftStationSlotType type, LiftStationEndpointKind endpoint)
        {
            return new List<LiftStationSlotRuntime>(Get(type, endpoint)).ToArray();
        }

        private void BuildSet(LiftStationSlotType type, LiftStationEndpointKind endpoint, int count, float forward)
        {
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"{type}_{endpoint}_{i + 1:00}");
                go.transform.SetParent(transform, false);
                float lateral = (i - (count - 1) * .5f) * slotSpacing;
                go.transform.localPosition = new Vector3(lateral, 0f, forward);
                var slot = go.AddComponent<LiftStationSlotRuntime>();
                slot.Configure($"{name}_{endpoint}_{type}_{i + 1:00}", type, endpoint, i);
                slots.Add(slot);
            }
        }

        private void Clear()
        {
            for (int i = slots.Count - 1; i >= 0; i--)
                if (slots[i] != null) Destroy(slots[i].gameObject);
            slots.Clear();
        }
    }
}
