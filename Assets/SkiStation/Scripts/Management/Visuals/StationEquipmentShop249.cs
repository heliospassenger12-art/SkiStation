using UnityEngine;

namespace SkiStation
{
    // Management-only equipment shop: unlimited stock; prices do not debit visitor finances yet.
    public sealed class StationEquipmentShop249 : MonoBehaviour
    {
        public enum PricingMode { Unmanaged, Automatic, Manual }
        public enum Activity { AlpineSki, CrossCountrySki, Snowboard, Snowscoot, Sled }
        // All five departments share the same store, with unlimited inventory.
        public bool[] EnabledDepartments = { true, true, true, true, true };
        public PricingMode Mode = PricingMode.Unmanaged;
        public bool IsOpen = true;
        // Four independent levels: beginner, intermediate, advanced, expert.
        public int[] ManualPrices = { 25, 45, 75, 110 };
        private static readonly int[] BasePrices = { 25, 45, 75, 110 };
        [Range(0f, 1f)] public float ResortSize = .2f;
        [Range(0f, 1f)] public float Prestige = .2f;
        [Range(0f, 1f)] public float Excellence = .2f;
        [Range(0f, 1f)] public float Attendance = .2f;
        [Range(0f, 1f)] public float AffluentAudience = .2f;
        public int PriceForLevel(int level) { return PriceFor(Activity.AlpineSki, level); }
        public int PriceFor(Activity activity, int level)
        {
            int index = Mathf.Clamp(level, 0, 3);
            if (activity == Activity.Sled) index = 0; // Sleds are rental-only.
            float activityFactor = activity == Activity.CrossCountrySki ? .85f : activity == Activity.Snowboard ? 1.05f : activity == Activity.Snowscoot ? 1.2f : activity == Activity.Sled ? .45f : 1f;
            if (Mode == PricingMode.Manual)
                return Mathf.Max(0, Mathf.RoundToInt((ManualPrices != null && ManualPrices.Length > index ? ManualPrices[index] : BasePrices[index]) * activityFactor));
            if (Mode == PricingMode.Unmanaged) return Mathf.RoundToInt(BasePrices[index] * activityFactor);
            float factor = 1f + .12f * ResortSize + .18f * Prestige + .16f * Excellence
                             + .10f * Attendance + .14f * AffluentAudience;
            return Mathf.Max(0, Mathf.RoundToInt(BasePrices[index] * factor * activityFactor));
        }
        // Called by the visitor only after reaching the shop entrance.
        public bool TryProvideEquipment(int visitorLevel, out int indicativePrice)
        {
            return TryProvideEquipment(Activity.AlpineSki, visitorLevel, out indicativePrice);
        }
        public bool TryProvideEquipment(Activity activity, int visitorLevel, out int indicativePrice)
        {
            indicativePrice = PriceFor(activity, visitorLevel);
            var status = GetComponent<StationStructureStatus249>();
            return IsOpen && (status == null || status.CanWelcomeVisitors) && EnabledDepartments != null
                && EnabledDepartments.Length > (int)activity && EnabledDepartments[(int)activity]; // Unlimited stock; capacity/queue and actual payment are future systems.
        }
    }
}
