using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public sealed class StationServiceRuntime : MonoBehaviour
    {
        public static readonly List<StationServiceRuntime> All = new();
        public StationServiceDefinition definition;
        [SerializeField] private int activeCustomers;
        [SerializeField] private List<SkierRuntime> queue = new();
        private readonly Dictionary<SkierRuntime, float> startHour = new();
        private readonly Dictionary<SkierRuntime, float> queueHour = new();
        private StationServiceDailyStats stats;
        private float lastGameHour = -1f;
        private int lastDay = -1;

        public int ActiveCustomers => activeCustomers;
        public int QueueCount => queue.Count;
        public int EffectiveCapacity => definition == null ? 0 : (definition.staffAvailable >= definition.staffRequired ? definition.capacity : 0);
        public int AvailableCapacity => Mathf.Max(0, EffectiveCapacity - activeCustomers);
        public float Occupancy01 => definition == null ? 0f : Mathf.Clamp01(activeCustomers / (float)Mathf.Max(1, definition.capacity));
        public float QueuePressure01 => definition == null ? 0f : Mathf.Clamp01(queue.Count / (float)Mathf.Max(1, definition.maxQueue));
        public StationServiceDailyStats Stats => stats;
        public event Action Changed;

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() { All.Remove(this); }
        private void Awake() { lastGameHour = GameClock.I != null ? GameClock.I.CurrentHour : -1f; lastDay = GameClock.I != null ? GameClock.I.CurrentDay : -1; }
        private void Update() { ProcessClock(); }

        public bool IsOpen()
        {
            if (definition == null || GameClock.I == null) return false;
            if (definition.staffAvailable < definition.staffRequired) return false;
            float h = GameClock.I.CurrentHour;
            if (GameClock.I.IsNight) return definition.openAtNight;
            if (definition.openingHour <= definition.closingHour) return h >= definition.openingHour && h < definition.closingHour;
            return h >= definition.openingHour || h < definition.closingHour;
        }

        public bool CanRequest(SkierRuntime skier)
        {
            if (skier == null || definition == null || !IsOpen()) return false;
            if (!definition.unlimitedStock && definition.stock <= 0) return false;
            if (Contains(skier)) return false;
            return activeCustomers < EffectiveCapacity || (activeCustomers + queue.Count) < EffectiveCapacity + definition.maxQueue;
        }

        public bool RequestService(SkierRuntime skier)
        {
            if (skier == null || definition == null) return false;
            stats.totalRequests++;
            if (!CanRequest(skier)) { stats.refused++; Changed?.Invoke(); return false; }
            if (activeCustomers < EffectiveCapacity)
            {
                BeginService(skier);
            }
            else
            {
                if (queue.Count >= definition.maxQueue) { stats.refused++; Changed?.Invoke(); return false; }
                queue.Add(skier);
                queueHour[skier] = GameClock.I != null ? GameClock.I.CurrentHour : 0f;
                stats.queuePeak = Mathf.Max(stats.queuePeak, queue.Count);
                skier.SetState(SkierState.Resting);
                Changed?.Invoke();
            }
            return true;
        }

        public bool Contains(SkierRuntime skier) => skier != null && (queue.Contains(skier) || startHour.ContainsKey(skier));

        public bool TryComplete(SkierRuntime skier)
        {
            if (skier == null || definition == null || !startHour.ContainsKey(skier)) return false;
            float serviceMinutes = definition.serviceMinutes;
            float elapsed = ElapsedGameMinutes(startHour[skier], GameClock.I != null ? GameClock.I.CurrentHour : 0f);
            if (elapsed < serviceMinutes) return false;
            CompleteService(skier);
            return true;
        }

        private void BeginService(SkierRuntime skier)
        {
            activeCustomers++;
            startHour[skier] = GameClock.I != null ? GameClock.I.CurrentHour : 0f;
            skier.SetState(SkierState.Resting);
            if (definition.type == StationServiceType.SkiSchool)
            {
                var school = GetComponent<SkiSchoolRuntime>();
                if (school != null)
                {
                    SkiLessonFormat format = SkiLessonFormat.Individual;
                    if (definition.lessonFormat == SkiSchoolServiceFormat.Group) format = SkiLessonFormat.Group;
                    else if (definition.lessonFormat == SkiSchoolServiceFormat.Flexible)
                    {
                        var group = VisitorGroupRuntime.I != null ? VisitorGroupRuntime.I.Get(skier) : default;
                        format = group.memberCount >= 2 && UnityEngine.Random.value < .72f ? SkiLessonFormat.Group : SkiLessonFormat.Individual;
                    }
                    school.TryStartLesson(skier, format, GetOrCreateProgress(skier).CurrentLevel);
                }
            }
            Changed?.Invoke();
        }

        private void CompleteService(SkierRuntime skier)
        {
            float waitedBeforeService = WaitMinutes(skier);
            startHour.Remove(skier);
            queueHour.Remove(skier);
            activeCustomers = Mathf.Max(0, activeCustomers - 1);
            if (!definition.unlimitedStock) definition.stock = Mathf.Max(0, definition.stock - 1);

            CustomerProfile profile = skier.PopulationProfile != null ? skier.PopulationProfile.ToCustomerProfile() : CustomerProfile.Tourist;
            float price = PricingRuntime.I != null ? PricingRuntime.I.GetPrice(GetId(), ToFinanceCategory(), definition.basePrice, profile) : definition.basePrice;
            float waitingPenalty = Mathf.Clamp(waitedBeforeService * .65f, 0f, 40f);
            float quality = Mathf.Clamp(definition.quality - waitingPenalty, 0f, 100f);
            float satisfactionDelta = (quality - 65f) * .035f - waitingPenalty * .03f;
            skier.ChangeSatisfaction(satisfactionDelta);

            if (StationRuntime.I != null) StationRuntime.I.Earn(price, ToFinanceCategory(), profile, GetId(), definition.serviceName);
            if (VisitorServiceConsumptionRuntime.I != null) VisitorServiceConsumptionRuntime.I.RecordServiceTransaction(skier, ToVisitorServiceType(), GetId(), definition.serviceName, price, quality);
            if (VisitorDayPlanRuntime.I != null) VisitorDayPlanRuntime.I.RegisterServiceCompleted(skier, definition.type, definition.serviceMinutes);

            stats.served++; stats.revenue += price; stats.satisfaction += quality; stats.satisfactionSamples++;
            int pi = Mathf.Clamp((int)profile, 0, 8);
            if (stats.servedByProfile == null || stats.servedByProfile.Length != 9) stats.servedByProfile = new int[9];
            if (stats.revenueByProfile == null || stats.revenueByProfile.Length != 9) stats.revenueByProfile = new float[9];
            if (stats.satisfactionByProfile == null || stats.satisfactionByProfile.Length != 9) stats.satisfactionByProfile = new float[9];
            stats.servedByProfile[pi]++; stats.revenueByProfile[pi] += price; stats.satisfactionByProfile[pi] += quality;
            if (queue.Count > 0)
            {
                SkierRuntime next = queue[0]; queue.RemoveAt(0);
                float waited = WaitMinutes(next); stats.waitingMinutes += Mathf.Max(0f, waited);
                BeginService(next);
            }
            Changed?.Invoke();
        }

        private void ProcessClock()
        {
            if (definition == null || GameClock.I == null) return;
            int day = GameClock.I.CurrentDay;
            float hour = GameClock.I.CurrentHour;
            if (day != lastDay) { stats = new StationServiceDailyStats(day); lastDay = day; }
            lastGameHour = hour;

            var active = new List<SkierRuntime>(startHour.Keys);
            for (int i = 0; i < active.Count; i++) if (active[i] != null) TryComplete(active[i]);

            for (int i = queue.Count - 1; i >= 0; i--)
            {
                if (queue[i] == null || queue[i].State == SkierState.Leaving) { if (queue[i] != null) queueHour.Remove(queue[i]); queue.RemoveAt(i); }
            }
        }

        private float WaitMinutes(SkierRuntime skier)
        {
            if (skier == null || GameClock.I == null || !queueHour.TryGetValue(skier, out var from)) return 0f;
            return ElapsedGameMinutes(from, GameClock.I.CurrentHour);
        }

        private float ElapsedGameMinutes(float fromHour, float toHour)
        {
            float d = toHour - fromHour; if (d < 0f) d += 24f; return d * 60f;
        }

        private string GetId() => definition != null && !string.IsNullOrWhiteSpace(definition.serviceId) ? definition.serviceId : gameObject.GetInstanceID().ToString();

        private FinanceCategory ToFinanceCategory()
        {
            switch (definition.type)
            {
                case StationServiceType.Shop: return FinanceCategory.Shops;
                case StationServiceType.Restaurant:
                case StationServiceType.Bar: return FinanceCategory.Food;
                case StationServiceType.EquipmentRental: return FinanceCategory.Rentals;
                case StationServiceType.SkiSchool: return FinanceCategory.Lessons;
                case StationServiceType.Activity: return FinanceCategory.Leisure;
                case StationServiceType.Accommodation: return FinanceCategory.Hotel;
                default: return FinanceCategory.Other;
            }
        }

        private VisitorServiceType ToVisitorServiceType()
        {
            switch (definition.type)
            {
                case StationServiceType.Shop: return VisitorServiceType.Shop;
                case StationServiceType.Restaurant: return VisitorServiceType.Restaurant;
                case StationServiceType.Bar: return VisitorServiceType.Bar;
                case StationServiceType.EquipmentRental: return VisitorServiceType.EquipmentRental;
                case StationServiceType.SkiSchool: return VisitorServiceType.SkiSchool;
                case StationServiceType.Activity: return VisitorServiceType.MountainActivity;
                case StationServiceType.Accommodation: return VisitorServiceType.Hotel;
                default: return VisitorServiceType.Leisure;
            }
        }
        private static SkiLessonProgressRuntime GetOrCreateProgress(SkierRuntime skier)
        {
            if (skier == null) return null;
            return skier.GetComponent<SkiLessonProgressRuntime>() ?? skier.gameObject.AddComponent<SkiLessonProgressRuntime>();
        }

    }
}
