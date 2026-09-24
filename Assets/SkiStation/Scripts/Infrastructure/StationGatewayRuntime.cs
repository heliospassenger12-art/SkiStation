using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public class StationGatewayRuntime : MonoBehaviour
    {
        public static StationGatewayRuntime I { get; private set; }

        [SerializeField] private StationGatewayDefinition definition;
        [SerializeField] private Transform externalSpawnPoint;
        [SerializeField] private Transform gatewayArrivalPoint;
        [SerializeField] private Transform dropOffPoint;
        [SerializeField] private Transform parkingTargetPoint;
        [SerializeField] private Transform pedestrianTargetPoint;

        private readonly HashSet<int> processedVisitors = new HashSet<int>();
        private int arrivalsThisHour;
        private int trackedDay = -1;
        private int trackedHour = -1;
        private float detectionTimer;

        public StationGatewayDefinition Definition => definition;
        public Transform ExternalSpawnPoint => externalSpawnPoint;
        public Transform GatewayArrivalPoint => gatewayArrivalPoint;
        public Transform DropOffPoint => dropOffPoint;
        public Transform ParkingTargetPoint => parkingTargetPoint;
        public Transform PedestrianTargetPoint => pedestrianTargetPoint;

        public Vector3 GetDropOffSlotPosition(int slotIndex)
        {
            if (dropOffPoint == null) return transform.position;
            var def = definition;
            float radius = def != null ? Mathf.Max(0.5f, def.dropOffSlotRadius) : 2.5f;
            if (slotIndex <= 0) return dropOffPoint.position;
            // Deterministic lateral slots keep cars/buses separated without requiring extra prefabs.
            int side = (slotIndex % 2 == 0) ? 1 : -1;
            int row = Mathf.CeilToInt(slotIndex / 2f);
            return dropOffPoint.position + dropOffPoint.right * (side * radius) + dropOffPoint.forward * (row * radius * 0.8f);
        }
        public int ArrivalsToday { get; private set; }
        public bool IsProcessed(SkierRuntime skier) => skier != null && processedVisitors.Contains(skier.GetInstanceID());
        public int ArrivalsThisHour => arrivalsThisHour;
        public event Action Changed;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        private void Update()
        {
            ResetHourlyCounterIfNeeded();
            detectionTimer -= Time.deltaTime;
            if (detectionTimer > 0f || gatewayArrivalPoint == null) return;
            detectionTimer = 0.25f;

            var skiers = FindObjectsOfType<SkierRuntime>();
            float radiusSq = Mathf.Pow(definition != null ? definition.arrivalDetectionRadius : 5f, 2f);
            foreach (var skier in skiers)
            {
                if (skier == null || processedVisitors.Contains(skier.GetInstanceID())) continue;
                if ((skier.transform.position - gatewayArrivalPoint.position).sqrMagnitude > radiusSq) continue;
                ProcessArrival(skier);
            }
        }

        public void Configure(StationGatewayDefinition def, Transform externalSpawn, Transform arrival, Transform dropOff = null, Transform parking = null, Transform pedestrian = null)
        {
            definition = def;
            externalSpawnPoint = externalSpawn;
            gatewayArrivalPoint = arrival;
            dropOffPoint = dropOff;
            parkingTargetPoint = parking;
            pedestrianTargetPoint = pedestrian;
            Changed?.Invoke();
        }

        public void ProcessArrival(SkierRuntime skier)
        {
            if (skier == null || processedVisitors.Contains(skier.GetInstanceID())) return;
            if (!CanAcceptArrival()) return;

            processedVisitors.Add(skier.GetInstanceID());
            arrivalsThisHour++;
            ArrivalsToday++;
            var profile = skier.GetComponent<SkierPopulationProfile>();
            if (StationRuntime.I != null && profile != null)
                StationRuntime.I.AddVisitor(profile.ToCustomerProfile());
            StationEntranceRuntime.I?.RegisterArrival(1);
            skier.SetState(SkierState.Arriving);
            Changed?.Invoke();
        }

        public bool CanAcceptArrival()
        {
            int capacity = definition != null ? definition.arrivalCapacityPerHour : 240;
            return arrivalsThisHour < Mathf.Max(1, capacity);
        }

        private void ResetHourlyCounterIfNeeded()
        {
            if (GameClock.I == null) return;
            int day = GameClock.I.CurrentDay;
            int hour = Mathf.FloorToInt(GameClock.I.GameHour);
            if (day != trackedDay || hour != trackedHour)
            {
                trackedDay = day;
                trackedHour = hour;
                arrivalsThisHour = 0;
            }
        }

        public static Transform ResolveExternalSpawnPoint()
        {
            return I != null && I.definition != null && I.definition.useExternalSpawn && I.externalSpawnPoint != null
                ? I.externalSpawnPoint : null;
        }
    }
}
