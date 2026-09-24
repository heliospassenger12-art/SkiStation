using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// 0.69 specialist management layer for mechanical lifts.
    /// Reads the real LiftRuntime/LiftStationLogic/InfrastructureCongestionRuntime data.
    /// It does not create visitors or alter the economy automatically.
    /// </summary>
    public sealed class LiftManagementRuntime : MonoBehaviour
    {
        public static LiftManagementRuntime I { get; private set; }
        public event Action Changed;
        public IReadOnlyList<LiftManagementSnapshot> Snapshots => snapshots;
        public LiftManagementSnapshot SelectedSnapshot { get; private set; }
        public LiftRuntime SelectedLift { get; private set; }

        private readonly List<LiftManagementSnapshot> snapshots = new List<LiftManagementSnapshot>();
        private float refreshTimer;
        [SerializeField] private float refreshSeconds = 1f;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start() => Rebuild();

        private void Update()
        {
            refreshTimer += Time.unscaledDeltaTime;
            if (refreshTimer < Mathf.Max(.2f, refreshSeconds)) return;
            refreshTimer = 0f;
            Rebuild();
        }

        public void Select(LiftRuntime lift)
        {
            SelectedLift = lift;
            RefreshSelected();
            Changed?.Invoke();
        }

        public void ToggleOpenSelected()
        {
            if (SelectedLift == null) return;
            SelectedLift.SetOpen(!SelectedLift.Open);
            RefreshSelected();
            Changed?.Invoke();
        }

        public void SetSelectedOpen(bool open)
        {
            if (SelectedLift == null) return;
            SelectedLift.SetOpen(open);
            RefreshSelected();
            Changed?.Invoke();
        }

        public void Rebuild()
        {
            snapshots.Clear();
            var network = ResortRouteNetwork.I;
            if (network != null)
            {
                foreach (var lift in network.Lifts)
                {
                    if (lift == null || lift.Definition == null) continue;
                    var logic = lift.StationLogic;
                    var congestion = InfrastructureCongestionRuntime.I;
                    float pressure = congestion != null ? congestion.GetLiftPressure01(lift) : 0f;
                    int queue = logic != null ? logic.QueueSize : 0;
                    int passengers = 0;
                    var vehicles = lift.GetComponentsInChildren<LiftVehicleRuntime>();
                    for (int i = 0; i < vehicles.Length; i++)
                        if (vehicles[i] != null) passengers += vehicles[i].OccupantCount;

                    float theoreticalSeats = Mathf.Max(1f, lift.Definition.seatsPerVehicle * Mathf.Max(1, lift.Definition.vehicles));
                    float utilization = Mathf.Clamp01(passengers / theoreticalSeats);
                    snapshots.Add(new LiftManagementSnapshot
                    {
                        id = string.IsNullOrWhiteSpace(lift.Definition.id) ? lift.name : lift.Definition.id,
                        displayName = string.IsNullOrWhiteSpace(lift.Definition.displayName) ? lift.name : lift.Definition.displayName,
                        type = lift.Definition.type,
                        open = lift.Open,
                        faulted = lift.Faulted,
                        length = lift.Length,
                        verticalDrop = lift.VerticalDrop,
                        speedKmh = lift.CurrentSpeedKmh,
                        capacityPerHour = lift.CapacityPerHour,
                        queue = queue,
                        passengersOnLine = passengers,
                        utilization01 = utilization,
                        pressure01 = pressure,
                        travelMinutes = float.IsInfinity(lift.TravelSeconds) ? 0f : lift.TravelSeconds / 60f,
                        underground = lift.IsUnderground
                    });
                }
            }
            RefreshSelected();
            Changed?.Invoke();
        }

        private void RefreshSelected()
        {
            SelectedSnapshot = default;
            if (SelectedLift == null) return;
            for (int i = 0; i < snapshots.Count; i++)
            {
                if (snapshots[i].id == (SelectedLift.Definition != null ? SelectedLift.Definition.id : string.Empty))
                {
                    SelectedSnapshot = snapshots[i];
                    return;
                }
            }
        }
    }
}
