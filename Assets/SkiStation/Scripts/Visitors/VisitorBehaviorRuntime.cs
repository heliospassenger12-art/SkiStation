using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    [Serializable]
    public struct VisitorBehaviorSnapshot
    {
        public CustomerProfile profile;
        public VisitorPreferenceProfile preferences;
        public string preferredSlopeId;
        public string preferredLiftId;
        public string preferredServiceId;
        public float decisionConfidence;
        public int decisionsMade;
    }

    /// <summary>
    /// Gives real management visitors lightweight individual preferences and decision memory.
    /// It never creates visitors and never affects the independent player simulation.
    /// </summary>
    public sealed class VisitorBehaviorRuntime : MonoBehaviour
    {
        public static VisitorBehaviorRuntime I { get; private set; }
        [SerializeField] private float refreshSeconds = 5f;
        [SerializeField, Range(0f,1f)] private float exploration = .12f;

        private readonly Dictionary<SkierRuntime, VisitorBehaviorSnapshot> snapshots = new();
        private readonly Dictionary<SkierRuntime, int> decisions = new();
        private float timer;
        public IReadOnlyDictionary<SkierRuntime, VisitorBehaviorSnapshot> Snapshots => snapshots;
        public event Action Changed;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start() => Recalculate();
        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < Mathf.Max(.5f, refreshSeconds)) return;
            timer = 0f;
            Recalculate();
        }

        public VisitorPreferenceProfile GetPreferences(SkierRuntime skier)
        {
            return VisitorPreferenceProfile.For(GetProfile(skier));
        }

        public SlopeRuntime ChooseSlope(SkierRuntime skier, LiftRuntime lift)
        {
            var network = ResortRouteNetwork.I;
            if (network == null) return null;
            var profile = GetProfile(skier);
            var pref = VisitorPreferenceProfile.For(profile);
            var candidates = new List<SlopeRuntime>();
            foreach (var slope in network.Slopes)
            {
                if (slope == null || !slope.Open || slope.Points.Count < 2) continue;
                if (Vector3.Distance(lift.Top, slope.TopPoint) > 35f) continue;
                candidates.Add(slope);
            }
            if (candidates.Count == 0) return network.FindSlopeFromLift(lift);
            if (UnityEngine.Random.value < exploration) return candidates[UnityEngine.Random.Range(0, candidates.Count)];

            SlopeRuntime best = null; float bestScore = float.MinValue;
            foreach (var slope in candidates)
            {
                // Safety first: normal visitors should not accidentally be routed onto
                // terrain far beyond their ability. Exploration may still discover a
                // slightly harder run, but never skips the hard safety gate.
                CustomerProfile visitorProfile = profile;
                int ability = SlopeVisitorExperienceRuntime.MaxDifficulty(visitorProfile);
                if ((int)slope.Difficulty > ability + 1) continue;
                float score = ScoreSlope(skier, slope, pref);
                if (SlopeCongestionRuntime.I != null)
                {
                    float pressure = SlopeCongestionRuntime.I.GetPressure01(slope);
                    float penalty = SlopeCongestionRuntime.I.GetRoutePenalty01(slope);
                    score -= penalty * (0.55f + pref.comfort * 0.45f);
                    if (SlopeCongestionRuntime.I.ShouldRedirect(slope)) score -= 0.65f + pref.comfort * 0.35f;
                }
                if (ManagedZoneRuntime.I != null)
                    score += ManagedZoneRuntime.I.BeginnerSlopeBonus(slope, visitorProfile);
                if (ManagedZoneReputationRuntime.I != null)
                {
                    foreach (var zone in ManagedZoneReputationRuntime.I.Zones)
                    {
                        if (zone.type == ManagedZoneType.Nature || zone.type == ManagedZoneType.Family || zone.type == ManagedZoneType.Beginner || zone.type == ManagedZoneType.Sport || zone.type == ManagedZoneType.Snowpark)
                        {
                            if (ManagedZoneRuntime.I.SlopeTouchesZone(slope, zone.type))
                            {
                                float fit = ManagedZoneSpecializationRuntime.I != null ? ManagedZoneSpecializationRuntime.I.GetAttraction(zone.type, visitorProfile) : .5f;
                                score += zone.attraction01 * fit * (.45f + pref.comfort * .35f);
                                break;
                            }
                        }
                    }
                }
                if (score > bestScore) { bestScore = score; best = slope; }
            }
            RegisterDecision(skier, best != null ? best.name : null, null, null, bestScore);
            if (VisitorIntentRuntime.I != null && best != null) VisitorIntentRuntime.I.RegisterSlopeChoice(skier, best);
            if (VisitorLoyaltyRuntime.I != null && best != null) VisitorLoyaltyRuntime.I.RegisterSlopeChoice(skier, best);
            if (VisitorGroupRuntime.I != null && best != null) VisitorGroupRuntime.I.RegisterSlopeChoice(skier, best);
            return best;
        }

        public LiftRuntime ChooseLift(SkierRuntime skier, Vector3 from, Vector3 preferredTop)
        {
            var network = ResortRouteNetwork.I;
            if (network == null) return null;
            var pref = VisitorPreferenceProfile.For(GetProfile(skier));
            LiftRuntime best = null; float bestScore = float.MinValue;
            foreach (var lift in network.Lifts)
            {
                if (lift == null || !lift.Open || lift.Faulted || lift.Definition == null) continue;
                if (Vector3.Distance(from, lift.Bottom) > 35f) continue;
                float pressure = InfrastructureCongestionRuntime.I != null ? InfrastructureCongestionRuntime.I.GetLiftPressure01(lift) : 0f;
                float destination = Vector3.Distance(lift.Top, preferredTop);
                float speed = Mathf.Clamp01(lift.EffectiveSpeedKmh / 30f);
                float score = speed * (0.25f + pref.sport * .25f) - pressure * (.45f + pref.comfort * .25f) - destination * .015f;
                if (VisitorIntentRuntime.I != null) score += VisitorIntentRuntime.I.ScoreLiftIntent(skier, lift);
                if (lift.Definition.detachable) score += pref.comfort * .12f;
                if (VisitorGroupRuntime.I != null) score += VisitorGroupRuntime.I.ScoreLift(skier, lift);
                if (score > bestScore) { bestScore = score; best = lift; }
            }
            RegisterDecision(skier, null, best != null ? best.name : null, null, bestScore);
            if (VisitorIntentRuntime.I != null && best != null) VisitorIntentRuntime.I.RegisterLiftChoice(skier, best);
            if (VisitorLoyaltyRuntime.I != null && best != null) VisitorLoyaltyRuntime.I.RegisterLiftChoice(skier, best);
            if (VisitorGroupRuntime.I != null && best != null) VisitorGroupRuntime.I.RegisterLiftChoice(skier, best);
            return best;
        }

        public string ChooseServiceId(SkierRuntime skier)
        {
            var profile = GetProfile(skier);
            var pref = VisitorPreferenceProfile.For(profile);
            string bestId = null; float bestScore = float.MinValue;
            foreach (var facility in LeisureFacilityRuntime.All)
            {
                if (facility == null || facility.definition == null || facility.AvailableCapacity <= 0) continue;
                float pressure = facility.Occupancy / Mathf.Max(1f, facility.definition.capacity);
                float score = pref.variety * .4f - pressure * (1f + pref.comfort) + ((profile == CustomerProfile.Family && facility.definition.familyFriendly) ? .65f : 0f);
                if (VisitorIntentRuntime.I != null) score += VisitorIntentRuntime.I.ScoreServiceIntent(skier, facility);
                if (VisitorGroupRuntime.I != null) score += VisitorGroupRuntime.I.ScoreService(skier, facility.definition.facilityName);
                if (score > bestScore) { bestScore = score; bestId = facility.definition.facilityName; }
            }
            RegisterDecision(skier, null, null, bestId, bestScore);
            if (VisitorIntentRuntime.I != null && !string.IsNullOrEmpty(bestId)) VisitorIntentRuntime.I.RegisterServiceChoice(skier, bestId);
            if (VisitorLoyaltyRuntime.I != null && !string.IsNullOrEmpty(bestId)) VisitorLoyaltyRuntime.I.RegisterServiceChoice(skier, bestId);
            if (VisitorGroupRuntime.I != null && !string.IsNullOrEmpty(bestId)) VisitorGroupRuntime.I.RegisterServiceChoice(skier, bestId);
            return bestId;
        }

        public void Recalculate()
        {
            var skiers = FindObjectsOfType<SkierRuntime>();
            var live = new HashSet<SkierRuntime>(skiers);
            foreach (var skier in skiers)
            {
                if (skier == null || skier.State == SkierState.Leaving) continue;
                var snapshot = snapshots.TryGetValue(skier, out var old) ? old : new VisitorBehaviorSnapshot();
                snapshot.profile = GetProfile(skier);
                snapshot.preferences = VisitorPreferenceProfile.For(snapshot.profile);
                snapshot.decisionsMade = decisions.TryGetValue(skier, out var count) ? count : 0;
                snapshot.decisionConfidence = 1f - exploration;
                snapshots[skier] = snapshot;
            }
            var dead = new List<SkierRuntime>();
            foreach (var pair in snapshots) if (!live.Contains(pair.Key)) dead.Add(pair.Key);
            foreach (var key in dead) snapshots.Remove(key);
            Changed?.Invoke();
        }

        private float ScoreSlope(SkierRuntime skier, SlopeRuntime slope, VisitorPreferenceProfile pref)
        {
            float pressure = InfrastructureCongestionRuntime.I != null ? InfrastructureCongestionRuntime.I.GetSlopePressure01(slope) : 0f;
            float difficulty = Difficulty01(slope.Difficulty);
            float desired = pref.highDifficulty - pref.lowDifficulty;
            float fit = 1f - Mathf.Clamp01(Mathf.Abs(difficulty - Mathf.Clamp01(.5f + desired * .5f)) / .65f);
            float abilityFit = SlopeVisitorExperienceRuntime.I != null
                ? SlopeVisitorExperienceRuntime.DifficultyFit(GetProfile(skier), slope.Difficulty)
                : 1f;
            float lengthScore = Mathf.Clamp01(slope.Length / 2500f);
            float widthScore = Mathf.Clamp01((slope.AverageWidth - 4f) / 16f);
            float trafficComfort = 1f - pressure;
            float score = fit * (1f + pref.sport + pref.adventure) * abilityFit;
            score += pref.variety * lengthScore;
            score += widthScore * (.25f + pref.comfort * .45f);
            score += trafficComfort * (0.5f + pref.comfort);
            score -= Mathf.Clamp01(slope.AverageAngle / 32f) * (pref.comfort * .35f);
            if (VisitorIntentRuntime.I != null) score += VisitorIntentRuntime.I.ScoreSlopeIntent(skier, slope);
            if (VisitorGroupRuntime.I != null) score += VisitorGroupRuntime.I.ScoreSlope(skier, slope);
            return score;
        }

        private static float Difficulty01(SlopeDifficulty difficulty)
        {
            switch (difficulty)
            {
                case SlopeDifficulty.Green: return .15f;
                case SlopeDifficulty.Blue: return .35f;
                case SlopeDifficulty.Red: return .60f;
                case SlopeDifficulty.Black: return .82f;
                default: return 1f;
            }
        }

        private void RegisterDecision(SkierRuntime skier, string slopeId, string liftId, string serviceId, float confidence)
        {
            if (skier == null) return;
            decisions[skier] = decisions.TryGetValue(skier, out var count) ? count + 1 : 1;
            if (!snapshots.TryGetValue(skier, out var snapshot)) snapshot = new VisitorBehaviorSnapshot();
            snapshot.profile = GetProfile(skier);
            snapshot.preferences = VisitorPreferenceProfile.For(snapshot.profile);
            if (!string.IsNullOrEmpty(slopeId)) snapshot.preferredSlopeId = slopeId;
            if (!string.IsNullOrEmpty(liftId)) snapshot.preferredLiftId = liftId;
            if (!string.IsNullOrEmpty(serviceId)) snapshot.preferredServiceId = serviceId;
            snapshot.decisionConfidence = Mathf.Clamp01(confidence);
            snapshot.decisionsMade = decisions[skier];
            snapshots[skier] = snapshot;
        }

        private static CustomerProfile GetProfile(SkierRuntime skier)
        {
            return skier != null && skier.PopulationProfile != null ? skier.PopulationProfile.ToCustomerProfile() : CustomerProfile.Tourist;
        }
    }
}
