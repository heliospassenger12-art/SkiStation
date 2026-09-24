using UnityEngine;

namespace SkiStation
{
    // A management visitor's optional lodging reservation. Day visitors need no bed.
    [DisallowMultipleComponent]
    public sealed class StationVisitorStay249 : MonoBehaviour
    {
        private StationAccommodationBookings249 ledger;
        private int bookingId;
        private int checkoutDay;
        private StationAccommodation249 bookedAccommodation;
        public StationAccommodation249 BookedAccommodation { get { return HasReservation ? bookedAccommodation : null; } }
        public bool HasReservation { get { return bookingId != 0; } }
        public int CheckoutDay { get { return checkoutDay; } }

        public void AssignOnArrival()
        {
            if (bookingId != 0 || Random.value >= .35f) return; // 65% day visitors in prototype.
            int day = GameClock.I != null ? GameClock.I.CurrentDay : 1;
            int nights = Random.Range(1, 5);
            int budget = Random.Range(65, 360);
            int preferredStars = Mathf.Clamp(Mathf.RoundToInt(budget / 85f), 1, 5);
            StationAccommodation249 choice = null;
            float bestScore = float.NegativeInfinity;
            foreach (var lodging in FindObjectsOfType<StationAccommodation249>())
            {
                if (lodging == null || !lodging.AcceptsBookings || lodging.AvailablePlaces < 1) continue;
                int nightly = lodging.NightlyRate();
                if (nightly > budget) continue;
                float score = -Mathf.Abs(lodging.Stars - preferredStars) * 40f - nightly * .05f;
                // Stable preference for nearby lodging when two offers are comparable.
                score -= Vector3.Distance(transform.position, lodging.transform.position) * .01f;
                if (score > bestScore) { bestScore = score; choice = lodging; }
            }
            if (choice == null) return; // No affordable lodging: remain a day visitor.
            ledger = StationAccommodationBookings249.GetOrCreate();
            int id;
            if (ledger != null && ledger.TryBook(choice, 1, day, nights, out id))
            {
                bookingId = id;
                checkoutDay = day + nights;
                bookedAccommodation = choice;
            }
        }

        private void OnDestroy()
        {
            // A visitor leaving early must not occupy a bed indefinitely.
            if (bookingId != 0 && ledger != null) ledger.Checkout(bookingId);
            bookingId = 0;
            bookedAccommodation = null;
        }
    }
}
