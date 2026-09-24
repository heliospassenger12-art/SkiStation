using UnityEngine;

namespace SkiStation.PisteOperations
{
    public enum GroomingShiftMode { Day, Night, Flexible }

    /// <summary>Human operations layer: teams provide qualified drivers for grooming vehicles.</summary>
    public class GroomingTeamRuntime : MonoBehaviour
    {
        public static GroomingTeamRuntime I { get; private set; }
        [Min(0)] public int teamCount = 2;
        [Min(0)] public int driversPerTeam = 1;
        [Range(0.5f, 2f)] public float efficiency = 1f;
        public GroomingShiftMode shift = GroomingShiftMode.Flexible;
        public float nightStart = 20f;
        public float nightEnd = 7f;
        public float weeklyCost = 3200f;

        public int AvailableDrivers => Mathf.Max(0, teamCount * driversPerTeam);
        public int StaffedVehicles => Mathf.Min(AvailableDrivers, GroomingFleetRuntime.I != null ? GroomingFleetRuntime.I.Vehicles.Count : 0);

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        public bool CanOperate(float hour)
        {
            if (AvailableDrivers <= 0) return false;
            if (shift == GroomingShiftMode.Flexible) return true;
            if (shift == GroomingShiftMode.Day) return hour >= 6f && hour < 19f;
            if (nightStart > nightEnd) return hour >= nightStart || hour < nightEnd;
            return hour >= nightStart && hour < nightEnd;
        }

        public float EfficiencyMultiplier => Mathf.Clamp(efficiency, .5f, 2f);
    }
}
