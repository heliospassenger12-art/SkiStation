using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum GroomingVehicleType { Snowcat, Snowmobile, ServiceVehicle }

    [Serializable]
    public class GroomingVehicle
    {
        public string id;
        public GroomingVehicleType type;
        public bool available = true;
        public float efficiency = 1f;
        public GroomingVehicle(string id, GroomingVehicleType type, float efficiency)
        { this.id = id; this.type = type; this.efficiency = efficiency; }
    }

    /// <summary>Small operations layer: a real fleet, finite crews and nightly piste preparation.</summary>
    public sealed class SnowGroomingFleetRuntime : MonoBehaviour
    {
        public static SnowGroomingFleetRuntime I { get; private set; }
        public List<GroomingVehicle> Fleet { get; } = new List<GroomingVehicle>();
        public int CrewCount { get; private set; } = 2;
        public int ActiveJobs { get; private set; }
        public float LastPreparationScore { get; private set; }
        public string LastJob { get; private set; } = "Aucun";
        public event Action Changed;

        private readonly Dictionary<SlopeRuntime, float> jobs = new Dictionary<SlopeRuntime, float>();
        private bool morningPrepared;
        private int preparedDay;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
            Fleet.Add(new GroomingVehicle("Dameuse-01", GroomingVehicleType.Snowcat, 1.15f));
            Fleet.Add(new GroomingVehicle("Dameuse-02", GroomingVehicleType.Snowcat, 1.00f));
            Fleet.Add(new GroomingVehicle("Motoneige-01", GroomingVehicleType.Snowmobile, .55f));
        }

        private void Update()
        {
            if (GameClock.I == null) return;
            if (GameClock.I.CurrentDay != preparedDay) { preparedDay = GameClock.I.CurrentDay; morningPrepared = false; }

            // Night preparation: prioritize the most degraded open/closed pistes before 06:00.
            if (!morningPrepared && GameClock.I.GameHour >= 4f && GameClock.I.GameHour < 6f)
            {
                RunNightPreparation();
                morningPrepared = true;
            }

            if (jobs.Count == 0) return;
            var finished = new List<SlopeRuntime>();
            foreach (var kv in jobs)
            {
                float duration = kv.Key.Length <= 1f ? .5f : Mathf.Clamp(kv.Key.Length / 260f, .8f, 4.5f);
                if (Time.unscaledDeltaTime * Mathf.Max(.1f, GameSpeedMultiplier()) >= duration / Mathf.Max(.1f, FleetEfficiency()))
                    finished.Add(kv.Key);
            }
            foreach (var slope in finished) CompleteJob(slope);
        }

        public bool QueueGrooming(SlopeRuntime slope, float intensity = 1f)
        {
            if (slope == null || jobs.ContainsKey(slope) || ActiveJobs >= Mathf.Max(1, CrewCount)) return false;
            var vehicle = FindAvailableVehicle();
            if (vehicle == null) return false;
            float cost = Mathf.Max(350f, slope.Length * .8f * Mathf.Clamp(intensity, .5f, 2f));
            if (StationRuntime.I == null || !StationRuntime.I.Spend(cost, FinanceCategory.Maintenance, slope.name, "Exploitation dameuse")) return false;
            vehicle.available = false;
            jobs[slope] = Mathf.Clamp(intensity, .5f, 2f);
            ActiveJobs++;
            LastJob = $"{slope.name} — {(intensity > 1.2f ? "gros damage" : "damage")}";
            Changed?.Invoke();
            return true;
        }

        public void RunNightPreparation()
        {
            var slopes = UnityEngine.Object.FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None);
            Array.Sort(slopes, (a,b) => ConditionScore(b).CompareTo(ConditionScore(a)));
            int started = 0;
            foreach (var slope in slopes)
            {
                if (slope == null || !NeedsPreparation(slope)) continue;
                if (QueueGrooming(slope, slope.GetComponent<SlopeConditionRuntime>()?.SnowQuality01 < .45f ? 1.6f : 1f)) started++;
                if (ActiveJobs >= CrewCount) break;
            }
            LastPreparationScore = CalculateDomainPreparation();
            LastJob = started > 0 ? $"Préparation nocturne : {started} piste(s)" : "Préparation nocturne : aucune intervention";
            Changed?.Invoke();
        }

        public float CalculateDomainPreparation()
        {
            var slopes = UnityEngine.Object.FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None);
            if (slopes.Length == 0) return 0f;
            float total = 0f;
            int count = 0;
            foreach (var s in slopes)
            {
                var c = s?.GetComponent<SlopeConditionRuntime>();
                if (c == null) continue;
                total += c.VisitorQualityFactor;
                count++;
            }
            return count == 0 ? 0f : Mathf.Clamp01(total / count) * 100f;
        }

        private bool NeedsPreparation(SlopeRuntime s)
        {
            var c = s.GetComponent<SlopeConditionRuntime>();
            return c != null && (c.RequiresGrooming || !s.Groomed || !s.Open);
        }
        private float ConditionScore(SlopeRuntime s)
        {
            var c = s.GetComponent<SlopeConditionRuntime>();
            return c == null ? 0f : (1f - c.SnowQuality01) + (1f - c.GroomingProgress01);
        }
        private GroomingVehicle FindAvailableVehicle()
        {
            foreach (var v in Fleet) if (v.available) return v;
            return null;
        }
        private float FleetEfficiency()
        {
            float sum = 0f; int n = 0;
            foreach (var v in Fleet) { sum += v.efficiency; n++; }
            return n == 0 ? 1f : sum / n;
        }
        private float GameSpeedMultiplier()
        {
            if (GameSpeedController.I == null) return 1f;
            switch (GameSpeedController.I.Current) { case GameSpeed.Fast: return 3f; case GameSpeed.VeryFast: return 8f; default: return 1f; }
        }
        private void CompleteJob(SlopeRuntime slope)
        {
            float intensity = jobs[slope]; jobs.Remove(slope); ActiveJobs = Mathf.Max(0, ActiveJobs - 1);
            var v = FindBusyVehicle(); if (v != null) v.available = true;
            slope.GetComponent<SlopeConditionRuntime>()?.ApplyGroomingResult(intensity);
            LastJob = $"Terminé : {slope.name}";
            Changed?.Invoke();
        }
        private GroomingVehicle FindBusyVehicle()
        {
            foreach (var v in Fleet) if (!v.available) return v;
            return null;
        }
    }
}
