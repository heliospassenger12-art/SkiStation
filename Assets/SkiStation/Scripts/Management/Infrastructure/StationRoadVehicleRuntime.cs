using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>0.95: lightweight autonomous visual vehicles, always backed by real arriving visitors.</summary>
    public sealed class StationRoadVehicleRuntime : MonoBehaviour
    {
        public static StationRoadVehicleRuntime I { get; private set; }
        [SerializeField] private float refreshSeconds = 0.25f;
        [SerializeField] private float vehicleHeight = 0.65f;
        private readonly Dictionary<int, GameObject> vehicles = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, RoadVehicleAgentRuntime> agents = new Dictionary<int, RoadVehicleAgentRuntime>();
        private float nextRefresh;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }
        private void OnDestroy()
        {
            if (I == this) I = null;
            foreach (var kv in vehicles) if (kv.Value != null) Destroy(kv.Value);
            vehicles.Clear(); agents.Clear();
        }
        private void Update()
        {
            float dt = Mathf.Max(0.01f, refreshSeconds);
            foreach (var kv in agents)
                if (kv.Value != null) kv.Value.Tick(dt);

            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + refreshSeconds;
            Refresh();
        }

        private void Refresh()
        {
            var live = new HashSet<int>();
            foreach (var skier in FindObjectsOfType<SkierRuntime>())
            {
                if (skier == null || skier.GetComponent<ArrivalIdentity250>() != null) continue;
                int id = skier.GetInstanceID();
                var profile = skier.GetComponent<StationArrivalTransportProfile>();
                if (profile == null || !profile.IsVehicle) continue;
                if (skier.State == SkierState.Leaving ||
                    (StationArrivalFlowRuntime.I != null && StationArrivalFlowRuntime.I.IsPostDropOff(skier)))
                    continue;

                live.Add(id);
                if (!vehicles.TryGetValue(id, out var vehicle) || vehicle == null)
                {
                    vehicle = CreateVehicle(profile.transportType, skier.name);
                    vehicles[id] = vehicle;
                    var agent = vehicle.AddComponent<RoadVehicleAgentRuntime>();
                    agents[id] = agent;
                    agent.Initialize(skier, profile.transportType);
                }
            }

            var stale = new List<int>();
            foreach (var kv in vehicles)
                if (!live.Contains(kv.Key) || kv.Value == null) stale.Add(kv.Key);

            foreach (var id in stale)
            {
                if (vehicles.TryGetValue(id, out var vehicle) && vehicle != null) Destroy(vehicle);
                vehicles.Remove(id);
                agents.Remove(id);
            }
        }

        private GameObject CreateVehicle(StationArrivalTransportType type, string ownerName)
        {
            bool bus = type == StationArrivalTransportType.CoachBus;
            var root = new GameObject(bus ? "Vehicule_Autocar_" + ownerName : "Vehicule_Voiture_" + ownerName);
            root.transform.SetParent(transform, true);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = bus ? "Carrosserie_Autocar" : "Carrosserie_Voiture";
            var bodyCollider = body.GetComponent<Collider>();
            if (bodyCollider != null) bodyCollider.enabled = false;
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.up * vehicleHeight;
            body.transform.localScale = bus ? new Vector3(2.6f, 1.5f, 6f) : new Vector3(2f, 1.1f, 4.2f);

            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Habitacle";
            var roofCollider = roof.GetComponent<Collider>();
            if (roofCollider != null) roofCollider.enabled = false;
            roof.transform.SetParent(root.transform, false);
            roof.transform.localPosition = Vector3.up * (vehicleHeight + (bus ? 0.9f : 0.65f));
            roof.transform.localScale = bus ? new Vector3(2.25f, 1.0f, 3.2f) : new Vector3(1.7f, 0.75f, 2.1f);
            return root;
        }

        public int ActiveVehicleCount => vehicles.Count;

        public GameObject GetVehicle(SkierRuntime skier)
        {
            if (skier == null) return null;
            vehicles.TryGetValue(skier.GetInstanceID(), out var vehicle);
            return vehicle;
        }

        public RoadVehicleAgentRuntime GetAgent(SkierRuntime skier)
        {
            if (skier == null) return null;
            agents.TryGetValue(skier.GetInstanceID(), out var agent);
            return agent;
        }
    }
}
