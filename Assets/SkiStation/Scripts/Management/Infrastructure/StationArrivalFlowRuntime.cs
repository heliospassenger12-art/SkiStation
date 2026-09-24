using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum StationArrivalFlowState { ExternalWorld, Gateway, DropOffQueue, DropOff, Parking, PedestrianTransfer, StationReached, ParkingFull }

    [Serializable]
    public struct StationArrivalFlowSnapshot
    {
        public int visitorInstanceId;
        public string visitorName;
        public StationArrivalFlowState state;
        public bool parked;
        public bool parkingRequested;
        public int pathIndex;
        public int pathPoints;
        public float distanceToTarget;
    }

    /// <summary>0.92: extends the real visitor arrival chain without creating visitors.</summary>
    public sealed class StationArrivalFlowRuntime : MonoBehaviour
    {
        public static StationArrivalFlowRuntime I { get; private set; }
        [SerializeField] private float refreshSeconds = 0.35f;
        [SerializeField] private float waypointReachDistance = 1.2f;
        private readonly Dictionary<int, StationArrivalFlowState> states = new Dictionary<int, StationArrivalFlowState>();
        private readonly Dictionary<int, bool> parked = new Dictionary<int, bool>();
        private readonly Dictionary<int, List<Vector3>> paths = new Dictionary<int, List<Vector3>>();
        private readonly Dictionary<int, int> pathIndex = new Dictionary<int, int>();
        private float nextRefresh;
        public event Action Changed;

        public bool IsPostDropOff(SkierRuntime skier)
        {
            if (skier == null) return false;
            StationArrivalFlowState state;
            if (!states.TryGetValue(skier.GetInstanceID(), out state)) return false;
            return state == StationArrivalFlowState.Parking || state == StationArrivalFlowState.PedestrianTransfer ||
                   state == StationArrivalFlowState.StationReached || state == StationArrivalFlowState.ParkingFull;
        }

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }
        private void OnDestroy() { if (I == this) I = null; }
        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + refreshSeconds;
            Refresh();
        }

        public IReadOnlyDictionary<int, StationArrivalFlowState> States => states;

        public bool TryGetSnapshot(SkierRuntime skier, out StationArrivalFlowSnapshot snapshot)
        {
            snapshot = default;
            if (skier == null || !states.ContainsKey(skier.GetInstanceID())) return false;
            int id = skier.GetInstanceID();
            List<Vector3> path;
            paths.TryGetValue(id, out path);
            int index = pathIndex.ContainsKey(id) ? pathIndex[id] : 0;
            snapshot = new StationArrivalFlowSnapshot
            {
                visitorInstanceId = id,
                visitorName = skier.name,
                state = states[id],
                parked = parked.ContainsKey(id) && parked[id],
                parkingRequested = states[id] == StationArrivalFlowState.Parking || states[id] == StationArrivalFlowState.ParkingFull,
                pathIndex = index,
                pathPoints = path != null ? path.Count : 0,
                distanceToTarget = path != null && index < path.Count ? Vector3.Distance(skier.transform.position, path[index]) : 0f
            };
            return true;
        }

        private void Refresh()
        {
            var gateway = StationGatewayRuntime.I;
            if (gateway == null) return;
            var skiers = FindObjectsOfType<SkierRuntime>();
            bool changed = false;
            foreach (var skier in skiers)
            {
                if (skier == null || skier.GetComponent<ArrivalIdentity250>() != null) continue;
                int id = skier.GetInstanceID();
                if (!states.ContainsKey(id))
                {
                    if (skier.State != SkierState.Arriving) continue;
                    states[id] = StationArrivalFlowState.ExternalWorld;
                    changed = true;
                }
                StationArrivalFlowState state = states[id];
                if (skier.State == SkierState.Leaving)
                {
                    if (parked.ContainsKey(id) && parked[id])
                    {
                        var leavingProfile = skier.GetComponent<StationArrivalTransportProfile>();
                        var leavingType = leavingProfile != null ? leavingProfile.parkingType : ParkingType.DayVisitor;
                        StationParkingRuntime.I?.Leave(leavingType);
                    }
                    parked.Remove(id);
                    StationDropOffRuntime.I?.ReleaseVisitor(skier);
                    states.Remove(id); paths.Remove(id); pathIndex.Remove(id);
                    changed = true;
                    continue;
                }
                if (state == StationArrivalFlowState.StationReached) continue;

                Transform dropOff = gateway.DropOffPoint;
                Transform gatewayArrival = gateway.GatewayArrivalPoint;
                Transform parkingTarget = gateway.ParkingTargetPoint;
                Transform pedestrianTarget = gateway.PedestrianTargetPoint;

                switch (state)
                {
                    case StationArrivalFlowState.ExternalWorld:
                        if (gatewayArrival == null) { states[id] = StationArrivalFlowState.Gateway; changed = true; break; }
                        if (!Reached(skier, gatewayArrival.position))
                        {
                            if (!paths.ContainsKey(id)) BeginPath(skier, gatewayArrival.position);
                            else FollowPath(skier);
                        }
                        else
                        {
                            states[id] = StationArrivalFlowState.Gateway;
                            paths.Remove(id); pathIndex.Remove(id);
                            changed = true;
                        }
                        break;
                    case StationArrivalFlowState.Gateway:
                        var transport = skier.GetComponent<StationArrivalTransportProfile>();
                        if (transport != null && transport.transportType == StationArrivalTransportType.Pedestrian)
                        {
                            states[id] = StationArrivalFlowState.PedestrianTransfer;
                            if (pedestrianTarget != null) BeginPath(skier, pedestrianTarget.position);
                            else states[id] = StationArrivalFlowState.StationReached;
                            changed = true;
                            break;
                        }
                        if (dropOff != null && StationDropOffRuntime.I != null)
                        {
                            if (StationDropOffRuntime.I.TryEnqueue(skier)) states[id] = StationArrivalFlowState.DropOffQueue;
                            else states[id] = StationArrivalFlowState.ParkingFull;
                        }
                        else if (dropOff != null)
                        {
                            skier.MoveTo(dropOff.position); states[id] = StationArrivalFlowState.DropOff;
                        }
                        else states[id] = StationArrivalFlowState.Parking;
                        changed = true;
                        break;
                    case StationArrivalFlowState.DropOffQueue:
                        if (StationDropOffRuntime.I != null && StationDropOffRuntime.I.IsServing(skier))
                        {
                            Vector3 slotPoint;
                            if (StationDropOffRuntime.I.TryGetTargetPoint(skier, out slotPoint)) skier.MoveTo(slotPoint);
                            states[id] = StationArrivalFlowState.DropOff;
                            changed = true;
                        }
                        break;
                    case StationArrivalFlowState.DropOff:
                        if (StationDropOffRuntime.I == null || StationDropOffRuntime.I.IsComplete(skier) || Reached(skier, dropOff != null ? dropOff.position : skier.transform.position))
                        { states[id] = StationArrivalFlowState.Parking; changed = true; }
                        break;
                    case StationArrivalFlowState.Parking:
                        var transportProfile = skier.GetComponent<StationArrivalTransportProfile>();
                        bool shouldPark = transportProfile == null || transportProfile.parksAfterDropOff;
                        ParkingType parkingType = transportProfile != null ? transportProfile.parkingType : ParkingType.DayVisitor;
                        if (!shouldPark)
                        {
                            parked[id] = false; states[id] = StationArrivalFlowState.PedestrianTransfer; changed = true;
                        }
                        else
                        {
                            if (parkingTarget != null && !parked.ContainsKey(id) && !Reached(skier, parkingTarget.position))
                            { skier.MoveTo(parkingTarget.position); break; }
                            if (!parked.ContainsKey(id))
                            {
                                bool success = StationParkingRuntime.I != null && StationParkingRuntime.I.TryPark(parkingType);
                                parked[id] = success;
                                if (success) { states[id] = StationArrivalFlowState.PedestrianTransfer; changed = true; }
                                else { states[id] = StationArrivalFlowState.ParkingFull; changed = true; }
                            }
                        }
                        if (states[id] == StationArrivalFlowState.PedestrianTransfer && pedestrianTarget != null && !paths.ContainsKey(id))
                            BeginPath(skier, pedestrianTarget.position);
                        else if (states[id] == StationArrivalFlowState.PedestrianTransfer && pedestrianTarget == null)
                            states[id] = StationArrivalFlowState.StationReached;
                        break;
                    case StationArrivalFlowState.ParkingFull:
                        if (pedestrianTarget != null)
                        {
                            BeginPath(skier, pedestrianTarget.position);
                            states[id] = StationArrivalFlowState.PedestrianTransfer;
                            changed = true;
                        }
                        else
                        {
                            states[id] = StationArrivalFlowState.StationReached;
                            changed = true;
                        }
                        break;
                    case StationArrivalFlowState.PedestrianTransfer:
                        if (FollowPath(skier)) { states[id] = StationArrivalFlowState.StationReached; changed = true; }
                        break;
                }
            }
            CleanupDeadVisitors(skiers);
            if (changed) Changed?.Invoke();
        }

        private void BeginPath(SkierRuntime skier, Vector3 target)
        {
            int id = skier.GetInstanceID();
            var path = new List<Vector3>();
            bool built = false;
            var router = FindObjectOfType<SkierAccessPathRuntime>();
            if (router != null) built = router.TryBuildPath(skier, skier.transform.position, target, path);
            if (!built) { path.Clear(); path.Add(skier.transform.position); path.Add(target); }
            paths[id] = path;
            pathIndex[id] = path.Count > 1 ? 1 : 0;
            if (path.Count > 1) skier.MoveTo(path[pathIndex[id]]);
        }

        private bool FollowPath(SkierRuntime skier)
        {
            int id = skier.GetInstanceID();
            List<Vector3> path;
            if (!paths.TryGetValue(id, out path) || path == null || path.Count == 0) return true;
            int index = pathIndex.ContainsKey(id) ? pathIndex[id] : 0;
            if (index >= path.Count) return true;
            if (!Reached(skier, path[index])) return false;
            index++;
            if (index >= path.Count) return true;
            pathIndex[id] = index;
            skier.MoveTo(path[index]);
            return false;
        }

        private bool Reached(SkierRuntime skier, Vector3 target)
        {
            return skier != null && (skier.transform.position - target).sqrMagnitude <= waypointReachDistance * waypointReachDistance;
        }

        private void CleanupDeadVisitors(SkierRuntime[] skiers)
        {
            var alive = new HashSet<int>();
            foreach (var skier in skiers) if (skier != null) alive.Add(skier.GetInstanceID());
            var remove = new List<int>();
            foreach (var kv in states) if (!alive.Contains(kv.Key)) remove.Add(kv.Key);
            foreach (int id in remove)
            {
                states.Remove(id); paths.Remove(id); pathIndex.Remove(id); parked.Remove(id);
            }
        }
    }
}
