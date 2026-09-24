using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public class LiftRuntime : MonoBehaviour
    {
        public LiftDefinition Definition { get; private set; }
        public Vector3 Bottom { get; private set; }
        public Vector3 Top { get; private set; }
        public IReadOnlyList<Vector3> Route => route;
        public bool IsUnderground { get; private set; }
        public float Length { get; private set; }
        public float VerticalDrop { get; private set; }
        public float HorizontalLength { get; private set; }
        public bool Open { get; private set; } = true;
        public bool Faulted { get; private set; }
        public float CurrentSpeedKmh { get; private set; }

        private readonly List<Vector3> route = new List<Vector3>();

        public string LiftId => Definition != null && !string.IsNullOrWhiteSpace(Definition.id) ? Definition.id : name;

        public int EffectiveVehicleCapacity
        {
            get
            {
                if (Definition == null) return 1;
                float multiplier = LiftTechnologyRuntime.Instance != null
                    ? LiftTechnologyRuntime.Instance.GetCapacityMultiplier(LiftId) : 1f;
                return Mathf.Max(1, Mathf.CeilToInt(Definition.seatsPerVehicle * multiplier));
            }
        }

        public float CapacityPerHour
        {
            get
            {
                if (Definition == null || Definition.spacingSeconds <= 0f) return 0f;
                return (3600f / Definition.spacingSeconds) * EffectiveVehicleCapacity;
            }
        }

        public float EffectiveSpeedKmh
        {
            get
            {
                if (Definition == null) return 0f;
                float multiplier = 1f;
                if (Definition.detachable) multiplier *= 1.25f;
                if (Definition.variableSpeed) multiplier *= 1.08f;
                if (LiftTechnologyRuntime.Instance != null)
                    multiplier *= LiftTechnologyRuntime.Instance.GetSpeedMultiplier(LiftId);
                if (MountainWeatherRuntime.I != null)
                    multiplier *= MountainWeatherRuntime.I.LiftSpeedMultiplier;
                return Definition.speedKmh * multiplier;
            }
        }

        public void Build(LiftDefinition definition, Vector3 bottom, Vector3 top)
        {
            var simpleRoute = new List<Vector3> { bottom, top };
            Build(definition, simpleRoute, false);
        }

        public void Build(LiftDefinition definition, IReadOnlyList<Vector3> points, bool underground)
        {
            Definition = definition;
            route.Clear();
            if (points != null) route.AddRange(points);
            if (route.Count < 2) return;

            Bottom = route[0];
            Top = route[route.Count - 1];
            IsUnderground = underground;
            Length = 0f;
            for (int i = 1; i < route.Count; i++) Length += Vector3.Distance(route[i - 1], route[i]);
            VerticalDrop = Mathf.Abs(Top.y - Bottom.y);
            HorizontalLength = Vector3.Distance(new Vector3(Bottom.x, 0f, Bottom.z), new Vector3(Top.x, 0f, Top.z));
            CurrentSpeedKmh = EffectiveSpeedKmh;

            if (!underground && AlpineDomainTerrainBridgeRuntime.I != null)
            {
                AlpineCorridorAssessment terrainAssessment = AlpineDomainTerrainBridgeRuntime.I.AssessLift(route);
                Debug.Log("Lift terrain assessment: " + LiftId + " | score " + terrainAssessment.score.ToString("0") + "/100 | stations/corridor " + (terrainAssessment.suitable ? "OK" : "CHECK"));
            }

            var visual = GetComponent<LiftVisualBuilder>() ?? gameObject.AddComponent<LiftVisualBuilder>();
            visual.Build(definition, route, underground);
            var protection = GetComponent<LiftProtectionRuntime>() ?? gameObject.AddComponent<LiftProtectionRuntime>();
            protection.Bind(this);
            var stationLogic = GetComponent<LiftStationLogic>() ?? gameObject.AddComponent<LiftStationLogic>();
            stationLogic.Bind(this);
            var stationOperations = GetComponent<LiftStationOperationsRuntime>() ?? gameObject.AddComponent<LiftStationOperationsRuntime>();
            stationOperations.Bind(this);
            LiftStationFlowBootstrap.Ensure(this);
            if (ResortRouteNetwork.I == null)
                new GameObject("ResortRouteNetwork").AddComponent<ResortRouteNetwork>();
            ResortRouteNetwork.I?.RegisterLift(this);
        }

        public void SetOpen(bool value)
        {
            Open = value;
            CurrentSpeedKmh = (!Open || Faulted) ? 0f : EffectiveSpeedKmh;
        }
        public void RefreshPerformance()
        {
            CurrentSpeedKmh = (!Open || Faulted) ? 0f : EffectiveSpeedKmh;
        }

        public void SetFaulted(bool value) { Faulted = value; RefreshPerformance(); }
        public float TravelSeconds => CurrentSpeedKmh <= 0.01f ? float.PositiveInfinity : Length / (CurrentSpeedKmh / 3.6f);

        public LiftStationLogic StationLogic => GetComponent<LiftStationLogic>();

        public bool TryQueueSkier(SkierRuntime skier) => StationLogic != null && StationLogic.Enqueue(skier);
    }
}
