using System;
using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// Generates the actual management visitors of the game. These are real SkierRuntime
    /// agents consumed by the existing economy, route, lift and service systems — not debug ghosts.
    /// </summary>
    public sealed class StationVisitorDemandRuntime : MonoBehaviour
    {
        public static StationVisitorDemandRuntime I { get; private set; }
        [SerializeField] private float visitorsPerGameHour = 8f;
        [SerializeField] private int maxActiveVisitors = 80;
        [SerializeField] private float firstArrivalHour = 7f;
        [SerializeField] private float lastArrivalHour = 18f;
        [SerializeField] private float spawnRadius = 18f;
        private float accumulator;
        private int arrivalSequence;
        public int VisitorsOnRoad => ArrivalHub250.I != null ? ArrivalHub250.I.PendingVisitors : 0;

        private int activeVisitors;
        private float nextCountRefresh;
        public int ActiveVisitors => activeVisitors;
        public string ArrivalDiagnostic { get; private set; } = "Initialisation des arrivees...";
        private float nextDiagnosticLog;
        private void Explain(string reason)
        {
            ArrivalDiagnostic = reason;
            if (Time.unscaledTime >= nextDiagnosticLog)
            {
                nextDiagnosticLog = Time.unscaledTime + 12f;
                Debug.Log("[SkiStation ARRIVEES] " + reason, this);
            }
        }

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextCountRefresh)
            {
                activeVisitors = FindObjectsByType<SkierRuntime>(FindObjectsSortMode.None).Length;
                nextCountRefresh = Time.unscaledTime + .5f;
            }
            var clock = GameClock.I;
            // Visitors may arrive to visit the village even before the first piste/lift.
            // Skiing itself remains gated by the existing lift/slope journey logic.
            if (clock == null) { Explain("Attente de l'horloge du jeu."); return; }
            if (clock.CurrentHour < firstArrivalHour || clock.CurrentHour >= lastArrivalHour)
            { Explain("Hors horaires d'arrivee (07h-18h), heure : " + clock.CurrentHour); return; }
            if (ActiveVisitors + VisitorsOnRoad >= maxActiveVisitors)
            { Explain("Capacite de visiteurs atteinte : " + ActiveVisitors + "/" + maxActiveVisitors); return; }
            var road = StationRoadNetwork247.I;
            if (road == null || !road.IsReady)
            { Explain("Route principale non initialisee : impossible d'arriver hors de la route."); return; }
            ArrivalDiagnostic = "Route accessible ; arrivees autorisees (" + ActiveVisitors + "/" + maxActiveVisitors + ").";

            float reputationFactor = ManagedZoneReputationRuntime.I != null
                ? Mathf.Lerp(.72f, 1.35f, ManagedZoneReputationRuntime.I.StationReputation01) : 1f;
            accumulator += Time.deltaTime / Mathf.Max(.1f, clock.RealSecondsPerGameHour) * visitorsPerGameHour * reputationFactor;
            accumulator = Mathf.Min(accumulator, 16f);
            var hub = ArrivalHub250.I;
            if (hub == null || !hub.Ready) return;
            // Repeatable mix for this prototype: walk, car, walk, bus, car.
            int sequence = arrivalSequence % 5;
            ArrivalMode250 mode = sequence == 0 || sequence == 2 ? ArrivalMode250.Walk : sequence == 3 ? ArrivalMode250.Bus : ArrivalMode250.Car;
            if (!hub.HasSpace(mode)) mode = ArrivalMode250.Walk;
            int groupSize = mode == ArrivalMode250.Bus ? 8 : mode == ArrivalMode250.Car ? 2 : 1;
            if (accumulator >= groupSize && ActiveVisitors + VisitorsOnRoad + groupSize <= maxActiveVisitors && hub.TryDispatch(mode, groupSize))
            { accumulator -= groupSize; arrivalSequence++; }
            ArrivalDiagnostic = "En vehicule : " + VisitorsOnRoad + " | Parking : " + hub.ParkedCars + "/12 voitures, " + hub.ParkedBuses + "/3 bus.";
        }

        internal SkierRuntime CreateArrivedVisitor(Vector3[] roadPath, ArrivalMode250 mode)
        {
            if (roadPath == null || roadPath.Length < 2) return null;
            Vector3 basePoint = roadPath[0];
            var go = new GameObject();
            go.name = "Visitor_" + DateTime.Now.Ticks;
            Vector3 arrival = basePoint; // No random spawn on cliffs or outside the road.
            go.transform.position = arrival;
            activeVisitors++;
            ArrivalDiagnostic = "Visiteur cree sur la route principale ; progression vers le village.";

            var identity = go.AddComponent<ArrivalIdentity250>();
            identity.Mode = mode; identity.OwnEquipment = UnityEngine.Random.value < .65f; identity.PrepaidPass = UnityEngine.Random.value < .2f;
            var skier = go.AddComponent<SkierRuntime>();
            if (identity.PrepaidPass) VisitorSkiAdmission.GrantPrepaid(skier);
            var profile = go.AddComponent<SkierPopulationProfile>();
            ConfigureProfile(profile);
            ApplyVisitorLook(go, profile.ToCustomerProfile());
            go.AddComponent<SkierRouteAgent>();
            go.AddComponent<SkierMountainJourneyRuntime>();
            go.AddComponent<VisitorLifecycleRuntime>();
            var arrivalWalk = go.AddComponent<StationVisitorRoadArrival248>();
            arrivalWalk.Configure(roadPath);

            CustomerProfile customer = profile.ToCustomerProfile();
            StationRuntime.I?.AddVisitor(customer, skier.Satisfaction);
            return skier; // Admission continues on foot; the vehicle has already parked.

        }

        internal static void SellDayPass(CustomerProfile profile)
        {
            if (StationRuntime.I == null) return;
            const float basePrice = 52f;
            float price = PricingRuntime.I != null
                ? PricingRuntime.I.GetPrice("ski_pass_day", FinanceCategory.SkiPass, basePrice, profile)
                : basePrice;

            // Lightweight profile discounts for the first playable economy loop.
            if (profile == CustomerProfile.Youth) price *= .65f;
            else if (profile == CustomerProfile.Family) price *= .82f;
            else if (profile == CustomerProfile.Athlete) price *= 1.05f;

            StationRuntime.I.Earn(price, FinanceCategory.SkiPass, profile, "ski_pass_day", "Forfait jour");
            PlayableLoopRuntime.I?.SetMessage("Nouveau visiteur : forfait vendu " + price.ToString("N0") + " EUR.");
        }

        private static void ApplyVisitorLook(GameObject go, CustomerProfile profile)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Veste";
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = Vector3.up * .95f;
            body.transform.localScale = new Vector3(.65f, .8f, .55f);
            UnityEngine.Object.Destroy(body.GetComponent<Collider>());
            var renderer = body.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = RuntimeMaterialFactory.Create(Color.white);
                if (mat != null)
                {
                    switch (profile)
                    {
                        case CustomerProfile.Family: mat.color = new Color(.95f,.45f,.12f); break;
                        case CustomerProfile.Youth: mat.color = new Color(.15f,.55f,.92f); break;
                        case CustomerProfile.Athlete: mat.color = new Color(.85f,.12f,.18f); break;
                        case CustomerProfile.Beginner: mat.color = new Color(.25f,.72f,.32f); break;
                        default: mat.color = new Color(.25f,.38f,.65f); break;
                    }
                    renderer.sharedMaterial = mat;
                    var helmet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    helmet.name = "Casque"; helmet.transform.SetParent(go.transform, false);
                    helmet.transform.localPosition = Vector3.up * 1.85f;
                    helmet.transform.localScale = Vector3.one * .42f;
                    helmet.GetComponent<Renderer>().sharedMaterial = mat;
                    UnityEngine.Object.Destroy(helmet.GetComponent<Collider>());
                    for (int side = -1; side <= 1; side += 2)
                    {
                        var ski = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        ski.name = "Ski"; ski.transform.SetParent(go.transform, false);
                        ski.transform.localPosition = new Vector3(side * .22f, .08f, .15f);
                        ski.transform.localScale = new Vector3(.13f, .08f, 1.9f);
                        ski.GetComponent<Renderer>().sharedMaterial = mat;
                        UnityEngine.Object.Destroy(ski.GetComponent<Collider>());
                    }
                }
            }
        }

        private Vector3 FindSpawnPoint()
        {
            return ManagementSurface.Entrance;
        }

        private static LiftRuntime FindNearestOpenLift(Vector3 point)
        {
            LiftRuntime best = null;
            float bestDistance = float.MaxValue;
            foreach (var lift in FindObjectsByType<LiftRuntime>(FindObjectsSortMode.None))
            {
                if (lift == null || !lift.Open || lift.Faulted) continue;
                if (ResortRouteNetwork.I == null || ResortRouteNetwork.I.FindSlopeFromLift(lift) == null) continue;
                float d = (lift.Bottom - point).sqrMagnitude;
                if (d < bestDistance) { bestDistance = d; best = lift; }
            }
            return best;
        }

        private static void ConfigureProfile(SkierPopulationProfile profile)
        {
            float reputation = ManagedZoneReputationRuntime.I != null ? ManagedZoneReputationRuntime.I.StationReputation01 : .5f;
            float family = .20f * (ManagedZoneReputationRuntime.I != null ? Mathf.Lerp(.8f, 1.35f, ManagedZoneReputationRuntime.I.GetProfileAttraction(CustomerProfile.Family)) : 1f);
            float youth = .08f * (ManagedZoneReputationRuntime.I != null ? Mathf.Lerp(.8f, 1.35f, ManagedZoneReputationRuntime.I.GetProfileAttraction(CustomerProfile.Youth)) : 1f);
            float sport = .10f * (ManagedZoneReputationRuntime.I != null ? Mathf.Lerp(.8f, 1.45f, ManagedZoneReputationRuntime.I.GetProfileAttraction(CustomerProfile.Athlete)) : 1f);
            float scale = Mathf.Max(.01f, family + youth + sport);
            float baseSpecialized = .38f + reputation * .18f;
            family = family / scale * baseSpecialized;
            youth = youth / scale * baseSpecialized;
            sport = sport / scale * baseSpecialized;
            float r = UnityEngine.Random.value;
            if (r < family) profile.Configure("Famille", SkierPopulationRole.Adult, SkierPopulationArchetype.Family, "F" + UnityEngine.Random.Range(1000, 9999), false, null, "F" + UnityEngine.Random.Range(1000, 9999), UnityEngine.Random.Range(28, 56));
            else if (r < family + youth) profile.Configure("Jeune", SkierPopulationRole.Child, SkierPopulationArchetype.Family, "F" + UnityEngine.Random.Range(1000, 9999), false, null, "F" + UnityEngine.Random.Range(1000, 9999), UnityEngine.Random.Range(3, 16));
            else if (r < family + youth + sport) profile.Configure("Sportif", SkierPopulationRole.EliteAthlete, SkierPopulationArchetype.EliteTeam, null, false, "Ski", null, UnityEngine.Random.Range(18, 36));
            else if (r < .55f) profile.Configure("Couple", SkierPopulationRole.Adult, SkierPopulationArchetype.Couple, null, false, null, null, UnityEngine.Random.Range(24, 60));
            else if (r < .78f) profile.Configure("Amis", SkierPopulationRole.Adult, SkierPopulationArchetype.Friends, null, false, null, null, UnityEngine.Random.Range(18, 55));
            else profile.Configure("Vacancier", SkierPopulationRole.Adult, SkierPopulationArchetype.Solo, null, false, null, null, UnityEngine.Random.Range(18, 65));
        }
    }
}
