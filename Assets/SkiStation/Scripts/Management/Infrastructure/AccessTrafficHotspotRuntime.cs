using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum AccessHotspotSeverity { None, Watch, Critical }

    [Serializable]
    public struct AccessHotspotSnapshot
    {
        public string id;
        public string name;
        public string locationType;
        public AccessHotspotSeverity severity;
        public float pressure01;
        public int realVisitors;
        public int connectedRoads;
        public string recommendation;
    }

    /// <summary>0.88: management-facing list of real access bottlenecks.</summary>
    public sealed class AccessTrafficHotspotRuntime : MonoBehaviour
    {
        public static AccessTrafficHotspotRuntime I { get; private set; }
        [SerializeField] private float refreshSeconds = 1.5f;
        private readonly List<AccessHotspotSnapshot> hotspots = new List<AccessHotspotSnapshot>();
        private float nextRefresh;
        public IReadOnlyList<AccessHotspotSnapshot> Hotspots => hotspots;
        public event Action Changed;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }
        private void OnDestroy() { if (I == this) I = null; }
        private void Start() { Rebuild(); }
        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + refreshSeconds;
            Rebuild();
        }

        public void Rebuild()
        {
            hotspots.Clear();
            var intersections = AccessIntersectionTrafficRuntime.I;
            if (intersections == null) return;
            foreach (var kv in intersections.Snapshots)
            {
                var s = kv.Value;
                if (!s.isHotspot) continue;
                var severity = s.pressure01 >= 0.85f ? AccessHotspotSeverity.Critical : AccessHotspotSeverity.Watch;
                hotspots.Add(new AccessHotspotSnapshot
                {
                    id = s.nodeId,
                    name = s.displayName,
                    locationType = s.nodeType.ToString(),
                    severity = severity,
                    pressure01 = s.pressure01,
                    realVisitors = s.nearbyVisitors,
                    connectedRoads = s.accessibleRoads,
                    recommendation = severity == AccessHotspotSeverity.Critical
                        ? "Créer ou renforcer un itinéraire piéton alternatif."
                        : "Surveiller la circulation et envisager une liaison secondaire."
                });
            }
            hotspots.Sort((a, b) => b.pressure01.CompareTo(a.pressure01));
            Changed?.Invoke();
        }
    }
}
