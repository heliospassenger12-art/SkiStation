using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum StationRoadTrafficState { FreeFlow, Slowing, Following, Waiting, Jammed, Crossing }

    [Serializable]
    public struct StationRoadVehicleSnapshot
    {
        public int visitorInstanceId;
        public string visitorName;
        public StationArrivalTransportType transportType;
        public StationRoadTrafficState state;
        public float speedMetersPerSecond;
        public float targetSpeedMetersPerSecond;
        public float safetyDistanceMeters;
        public float distanceToLeaderMeters;
        public string roadName;
        public bool atIntersection;
        public bool hasPriority;
        public bool stoppedAtSign;
        public bool yielding;
    }

    [Serializable]
    public struct StationRoadTrafficSnapshot
    {
        public int activeVehicles;
        public int slowingVehicles;
        public int waitingVehicles;
        public int jammedVehicles;
        public int crossingVehicles;
        public int stoppedVehicles;
        public int yieldingVehicles;
        public int saturatedRoads;
        public float averageSpeedMetersPerSecond;
        public float pressure01;
        public int dropOffQueue;
    }

    /// <summary>0.94: road priority, stops/give-way, capacity and congestion propagation for real vehicles.</summary>
    public sealed class StationRoadTrafficRuntime : MonoBehaviour
    {
        public static StationRoadTrafficRuntime I { get; private set; }
        [SerializeField] private float refreshSeconds = 0.15f;
        [SerializeField] private float carSafetyDistance = 7f;
        [SerializeField] private float busSafetyDistance = 11f;
        [SerializeField] private float intersectionRadius = 8f;
        [SerializeField] private float jamDistanceFactor = 0.55f;
        [SerializeField] private float jamSpeed = 0.8f;
        [SerializeField] private float roadCapacityPerKm = 55f;
        private readonly Dictionary<int, StationRoadVehicleSnapshot> snapshots = new Dictionary<int, StationRoadVehicleSnapshot>();
        private readonly Dictionary<AccessRoadRuntime, int> roadVehicleCounts = new Dictionary<AccessRoadRuntime, int>();
        private float nextRefresh;
        private StationRoadTrafficSnapshot summary;
        public IReadOnlyDictionary<int, StationRoadVehicleSnapshot> Vehicles => snapshots;
        public IReadOnlyDictionary<AccessRoadRuntime, int> RoadVehicleCounts => roadVehicleCounts;
        public StationRoadTrafficSnapshot Summary => summary;
        public event Action Changed;

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
            Refresh(Mathf.Max(0.01f, refreshSeconds));
        }
        public bool TryGet(int visitorId, out StationRoadVehicleSnapshot snapshot) => snapshots.TryGetValue(visitorId, out snapshot);
        public int GetRoadVehicleCount(AccessRoadRuntime road) => road != null && roadVehicleCounts.TryGetValue(road, out var c) ? c : 0;

        private void Refresh(float dt)
        {
            snapshots.Clear(); roadVehicleCounts.Clear();
            var all = new List<VehicleRef>();
            foreach (var skier in FindObjectsOfType<SkierRuntime>())
            {
                if (skier == null || skier.State == SkierState.Leaving) continue;
                var profile = skier.GetComponent<StationArrivalTransportProfile>();
                if (profile == null || !profile.IsVehicle) continue;
                var visual = StationRoadVehicleRuntime.I != null ? StationRoadVehicleRuntime.I.GetVehicle(skier) : null;
                if (visual == null) continue;
                var road = FindNearestRoad(visual.transform.position);
                if (road != null) { if (!roadVehicleCounts.ContainsKey(road)) roadVehicleCounts[road] = 0; roadVehicleCounts[road]++; }
                all.Add(new VehicleRef { skier = skier, visual = visual, profile = profile, road = road });
            }

            int slowing=0, waiting=0, jammed=0, crossing=0, stopped=0, yielding=0; float speedSum=0f;
            foreach (var v in all)
            {
                float safety = v.profile.transportType == StationArrivalTransportType.CoachBus ? busSafetyDistance : carSafetyDistance;
                var leader = FindLeader(v, all);
                float distance = leader.visual != null ? Vector3.Distance(v.visual.transform.position, leader.visual.transform.position) : float.PositiveInfinity;
                var node = FindIntersection(v.visual.transform.position);
                bool near = node != null;
                bool opposing = near && HasOpposingVehicle(v, all);
                bool priority = HasPriority(v.road, node);
                bool stopSign = near && !priority && IsStopOrYield(node);
                bool yield = near && !priority;
                float target = v.profile.transportType == StationArrivalTransportType.CoachBus ? 10f : 13f;
                StationRoadTrafficState state = StationRoadTrafficState.FreeFlow;
                if (near && opposing) { state = StationRoadTrafficState.Crossing; target *= 0.35f; crossing++; }
                if (near && stopSign) { target = 0f; state = StationRoadTrafficState.Waiting; stopped++; waiting++; }
                else if (near && yield) { target = Mathf.Min(target, AccessRoadTrafficRules.ApproachSpeed(v.road, false, true)); state = StationRoadTrafficState.Slowing; yielding++; slowing++; }
                if (leader.visual != null && distance < safety)
                {
                    float ratio = Mathf.Clamp01((distance - safety * jamDistanceFactor) / Mathf.Max(0.1f, safety * (1f-jamDistanceFactor)));
                    target *= ratio;
                    if (ratio <= 0.05f) { state = StationRoadTrafficState.Jammed; jammed++; }
                    else if (ratio < 0.75f) { state = StationRoadTrafficState.Following; slowing++; }
                }
                float capacityPressure = RoadPressure01(v.road);
                if (capacityPressure > 0.9f && state == StationRoadTrafficState.FreeFlow) { state = StationRoadTrafficState.Slowing; target *= 0.65f; slowing++; }
                float speed = EstimateSpeed(v, dt);
                if (speed < jamSpeed && state != StationRoadTrafficState.FreeFlow && state != StationRoadTrafficState.Jammed) { state = StationRoadTrafficState.Waiting; waiting++; }
                snapshots[v.skier.GetInstanceID()] = new StationRoadVehicleSnapshot {
                    visitorInstanceId=v.skier.GetInstanceID(), visitorName=v.skier.name, transportType=v.profile.transportType,
                    state=state, speedMetersPerSecond=speed, targetSpeedMetersPerSecond=target, safetyDistanceMeters=safety,
                    distanceToLeaderMeters=float.IsInfinity(distance)?-1f:distance, roadName=v.road!=null?v.road.name:string.Empty,
                    atIntersection=near, hasPriority=priority, stoppedAtSign=stopSign, yielding=yield
                };
                speedSum += speed;
            }
            int queue = StationDropOffRuntime.I != null ? StationDropOffRuntime.I.QueueCount : 0;
            int saturated=0; foreach(var kv in roadVehicleCounts) if(RoadPressure01(kv.Key)>=1f) saturated++;
            float pressure = Mathf.Clamp01(all.Count/12f + jammed*0.08f + saturated*0.1f + (queue/20f)*0.2f);
            summary = new StationRoadTrafficSnapshot { activeVehicles=all.Count, slowingVehicles=slowing, waitingVehicles=waiting, jammedVehicles=jammed,
                crossingVehicles=crossing, stoppedVehicles=stopped, yieldingVehicles=yielding, saturatedRoads=saturated,
                averageSpeedMetersPerSecond=all.Count>0?speedSum/all.Count:0f, pressure01=pressure, dropOffQueue=queue };
            Changed?.Invoke();
        }

        private float RoadPressure01(AccessRoadRuntime road)
        {
            if (road == null) return 0f;
            float capacity = Mathf.Max(1f, road.LengthMeters/1000f * roadCapacityPerKm);
            if (road.definition != null && road.definition.type == AccessRoadType.ServiceRoad) capacity *= 0.8f;
            return Mathf.Clamp01(GetRoadVehicleCount(road)/capacity);
        }
        private bool HasPriority(AccessRoadRuntime road, AccessNetworkNode node)
        {
            if (road == null || node == null) return false;
            return AccessRoadTrafficRules.GetPriority(road) == AccessIntersectionPriority.PriorityRoad || AccessRoadTrafficRules.GetPriority(road) == AccessIntersectionPriority.ServicePriority;
        }
        private bool IsStopOrYield(AccessNetworkNode node) { return node != null && (node.nodeType==AccessNodeType.RoadJunction || node.nodeType==AccessNodeType.PedestrianJunction); }
        private AccessNetworkNode FindIntersection(Vector3 p)
        {
            var n=AccessNetworkRuntime.I; if(n==null||n.Nodes==null)return null;
            AccessNetworkNode best=null; float sq=float.MaxValue;
            foreach(var node in n.Nodes){ if(node==null||node.Connections==null||node.Connections.Count<2)continue; float d=(node.transform.position-p).sqrMagnitude; if(d<intersectionRadius*intersectionRadius&&d<sq){sq=d;best=node;} }
            return best;
        }
        private AccessRoadRuntime FindNearestRoad(Vector3 p)
        {
            var n=AccessNetworkRuntime.I; if(n==null||n.Roads==null)return null; AccessRoadRuntime best=null; float bestSq=float.MaxValue;
            foreach(var road in n.Roads){if(road==null||road.ControlPoints==null)continue; for(int i=1;i<road.ControlPoints.Count;i++){var a=road.ControlPoints[i-1];var b=road.ControlPoints[i];if(a==null||b==null)continue;float d=DistancePointSegmentSq(p,a.position,b.position);if(d<bestSq){bestSq=d;best=road;}}} return best;
        }
        private VehicleRef FindLeader(VehicleRef subject,List<VehicleRef> all)
        { Vector3 f=subject.visual.transform.forward;f.y=0f;if(f.sqrMagnitude<0.01f)return default;f.Normalize();VehicleRef best=default;float bd=float.MaxValue;foreach(var o in all){if(o.skier==subject.skier)continue;Vector3 d=o.visual.transform.position-subject.visual.transform.position;d.y=0f;float dist=d.magnitude;if(dist<0.1f||dist>busSafetyDistance*3f)continue;if(Vector3.Dot(f,d.normalized)<0.65f||dist>=bd)continue;best=o;bd=dist;}return best; }
        private bool HasOpposingVehicle(VehicleRef s,List<VehicleRef> all){Vector3 f=s.visual.transform.forward;f.y=0f;if(f.sqrMagnitude<.01f)return false;f.Normalize();foreach(var o in all){if(o.skier==s.skier)continue;Vector3 d=o.visual.transform.position-s.visual.transform.position;d.y=0f;if(d.sqrMagnitude>intersectionRadius*intersectionRadius||d.sqrMagnitude<.01f)continue;Vector3 of=o.visual.transform.forward;of.y=0f;if(of.sqrMagnitude>.01f&&Vector3.Dot(f,of.normalized)<-.35f)return true;}return false;}
        private float EstimateSpeed(VehicleRef v,float dt){var state=v.visual.GetComponent<StationRoadVehicleMotionState>();if(state==null)state=v.visual.AddComponent<StationRoadVehicleMotionState>();float speed=dt>0f?Vector3.Distance(v.visual.transform.position,state.lastPosition)/dt:0f;state.lastPosition=v.visual.transform.position;return speed;}
        private float DistancePointSegmentSq(Vector3 p,Vector3 a,Vector3 b){Vector3 ab=b-a;float t=ab.sqrMagnitude<.0001f?0f:Mathf.Clamp01(Vector3.Dot(p-a,ab)/ab.sqrMagnitude);return (p-Vector3.Lerp(a,b,t)).sqrMagnitude;}
        private struct VehicleRef{public SkierRuntime skier;public GameObject visual;public StationArrivalTransportProfile profile;public AccessRoadRuntime road;}
    }
}
