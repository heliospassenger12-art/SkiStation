using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum InfrastructureCongestionType { Lift, Slope }

    [Serializable]
    public struct InfrastructureCongestionSnapshot
    {
        public string id;
        public InfrastructureCongestionType type;
        [Range(0f, 1f)] public float pressure01;
        [Range(0f, 1f)] public float utilization01;
        [Range(0f, 1f)] public float queue01;
        public int activeUsers;
        public int queueUsers;
        public bool hotspot;
    }

    /// <summary>
    /// Local congestion map for individual lifts and slopes. It reads the real
    /// management population and never creates or moves visitors by itself.
    /// The route network can use the pressure to choose less crowded alternatives.
    /// </summary>
    public sealed class InfrastructureCongestionRuntime : MonoBehaviour
    {
        public static InfrastructureCongestionRuntime I { get; private set; }

        [SerializeField] private float refreshSeconds = 2f;
        [SerializeField] private float slopeConcurrentWindowHours = .15f;
        [SerializeField] private float hotspotThreshold = .75f;
        [SerializeField] private float liftQueueWeight = .75f;
        [SerializeField] private float liftVehicleWeight = .25f;
        [SerializeField] private float slopeTrafficWeight = 1f;

        private readonly Dictionary<LiftRuntime, InfrastructureCongestionSnapshot> liftSnapshots =
            new Dictionary<LiftRuntime, InfrastructureCongestionSnapshot>();
        private readonly Dictionary<SlopeRuntime, InfrastructureCongestionSnapshot> slopeSnapshots =
            new Dictionary<SlopeRuntime, InfrastructureCongestionSnapshot>();
        private float timer;

        public event Action Changed;
        public IReadOnlyDictionary<LiftRuntime, InfrastructureCongestionSnapshot> LiftSnapshots => liftSnapshots;
        public IReadOnlyDictionary<SlopeRuntime, InfrastructureCongestionSnapshot> SlopeSnapshots => slopeSnapshots;

        public LiftRuntime MostCongestedLift { get; private set; }
        public SlopeRuntime MostCongestedSlope { get; private set; }
        public float PeakPressure01 { get; private set; }

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start() => Recalculate();

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < Mathf.Max(.25f, refreshSeconds)) return;
            timer = 0f;
            Recalculate();
        }

        public void Recalculate()
        {
            liftSnapshots.Clear();
            slopeSnapshots.Clear();
            MostCongestedLift = null;
            MostCongestedSlope = null;
            PeakPressure01 = 0f;

            var network = ResortRouteNetwork.I;
            if (network == null)
            {
                Changed?.Invoke();
                return;
            }

            foreach (var lift in network.Lifts)
            {
                if (lift == null || !lift.Open || lift.Faulted) continue;
                var logic = lift.StationLogic;
                int queue = logic != null ? Mathf.Max(0, logic.QueueSize) : 0;
                int occupants = 0;
                var vehicles = lift.GetComponentsInChildren<LiftVehicleRuntime>();
                for (int i = 0; i < vehicles.Length; i++)
                    if (vehicles[i] != null) occupants += vehicles[i].OccupantCount;

                float comfortableQueue = Mathf.Max(1f, lift.CapacityPerHour * .20f);
                float queue01 = Mathf.Clamp01(queue / comfortableQueue);
                float vehicleCapacity = Mathf.Max(1f, lift.Definition != null ? lift.Definition.seatsPerVehicle : 1);
                float utilization01 = Mathf.Clamp01(occupants / vehicleCapacity);
                float pressure = Mathf.Clamp01(queue01 * liftQueueWeight + utilization01 * liftVehicleWeight);

                var snapshot = new InfrastructureCongestionSnapshot
                {
                    id = lift.name,
                    type = InfrastructureCongestionType.Lift,
                    pressure01 = pressure,
                    utilization01 = utilization01,
                    queue01 = queue01,
                    activeUsers = occupants,
                    queueUsers = queue,
                    hotspot = pressure >= hotspotThreshold
                };
                liftSnapshots[lift] = snapshot;
                if (pressure > PeakPressure01)
                {
                    PeakPressure01 = pressure;
                    MostCongestedLift = lift;
                }
            }

            foreach (var slope in network.Slopes)
            {
                if (slope == null || !slope.Open) continue;
                float hourlyCapacity = slope.GetComponent<SlopeTraffic>() != null
                    ? Mathf.Max(1f, slope.GetComponent<SlopeTraffic>().CapacityPerHour)
                    : 180f;
                float concurrentCapacity = Mathf.Max(1f, hourlyCapacity * Mathf.Max(.05f, slopeConcurrentWindowHours));
                float utilization01 = Mathf.Clamp01(slope.TrafficCount / concurrentCapacity);
                float pressure = Mathf.Clamp01(utilization01 * slopeTrafficWeight);

                var snapshot = new InfrastructureCongestionSnapshot
                {
                    id = slope.name,
                    type = InfrastructureCongestionType.Slope,
                    pressure01 = pressure,
                    utilization01 = utilization01,
                    queue01 = 0f,
                    activeUsers = slope.TrafficCount,
                    queueUsers = 0,
                    hotspot = pressure >= hotspotThreshold
                };
                slopeSnapshots[slope] = snapshot;
                if (pressure > PeakPressure01)
                {
                    PeakPressure01 = pressure;
                    MostCongestedSlope = slope;
                }
            }

            Changed?.Invoke();
        }

        public float GetLiftPressure01(LiftRuntime lift)
            => lift != null && liftSnapshots.TryGetValue(lift, out var snapshot) ? snapshot.pressure01 : 0f;

        public float GetSlopePressure01(SlopeRuntime slope)
            => slope != null && slopeSnapshots.TryGetValue(slope, out var snapshot) ? snapshot.pressure01 : 0f;

        public bool IsLiftHotspot(LiftRuntime lift) => GetLiftPressure01(lift) >= hotspotThreshold;
        public bool IsSlopeHotspot(SlopeRuntime slope) => GetSlopePressure01(slope) >= hotspotThreshold;
    }
}
