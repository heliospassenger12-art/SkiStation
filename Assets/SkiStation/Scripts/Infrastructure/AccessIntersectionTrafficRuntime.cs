using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    [Serializable]
    public struct AccessIntersectionSnapshot
    {
        public string nodeId;
        public string displayName;
        public AccessNodeType nodeType;
        public int connectedRoads;
        public int accessibleRoads;
        public int nearbyVisitors;
        public float pressure01;
        public float averageRoadPressure01;
        public bool isHotspot;
    }

    /// <summary>
    /// 0.88: converts real visitor traffic into intersection/junction pressure.
    /// It never creates traffic and never changes the management population.
    /// </summary>
    public sealed class AccessIntersectionTrafficRuntime : MonoBehaviour
    {
        public static AccessIntersectionTrafficRuntime I { get; private set; }
        [SerializeField] private float sampleRadius = 7f;
        [SerializeField] private float refreshSeconds = 0.75f;
        [SerializeField] private float hotspotThreshold01 = 0.65f;
        [SerializeField] private int hotspotMinimumRoads = 2;
        private readonly Dictionary<AccessNetworkNode, AccessIntersectionSnapshot> snapshots = new Dictionary<AccessNetworkNode, AccessIntersectionSnapshot>();
        private float nextRefresh;
        public IReadOnlyDictionary<AccessNetworkNode, AccessIntersectionSnapshot> Snapshots => snapshots;
        public event Action Changed;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy() { if (I == this) I = null; }

        private void Update()
        {
            if (Time.time < nextRefresh) return;
            nextRefresh = Time.time + refreshSeconds;
            Refresh();
        }

        public void Refresh()
        {
            snapshots.Clear();
            var network = AccessNetworkRuntime.I;
            if (network == null || network.Nodes == null) return;
            var skiers = FindObjectsOfType<SkierRuntime>();
            foreach (var node in network.Nodes)
            {
                if (node == null) continue;
                int connected = 0, accessible = 0;
                float roadPressure = 0f;
                foreach (var road in node.Connections)
                {
                    if (road == null) continue;
                    connected++;
                    if (!road.IsPedestrianAccessible) continue;
                    accessible++;
                    if (AccessRoadTrafficRuntime.I != null)
                        roadPressure += AccessRoadTrafficRuntime.I.GetPressure01(road);
                }
                float average = accessible > 0 ? roadPressure / accessible : 0f;
                int nearby = 0;
                foreach (var skier in skiers)
                {
                    if (skier != null && Vector3.Distance(node.transform.position, skier.transform.position) <= sampleRadius)
                        nearby++;
                }
                float local = Mathf.Clamp01(average * 0.7f + Mathf.Clamp01(nearby / 8f) * 0.3f);
                snapshots[node] = new AccessIntersectionSnapshot
                {
                    nodeId = string.IsNullOrEmpty(node.nodeId) ? node.name : node.nodeId,
                    displayName = node.name,
                    nodeType = node.nodeType,
                    connectedRoads = connected,
                    accessibleRoads = accessible,
                    nearbyVisitors = nearby,
                    pressure01 = local,
                    averageRoadPressure01 = average,
                    isHotspot = accessible >= hotspotMinimumRoads && local >= hotspotThreshold01
                };
            }
            Changed?.Invoke();
        }

        public bool TryGet(AccessNetworkNode node, out AccessIntersectionSnapshot snapshot)
        {
            return snapshots.TryGetValue(node, out snapshot);
        }

        public AccessIntersectionSnapshot GetWorstHotspot()
        {
            AccessIntersectionSnapshot best = default;
            float pressure = -1f;
            foreach (var kv in snapshots)
                if (kv.Value.pressure01 > pressure) { pressure = kv.Value.pressure01; best = kv.Value; }
            return best;
        }
    }
}
