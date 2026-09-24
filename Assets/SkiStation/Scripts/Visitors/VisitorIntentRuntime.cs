using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum VisitorIntent
    {
        SkiDay,
        FamilyDay,
        SportPerformance,
        MountainDiscovery,
        Relaxation,
        PremiumExperience,
        ApresSki,
        Training
    }

    public enum VisitorJourneyStage
    {
        Arrived,
        Seeking,
        Traveling,
        Skiing,
        Service,
        Resting,
        Leaving
    }

    [Serializable]
    public struct VisitorJourneySnapshot
    {
        public VisitorIntent intent;
        public VisitorJourneyStage stage;
        public string currentTargetId;
        public string lastSlopeId;
        public string lastLiftId;
        public string lastServiceId;
        public int decisions;
        public int intentChanges;
        public int completedActivities;
        public float intentStrength01;
        public int startDay;
        public float startHour;
        public int lastUpdateDay;
        public float lastUpdateHour;
    }

    /// <summary>
    /// Gives each real management visitor a lightweight goal and journey memory.
    /// Intent influences choices but never teleports or creates visitors. The player
    /// simulation is deliberately outside this system.
    /// </summary>
    public sealed class VisitorIntentRuntime : MonoBehaviour
    {
        public static VisitorIntentRuntime I { get; private set; }
        [SerializeField] private float refreshSeconds = 3f;
        [SerializeField, Range(0f, 1f)] private float intentPersistence = .82f;
        [SerializeField, Range(0f, 1f)] private float intentChangePressure = .72f;

        private readonly Dictionary<SkierRuntime, VisitorJourneySnapshot> journeys =
            new Dictionary<SkierRuntime, VisitorJourneySnapshot>();
        private float timer;

        public IReadOnlyDictionary<SkierRuntime, VisitorJourneySnapshot> Journeys => journeys;
        public event Action Changed;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start() => Refresh();

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < Mathf.Max(.5f, refreshSeconds)) return;
            timer = 0f;
            Refresh();
        }

        public VisitorJourneySnapshot GetJourney(SkierRuntime skier)
        {
            EnsureJourney(skier);
            return skier != null && journeys.TryGetValue(skier, out var snapshot)
                ? snapshot : default;
        }

        public VisitorIntent GetIntent(SkierRuntime skier)
        {
            return GetJourney(skier).intent;
        }

        public void SetStage(SkierRuntime skier, VisitorJourneyStage stage, string targetId = null)
        {
            if (skier == null) return;
            EnsureJourney(skier);
            var snapshot = journeys[skier];
            snapshot.stage = stage;
            if (!string.IsNullOrEmpty(targetId)) snapshot.currentTargetId = targetId;
            UpdateTime(ref snapshot);
            journeys[skier] = snapshot;
            Changed?.Invoke();
        }

        public void RegisterSlopeChoice(SkierRuntime skier, SlopeRuntime slope)
        {
            if (skier == null) return;
            EnsureJourney(skier);
            var snapshot = journeys[skier];
            snapshot.lastSlopeId = slope != null ? slope.name : null;
            snapshot.currentTargetId = snapshot.lastSlopeId;
            snapshot.decisions++;
            snapshot.stage = VisitorJourneyStage.Skiing;
            UpdateTime(ref snapshot);
            journeys[skier] = snapshot;
            Changed?.Invoke();
        }

        public void RegisterLiftChoice(SkierRuntime skier, LiftRuntime lift)
        {
            if (skier == null) return;
            EnsureJourney(skier);
            var snapshot = journeys[skier];
            snapshot.lastLiftId = lift != null ? lift.name : null;
            snapshot.currentTargetId = snapshot.lastLiftId;
            snapshot.decisions++;
            snapshot.stage = VisitorJourneyStage.Traveling;
            UpdateTime(ref snapshot);
            journeys[skier] = snapshot;
            Changed?.Invoke();
        }

        public void RegisterServiceChoice(SkierRuntime skier, string serviceId)
        {
            if (skier == null) return;
            EnsureJourney(skier);
            var snapshot = journeys[skier];
            snapshot.lastServiceId = serviceId;
            snapshot.currentTargetId = serviceId;
            snapshot.decisions++;
            snapshot.completedActivities++;
            snapshot.stage = VisitorJourneyStage.Service;
            UpdateTime(ref snapshot);
            journeys[skier] = snapshot;
            Changed?.Invoke();
        }

        public void RegisterSlopeCompleted(SkierRuntime skier, SlopeRuntime slope)
        {
            if (skier == null) return;
            EnsureJourney(skier);
            var snapshot = journeys[skier];
            if (slope != null) snapshot.lastSlopeId = slope.name;
            snapshot.completedActivities++;
            snapshot.stage = VisitorJourneyStage.Seeking;
            snapshot.currentTargetId = null;
            UpdateTime(ref snapshot);
            journeys[skier] = snapshot;
            Changed?.Invoke();
        }

        /// <summary>Returns a signed intent preference used by VisitorBehaviorRuntime.</summary>
        public float ScoreSlopeIntent(SkierRuntime skier, SlopeRuntime slope)
        {
            if (skier == null || slope == null) return 0f;
            var intent = GetIntent(skier);
            float difficulty = Difficulty01(slope.Difficulty);
            float pressure = InfrastructureCongestionRuntime.I != null
                ? InfrastructureCongestionRuntime.I.GetSlopePressure01(slope) : 0f;
            switch (intent)
            {
                case VisitorIntent.FamilyDay:
                    return (1f - difficulty) * .65f - pressure * .20f;
                case VisitorIntent.SportPerformance:
                    return difficulty * .75f - pressure * .15f;
                case VisitorIntent.Training:
                    return difficulty * .45f + Mathf.Clamp01(slope.Length / 2500f) * .20f - pressure * .10f;
                case VisitorIntent.MountainDiscovery:
                    return Mathf.Clamp01(slope.Length / 3000f) * .35f - pressure * .10f;
                case VisitorIntent.Relaxation:
                    return (1f - difficulty) * .30f - pressure * .35f;
                case VisitorIntent.PremiumExperience:
                    return (1f - pressure) * .25f + Mathf.Clamp01(slope.Length / 3500f) * .20f;
                default:
                    return Mathf.Clamp01(.55f - Mathf.Abs(difficulty - .55f)) * .25f - pressure * .12f;
            }
        }

        public float ScoreLiftIntent(SkierRuntime skier, LiftRuntime lift)
        {
            if (skier == null || lift == null) return 0f;
            float pressure = InfrastructureCongestionRuntime.I != null
                ? InfrastructureCongestionRuntime.I.GetLiftPressure01(lift) : 0f;
            float speed = Mathf.Clamp01(lift.EffectiveSpeedKmh / 30f);
            switch (GetIntent(skier))
            {
                case VisitorIntent.SportPerformance:
                case VisitorIntent.Training:
                    return speed * .40f - pressure * .15f;
                case VisitorIntent.FamilyDay:
                case VisitorIntent.Relaxation:
                    return (1f - pressure) * .30f + (lift.Definition != null && lift.Definition.detachable ? .08f : 0f);
                case VisitorIntent.PremiumExperience:
                    return speed * .20f + (lift.Definition != null && lift.Definition.detachable ? .18f : 0f) - pressure * .25f;
                case VisitorIntent.MountainDiscovery:
                    return speed * .15f - pressure * .10f;
                default:
                    return speed * .12f - pressure * .12f;
            }
        }

        public float ScoreServiceIntent(SkierRuntime skier, LeisureFacilityRuntime facility)
        {
            if (skier == null || facility == null || facility.definition == null) return 0f;
            float pressure = facility.Occupancy / Mathf.Max(1f, facility.definition.capacity);
            switch (GetIntent(skier))
            {
                case VisitorIntent.FamilyDay:
                    return (facility.definition.familyFriendly ? .55f : -.25f) - pressure * .35f;
                case VisitorIntent.PremiumExperience:
                    return (1f - pressure) * .35f + (facility.definition.familyFriendly ? .05f : .15f);
                case VisitorIntent.ApresSki:
                    return (facility.definition.type == LeisureFacilityType.IceRink || facility.definition.type == LeisureFacilityType.Other ? .20f : .05f) - pressure * .25f;
                case VisitorIntent.Relaxation:
                    return (1f - pressure) * .45f;
                case VisitorIntent.MountainDiscovery:
                    return (1f - pressure) * .20f + .05f;
                default:
                    return (1f - pressure) * .12f;
            }
        }

        public void RegisterActivityChoice(SkierRuntime skier, MountainActivityRuntime activity)
        {
            if (skier == null || activity == null) return;
            SetStage(skier, VisitorJourneyStage.Resting, activity.name);
        }

        public float ScoreActivityIntent(SkierRuntime skier, MountainActivityRuntime activity)
        {
            if (skier == null || activity == null || activity.definition == null) return 0f;
            float pressure = activity.CurrentOccupancy / Mathf.Max(1f, activity.definition.capacity);
            switch (GetIntent(skier))
            {
                case VisitorIntent.FamilyDay:
                    return IsFamilyActivity(activity.definition.type) ? .55f : -.15f;
                case VisitorIntent.MountainDiscovery:
                    return (activity.definition.type == MountainActivityType.Viewpoint ||
                            activity.definition.type == MountainActivityType.Paragliding ||
                            activity.definition.type == MountainActivityType.Zipline) ? .50f : .15f;
                case VisitorIntent.Relaxation:
                    return (activity.definition.type == MountainActivityType.MountainRestaurant ||
                            activity.definition.type == MountainActivityType.Refuge ||
                            activity.definition.type == MountainActivityType.Viewpoint) ? .50f : .05f;
                case VisitorIntent.ApresSki:
                    return (activity.definition.type == MountainActivityType.Concert ||
                            activity.definition.type == MountainActivityType.FestivalStage) ? .65f : .10f;
                case VisitorIntent.SportPerformance:
                case VisitorIntent.Training:
                    return (activity.definition.type == MountainActivityType.Snowpark ||
                            activity.definition.type == MountainActivityType.Snowmobile) ? .45f : .05f;
                case VisitorIntent.PremiumExperience:
                    return (activity.definition.type == MountainActivityType.MountainRestaurant ||
                            activity.definition.type == MountainActivityType.Paragliding ||
                            activity.definition.type == MountainActivityType.Zipline) ? .45f : .12f;
                default:
                    return .10f;
            }
            // Pressure is deliberately applied after intent matching so a popular
            // activity can still be selected when its intent fit is exceptional.
        }

        private void Refresh()
        {
            var skiers = FindObjectsOfType<SkierRuntime>();
            var live = new HashSet<SkierRuntime>(skiers);
            bool changed = false;
            foreach (var skier in skiers)
            {
                if (skier == null || skier.State == SkierState.Leaving) continue;
                if (!journeys.ContainsKey(skier))
                {
                    CreateJourney(skier);
                    changed = true;
                }
                else
                {
                    var snapshot = journeys[skier];
                    float pressure = CurrentChangePressure(skier, snapshot);
                    if (pressure > intentChangePressure && UnityEngine.Random.value > intentPersistence)
                    {
                        var next = ChooseIntent(skier);
                        if (next != snapshot.intent)
                        {
                            snapshot.intent = next;
                            snapshot.intentChanges++;
                            snapshot.intentStrength01 = Mathf.Clamp01(.75f + UnityEngine.Random.value * .25f);
                            changed = true;
                        }
                    }
                    UpdateTime(ref snapshot);
                    journeys[skier] = snapshot;
                }
            }

            var dead = new List<SkierRuntime>();
            foreach (var pair in journeys)
                if (pair.Key == null || !live.Contains(pair.Key)) dead.Add(pair.Key);
            foreach (var key in dead) journeys.Remove(key);
            if (changed) Changed?.Invoke();
        }

        private void EnsureJourney(SkierRuntime skier)
        {
            if (skier == null || skier.State == SkierState.Leaving) return;
            if (!journeys.ContainsKey(skier))
            {
                CreateJourney(skier);
                Changed?.Invoke();
            }
        }

        private void CreateJourney(SkierRuntime skier)
        {
            int day = StationRuntime.I != null ? StationRuntime.I.State.day : 1;
            float hour = GameClock.I != null ? GameClock.I.CurrentHour : 6f;
            journeys[skier] = new VisitorJourneySnapshot
            {
                intent = ChooseIntent(skier),
                stage = VisitorJourneyStage.Arrived,
                currentTargetId = null,
                decisions = 0,
                intentChanges = 0,
                completedActivities = 0,
                intentStrength01 = .85f,
                startDay = day,
                startHour = hour,
                lastUpdateDay = day,
                lastUpdateHour = hour
            };
        }

        private VisitorIntent ChooseIntent(SkierRuntime skier)
        {
            CustomerProfile profile = skier != null && skier.PopulationProfile != null
                ? skier.PopulationProfile.ToCustomerProfile() : CustomerProfile.Tourist;
            switch (profile)
            {
                case CustomerProfile.Family: return VisitorIntent.FamilyDay;
                case CustomerProfile.Athlete: return UnityEngine.Random.value < .65f ? VisitorIntent.SportPerformance : VisitorIntent.Training;
                case CustomerProfile.Expert:
                case CustomerProfile.Advanced: return VisitorIntent.SportPerformance;
                case CustomerProfile.Premium: return VisitorIntent.PremiumExperience;
                case CustomerProfile.Beginner: return UnityEngine.Random.value < .65f ? VisitorIntent.SkiDay : VisitorIntent.MountainDiscovery;
                case CustomerProfile.Youth: return UnityEngine.Random.value < .60f ? VisitorIntent.FamilyDay : VisitorIntent.SportPerformance;
                case CustomerProfile.Intermediate: return UnityEngine.Random.value < .55f ? VisitorIntent.SkiDay : VisitorIntent.MountainDiscovery;
                default:
                    float r = UnityEngine.Random.value;
                    if (r < .35f) return VisitorIntent.SkiDay;
                    if (r < .55f) return VisitorIntent.MountainDiscovery;
                    if (r < .72f) return VisitorIntent.Relaxation;
                    return VisitorIntent.ApresSki;
            }
        }

        private float CurrentChangePressure(SkierRuntime skier, VisitorJourneySnapshot snapshot)
        {
            float pressure = 0f;
            if (skier != null && skier.State == SkierState.Resting) pressure += .45f;
            if (InfrastructureCongestionRuntime.I != null) pressure += InfrastructureCongestionRuntime.I.PeakPressure01 * .55f;
            if (GameClock.I != null && GameClock.I.CurrentHour >= 16f) pressure += .18f;
            return Mathf.Clamp01(pressure);
        }

        private static void UpdateTime(ref VisitorJourneySnapshot snapshot)
        {
            snapshot.lastUpdateDay = StationRuntime.I != null ? StationRuntime.I.State.day : snapshot.lastUpdateDay;
            snapshot.lastUpdateHour = GameClock.I != null ? GameClock.I.CurrentHour : snapshot.lastUpdateHour;
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

        private static bool IsFamilyActivity(MountainActivityType type)
        {
            return type == MountainActivityType.Viewpoint || type == MountainActivityType.MountainRestaurant ||
                   type == MountainActivityType.Dogsled || type == MountainActivityType.Zipline;
        }
    }
}
