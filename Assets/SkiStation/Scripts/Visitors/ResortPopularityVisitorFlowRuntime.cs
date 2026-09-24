using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    [Serializable]
    public struct VisitorDemandSnapshot
    {
        public CustomerProfile profile;
        public float reputation01;
        public float demandIndex;
        public int desiredVisitors;
        public int capacityAdjustedVisitors;
        public string limitingFactor;
    }

    [Serializable]
    public struct ResortPopularitySnapshot
    {
        public float popularity01;
        public int desiredDailyVisitors;
        public int sustainableDailyVisitors;
        public float domainPressure01;
        public int accommodationCapacity;
        public string limitingFactor;
    }

    /// <summary>
    /// 1.89 - Converts long-term reputation into visitor demand without directly
    /// spawning visitors. Existing population/lifecycle systems remain authoritative.
    /// This layer provides a stable demand target that future arrival systems can consume.
    /// </summary>
    public sealed class ResortPopularityVisitorFlowRuntime : MonoBehaviour
    {
        public static ResortPopularityVisitorFlowRuntime I { get; private set; }
        [SerializeField, Min(10)] private int baselineDailyVisitors = 180;
        [SerializeField, Min(20)] private int minimumDomainDailyCapacity = 300;
        [SerializeField, Min(2f)] private float refreshSeconds = 10f;
        [SerializeField, Range(.1f, 1f)] private float saturationProtection = .82f;
        private readonly Dictionary<CustomerProfile, VisitorDemandSnapshot> demand = new Dictionary<CustomerProfile, VisitorDemandSnapshot>();
        private float timer;

        public ResortPopularitySnapshot Snapshot { get; private set; }
        public IReadOnlyDictionary<CustomerProfile, VisitorDemandSnapshot> Demand => demand;
        public event Action Changed;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
        }

        private void Start() { Recalculate(); }
        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < Mathf.Max(2f, refreshSeconds)) return;
            timer = 0f; Recalculate();
        }

        public bool TryGet(CustomerProfile profile, out VisitorDemandSnapshot value) { return demand.TryGetValue(profile, out value); }

        public void Recalculate()
        {
            ResortReputationLoyaltyRuntime reputation = ResortReputationLoyaltyRuntime.I;
            DomainCapacityRuntime domain = DomainCapacityRuntime.I;
            float pressure = domain != null ? domain.DomainPressure01 : 0f;
            int accommodation = GetAccommodationCapacity();
            int domainDaily = EstimateDomainDailyCapacity(domain);
            int sustainable = Mathf.Max(20, Mathf.Min(domainDaily, accommodation > 0 ? accommodation : domainDaily));

            float repSum = 0f; int repCount = 0; int desiredTotal = 0;
            demand.Clear();
            foreach (CustomerProfile profile in Enum.GetValues(typeof(CustomerProfile)))
            {
                ResortReputationSnapshot r;
                float rep = reputation != null && reputation.TryGet(profile, out r) ? r.reputation01 : .55f;
                float loyalty = reputation != null && reputation.TryGet(profile, out r) ? r.loyalty01 : .40f;
                float word = reputation != null && reputation.TryGet(profile, out r) ? r.wordOfMouth01 : .50f;
                float fit = ProfileDemandBias(profile);
                float index = Mathf.Clamp(.35f + rep * .85f + loyalty * .30f + word * .25f + fit, .20f, 1.85f);
                int desired = Mathf.RoundToInt((baselineDailyVisitors / 9f) * index);
                desiredTotal += desired; repSum += rep; repCount++;
                demand[profile] = new VisitorDemandSnapshot { profile=profile, reputation01=rep, demandIndex=index, desiredVisitors=desired, capacityAdjustedVisitors=desired, limitingFactor="-" };
            }

            int protectedCapacity = Mathf.Max(20, Mathf.RoundToInt(sustainable * saturationProtection));
            float scale = desiredTotal > protectedCapacity ? protectedCapacity / (float)Mathf.Max(1, desiredTotal) : 1f;
            string limit = DetermineLimit(domainDaily, accommodation, desiredTotal, protectedCapacity, pressure);
            var keys = new List<CustomerProfile>(demand.Keys);
            foreach (CustomerProfile p in keys)
            {
                VisitorDemandSnapshot d = demand[p];
                d.capacityAdjustedVisitors = Mathf.Max(0, Mathf.RoundToInt(d.desiredVisitors * scale));
                d.limitingFactor = scale < .999f ? limit : "-";
                demand[p] = d;
            }

            Snapshot = new ResortPopularitySnapshot {
                popularity01 = repCount > 0 ? Mathf.Clamp01(repSum / repCount) : .5f,
                desiredDailyVisitors = desiredTotal,
                sustainableDailyVisitors = Mathf.Min(desiredTotal, protectedCapacity),
                domainPressure01 = pressure,
                accommodationCapacity = accommodation,
                limitingFactor = scale < .999f ? limit : "Aucune limite majeure"
            };
            Changed?.Invoke();
        }

        private int GetAccommodationCapacity()
        {
            int total = 0;
            foreach (StationServiceRuntime service in StationServiceRuntime.All)
                if (service != null && service.definition != null && service.definition.type == StationServiceType.Accommodation)
                    total += Mathf.Max(0, service.EffectiveCapacity);
            return total;
        }

        private int EstimateDomainDailyCapacity(DomainCapacityRuntime domain)
        {
            if (domain == null || domain.Snapshot.effectiveCapacityPerHour <= 0f) return minimumDomainDailyCapacity;
            return Mathf.Max(minimumDomainDailyCapacity, Mathf.RoundToInt(domain.Snapshot.effectiveCapacityPerHour * 2.2f));
        }

        private static string DetermineLimit(int domainCapacity, int accommodation, int desired, int protectedCapacity, float pressure)
        {
            if (pressure >= .88f) return "Domaine skiable sous pression";
            if (accommodation > 0 && accommodation <= domainCapacity && desired > protectedCapacity) return "Hebergements";
            if (desired > protectedCapacity) return "Capacite du domaine";
            return "-";
        }

        private static float ProfileDemandBias(CustomerProfile p)
        {
            switch (p)
            {
                case CustomerProfile.Family: return .10f;
                case CustomerProfile.Tourist: return .08f;
                case CustomerProfile.Beginner: return .04f;
                case CustomerProfile.Athlete: return -.10f;
                case CustomerProfile.Expert: return -.04f;
                case CustomerProfile.Premium: return -.06f;
                default: return 0f;
            }
        }
    }

    public static class ResortPopularityVisitorFlowBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (ResortPopularityVisitorFlowRuntime.I != null) return;
            new GameObject("Resort Popularity & Visitor Flow 1.89").AddComponent<ResortPopularityVisitorFlowRuntime>();
        }
    }
}
