using UnityEngine;
namespace SkiStation
{
    [DisallowMultipleComponent]
    public sealed class VisitorSkiAdmission : MonoBehaviour
    {
        private int paidDay = int.MinValue;
        private static int Day => GameClock.I != null ? GameClock.I.CurrentDay : 0;
        public static bool HasPass(SkierRuntime skier)
        { var a = skier != null ? skier.GetComponent<VisitorSkiAdmission>() : null; return a != null && a.paidDay == Day; }
        public static void GrantPrepaid(SkierRuntime skier)
        { var a = skier.GetComponent<VisitorSkiAdmission>() ?? skier.gameObject.AddComponent<VisitorSkiAdmission>(); a.paidDay = Day; }
        public static bool BuyAtOffice(SkierRuntime skier)
        {
            if (skier == null || StationRuntime.I == null) return false;
            if (HasPass(skier)) return true;
            var profile = skier.GetComponent<SkierPopulationProfile>();
            if (profile == null) return false;
            StationVisitorDemandRuntime.SellDayPass(profile.ToCustomerProfile());
            GrantPrepaid(skier); return true;
        }
        public static bool CanSki(SkierRuntime skier)
        {
            if (skier == null || skier.State == SkierState.Leaving) return false;
            var arrival = skier.GetComponent<StationVisitorRoadArrival248>();
            return (arrival == null || arrival.ReadyToSki) && (skier.GetComponent<ArrivalIdentity250>() == null || HasPass(skier));
        }
        public static void RecordBoarding(SkierRuntime skier)
        {
            if (!CanSki(skier) || skier.GetComponent<StationVisitorRoadArrival248>() == null || StationRuntime.I == null) return;
            var admission = skier.GetComponent<VisitorSkiAdmission>() ?? skier.gameObject.AddComponent<VisitorSkiAdmission>();
            int day = GameClock.I != null ? GameClock.I.CurrentDay : 0;
            if (admission.paidDay == day) return;
            var profile = skier.GetComponent<SkierPopulationProfile>();
            if (profile == null) return;
            admission.paidDay = day;
            StationVisitorDemandRuntime.SellDayPass(profile.ToCustomerProfile());
        }
    }
}
