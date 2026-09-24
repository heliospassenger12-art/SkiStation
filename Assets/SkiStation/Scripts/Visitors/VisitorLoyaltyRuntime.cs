using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    [Serializable]
    public struct VisitorLoyaltySnapshot
    {
        [Range(0f, 1f)] public float familiarity01;
        [Range(0f, 1f)] public float loyalty01;
        [Range(0f, 1f)] public float returnIntent01;
        public string memoryKey;
        public string favoriteSlopeId;
        public string favoriteLiftId;
        public string favoriteServiceId;
        public int decisions;
        public int repeatedChoices;
        public int positiveExperiences;
        public int negativeExperiences;
        public int visitsThisSession;
        public float averageExperience;
    }

    [Serializable]
    public struct VisitorCohortLoyaltySnapshot
    {
        public string key;
        [Range(0f, 1f)] public float loyalty01;
        [Range(0f, 1f)] public float returnIntent01;
        public int visits;
        public float averageSatisfaction;
        public string favoriteSlopeId;
        public string favoriteServiceId;
    }

    /// <summary>
    /// Builds lightweight habits, familiarity and return intent for real management visitors.
    /// FamilyId is used when available so a family can accumulate a shared memory across visits.
    /// Player simulation is deliberately excluded.
    /// </summary>
    public sealed class VisitorLoyaltyRuntime : MonoBehaviour
    {
        public static VisitorLoyaltyRuntime I { get; private set; }

        [SerializeField] private float refreshSeconds = 4f;
        [SerializeField, Range(0f, 1f)] private float satisfactionLearning = .035f;
        [SerializeField, Range(0f, 1f)] private float familiarityPerDecision = .025f;
        [SerializeField, Range(0f, 1f)] private float repeatChoiceBonus = .035f;
        [SerializeField, Range(0f, 1f)] private float loyaltyMemoryRate = .08f;

        private readonly Dictionary<SkierRuntime, VisitorLoyaltySnapshot> live =
            new Dictionary<SkierRuntime, VisitorLoyaltySnapshot>();
        private readonly Dictionary<string, VisitorCohortLoyaltySnapshot> cohortMemory =
            new Dictionary<string, VisitorCohortLoyaltySnapshot>();
        private readonly Dictionary<SkierRuntime, int> slopeChoiceCounts = new Dictionary<SkierRuntime, int>();
        private readonly Dictionary<SkierRuntime, int> serviceChoiceCounts = new Dictionary<SkierRuntime, int>();
        private float timer;

        public IReadOnlyDictionary<SkierRuntime, VisitorLoyaltySnapshot> Live => live;
        public IReadOnlyDictionary<string, VisitorCohortLoyaltySnapshot> Cohorts => cohortMemory;
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

        public VisitorLoyaltySnapshot Get(SkierRuntime skier)
        {
            if (skier == null) return default;
            if (!live.TryGetValue(skier, out var snapshot))
            {
                CreateLive(skier);
                snapshot = live[skier];
            }
            return snapshot;
        }

        public VisitorCohortLoyaltySnapshot GetCohort(string key)
        {
            if (string.IsNullOrEmpty(key)) return default;
            return cohortMemory.TryGetValue(key, out var snapshot) ? snapshot : default;
        }

        public void RegisterSlopeChoice(SkierRuntime skier, SlopeRuntime slope)
        {
            if (skier == null || slope == null) return;
            Ensure(skier);
            var snapshot = live[skier];
            snapshot.decisions++;
            int count = slopeChoiceCounts.TryGetValue(skier, out var old) ? old + 1 : 1;
            slopeChoiceCounts[skier] = count;
            if (count > 1) snapshot.repeatedChoices++;
            snapshot.favoriteSlopeId = PickFavorite(snapshot.favoriteSlopeId, slope.name, count);
            snapshot.familiarity01 = Mathf.Clamp01(snapshot.familiarity01 + familiarityPerDecision);
            snapshot.loyalty01 = Mathf.Clamp01(snapshot.loyalty01 + (count > 1 ? repeatChoiceBonus : repeatChoiceBonus * .25f));
            live[skier] = snapshot;
            Changed?.Invoke();
        }

        public void RegisterLiftChoice(SkierRuntime skier, LiftRuntime lift)
        {
            if (skier == null || lift == null) return;
            Ensure(skier);
            var snapshot = live[skier];
            snapshot.decisions++;
            snapshot.favoriteLiftId = lift.name;
            snapshot.familiarity01 = Mathf.Clamp01(snapshot.familiarity01 + familiarityPerDecision * .7f);
            live[skier] = snapshot;
        }

        public void RegisterServiceChoice(SkierRuntime skier, string serviceId)
        {
            if (skier == null || string.IsNullOrEmpty(serviceId)) return;
            Ensure(skier);
            var snapshot = live[skier];
            snapshot.decisions++;
            int count = serviceChoiceCounts.TryGetValue(skier, out var old) ? old + 1 : 1;
            serviceChoiceCounts[skier] = count;
            if (count > 1) snapshot.repeatedChoices++;
            snapshot.favoriteServiceId = PickFavorite(snapshot.favoriteServiceId, serviceId, count);
            snapshot.familiarity01 = Mathf.Clamp01(snapshot.familiarity01 + familiarityPerDecision);
            live[skier] = snapshot;
        }

        public void RecordExperience(SkierRuntime skier, float experience01)
        {
            if (skier == null || skier.State == SkierState.Leaving) return;
            Ensure(skier);
            var snapshot = live[skier];
            float target = Mathf.Clamp01(experience01);
            float previous = Mathf.Clamp01(snapshot.averageExperience / 100f);
            if (snapshot.averageExperience <= 0f) previous = target;
            else previous = Mathf.Lerp(previous, target, .18f);
            snapshot.averageExperience = previous * 100f;
            if (target >= .70f) snapshot.positiveExperiences++;
            if (target <= .45f) snapshot.negativeExperiences++;
            float satisfactionDelta = (target - .60f) * satisfactionLearning;
            snapshot.loyalty01 = Mathf.Clamp01(snapshot.loyalty01 + satisfactionDelta);
            snapshot.returnIntent01 = CalculateReturnIntent(snapshot);
            live[skier] = snapshot;
            Changed?.Invoke();
        }

        public void RegisterDeparture(SkierRuntime skier)
        {
            if (skier == null || !live.TryGetValue(skier, out var snapshot)) return;
            snapshot.visitsThisSession = Mathf.Max(1, snapshot.visitsThisSession);
            snapshot.returnIntent01 = CalculateReturnIntent(snapshot);
            string key = snapshot.memoryKey;
            if (!string.IsNullOrEmpty(key)) MergeCohort(key, snapshot);
            live.Remove(skier);
            slopeChoiceCounts.Remove(skier);
            serviceChoiceCounts.Remove(skier);
            Changed?.Invoke();
        }

        private void Refresh()
        {
            var skiers = FindObjectsOfType<SkierRuntime>();
            var active = new HashSet<SkierRuntime>();
            foreach (var skier in skiers)
            {
                if (skier == null) continue;
                if (skier.State == SkierState.Leaving)
                {
                    RegisterDeparture(skier);
                    continue;
                }
                active.Add(skier);
                Ensure(skier);
                var snapshot = live[skier];
                snapshot.returnIntent01 = CalculateReturnIntent(snapshot);
                live[skier] = snapshot;
            }

            var dead = new List<SkierRuntime>();
            foreach (var pair in live)
                if (pair.Key == null || !active.Contains(pair.Key)) dead.Add(pair.Key);
            foreach (var skier in dead)
            {
                if (skier != null) RegisterDeparture(skier);
                else live.Remove(skier);
            }
            Changed?.Invoke();
        }

        private void Ensure(SkierRuntime skier)
        {
            if (!live.ContainsKey(skier)) CreateLive(skier);
        }

        private void CreateLive(SkierRuntime skier)
        {
            string key = BuildMemoryKey(skier);
            float inheritedLoyalty = 0f;
            float inheritedReturn = .5f;
            int inheritedVisits = 0;
            if (!string.IsNullOrEmpty(key) && cohortMemory.TryGetValue(key, out var cohort))
            {
                inheritedLoyalty = cohort.loyalty01;
                inheritedReturn = cohort.returnIntent01;
                inheritedVisits = cohort.visits;
            }

            live[skier] = new VisitorLoyaltySnapshot
            {
                memoryKey = key,
                familiarity01 = Mathf.Clamp01(inheritedVisits * .08f),
                loyalty01 = Mathf.Clamp01(inheritedLoyalty + .08f),
                returnIntent01 = Mathf.Clamp01(inheritedReturn),
                decisions = 0,
                repeatedChoices = 0,
                positiveExperiences = 0,
                negativeExperiences = 0,
                visitsThisSession = 1,
                averageExperience = 0f
            };
        }

        private void MergeCohort(string key, VisitorLoyaltySnapshot visitor)
        {
            if (!cohortMemory.TryGetValue(key, out var cohort))
            {
                cohort = new VisitorCohortLoyaltySnapshot
                {
                    key = key,
                    loyalty01 = visitor.loyalty01,
                    returnIntent01 = visitor.returnIntent01,
                    visits = 0,
                    averageSatisfaction = visitor.averageExperience,
                    favoriteSlopeId = visitor.favoriteSlopeId,
                    favoriteServiceId = visitor.favoriteServiceId
                };
            }

            cohort.visits++;
            cohort.loyalty01 = Mathf.Clamp01(Mathf.Lerp(cohort.loyalty01, visitor.loyalty01, loyaltyMemoryRate));
            cohort.returnIntent01 = Mathf.Clamp01(Mathf.Lerp(cohort.returnIntent01, visitor.returnIntent01, loyaltyMemoryRate));
            if (visitor.averageExperience > 0f)
                cohort.averageSatisfaction = cohort.visits == 1
                    ? visitor.averageExperience
                    : Mathf.Lerp(cohort.averageSatisfaction, visitor.averageExperience, loyaltyMemoryRate);
            if (!string.IsNullOrEmpty(visitor.favoriteSlopeId)) cohort.favoriteSlopeId = visitor.favoriteSlopeId;
            if (!string.IsNullOrEmpty(visitor.favoriteServiceId)) cohort.favoriteServiceId = visitor.favoriteServiceId;
            cohortMemory[key] = cohort;
        }

        private static string BuildMemoryKey(SkierRuntime skier)
        {
            var profile = skier != null ? skier.PopulationProfile : null;
            if (profile != null && !string.IsNullOrEmpty(profile.FamilyId))
                return "family:" + profile.FamilyId;
            return skier != null ? "visit:" + skier.GetInstanceID() : null;
        }

        private static string PickFavorite(string current, string candidate, int count)
        {
            if (string.IsNullOrEmpty(current)) return candidate;
            if (count >= 2) return candidate;
            return current;
        }

        private static float CalculateReturnIntent(VisitorLoyaltySnapshot snapshot)
        {
            float experience = snapshot.averageExperience > 0f ? snapshot.averageExperience / 100f : .70f;
            float familiarity = snapshot.familiarity01;
            float loyalty = snapshot.loyalty01;
            float repeat = Mathf.Clamp01(snapshot.repeatedChoices / 6f);
            return Mathf.Clamp01(experience * .48f + loyalty * .32f + familiarity * .12f + repeat * .08f);
        }
    }
}
