using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum VisitorServiceNeed { None, Equipment, Food, Drink, Lesson, Leisure, Shopping, Accommodation }

    [Serializable]
    public struct VisitorServicePlanSnapshot
    {
        public VisitorServiceNeed need;
        public string targetId;
        public float score;
        public float plannedAtHour;
        public bool travelling;
        public bool inService;
        public int decisions;
    }

    /// <summary>Turns the 1.29 daily intention into concrete service destinations for real visitors.</summary>
    public sealed class VisitorServicePlannerRuntime : MonoBehaviour
    {
        public static VisitorServicePlannerRuntime I { get; private set; }
        [SerializeField] private float refreshSeconds = 3f;
        [SerializeField] private float minimumScore = .28f;
        [SerializeField] private float decisionCooldownMinutes = 18f;
        [SerializeField] private float needPressurePerHour = .045f;
        private readonly Dictionary<SkierRuntime, VisitorServicePlanSnapshot> plans = new();
        private readonly Dictionary<SkierRuntime, float> lastDecisionHour = new();
        private float timer;
        public IReadOnlyDictionary<SkierRuntime, VisitorServicePlanSnapshot> Plans => plans;
        public event Action Changed;

        private void Awake() { if (I != null && I != this) { Destroy(gameObject); return; } I = this; DontDestroyOnLoad(gameObject); }
        private void Start() => Refresh();
        private void Update() { timer += Time.unscaledDeltaTime; if (timer >= Mathf.Max(.5f, refreshSeconds)) { timer = 0f; Refresh(); } TickArrivals(); }

        public VisitorServicePlanSnapshot GetPlan(SkierRuntime skier) { if (skier != null && !plans.ContainsKey(skier)) Decide(skier, true); return skier != null && plans.TryGetValue(skier, out var p) ? p : default; }

        private void Refresh()
        {
            var live = new HashSet<SkierRuntime>(FindObjectsOfType<SkierRuntime>());
            foreach (var skier in live)
            {
                if (skier == null || skier.State == SkierState.Leaving || skier.State == SkierState.RidingLift || skier.State == SkierState.WaitingForLift || skier.State == SkierState.Skiing) continue;
                if (!plans.ContainsKey(skier) || ShouldReplan(skier)) Decide(skier, false);
            }
            var dead = new List<SkierRuntime>(); foreach (var pair in plans) if (!live.Contains(pair.Key)) dead.Add(pair.Key); foreach (var s in dead) { plans.Remove(s); lastDecisionHour.Remove(s); }
            Changed?.Invoke();
        }

        private bool ShouldReplan(SkierRuntime skier)
        {
            if (skier == null || !plans.TryGetValue(skier, out var p)) return true;
            if (p.inService || p.travelling) return false;
            float now = GameClock.I != null ? GameClock.I.CurrentHour : 0f;
            if (!lastDecisionHour.TryGetValue(skier, out var h)) return true;
            float elapsed = now - h; if (elapsed < 0f) elapsed += 24f;
            return elapsed * 60f >= decisionCooldownMinutes;
        }

        private void Decide(SkierRuntime skier, bool force)
        {
            if (skier == null) return;
            VisitorIntent intent = VisitorIntentRuntime.I != null ? VisitorIntentRuntime.I.GetIntent(skier) : VisitorIntent.SkiDay;
            CustomerProfile profile = skier.PopulationProfile != null ? skier.PopulationProfile.ToCustomerProfile() : CustomerProfile.Tourist;
            VisitorPlanActivity planActivity = VisitorDayPlanRuntime.I != null ? VisitorDayPlanRuntime.I.GetPlan(skier).next : VisitorPlanActivity.Ski;
            VisitorServiceNeed need = DetermineNeed(skier, intent, planActivity);
            StationServiceRuntime best = null; float bestScore = float.MinValue;

            for (int i = 0; i < StationServiceRuntime.All.Count; i++)
            {
                var service = StationServiceRuntime.All[i];
                if (service == null || service.definition == null || !service.IsOpen()) continue;
                if (service.QueueCount >= service.definition.maxQueue && service.AvailableCapacity <= 0) continue;
                float score = Score(service, skier, profile, intent, need, planActivity);
                if (score > bestScore) { bestScore = score; best = service; }
            }

            var snapshot = new VisitorServicePlanSnapshot
            { need = need, targetId = best != null ? GetId(best) : null, score = bestScore, plannedAtHour = GameClock.I != null ? GameClock.I.CurrentHour : 0f, travelling = false, inService = false, decisions = plans.TryGetValue(skier, out var old) ? old.decisions + 1 : 1 };
            plans[skier] = snapshot; lastDecisionHour[skier] = snapshot.plannedAtHour;

            if (best != null && bestScore >= minimumScore && !force)
            {
                float distance = Vector3.Distance(skier.transform.position, best.transform.position);
                if (distance <= best.definition.arrivalRadius) TryEnter(skier, best, ref snapshot);
                else { skier.MoveTo(best.transform.position); snapshot.travelling = true; plans[skier] = snapshot; }
            }
            Changed?.Invoke();
        }

        private void TickArrivals()
        {
            foreach (var pair in new List<KeyValuePair<SkierRuntime, VisitorServicePlanSnapshot>>(plans))
            {
                var skier = pair.Key; var p = pair.Value; if (skier == null || string.IsNullOrEmpty(p.targetId)) continue;
                StationServiceRuntime target = FindService(p.targetId); if (target == null) continue;
                if (p.inService)
                {
                    if (!target.Contains(skier)) { p.inService = false; p.travelling = false; plans[skier] = p; }
                    continue;
                }
                if (Vector3.Distance(skier.transform.position, target.transform.position) <= target.definition.arrivalRadius)
                {
                    TryEnter(skier, target, ref p); plans[skier] = p; Changed?.Invoke();
                }
            }
        }

        private void TryEnter(SkierRuntime skier, StationServiceRuntime service, ref VisitorServicePlanSnapshot snapshot)
        {
            if (service.Contains(skier)) { snapshot.inService = true; snapshot.travelling = false; return; }
            if (service.RequestService(skier)) { snapshot.inService = true; snapshot.travelling = false; }
        }

        private float Score(StationServiceRuntime service, SkierRuntime skier, CustomerProfile profile, VisitorIntent intent, VisitorServiceNeed need, VisitorPlanActivity planActivity)
        {
            var d = service.definition;
            float score = NeedFit(d.type, need) + PlanFit(d.type, planActivity);
            score += IntentFit(d.type, intent);
            score += ProfileFit(d, profile);
            if (d.type == StationServiceType.SkiSchool && skier.PopulationProfile != null)
            {
                if (skier.PopulationProfile.IsChild) score += .55f;
                else if (profile == CustomerProfile.Beginner || profile == CustomerProfile.Family) score += .22f;
            }
            score += (d.quality / 100f) * .55f;
            score += RangeFit(d.range, profile) * .35f;
            score -= service.QueuePressure01 * (1.0f + Comfort(profile) * .8f);
            float priceRatio = PricingRuntime.I != null ? PricingRuntime.I.GetPriceRatio(GetId(service), d.basePrice) : 1f;
            score -= Mathf.Max(0f, priceRatio - 1f) * (0.35f + Comfort(profile));
            float distanceKm = Vector3.Distance(skier.transform.position, service.transform.position) / 100f;
            score -= Mathf.Clamp01(distanceKm) * (0.15f + Comfort(profile) * .25f);
            // A small, bounded utility adjustment: this ranks accessible services only.
            // It never moves a visitor or bypasses the lift / pedestrian routing rules.
            score += VisitorChoiceUtility249.ServiceAdjustment(
                NeedFit(d.type, need), d.quality / 100f, service.QueuePressure01,
                Vector3.Distance(skier.transform.position, service.transform.position),
                priceRatio, Comfort(profile));
            if (d.staffAvailable < d.staffRequired) score -= .75f;
            if (VisitorGroupRuntime.I != null) score += .08f;
            return score;
        }

        private VisitorServiceNeed DetermineNeed(SkierRuntime skier, VisitorIntent intent, VisitorPlanActivity planActivity)
        {
            float hour = GameClock.I != null ? GameClock.I.CurrentHour : 12f;
            if (planActivity == VisitorPlanActivity.Shop) return VisitorServiceNeed.Shopping;
            if (planActivity == VisitorPlanActivity.Restaurant) return VisitorServiceNeed.Food;
            if (planActivity == VisitorPlanActivity.Leisure) return VisitorServiceNeed.Leisure;
            if (planActivity == VisitorPlanActivity.Rest && hour >= 16.5f) return VisitorServiceNeed.Drink;
            if (hour >= 11f && hour < 14.5f) return VisitorServiceNeed.Food;
            if (hour >= 19f && intent == VisitorIntent.ApresSki) return VisitorServiceNeed.Accommodation;
            if (hour >= 16.5f && intent == VisitorIntent.ApresSki) return VisitorServiceNeed.Drink;
            if (intent == VisitorIntent.Training || intent == VisitorIntent.SportPerformance) return VisitorServiceNeed.Lesson;
            if (intent == VisitorIntent.FamilyDay && skier != null && skier.PopulationProfile != null && skier.PopulationProfile.IsChild) return VisitorServiceNeed.Lesson;
            if (intent == VisitorIntent.FamilyDay || intent == VisitorIntent.Relaxation) return VisitorServiceNeed.Leisure;
            if (intent == VisitorIntent.PremiumExperience) return VisitorServiceNeed.Shopping;
            return VisitorServiceNeed.None;
        }

        private static float PlanFit(StationServiceType type, VisitorPlanActivity activity)
        {
            switch (activity)
            {
                case VisitorPlanActivity.Shop: return type == StationServiceType.Shop ? 1.0f : 0f;
                case VisitorPlanActivity.Restaurant: return type == StationServiceType.Restaurant ? 1.2f : type == StationServiceType.Bar ? .15f : 0f;
                case VisitorPlanActivity.Leisure: return type == StationServiceType.Activity ? 1.0f : type == StationServiceType.Restaurant ? .15f : 0f;
                case VisitorPlanActivity.Rest: return type == StationServiceType.Restaurant || type == StationServiceType.Bar || type == StationServiceType.Accommodation ? .25f : 0f;
                default: return 0f;
            }
        }

        private static float NeedFit(StationServiceType type, VisitorServiceNeed need)
        {
            if (need == VisitorServiceNeed.None) return .08f;
            if (need == VisitorServiceNeed.Food) return type == StationServiceType.Restaurant ? 1.2f : type == StationServiceType.Bar ? .25f : -.35f;
            if (need == VisitorServiceNeed.Drink) return type == StationServiceType.Bar ? 1.25f : type == StationServiceType.Restaurant ? .35f : -.25f;
            if (need == VisitorServiceNeed.Equipment) return type == StationServiceType.EquipmentRental ? 1.3f : -.35f;
            if (need == VisitorServiceNeed.Lesson) return type == StationServiceType.SkiSchool ? 1.25f : -.25f;
            if (need == VisitorServiceNeed.Leisure) return type == StationServiceType.Activity ? 1.05f : type == StationServiceType.Restaurant ? .25f : -.10f;
            if (need == VisitorServiceNeed.Shopping) return type == StationServiceType.Shop ? 1.0f : type == StationServiceType.Bar ? .15f : -.08f;
            return type == StationServiceType.Accommodation ? .7f : .05f;
        }

        private static float IntentFit(StationServiceType type, VisitorIntent intent)
        {
            switch (intent)
            {
                case VisitorIntent.FamilyDay: return type == StationServiceType.Restaurant || type == StationServiceType.Activity ? .45f : type == StationServiceType.SkiSchool ? .25f : 0f;
                case VisitorIntent.PremiumExperience: return type == StationServiceType.Bar || type == StationServiceType.Restaurant || type == StationServiceType.Shop || type == StationServiceType.Accommodation ? .5f : .08f;
                case VisitorIntent.ApresSki: return type == StationServiceType.Bar ? .8f : type == StationServiceType.Restaurant ? .3f : 0f;
                case VisitorIntent.SportPerformance:
                case VisitorIntent.Training: return type == StationServiceType.SkiSchool || type == StationServiceType.EquipmentRental ? .55f : type == StationServiceType.Restaurant ? .1f : 0f;
                case VisitorIntent.Relaxation: return type == StationServiceType.Restaurant || type == StationServiceType.Bar || type == StationServiceType.Activity ? .45f : 0f;
                default: return type == StationServiceType.Restaurant || type == StationServiceType.Shop ? .12f : .04f;
            }
        }

        private static float ProfileFit(StationServiceDefinition d, CustomerProfile p)
        {
            switch (p)
            {
                case CustomerProfile.Family: return d.familySpecialization + (d.familyFriendly ? .3f : 0f);
                case CustomerProfile.Beginner: return d.beginnerSpecialization;
                case CustomerProfile.Athlete: return d.sportSpecialization;
                case CustomerProfile.Premium: return d.premiumSpecialization;
                case CustomerProfile.Expert: return d.expertSpecialization;
                case CustomerProfile.Youth: return d.youthSpecialization;
                default: return .08f;
            }
        }

        private static float RangeFit(ServiceRange r, CustomerProfile p)
        {
            if (p == CustomerProfile.Premium) return r == ServiceRange.Premium || r == ServiceRange.Luxury ? 1f : r == ServiceRange.Standard ? .2f : -.4f;
            if (p == CustomerProfile.Expert || p == CustomerProfile.Athlete) return r == ServiceRange.Pro || r == ServiceRange.Expert ? 1f : r == ServiceRange.Standard ? .2f : 0f;
            return r == ServiceRange.Economy || r == ServiceRange.Standard ? .65f : r == ServiceRange.Premium ? .25f : -.15f;
        }

        private static float Comfort(CustomerProfile p) => p == CustomerProfile.Premium || p == CustomerProfile.Family ? .8f : p == CustomerProfile.Beginner ? .55f : .25f;
        private static string GetId(StationServiceRuntime s) => s.definition != null && !string.IsNullOrWhiteSpace(s.definition.serviceId) ? s.definition.serviceId : s.gameObject.GetInstanceID().ToString();
        private static StationServiceRuntime FindService(string id) { for (int i = 0; i < StationServiceRuntime.All.Count; i++) if (StationServiceRuntime.All[i] != null && GetId(StationServiceRuntime.All[i]) == id) return StationServiceRuntime.All[i]; return null; }
    }
}
