using UnityEngine;

namespace SkiStation
{
    public enum StationArrivalTransportType { Car, CoachBus, Pedestrian }

    /// <summary>Optional profile carried by a real visitor to describe how they reached the station.</summary>
    public sealed class StationArrivalTransportProfile : MonoBehaviour
    {
        public StationArrivalTransportType transportType = StationArrivalTransportType.Car;
        [Min(0f)] public float customDropOffDwellSeconds = 0f;
        public ParkingType parkingType = ParkingType.DayVisitor;
        public bool parksAfterDropOff = true;
        public bool IsVehicle => transportType != StationArrivalTransportType.Pedestrian;
        public float ResolveDwell(float fallback) => customDropOffDwellSeconds > 0f ? customDropOffDwellSeconds : fallback;
    }
}
