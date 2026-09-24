using UnityEngine;

namespace SkiStation.PisteOperations
{
    /// <summary>Owns the management-level garage setup and staffing. Keeps micro-management intentionally light.</summary>
    public class GroomingGarageOperationsRuntime : MonoBehaviour
    {
        public static GroomingGarageOperationsRuntime I { get; private set; }
        public float garageBuildCost = 180000f;
        public bool starterGarageProvided = true;
        public int recommendedCapacity = 4;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        public GroomingGarageRuntime EnsureStarterGarage()
        {
            if (GroomingGarageRuntime.I != null) return GroomingGarageRuntime.I;
            var go = new GameObject("SkiStation_Garage_Damage");
            go.transform.position = Vector3.zero;
            var garage = go.AddComponent<GroomingGarageRuntime>();
            garage.vehicleCapacity = Mathf.Max(1, recommendedCapacity);
            return garage;
        }

        public bool TryBuildGarage(Vector3 position)
        {
            if (StationRuntime.I != null && !StationRuntime.I.Spend(garageBuildCost, FinanceCategory.Maintenance, "garage_damage", "Construction garage des dameuses"))
                return false;
            var go = new GameObject("Garage_Damage");
            go.transform.position = position;
            var garage = go.AddComponent<GroomingGarageRuntime>();
            garage.vehicleCapacity = recommendedCapacity;
            return true;
        }
    }
}
