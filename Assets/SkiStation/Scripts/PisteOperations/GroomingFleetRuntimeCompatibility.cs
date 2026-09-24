using System.Collections.Generic;
using UnityEngine;
namespace SkiStation.PisteOperations
{
    public sealed class GroomingFleetVehicleCompat
    {
        public bool working, returning;
        public SkiStation.SlopeRuntime target;
    }
    public sealed class GroomingFleetRuntime : MonoBehaviour
    {
        public static GroomingFleetRuntime I { get; private set; }
        public List<GroomingFleetVehicleCompat> Vehicles { get; } = new List<GroomingFleetVehicleCompat>();
        void Awake() { if (I != null && I != this) { Destroy(gameObject); return; } I = this; if (Vehicles.Count == 0) { Vehicles.Add(new GroomingFleetVehicleCompat()); Vehicles.Add(new GroomingFleetVehicleCompat()); } }
    }
}
