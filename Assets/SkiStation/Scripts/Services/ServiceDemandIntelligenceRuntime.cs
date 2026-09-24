using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    [Serializable]
    public struct ServiceDemandSnapshot
    {
        public StationServiceType type;
        public int facilities;
        public int openFacilities;
        public int activeCustomers;
        public int queuedCustomers;
        public int capacity;
        public int requestsToday;
        public int refusedToday;
        public float pressure01;
        public float unmetDemand01;
        public string status;
    }

    /// <summary>
    /// 1.85 management intelligence. Converts the existing service simulation into concise
    /// demand signals that future construction/management menus can consume without adding
    /// micromanagement. It observes only; StationServiceRuntime remains authoritative.
    /// </summary>
    public sealed class ServiceDemandIntelligenceRuntime : MonoBehaviour
    {
        public static ServiceDemandIntelligenceRuntime I { get; private set; }
        [SerializeField, Min(1f)] private float refreshSeconds = 5f;
        private readonly Dictionary<StationServiceType, ServiceDemandSnapshot> snapshots = new Dictionary<StationServiceType, ServiceDemandSnapshot>();
        private float timer;
        public event Action Changed;

        public IReadOnlyDictionary<StationServiceType, ServiceDemandSnapshot> Snapshots => snapshots;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject); Refresh();
        }

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < refreshSeconds) return;
            timer = 0f; Refresh();
        }

        public bool TryGet(StationServiceType type, out ServiceDemandSnapshot snapshot) => snapshots.TryGetValue(type, out snapshot);

        public StationServiceType MostPressuredType()
        {
            StationServiceType best = StationServiceType.Restaurant; float pressure = -1f;
            foreach (var pair in snapshots) if (pair.Value.unmetDemand01 > pressure) { pressure = pair.Value.unmetDemand01; best = pair.Key; }
            return best;
        }

        public void Refresh()
        {
            snapshots.Clear();
            Array types = Enum.GetValues(typeof(StationServiceType));
            for (int i = 0; i < types.Length; i++)
            {
                StationServiceType type = (StationServiceType)types.GetValue(i);
                int facilities = 0, open = 0, active = 0, queued = 0, capacity = 0, requests = 0, refused = 0;
                for (int s = 0; s < StationServiceRuntime.All.Count; s++)
                {
                    StationServiceRuntime service = StationServiceRuntime.All[s];
                    if (service == null || service.definition == null || service.definition.type != type) continue;
                    facilities++; if (service.IsOpen()) open++;
                    active += service.ActiveCustomers; queued += service.QueueCount; capacity += service.EffectiveCapacity;
                    requests += service.Stats.totalRequests; refused += service.Stats.refused;
                }
                float queuePressure = capacity > 0 ? Mathf.Clamp01(queued / (float)Mathf.Max(1, capacity)) : (queued > 0 ? 1f : 0f);
                float refusalPressure = requests > 0 ? Mathf.Clamp01(refused / (float)requests) : 0f;
                float occupancy = capacity > 0 ? Mathf.Clamp01(active / (float)capacity) : 0f;
                float unmet = Mathf.Clamp01(queuePressure * .50f + refusalPressure * .35f + occupancy * .15f);
                string status = facilities == 0 ? "Absent" : open == 0 ? "Fermé" : unmet >= .72f ? "Saturé" : unmet >= .45f ? "Sous pression" : unmet >= .22f ? "Actif" : "Confortable";
                snapshots[type] = new ServiceDemandSnapshot { type = type, facilities = facilities, openFacilities = open, activeCustomers = active, queuedCustomers = queued, capacity = capacity, requestsToday = requests, refusedToday = refused, pressure01 = Mathf.Max(queuePressure, occupancy), unmetDemand01 = unmet, status = status };
            }
            Changed?.Invoke();
        }
    }

    public static class ServiceDemandIntelligenceBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (ServiceDemandIntelligenceRuntime.I != null) return;
            GameObject go = new GameObject("Service Demand Intelligence 1.85");
            go.AddComponent<ServiceDemandIntelligenceRuntime>();
        }
    }
}
