using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum InjurySeverity { Minor, Moderate, Serious, Critical }

    [System.Serializable]
    public class SafetyIncident
    {
        public SlopeRuntime slope;
        public InjurySeverity severity;
        public float createdHour;
        public float responseMinutes;
        public bool helicopterRequired;
        public bool resolved;
        public string Description => severity == InjurySeverity.Minor ? "Chute légère" : severity == InjurySeverity.Moderate ? "Blessure modérée" : severity == InjurySeverity.Serious ? "Blessure importante" : "Blessure critique";
    }

    /// <summary>Lightweight rescue simulation: realistic consequences without individual medical micromanagement.</summary>
    public sealed class SafetyOperationsRuntime : MonoBehaviour
    {
        public static SafetyOperationsRuntime I { get; private set; }
        public int PatrolTeams { get; private set; } = 2;
        public int Helicopters { get; private set; } = 1;
        public int ActiveIncidents => incidents.Count;
        public int ResolvedToday { get; private set; }
        public IReadOnlyList<SafetyIncident> Incidents => incidents;
        public float SafetyScore01 { get; private set; } = .92f;

        private readonly List<SafetyIncident> incidents = new();
        private float tick;
        private float nextIncidentHour = 9f;
        private int lastDay = -1;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (GameClock.I == null) return;
            if (lastDay != GameClock.I.CurrentDay) { lastDay = GameClock.I.CurrentDay; ResolvedToday = 0; nextIncidentHour = 9f; incidents.Clear(); }
            tick += Time.unscaledDeltaTime;
            if (tick < 1f) return;
            tick = 0f;
            ResolveDueIncidents();
            if (GameClock.I.CurrentHour >= nextIncidentHour && GameClock.I.CurrentHour < 18f)
            {
                TryGenerateIncident();
                nextIncidentHour = GameClock.I.CurrentHour + Random.Range(1.2f, 2.8f);
            }
            SafetyScore01 = Mathf.Lerp(SafetyScore01, Mathf.Clamp01(1f - incidents.Count * .07f), .08f);
        }

        private void TryGenerateIncident()
        {
            var slopes = FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None);
            if (slopes.Length == 0) return;
            SlopeRuntime best = null; float weightSum = 0f;
            foreach (var s in slopes)
            {
                if (s == null || !s.Open) continue;
                float w = Mathf.Max(.05f, s.TrafficDensity + .15f);
                if (SurfaceConditionOptionsRuntime.I != null && SurfaceConditionOptionsRuntime.I.AdvancedSurfaceConditions)
                {
                    var c = SlopeConditionRuntime.Get(s);
                    if (c != null) w *= 1f + c.IceRisk01 * .7f + c.SoupRisk01 * .5f;
                }
                weightSum += w;
                if (Random.value <= w / Mathf.Max(.01f, weightSum)) best = s;
            }
            if (best == null || incidents.Count >= 4) return;

            float risk = Mathf.Clamp01(.18f + best.TrafficDensity * 1.4f + Mathf.Max(0f, best.AverageAngle - 18f) / 45f);
            var signage = best.GetComponent<SlopeSafetySignageRuntime>();
            if (signage != null && signage.HasCompleteSignage) risk *= .88f;
            if (SurfaceConditionOptionsRuntime.I != null && SurfaceConditionOptionsRuntime.I.AdvancedSurfaceConditions)
            {
                var c = SlopeConditionRuntime.Get(best);
                if (c != null) risk += c.IceRisk01 * .18f + c.SoupRisk01 * .12f;
            }
            if (Random.value > Mathf.Clamp01(risk * .22f)) return;

            float roll = Random.value;
            InjurySeverity severity = roll < .58f ? InjurySeverity.Minor : roll < .86f ? InjurySeverity.Moderate : roll < .97f ? InjurySeverity.Serious : InjurySeverity.Critical;
            // A protected lift pylon can reduce the severity of collision-type incidents.
            if (severity >= InjurySeverity.Serious && LiftProtectionRuntime.AnyProtectedPylons && Random.value < .28f)
                severity = InjurySeverity.Moderate;

            bool helicopter = severity == InjurySeverity.Critical || (severity == InjurySeverity.Serious && Random.value < .38f);
            float response = severity == InjurySeverity.Minor ? 8f : severity == InjurySeverity.Moderate ? 15f : severity == InjurySeverity.Serious ? 28f : 45f;
            if (helicopter && Helicopters > 0) response *= .55f;
            else if (helicopter) response *= 1.7f;
            incidents.Add(new SafetyIncident { slope = best, severity = severity, createdHour = GameClock.I.CurrentHour, responseMinutes = response, helicopterRequired = helicopter });
        }

        private void ResolveDueIncidents()
        {
            for (int i = incidents.Count - 1; i >= 0; i--)
            {
                var incident = incidents[i];
                float elapsed = (GameClock.I.CurrentHour - incident.createdHour) * 60f;
                if (elapsed < incident.responseMinutes) continue;
                incident.resolved = true;
                incidents.RemoveAt(i);
                ResolvedToday++;
            }
        }

        public void SetPatrolTeams(int count) => PatrolTeams = Mathf.Clamp(count, 1, 12);
        public void SetHelicopters(int count) => Helicopters = Mathf.Clamp(count, 0, 4);
    }
}
