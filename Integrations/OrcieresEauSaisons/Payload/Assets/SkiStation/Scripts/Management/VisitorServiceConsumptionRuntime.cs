using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum VisitorServiceType { Restaurant, Shop, Hotel, Leisure, MountainActivity, Bar, EquipmentRental, SkiSchool }

    [Serializable]
    public struct VisitorServiceConsumption
    {
        public VisitorServiceType type;
        public string sourceId;
        public string sourceName;
        public CustomerProfile profile;
        public float price;
        [Range(0f, 100f)] public float quality;
        public int day;
        public float hour;

        public VisitorServiceConsumption(VisitorServiceType type, string sourceId, string sourceName,
            CustomerProfile profile, float price, float quality, int day, float hour)
        {
            this.type = type; this.sourceId = sourceId; this.sourceName = sourceName;
            this.profile = profile; this.price = price; this.quality = quality;
            this.day = day; this.hour = hour;
        }
    }

    /// <summary>
    /// Records real management visitor interactions with station services.
    /// No artificial visitors are created and the player simulation is never included.
    /// </summary>
    public sealed class VisitorServiceConsumptionRuntime : MonoBehaviour
    {
        public static VisitorServiceConsumptionRuntime I { get; private set; }

        [SerializeField] private int historyLimit = 5000;
        [SerializeField] private float automaticChoiceSeconds = 15f;
        [SerializeField, Range(0f, 1f)] private float optionalServiceChance = 0f;

        private readonly List<VisitorServiceConsumption> history = new();
        private readonly Dictionary<SkierRuntime, float> lastChoice = new();
        private float timer;

        public IReadOnlyList<VisitorServiceConsumption> History => history;
        public event Action<VisitorServiceConsumption> ServiceConsumed;
        public event Action Changed;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < Mathf.Max(1f, automaticChoiceSeconds)) return;
            timer = 0f;
            SimulateOptionalChoices();
        }

        /// <summary>Consumes a leisure facility using a real management visitor.</summary>
        public bool TryConsumeLeisure(SkierRuntime skier, LeisureFacilityRuntime facility)
        {
            if (skier == null || facility == null || facility.definition == null) return false;
            if (!facility.TryEnter()) return false;

            float price = PricingRuntime.I != null
                ? PricingRuntime.I.GetPrice(facility.definition.facilityName, FinanceCategory.Leisure,
                    facility.definition.entryPrice, GetProfile(skier))
                : facility.definition.entryPrice;
            float quality = CalculateFacilityQuality(facility.Occupancy, facility.definition.capacity, GetProfile(skier), facility.definition.familyFriendly);
            Record(skier, VisitorServiceType.Leisure, facility.definition.facilityName,
                facility.definition.facilityName, price, quality);
            return true;
        }

        /// <summary>Consumes a mountain activity using a real management visitor.</summary>
        public bool TryConsumeMountainActivity(SkierRuntime skier, MountainActivityRuntime activity)
        {
            if (skier == null || activity == null || activity.definition == null) return false;
            if (!activity.TryEnter()) return false;

            float price = PricingRuntime.I != null
                ? PricingRuntime.I.GetPrice(activity.definition.activityName, FinanceCategory.Leisure,
                    activity.definition.price, GetProfile(skier))
                : activity.definition.price;
            float quality = CalculateFacilityQuality(activity.CurrentOccupancy,
                activity.definition.capacity, GetProfile(skier), IsFamilyFriendly(activity.definition.type));
            Record(skier, VisitorServiceType.MountainActivity, activity.definition.activityName,
                activity.definition.activityName, price, quality);
            if (VisitorIntentRuntime.I != null) VisitorIntentRuntime.I.RegisterServiceChoice(skier, activity.definition.activityName);
            return true;
        }

        /// <summary>Registers a shop purchase without requiring a concrete shop class yet.</summary>
        public bool RecordShopPurchase(SkierRuntime skier, string shopId, string shopName, float basePrice, float quality = 78f)
        {
            if (skier == null || basePrice < 0f) return false;
            float price = PricingRuntime.I != null
                ? PricingRuntime.I.GetPrice(shopId, FinanceCategory.Shops, basePrice, GetProfile(skier))
                : basePrice;
            Record(skier, VisitorServiceType.Shop, shopId, shopName, price, quality);
            return true;
        }

        /// <summary>Registers a restaurant visit/purchase using the same financial truth.</summary>
        public bool RecordRestaurantPurchase(SkierRuntime skier, string restaurantId, string restaurantName,
            float basePrice, float quality = 78f)
        {
            if (skier == null || basePrice < 0f) return false;
            float price = PricingRuntime.I != null
                ? PricingRuntime.I.GetPrice(restaurantId, FinanceCategory.Food, basePrice, GetProfile(skier))
                : basePrice;
            Record(skier, VisitorServiceType.Restaurant, restaurantId, restaurantName, price, quality);
            return true;
        }

        /// <summary>Checks in a real management visitor and records one nightly hotel transaction.</summary>
        public bool TryHotelNight(SkierRuntime skier, string hotelId, string hotelName, float nightlyPrice,
            float quality = 82f)
        {
            if (skier == null || StationAccommodationRuntime.I == null) return false;
            if (!StationAccommodationRuntime.I.TryCheckIn(1)) return false;
            float price = PricingRuntime.I != null
                ? PricingRuntime.I.GetPrice(hotelId, FinanceCategory.Hotel, nightlyPrice, GetProfile(skier))
                : nightlyPrice;
            Record(skier, VisitorServiceType.Hotel, hotelId, hotelName, price, quality);
            return true;
        }

        /// <summary>Records a completed 1.30 intelligent-service transaction for a real visitor.</summary>
        public void RecordServiceTransaction(SkierRuntime skier, VisitorServiceType type, string sourceId, string sourceName, float price, float quality)
            => RecordServiceTransaction(skier, type, sourceId, sourceName, price, quality, null);

        internal void RecordServiceTransaction(SkierRuntime skier, VisitorServiceType type, string sourceId, string sourceName,
            float price, float quality, StationServiceRuntime provider)
        {
            if (skier == null || price < 0f) return;
            Record(skier, type, sourceId, sourceName, price, quality, provider);
        }

        public float GetServiceQuality(SkierRuntime skier)
        {
            if (skier == null) return 72f;
            float sum = 0f; int count = 0;
            for (int i = history.Count - 1; i >= 0 && count < 8; i--)
            {
                if (history[i].sourceId == null) continue;
                if (history[i].profile != GetProfile(skier)) continue;
                sum += history[i].quality; count++;
            }
            return count == 0 ? 72f : Mathf.Clamp(sum / count, 0f, 100f);
        }

        private void SimulateOptionalChoices()
        {
            var skiers = FindObjectsOfType<SkierRuntime>();
            for (int i = 0; i < skiers.Length; i++)
            {
                var skier = skiers[i];
                if (skier == null || skier.State == SkierState.Leaving || skier.State == SkierState.Skiing ||
                    skier.State == SkierState.RidingLift || skier.State == SkierState.WaitingForLift) continue;

                if (lastChoice.TryGetValue(skier, out var lastDayHour) && CurrentGameHour() - lastDayHour < .75f) continue;
                if (UnityEngine.Random.value > optionalServiceChance) continue;

                bool consumed = false;
                var profile = GetProfile(skier);

                if (LeisureFacilityRuntime.All.Count > 0)
                {
                    LeisureFacilityRuntime best = FindBestLeisure(profile);
                    if (VisitorBehaviorRuntime.I != null)
                    {
                        string preferredId = VisitorBehaviorRuntime.I.ChooseServiceId(skier);
                        foreach (var facility in LeisureFacilityRuntime.All)
                        {
                            if (facility != null && facility.definition != null && facility.definition.facilityName == preferredId && facility.AvailableCapacity > 0)
                            { best = facility; break; }
                        }
                    }
                    if (best != null) consumed = TryConsumeLeisure(skier, best);
                }

                if (!consumed && MountainActivityRuntime.All.Count > 0)
                {
                    MountainActivityRuntime best = FindBestActivity(profile,skier.transform.position);
                    if (best != null) consumed = TryConsumeMountainActivity(skier, best);
                }

                if (consumed) lastChoice[skier] = CurrentGameHour();
            }

            CleanupVisitorCache(skiers);
        }

        private LeisureFacilityRuntime FindBestLeisure(CustomerProfile profile)
        {
            LeisureFacilityRuntime best = null; float bestScore = float.MaxValue;
            foreach (var facility in LeisureFacilityRuntime.All)
            {
                if (facility == null || facility.definition == null || facility.Occupancy >= facility.definition.capacity) continue;
                float pressure = facility.Occupancy / Mathf.Max(1f, facility.definition.capacity);
                float familyPenalty = (profile == CustomerProfile.Family || profile == CustomerProfile.Youth)
                    ? (facility.definition.familyFriendly ? 0f : 30f) : 0f;
                float score = pressure * 100f + familyPenalty;
                if (score < bestScore) { bestScore = score; best = facility; }
            }
            return best;
        }

        private MountainActivityRuntime FindBestActivity(CustomerProfile profile,Vector3 visitor)
        {
            MountainActivityRuntime best = null; float bestScore = float.MaxValue;
            foreach (var activity in MountainActivityRuntime.All)
            {
                if (activity == null || activity.definition == null || activity.AvailableCapacity <= 0) continue;
                if (activity.IsSeasonalSite && (visitor-activity.transform.position).sqrMagnitude>130f*130f) continue;
                float pressure = activity.CurrentOccupancy / Mathf.Max(1f, activity.definition.capacity);
                float score = pressure * 100f + (profile == CustomerProfile.Family && !IsFamilyFriendly(activity.definition.type) ? 20f : 0f);
                if (score < bestScore) { bestScore = score; best = activity; }
            }
            return best;
        }

        private void Record(SkierRuntime skier, VisitorServiceType type, string sourceId, string sourceName,
            float price, float quality, StationServiceRuntime provider = null)
        {
            var profile = GetProfile(skier);
            int day = StationRuntime.I != null ? StationRuntime.I.State.day : 1;
            float hour = GameClock.I != null ? GameClock.I.CurrentHour : 0f;
            var entry = new VisitorServiceConsumption(type, sourceId, sourceName, profile,
                Mathf.Max(0f, price), Mathf.Clamp(quality, 0f, 100f), day, hour);
            history.Add(entry);
            while (history.Count > Mathf.Max(100, historyLimit)) history.RemoveAt(0);

            FinanceCategory category = type switch
            {
                VisitorServiceType.Restaurant => FinanceCategory.Food,
                VisitorServiceType.Bar => FinanceCategory.Food,
                VisitorServiceType.EquipmentRental => FinanceCategory.Rentals,
                VisitorServiceType.SkiSchool => FinanceCategory.Lessons,
                VisitorServiceType.Shop => FinanceCategory.Shops,
                VisitorServiceType.Hotel => FinanceCategory.Hotel,
                _ => FinanceCategory.Leisure
            };
            if (entry.price > 0f)
            {
                if (provider != null && provider.UsesAlliedAccount)
                    provider.PostAlliedReceipt(AlpineEmpires.AlpineActionKind.ServiceRevenue, entry.price,
                        category, profile, GameClock.I != null ? GameClock.I.CurrentDay : day, hour);
                else StationRuntime.I?.Earn(entry.price, category, profile, sourceId, sourceName);
            }

            ServiceConsumed?.Invoke(entry);
            Changed?.Invoke();
        }

        private static CustomerProfile GetProfile(SkierRuntime skier)
        {
            return skier != null && skier.PopulationProfile != null
                ? skier.PopulationProfile.ToCustomerProfile() : CustomerProfile.Tourist;
        }

        private static float CalculateFacilityQuality(int occupancy, int capacity, CustomerProfile profile, bool familyFriendly)
        {
            float pressure = Mathf.Clamp01(occupancy / Mathf.Max(1f, capacity));
            float quality = 88f - pressure * 35f;
            if ((profile == CustomerProfile.Family || profile == CustomerProfile.Youth) && !familyFriendly) quality -= 18f;
            return Mathf.Clamp(quality, 0f, 100f);
        }

        private static bool IsFamilyFriendly(MountainActivityType type)
        {
            return type == MountainActivityType.Viewpoint || type == MountainActivityType.MountainRestaurant ||
                   type == MountainActivityType.Dogsled || type == MountainActivityType.Zipline;
        }

        private float CurrentGameHour()
        {
            int day = StationRuntime.I != null ? StationRuntime.I.State.day : 1;
            return day * 24f + (GameClock.I != null ? GameClock.I.CurrentHour : 0f);
        }

        private void CleanupVisitorCache(SkierRuntime[] skiers)
        {
            var live = new HashSet<SkierRuntime>(skiers);
            var remove = new List<SkierRuntime>();
            foreach (var pair in lastChoice) if (pair.Key == null || !live.Contains(pair.Key)) remove.Add(pair.Key);
            foreach (var skier in remove) lastChoice.Remove(skier);
        }
    }
}
