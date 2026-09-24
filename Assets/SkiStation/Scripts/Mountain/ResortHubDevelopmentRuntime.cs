using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum ResortHubDevelopmentStage { Dormant, AccessPoint, Hamlet, Village, ResortCentre }

    [Serializable]
    public sealed class ResortHubDevelopment
    {
        public string hubId;
        public string displayName;
        public ResortHubDevelopmentStage stage;
        public float developmentScore;
        public float accessScore;
        public float serviceScore;
        public float connectionScore;
        public bool needsServices;
        public bool needsLiftAccess;
        public bool isolated;
    }

    // 2.21 — hubs grow from what the player actually builds. No village is pre-created or forced.
    // This layer gives future visitors, economy and objectives a stable notion of resort centres.
    public sealed class ResortHubDevelopmentRuntime : MonoBehaviour
    {
        public static ResortHubDevelopmentRuntime I { get; private set; }
        public IReadOnlyList<ResortHubDevelopment> Developments => developments;
        readonly List<ResortHubDevelopment> developments = new List<ResortHubDevelopment>();
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
            nextRefresh = Time.unscaledTime + 2.5f;
            Refresh();
        }

        public void Refresh()
        {
            var network = ResortHubNetworkRuntime.I;
            if (network == null) return;
            network.RefreshActivity();
            developments.Clear();

            foreach (var hub in network.Hubs)
            {
                int connectedLinks = 0;
                foreach (var link in network.Links)
                    if ((link.fromId == hub.id || link.toId == hub.id) && (link.liftConnection || link.pisteConnection)) connectedLinks++;

                float access = Mathf.Clamp01((hub.liftAccessCount * 0.55f) + (hub.slopeAccessCount * 0.30f));
                float services = Mathf.Clamp01(hub.serviceCount / 5f);
                float connections = Mathf.Clamp01(connectedLinks / 2f);
                float score = Mathf.Clamp01(access * 0.42f + services * 0.34f + connections * 0.24f);

                ResortHubDevelopmentStage stage;
                if (!hub.IsActive) stage = ResortHubDevelopmentStage.Dormant;
                else if (hub.serviceCount == 0) stage = ResortHubDevelopmentStage.AccessPoint;
                else if (score < 0.42f) stage = ResortHubDevelopmentStage.Hamlet;
                else if (score < 0.72f) stage = ResortHubDevelopmentStage.Village;
                else stage = ResortHubDevelopmentStage.ResortCentre;

                developments.Add(new ResortHubDevelopment {
                    hubId = hub.id, displayName = hub.displayName, stage = stage,
                    developmentScore = score, accessScore = access, serviceScore = services, connectionScore = connections,
                    needsServices = hub.IsActive && hub.serviceCount == 0,
                    needsLiftAccess = hub.IsActive && hub.liftAccessCount == 0,
                    isolated = hub.IsActive && connectedLinks == 0
                });
            }
        }

        public ResortHubDevelopment Get(string hubId)
        {
            for (int i = 0; i < developments.Count; i++) if (developments[i].hubId == hubId) return developments[i];
            return null;
        }
    }

    public static class ResortHubDevelopmentBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (UnityEngine.Object.FindFirstObjectByType<ResortHubDevelopmentRuntime>() != null) return;
            new GameObject("SkiStation_2.21_RESORT_HUB_DEVELOPMENT").AddComponent<ResortHubDevelopmentRuntime>();
        }
    }
}
