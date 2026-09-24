using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    [Serializable]
    public sealed class ManagedZoneReputationSnapshot
    {
        public string name;
        public ManagedZoneType type;
        public float reputation01;
        public float attraction01;
        public string reputationLabel;
        public string attractionLabel;
        public int visitorsInside;
    }

    /// <summary>
    /// Turns zone specialization into a living reputation/attraction loop.
    /// Reputation is an outcome of the player's choices; it is deliberately not a
    /// manual slider, avoiding another layer of micromanagement.
    /// </summary>
    public sealed class ManagedZoneReputationRuntime : MonoBehaviour
    {
        public static ManagedZoneReputationRuntime I { get; private set; }
        public IReadOnlyList<ManagedZoneReputationSnapshot> Zones => snapshots;
        public float StationReputation01 { get; private set; } = .5f;
        public string StationReputationLabel => Label(StationReputation01);
        public event Action Changed;
        [SerializeField] private float refreshSeconds = 5f;
        private float timer;
        private readonly List<ManagedZoneReputationSnapshot> snapshots = new();

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < Mathf.Max(.75f, refreshSeconds)) return;
            timer = 0f;
            Recalculate();
        }

        public void Recalculate()
        {
            snapshots.Clear();
            float total = 0f; int count = 0;
            if (ManagedZoneRuntime.I != null)
            {
                foreach (var zone in ManagedZoneRuntime.I.Zones)
                {
                    float congestion = ZoneCongestion(zone.type);
                    float satisfaction = ZoneSatisfaction(zone.type);
                    float infrastructure = Mathf.Clamp01((zone.slopesInside + zone.liftsInside) / 8f);
                    float boundary = zone.physicalBoundary ? 1f : .5f;
                    float rep = Mathf.Clamp01(
                        zone.specializationScore01 * .42f +
                        satisfaction * .25f +
                        (1f - congestion) * .18f +
                        infrastructure * .10f +
                        boundary * .05f);
                    float attraction = Mathf.Clamp01(rep * .72f + zone.specializationScore01 * .28f);
                    snapshots.Add(new ManagedZoneReputationSnapshot {
                        name = zone.name, type = zone.type, reputation01 = rep,
                        attraction01 = attraction, reputationLabel = Label(rep),
                        attractionLabel = AttractionLabel(attraction), visitorsInside = zone.visitorsInside
                    });
                    total += rep; count++;
                }
            }

            float visitorExperience = GlobalVisitorSatisfaction();
            StationReputation01 = count > 0
                ? Mathf.Clamp01((total / count) * .72f + visitorExperience * .28f)
                : Mathf.Clamp01(.45f + visitorExperience * .20f);
            Changed?.Invoke();
        }

        public float GetZoneReputation(string zoneName)
        {
            foreach (var z in snapshots) if (z.name == zoneName) return z.reputation01;
            return .5f;
        }

        public float GetZoneAttraction(ManagedZoneType type, CustomerProfile profile)
        {
            float best = 0f;
            foreach (var z in snapshots)
                if (z.type == type)
                {
                    float fit = ManagedZoneSpecializationRuntime.I != null
                        ? ManagedZoneSpecializationRuntime.I.GetAttraction(type, profile) : .5f;
                    best = Mathf.Max(best, z.attraction01 * (.45f + fit * .55f));
                }
            return best;
        }

        public float GetProfileAttraction(CustomerProfile profile)
        {
            float best = .15f;
            foreach (var z in snapshots)
            {
                float fit = ManagedZoneSpecializationRuntime.I != null
                    ? ManagedZoneSpecializationRuntime.I.GetAttraction(z.type, profile) : .5f;
                best = Mathf.Max(best, z.attraction01 * fit);
            }
            return Mathf.Clamp01(best);
        }

        private float ZoneCongestion(ManagedZoneType type)
        {
            if (ManagedZoneRuntime.I == null || SlopeCongestionRuntime.I == null) return .15f;
            float sum = 0f; int n = 0;
            foreach (var slope in FindObjectsOfType<SlopeRuntime>())
                if (slope != null && ManagedZoneRuntime.I.SlopeTouchesZone(slope, type))
                { sum += SlopeCongestionRuntime.I.GetPressure01(slope); n++; }
            return n == 0 ? .15f : Mathf.Clamp01(sum / n);
        }

        private float ZoneSatisfaction(ManagedZoneType type)
        {
            float sum = 0f; int n = 0;
            if (ManagedZoneRuntime.I == null) return .6f;
            foreach (var skier in FindObjectsOfType<SkierRuntime>())
            {
                if (skier == null || skier.State == SkierState.Leaving) continue;
                if (!ManagedZoneRuntime.I.IsPointInside(skier.transform.position, type)) continue;
                sum += Mathf.Clamp01(skier.Satisfaction / 100f); n++;
            }
            return n == 0 ? .62f : Mathf.Clamp01(sum / n);
        }

        private static float GlobalVisitorSatisfaction()
        {
            float sum = 0f; int n = 0;
            foreach (var skier in FindObjectsOfType<SkierRuntime>())
            {
                if (skier == null || skier.State == SkierState.Leaving) continue;
                sum += Mathf.Clamp01(skier.Satisfaction / 100f); n++;
            }
            return n == 0 ? .60f : Mathf.Clamp01(sum / n);
        }

        private static string Label(float value)
        {
            if (value >= .82f) return "Excellente";
            if (value >= .65f) return "Très bonne";
            if (value >= .48f) return "Bonne";
            if (value >= .30f) return "Fragile";
            return "Faible";
        }

        private static string AttractionLabel(float value)
        {
            if (value >= .82f) return "Très attractive";
            if (value >= .65f) return "Attractive";
            if (value >= .48f) return "Correcte";
            return "Peu attractive";
        }
    }
}
