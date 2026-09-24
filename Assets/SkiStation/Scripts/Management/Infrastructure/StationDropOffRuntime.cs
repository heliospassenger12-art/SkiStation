using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum StationDropOffState { None, Queued, Serving, Completed }

    [Serializable]
    public struct StationDropOffSnapshot
    {
        public int visitorInstanceId;
        public string visitorName;
        public StationArrivalTransportType transportType;
        public StationDropOffState state;
        public int queuePosition;
        public float remainingSeconds;
        public int occupiedSlots;
        public int capacity;
    }

    /// <summary>0.91: real drop-off queue/slots/dwell time. No artificial traffic is created.</summary>
    public sealed class StationDropOffRuntime : MonoBehaviour
    {
        public static StationDropOffRuntime I { get; private set; }
        private readonly List<int> queue = new List<int>();
        private readonly Dictionary<int, float> dwellUntil = new Dictionary<int, float>();
        private readonly Dictionary<int, int> slotByVisitor = new Dictionary<int, int>();
        private readonly Dictionary<int, StationDropOffState> states = new Dictionary<int, StationDropOffState>();
        private readonly Dictionary<int, StationArrivalTransportType> modes = new Dictionary<int, StationArrivalTransportType>();
        private float nextRefresh;
        [SerializeField] private float refreshSeconds = 0.25f;
        public event Action Changed;
        public int QueueCount => queue.Count;
        public int OccupiedSlots => slotByVisitor.Count;
        public int Capacity { get; private set; }

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
        }
        private void OnDestroy() { if (I == this) I = null; }
        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + refreshSeconds;
            Refresh();
        }

        public bool TryEnqueue(SkierRuntime visitor)
        {
            if (visitor == null) return false;
            int id = visitor.GetInstanceID();
            if (states.ContainsKey(id)) return true;
            var gateway = StationGatewayRuntime.I;
            var def = gateway != null ? gateway.Definition : null;
            int maxQueue = def != null ? Mathf.Max(1, def.dropOffQueueCapacity) : 20;
            if (queue.Count >= maxQueue) return false;
            var profile = visitor.GetComponent<StationArrivalTransportProfile>();
            var mode = profile != null ? profile.transportType : StationArrivalTransportType.Car;
            if (mode == StationArrivalTransportType.Pedestrian) return true;
            modes[id] = mode; states[id] = StationDropOffState.Queued; queue.Add(id); Changed?.Invoke(); return true;
        }

        public bool IsComplete(SkierRuntime visitor)
        {
            if (visitor == null) return true;
            StationDropOffState state;
            return states.TryGetValue(visitor.GetInstanceID(), out state) && state == StationDropOffState.Completed;
        }
        public bool IsQueued(SkierRuntime visitor) => visitor != null && states.ContainsKey(visitor.GetInstanceID()) && states[visitor.GetInstanceID()] == StationDropOffState.Queued;
        public bool IsServing(SkierRuntime visitor) => visitor != null && states.ContainsKey(visitor.GetInstanceID()) && states[visitor.GetInstanceID()] == StationDropOffState.Serving;

        public bool TryGetSnapshot(SkierRuntime visitor, out StationDropOffSnapshot snapshot)
        {
            snapshot = default;
            if (visitor == null || !states.ContainsKey(visitor.GetInstanceID())) return false;
            int id = visitor.GetInstanceID();
            int pos = queue.IndexOf(id);
            float remaining = dwellUntil.ContainsKey(id) ? Mathf.Max(0f, dwellUntil[id] - Time.unscaledTime) : 0f;
            snapshot = new StationDropOffSnapshot
            {
                visitorInstanceId = id, visitorName = visitor.name,
                transportType = modes.ContainsKey(id) ? modes[id] : StationArrivalTransportType.Car,
                state = states[id], queuePosition = pos >= 0 ? pos + 1 : 0,
                remainingSeconds = remaining, occupiedSlots = slotByVisitor.Count, capacity = Capacity
            };
            return true;
        }

        public bool TryGetTargetPoint(SkierRuntime visitor, out Vector3 point)
        {
            point = visitor != null ? visitor.transform.position : Vector3.zero;
            if (visitor == null) return false;
            var gateway = StationGatewayRuntime.I;
            if (gateway == null || gateway.DropOffPoint == null) return false;
            int id = visitor.GetInstanceID();
            int slot = slotByVisitor.ContainsKey(id) ? slotByVisitor[id] : -1;
            point = gateway.GetDropOffSlotPosition(slot);
            return true;
        }

        public void ReleaseVisitor(SkierRuntime visitor)
        {
            if (visitor == null) return;
            int id = visitor.GetInstanceID();
            queue.Remove(id); dwellUntil.Remove(id); slotByVisitor.Remove(id); states.Remove(id); modes.Remove(id);
            Changed?.Invoke();
        }

        private void Refresh()
        {
            var gateway = StationGatewayRuntime.I;
            if (gateway == null) return;
            var def = gateway.Definition;
            Capacity = def != null ? Mathf.Max(1, def.dropOffVehicleCapacity) : 4;
            var skiers = FindObjectsOfType<SkierRuntime>();
            var alive = new HashSet<int>();
            foreach (var s in skiers) if (s != null) alive.Add(s.GetInstanceID());

            // Start as many real visitors as the physical drop-off can hold.
            while (slotByVisitor.Count < Capacity && queue.Count > 0)
            {
                int id = queue[0]; queue.RemoveAt(0);
                if (!alive.Contains(id)) continue;
                var visitor = FindVisitor(skiers, id); if (visitor == null) continue;
                int slot = FindFreeSlot();
                if (slot < 0) { queue.Insert(0, id); break; }
                slotByVisitor[id] = slot; states[id] = StationDropOffState.Serving;
                var profile = visitor.GetComponent<StationArrivalTransportProfile>();
                var mode = modes[id];
                float dwell = mode == StationArrivalTransportType.CoachBus
                    ? (def != null ? def.busDropOffDwellSeconds : 45f)
                    : (def != null ? def.carDropOffDwellSeconds : 18f);
                if (profile != null) dwell = profile.ResolveDwell(dwell);
                dwellUntil[id] = Time.unscaledTime + Mathf.Max(1f, dwell);
            }

            foreach (var kv in new List<int>(slotByVisitor.Keys))
            {
                int id = kv;
                if (!alive.Contains(id)) { ReleaseVisitorById(id); continue; }
                if (!dwellUntil.ContainsKey(id) || Time.unscaledTime < dwellUntil[id]) continue;
                slotByVisitor.Remove(id); dwellUntil.Remove(id); states[id] = StationDropOffState.Completed;
                Changed?.Invoke();
            }

            // Remove stale completed entries once the visitor leaves the arrival flow.
            var stale = new List<int>();
            foreach (var kv in states) if (!alive.Contains(kv.Key)) stale.Add(kv.Key);
            foreach (int id in stale) ReleaseVisitorById(id);
        }

        private int FindFreeSlot()
        {
            for (int i = 0; i < Capacity; i++) if (!slotByVisitor.ContainsValue(i)) return i;
            return -1;
        }
        private SkierRuntime FindVisitor(SkierRuntime[] visitors, int id)
        {
            foreach (var v in visitors) if (v != null && v.GetInstanceID() == id) return v;
            return null;
        }
        private void ReleaseVisitorById(int id)
        {
            queue.Remove(id); slotByVisitor.Remove(id); dwellUntil.Remove(id); states.Remove(id); modes.Remove(id);
        }
    }
}
