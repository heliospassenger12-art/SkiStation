using UnityEngine;

namespace SkiStation
{
    // Accommodation capacity and reservation ledger; no automatic visitor assignment yet.
    public sealed class StationAccommodation249 : MonoBehaviour
    {
        [SerializeField] private StationStructureCatalog249.Kind kind;
        [SerializeField] private int capacity;
        [SerializeField] private int currentGuests;
        public enum PriceMode { SansGestion, Automatique, Manuel }
        [SerializeField, Range(1, 5)] private int stars = 1;
        [SerializeField, Range(1, 9)] private int floors = 1;
        [SerializeField] private int sizeLevel = 1;
        [SerializeField] private int amenitiesMask;
        [SerializeField] private PriceMode priceMode = PriceMode.SansGestion;
        [SerializeField] private int manualNightlyRate = 90;
        [SerializeField] private int indicativeConstructionCost;
        [SerializeField] private float totalLodgingRevenue;
        [SerializeField] private int paidGuestNights;
        public float TotalLodgingRevenue { get { return totalLodgingRevenue; } }
        public int PaidGuestNights { get { return paidGuestNights; } }
        public void RecordNightlyRevenue(float amount, int guestNights)
        {
            totalLodgingRevenue += Mathf.Max(0f, amount);
            paidGuestNights += Mathf.Max(0, guestNights);
        }
        public int Stars { get { return stars; } }
        public int Floors { get { return floors; } }
        public int AmenitiesMask { get { return amenitiesMask; } }
        public PriceMode TariffMode { get { return priceMode; } }
        public int IndicativeConstructionCost { get { return indicativeConstructionCost; } }
        public int NightlyRate(float stationDemandFactor = 1f)
        {
            if (priceMode == PriceMode.Manuel) return Mathf.Max(0, manualNightlyRate);
            int baseline = Mathf.RoundToInt((35f + 30f * stars + 12f * amenitiesMaskCount()) * (kind == StationStructureCatalog249.Kind.Chalet ? 1.5f : 1f));
            return priceMode == PriceMode.Automatique ? Mathf.RoundToInt(baseline * Mathf.Clamp(stationDemandFactor, .6f, 2.5f)) : baseline;
        }
        private int amenitiesMaskCount() { int count = 0; for (int bit = 0; bit < 6; bit++) if ((amenitiesMask & (1 << bit)) != 0) count++; return count; }
        public void ConfigureDetails(int qualityStars, int storeys, int size, int amenities, PriceMode mode, int manualRate, int constructionCost)
        {
            stars = Mathf.Clamp(qualityStars, 1, 5);
            floors = Mathf.Clamp(storeys, 1, 9);
            sizeLevel = Mathf.Clamp(size, 1, 3);
            amenitiesMask = kind == StationStructureCatalog249.Kind.Hotel ? amenities & 63 : 0;
            priceMode = mode;
            manualNightlyRate = Mathf.Max(0, manualRate);
            indicativeConstructionCost = Mathf.Max(0, constructionCost);
        }
        public StationStructureCatalog249.Kind Kind { get { return kind; } }
        public int Capacity { get { return capacity; } }
        public int CurrentGuests { get { return currentGuests; } }
        public float OccupancyRate { get { return capacity <= 0 ? 0f : (float)currentGuests / capacity; } }
        public bool AcceptsBookings
        {
            get
            {
                StationStructureStatus249 status = GetComponent<StationStructureStatus249>();
                return status == null || status.CanWelcomeVisitors;
            }
        }
        // A booking reserves beds for the entire stay, including daytime skiing.
        public bool TryReserve(int guests)
        {
            if (guests <= 0 || !AcceptsBookings || guests > AvailablePlaces) return false;
            currentGuests += guests;
            return true;
        }
        public void ReleaseBeds(int guests)
        {
            if (guests <= 0) return;
            currentGuests = Mathf.Max(0, currentGuests - guests);
        }
        public int AvailablePlaces { get { return Mathf.Max(0, capacity - currentGuests); } }
        public void Configure(StationStructureCatalog249.Kind type, int places)
        {
            kind = type;
            capacity = Mathf.Max(0, places);
            currentGuests = 0;
        }
    }
}
