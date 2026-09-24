using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum ResortTransferMode { Walk, Ski, Lift, InternalTransport }

    [Serializable]
    public sealed class ResortVisitorFlow
    {
        public string fromHubId;
        public string toHubId;
        public ResortTransferMode preferredMode;
        public int nearbyVisitors;
        public float demand;
        public float connectionQuality;
        public bool underserved;
    }

    // 2.22 — visitor-flow planning between organically activated resort hubs.
    // It observes real visitors and player-built connections, but deliberately does not
    // issue MoveTo commands: existing skier/lift movement remains authoritative.
    public sealed class ResortVisitorFlowRuntime : MonoBehaviour
    {
        public static ResortVisitorFlowRuntime I { get; private set; }
        public IReadOnlyList<ResortVisitorFlow> Flows => flows;
        readonly List<ResortVisitorFlow> flows = new List<ResortVisitorFlow>();
        float nextRefresh;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        void Start() { Refresh(); }
        void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 3f;
            Refresh();
        }

        public void Refresh()
        {
            var network = ResortHubNetworkRuntime.I;
            var development = ResortHubDevelopmentRuntime.I;
            if (network == null) return;
            network.RefreshActivity();
            if (development != null) development.Refresh();
            flows.Clear();

            var visitors = FindObjectsByType<SkierRuntime>(FindObjectsSortMode.None);
            foreach (var link in network.Links)
            {
                var a = FindHub(network, link.fromId);
                var b = FindHub(network, link.toId);
                if (a == null || b == null || !a.IsActive || !b.IsActive) continue;

                AddDirectionalFlow(a, b, link, visitors, development);
                AddDirectionalFlow(b, a, link, visitors, development);
            }
        }

        void AddDirectionalFlow(ResortHub from, ResortHub to, ResortHubLink link, SkierRuntime[] visitors, ResortHubDevelopmentRuntime development)
        {
            int nearby = 0;
            for (int i = 0; i < visitors.Length; i++)
                if (visitors[i] != null && Vector3.Distance(visitors[i].transform.position, from.center) <= from.influenceRadius) nearby++;

            float destinationPull = .25f + to.serviceCount * .10f + to.liftAccessCount * .14f + to.slopeAccessCount * .10f;
            var dev = development != null ? development.Get(to.id) : null;
            if (dev != null) destinationPull += dev.developmentScore * .30f;
            float demand = Mathf.Clamp01((nearby / 12f) * .45f + destinationPull * .55f);

            ResortTransferMode mode;
            if (link.liftConnection) mode = ResortTransferMode.Lift;
            else if (link.pisteConnection) mode = ResortTransferMode.Ski;
            else if (link.distance <= 450f) mode = ResortTransferMode.Walk;
            else mode = ResortTransferMode.InternalTransport;

            float quality = link.liftConnection ? 1f : link.pisteConnection ? .82f : link.distance <= 450f ? .55f : .18f;
            flows.Add(new ResortVisitorFlow {
                fromHubId = from.id, toHubId = to.id, preferredMode = mode,
                nearbyVisitors = nearby, demand = demand, connectionQuality = quality,
                underserved = demand >= .35f && quality < .50f
            });
        }

        static ResortHub FindHub(ResortHubNetworkRuntime network, string id)
        {
            foreach (var h in network.Hubs) if (h.id == id) return h;
            return null;
        }

        public ResortVisitorFlow StrongestUnderservedFlow()
        {
            ResortVisitorFlow best = null;
            for (int i = 0; i < flows.Count; i++)
                if (flows[i].underserved && (best == null || flows[i].demand > best.demand)) best = flows[i];
            return best;
        }
    }

    public static class ResortVisitorFlowBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (UnityEngine.Object.FindFirstObjectByType<ResortVisitorFlowRuntime>() != null) return;
            new GameObject("SkiStation_2.22_RESORT_VISITOR_FLOW").AddComponent<ResortVisitorFlowRuntime>();
            Debug.Log("SkiStation 2.22: RESORT VISITOR FLOW READY — active hubs now generate measurable inter-village demand without overriding skier movement.");
        }
    }
}
