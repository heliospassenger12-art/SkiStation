using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum ManagementInsightSeverity { Info, Attention, Important }

    [Serializable]
    public struct ManagementInsight
    {
        public string id;
        public ManagementInsightSeverity severity;
        public string title;
        public string message;
        public StationServiceType serviceType;
        public float score01;
    }

    /// <summary>
    /// 1.86 concise management advisor. Turns raw service-demand telemetry into a very small,
    /// prioritised set of actionable insights for the clean HUD. It never opens UI or builds
    /// anything automatically: the player remains in control.
    /// </summary>
    public sealed class ManagementInsightRuntime : MonoBehaviour
    {
        public static ManagementInsightRuntime I { get; private set; }
        [SerializeField, Min(2f)] private float refreshSeconds = 6f;
        [SerializeField, Range(1, 8)] private int maximumVisibleInsights = 3;
        private readonly List<ManagementInsight> insights = new List<ManagementInsight>();
        private float timer;
        public event Action Changed;
        public IReadOnlyList<ManagementInsight> Insights => insights;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject); Refresh();
        }

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < refreshSeconds) return;
            timer = 0f; Refresh();
        }

        public void Refresh()
        {
            insights.Clear();
            ServiceDemandIntelligenceRuntime demand = ServiceDemandIntelligenceRuntime.I;
            if (demand == null) { Changed?.Invoke(); return; }
            demand.Refresh();

            foreach (var pair in demand.Snapshots)
            {
                ServiceDemandSnapshot s = pair.Value;
                if (s.facilities == 0)
                {
                    // Absence alone is not an alert: only simulated demand should eventually promote it.
                    continue;
                }
                if (s.unmetDemand01 < .45f && s.refusedToday <= 0) continue;

                ManagementInsightSeverity severity = s.unmetDemand01 >= .72f || s.refusedToday >= 8
                    ? ManagementInsightSeverity.Important : ManagementInsightSeverity.Attention;
                string title = ServiceLabel(s.type) + (severity == ManagementInsightSeverity.Important ? " saturé" : " sous pression");
                string message = s.queuedCustomers > 0
                    ? s.queuedCustomers + " visiteur(s) en attente. Vérifier capacité ou offre."
                    : s.refusedToday + " demande(s) non satisfaite(s) aujourd'hui.";
                insights.Add(new ManagementInsight {
                    id = "service:" + s.type,
                    severity = severity,
                    title = title,
                    message = message,
                    serviceType = s.type,
                    score01 = Mathf.Clamp01(Mathf.Max(s.unmetDemand01, s.refusedToday / 12f))
                });
            }

            insights.Sort((a,b) => {
                int sev = b.severity.CompareTo(a.severity);
                return sev != 0 ? sev : b.score01.CompareTo(a.score01);
            });
            if (insights.Count > maximumVisibleInsights)
                insights.RemoveRange(maximumVisibleInsights, insights.Count - maximumVisibleInsights);
            Changed?.Invoke();
        }

        public bool TryGetTop(out ManagementInsight insight)
        {
            if (insights.Count > 0) { insight = insights[0]; return true; }
            insight = default(ManagementInsight); return false;
        }

        private static string ServiceLabel(StationServiceType type)
        {
            switch (type)
            {
                case StationServiceType.Restaurant: return "Restaurants";
                case StationServiceType.Bar: return "Bars";
                case StationServiceType.Shop: return "Commerces";
                case StationServiceType.EquipmentRental: return "Location de matériel";
                case StationServiceType.SkiSchool: return "École de ski";
                case StationServiceType.Activity: return "Activités";
                case StationServiceType.Accommodation: return "Hébergements";
                default: return type.ToString();
            }
        }
    }

    public static class ManagementInsightBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (ManagementInsightRuntime.I != null) return;
            new GameObject("Management Insights 1.86").AddComponent<ManagementInsightRuntime>();
        }
    }
}
