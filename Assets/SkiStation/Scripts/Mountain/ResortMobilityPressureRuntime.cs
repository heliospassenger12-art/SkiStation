using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum ResortMobilityPressure { None, Low, Moderate, High, Critical }

    [Serializable]
    public sealed class ResortMobilityNeed
    {
        public string fromHubId;
        public string toHubId;
        public ResortTransferMode currentMode;
        public ResortTransferMode recommendedMode;
        public float demand;
        public float connectionQuality;
        public float pressureScore;
        public ResortMobilityPressure pressure;
        public bool needsIntervention;
        public string reason;
    }

    // 2.23 — management intelligence on top of 2.22 flows.
    // Read-only planning layer: it never builds infrastructure and never moves visitors.
    public sealed class ResortMobilityPressureRuntime : MonoBehaviour
    {
        public static ResortMobilityPressureRuntime I { get; private set; }
        public IReadOnlyList<ResortMobilityNeed> Needs => needs;
        readonly List<ResortMobilityNeed> needs = new List<ResortMobilityNeed>();
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
            nextRefresh = Time.unscaledTime + 4f;
            Refresh();
        }

        public void Refresh()
        {
            var flowRuntime = ResortVisitorFlowRuntime.I;
            if (flowRuntime == null) return;
            flowRuntime.Refresh();
            needs.Clear();

            foreach (var flow in flowRuntime.Flows)
            {
                float deficit = 1f - Mathf.Clamp01(flow.connectionQuality);
                float crowding = Mathf.Clamp01(flow.nearbyVisitors / 18f);
                float pressure = Mathf.Clamp01(flow.demand * .62f + deficit * .28f + crowding * .10f);

                ResortMobilityPressure band = pressure >= .82f ? ResortMobilityPressure.Critical :
                    pressure >= .64f ? ResortMobilityPressure.High :
                    pressure >= .43f ? ResortMobilityPressure.Moderate :
                    pressure >= .22f ? ResortMobilityPressure.Low : ResortMobilityPressure.None;

                ResortTransferMode recommended = flow.preferredMode;
                string reason = "Liaison adaptée à la demande actuelle.";
                bool intervene = false;

                if (flow.connectionQuality < .5f && flow.demand >= .35f)
                {
                    intervene = true;
                    recommended = flow.preferredMode == ResortTransferMode.Walk && flow.demand < .58f
                        ? ResortTransferMode.Walk : ResortTransferMode.InternalTransport;
                    reason = flow.preferredMode == ResortTransferMode.Walk
                        ? "La demande augmente entre deux pôles faiblement reliés."
                        : "La liaison inter-villages est insuffisante pour la demande observée.";
                }
                else if (flow.preferredMode == ResortTransferMode.Ski && pressure >= .68f)
                {
                    intervene = true;
                    recommended = ResortTransferMode.Lift;
                    reason = "Le flux dépend fortement d'une liaison ski : prévoir une alternative mécanique si le pôle devient majeur.";
                }

                needs.Add(new ResortMobilityNeed {
                    fromHubId = flow.fromHubId, toHubId = flow.toHubId,
                    currentMode = flow.preferredMode, recommendedMode = recommended,
                    demand = flow.demand, connectionQuality = flow.connectionQuality,
                    pressureScore = pressure, pressure = band, needsIntervention = intervene, reason = reason
                });
            }
        }

        public ResortMobilityNeed HighestPriority()
        {
            ResortMobilityNeed best = null;
            foreach (var need in needs)
                if (need.needsIntervention && (best == null || need.pressureScore > best.pressureScore)) best = need;
            return best;
        }
    }

    public static class ResortMobilityPressureBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (UnityEngine.Object.FindFirstObjectByType<ResortMobilityPressureRuntime>() != null) return;
            new GameObject("SkiStation_2.23_MOBILITY_PRESSURE").AddComponent<ResortMobilityPressureRuntime>();
            Debug.Log("SkiStation 2.23: MOBILITY PRESSURE READY — inter-hub demand can now expose transport needs without moving visitors or auto-building anything.");
        }
    }
}
