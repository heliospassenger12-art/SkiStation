using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// Reservation ledger for multi-day stays. Call TryBook from a visitor/group planner;
    /// this component deliberately does not create guests or alter the ski loop.
    /// </summary>
    public sealed class StationAccommodationBookings249 : MonoBehaviour
    {
        private sealed class Booking
        {
            public StationAccommodation249 accommodation;
            public int beds;
            public int checkoutDay;
            public int nextBillDay;
            public int nightlyRate;
        }

        private readonly Dictionary<int, Booking> bookings = new Dictionary<int, Booking>();
        private int nextBookingId = 1;

        public int ActiveBookings { get { return bookings.Count; } }

        public static StationAccommodationBookings249 GetOrCreate()
        {
            var existing = FindObjectOfType<StationAccommodationBookings249>();
            if (existing != null) return existing;
            return new GameObject("Station accommodation reservations").AddComponent<StationAccommodationBookings249>();
        }

        private int lastProcessedDay = -1;
        private void Update()
        {
            if (GameClock.I == null) return;
            int day = GameClock.I.CurrentDay;
            if (day == lastProcessedDay) return;
            lastProcessedDay = day;
            ProcessNightlyRevenue(day);
            ProcessCheckoutDay(day);
        }


        public bool TryBook(StationAccommodation249 accommodation, int beds, int arrivalDay,
            int nights, out int bookingId)
        {
            bookingId = 0;
            if (accommodation == null || beds <= 0 || nights <= 0 || arrivalDay < 0) return false;
            if (arrivalDay > int.MaxValue - nights) return false;
            if (!accommodation.TryReserve(beds)) return false;
            bookingId = nextBookingId++;
            bookings.Add(bookingId, new Booking
            {
                accommodation = accommodation,
                beds = beds,
                checkoutDay = arrivalDay + nights,
                nextBillDay = arrivalDay + 1,
                nightlyRate = accommodation.NightlyRate()
            });
            return true;
        }

        // One charge per completed night, including when the clock skips several days.
        // Price is locked at booking so later price changes cannot rewrite existing stays.
        public void ProcessNightlyRevenue(int currentDay)
        {
            if (StationRuntime.I == null) return;
            foreach (var entry in bookings)
            {
                Booking booking = entry.Value;
                if (booking.accommodation == null) continue;
                BillBookingUntil(booking, currentDay);
            }
        }

        public bool Checkout(int bookingId)
        {
            Booking booking;
            if (!bookings.TryGetValue(bookingId, out booking)) return false;
            // A departure on checkout day must not lose the revenue of the last night.
            int day = GameClock.I != null ? GameClock.I.CurrentDay : 0;
            if (day > 0) BillBookingUntil(booking, day);
            bookings.Remove(bookingId);
            if (booking.accommodation != null) booking.accommodation.ReleaseBeds(booking.beds);
            return true;
        }

        private void BillBookingUntil(Booking booking, int day)
        {
            if (booking.accommodation == null || StationRuntime.I == null) return;
            int until = Mathf.Min(day, booking.checkoutDay);
            if (booking.nextBillDay > until) return;
            int nights = until - booking.nextBillDay + 1;
            float revenue = (float)nights * booking.beds * booking.nightlyRate;
            if (revenue > 0f)
                StationRuntime.I.Earn(revenue, FinanceCategory.Hotel, CustomerProfile.Tourist,
                    booking.accommodation.name, "Nuitees hebergement");
            booking.accommodation.RecordNightlyRevenue(revenue, nights * booking.beds);
            booking.nextBillDay = until + 1;
        }

        // Called by the station calendar when a new in-game day begins.
        public void ProcessCheckoutDay(int currentDay)
        {
            var expired = new List<int>();
            foreach (var entry in bookings)
                if (entry.Value.checkoutDay <= currentDay || entry.Value.accommodation == null)
                    expired.Add(entry.Key);
            foreach (int id in expired) Checkout(id);
        }

        private void OnDestroy()
        {
            var ids = new List<int>(bookings.Keys);
            foreach (int id in ids) Checkout(id);
        }
    }
}
