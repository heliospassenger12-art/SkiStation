using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    [Serializable]
    public struct AccessRoadFlowSnapshot
    {
        public string roadId;
        public string name;
        public int totalVisitors;
        public int forwardVisitors;
        public int reverseVisitors;
        public float pressure01;
        public float directionalImbalance01;
        public float delayMultiplier;
        public bool isBottleneck;
        public AccessTrafficDirection configuredDirection;
    }

    /// <summary>0.89: measures real visitor flow direction and detects directional bottlenecks.</summary>
    public sealed class AccessFlowDirectionRuntime : MonoBehaviour
    {
        public static AccessFlowDirectionRuntime I { get; private set; }
        [SerializeField] private float sampleRadius = 4f;
        [SerializeField] private float refreshSeconds = 0.5f;
        [SerializeField] private float bottleneckThreshold01 = 0.75f;
        private readonly Dictionary<AccessRoadRuntime, AccessRoadFlowSnapshot> snapshots = new Dictionary<AccessRoadRuntime, AccessRoadFlowSnapshot>();
        private readonly Dictionary<int, Vector3> previousPositions = new Dictionary<int, Vector3>();
        private float nextRefresh;
        public IReadOnlyDictionary<AccessRoadRuntime, AccessRoadFlowSnapshot> Snapshots => snapshots;
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
            Refresh();
        }

        public void Refresh()
        {
            snapshots.Clear();
            var roads = FindObjectsOfType<AccessRoadRuntime>();
            var skiers = FindObjectsOfType<SkierRuntime>();
            var seen = new HashSet<int>();
            foreach (var skier in skiers)
            {
                if (skier == null) continue;
                int id = skier.GetInstanceID(); seen.Add(id);
            }
            var stale = new List<int>();
            foreach (var kv in previousPositions) if (!seen.Contains(kv.Key)) stale.Add(kv.Key);
            foreach (var id in stale) previousPositions.Remove(id);

            foreach (var road in roads)
            {
                if (road == null || !road.IsPedestrianAccessible) continue;
                int total = 0, forward = 0, reverse = 0;
                Vector3 axis = RoadAxis(road);
                foreach (var skier in skiers)
                {
                    if (skier == null) continue;
                    if (DistanceToRoad(road, skier.transform.position) > sampleRadius) continue;
                    total++;
                    Vector3 delta = Vector3.zero;
                    int id = skier.GetInstanceID();
                    if (previousPositions.TryGetValue(id, out var prev)) delta = skier.transform.position - prev;
                    previousPositions[id] = skier.transform.position;
                    float dot = Vector3.Dot(delta, axis);
                    if (dot >= 0f) forward++; else reverse++;
                }
                float pressure = Mathf.Clamp01(total / 8f);
                float imbalance = total <= 0 ? 0f : Mathf.Abs(forward - reverse) / (float)total;
                snapshots[road] = new AccessRoadFlowSnapshot
                {
                    roadId = road.name,
                    name = road.name,
                    totalVisitors = total,
                    forwardVisitors = forward,
                    reverseVisitors = reverse,
                    pressure01 = pressure,
                    directionalImbalance01 = imbalance,
                    delayMultiplier = 1f + pressure * 1.5f,
                    isBottleneck = pressure >= bottleneckThreshold01,
                    configuredDirection = road.definition != null && !road.definition.allowTwoWayTraffic ? road.definition.trafficDirection : AccessTrafficDirection.TwoWay
                };
            }
            Changed?.Invoke();
        }

        public bool TryGet(AccessRoadRuntime road, out AccessRoadFlowSnapshot snapshot) => snapshots.TryGetValue(road, out snapshot);

        private Vector3 RoadAxis(AccessRoadRuntime road)
        {
            if (road.ControlPoints == null || road.ControlPoints.Count < 2) return Vector3.forward;
            Vector3 a = road.ControlPoints[0] != null ? road.ControlPoints[0].forward : Vector3.forward;
            Vector3 b = road.ControlPoints[road.ControlPoints.Count - 1] != null ? road.ControlPoints[road.ControlPoints.Count - 1].forward : Vector3.forward;
            Vector3 axis = Vector3.zero;
            if (road.ControlPoints[0] != null && road.ControlPoints[road.ControlPoints.Count - 1] != null)
                axis = road.ControlPoints[road.ControlPoints.Count - 1].position - road.ControlPoints[0].position;
            return axis.sqrMagnitude > 0.001f ? axis.normalized : (b.sqrMagnitude > 0.01f ? b.normalized : a.normalized);
        }
        private float DistanceToRoad(AccessRoadRuntime road, Vector3 point)
        {
            float best = float.MaxValue; var cp = road.ControlPoints;
            for (int i = 1; i < cp.Count; i++) if (cp[i-1] != null && cp[i] != null)
                best = Mathf.Min(best, DistancePointSegment(point, cp[i-1].position, cp[i].position));
            return best;
        }
        private float DistancePointSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b-a; float t = ab.sqrMagnitude < 0.0001f ? 0f : Mathf.Clamp01(Vector3.Dot(p-a,ab)/ab.sqrMagnitude);
            return Vector3.Distance(p, Vector3.Lerp(a,b,t));
        }
    }
}
