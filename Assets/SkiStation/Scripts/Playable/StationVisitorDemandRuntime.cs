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
            Color jacket = new Color(.25f,.38f,.65f);
            switch (profile)
            {
                case CustomerProfile.Family: jacket = new Color(.92f,.34f,.10f); break;
                case CustomerProfile.Youth: jacket = new Color(.08f,.48f,.88f); break;
                case CustomerProfile.Athlete: jacket = new Color(.82f,.08f,.12f); break;
                case CustomerProfile.Beginner: jacket = new Color(.18f,.62f,.28f); break;
            }
            Color pants = Color.Lerp(jacket, new Color(.035f,.045f,.06f), .72f);
            Color skin = new Color(.83f,.64f,.49f);
            Color gear = new Color(.055f,.065f,.08f);
            Material jacketMat = RuntimeMaterialFactory.Create(jacket);
            Material pantsMat = RuntimeMaterialFactory.Create(pants);
            Material skinMat = RuntimeMaterialFactory.Create(skin);
            Material gearMat = RuntimeMaterialFactory.Create(gear);
            // 2.70: visual-only individuality. No visitor state, route or decision is changed.
            Color accent = Color.Lerp(jacket, Color.white, .28f);
            Material accentMat = RuntimeMaterialFactory.Create(accent);
            bool hasBackpack = UnityEngine.Random.value < .42f;
            bool hasScarf = UnityEngine.Random.value < .30f;
            bool hasBeanie = UnityEngine.Random.value < .24f;
            bool hasBrightGloves = UnityEngine.Random.value < .55f;

            System.Func<string, PrimitiveType, Vector3, Vector3, Material, GameObject> part = (name,type,pos,scale,mat) => {
                var x=GameObject.CreatePrimitive(type); x.name=name; x.transform.SetParent(go.transform,false);
                x.transform.localPosition=pos; x.transform.localScale=scale; var c=x.GetComponent<Collider>(); if(c!=null) UnityEngine.Object.Destroy(c);
                var r=x.GetComponent<Renderer>(); if(r!=null && mat!=null) r.sharedMaterial=mat; return x; };

            // More human management avatar: torso, head, articulated-looking limbs and winter gear.
            part("Veste",PrimitiveType.Capsule,new Vector3(0,1.12f,0),new Vector3(.55f,.58f,.42f),jacketMat);
            part("Pantalon",PrimitiveType.Capsule,new Vector3(0,.55f,0),new Vector3(.43f,.38f,.36f),pantsMat);
            part("Tete",PrimitiveType.Sphere,new Vector3(0,1.82f,0),Vector3.one*.34f,skinMat);
            part("Casque",PrimitiveType.Sphere,new Vector3(0,1.96f,0),new Vector3(.39f,.24f,.39f),gearMat);
            part("Masque",PrimitiveType.Cube,new Vector3(0,1.91f,.285f),new Vector3(.34f,.12f,.055f),accentMat);
            part("BandeCasque",PrimitiveType.Cube,new Vector3(0,1.965f,.325f),new Vector3(.40f,.055f,.035f),accentMat);
            part("ZipVeste",PrimitiveType.Cube,new Vector3(0,1.18f,.225f),new Vector3(.035f,.48f,.025f),accentMat);
            if(hasBackpack){
                part("SacADos",PrimitiveType.Cube,new Vector3(0,1.16f,-.245f),new Vector3(.38f,.48f,.18f),gearMat);
                part("SacAccent",PrimitiveType.Cube,new Vector3(0,1.18f,-.345f),new Vector3(.22f,.16f,.025f),accentMat);
            }
            if(hasScarf) part("Echarpe",PrimitiveType.Cylinder,new Vector3(0,1.58f,0),new Vector3(.23f,.07f,.23f),accentMat);
            if(hasBeanie){
                part("Bonnet",PrimitiveType.Sphere,new Vector3(0,2.06f,0),new Vector3(.31f,.16f,.31f),accentMat);
                part("Pompon",PrimitiveType.Sphere,new Vector3(0,2.19f,0),Vector3.one*.10f,accentMat);
            }
            // 2.71: extra readable details from the management camera, still visual-only.
            part("ColVeste",PrimitiveType.Cylinder,new Vector3(0,1.55f,0),new Vector3(.25f,.055f,.25f),gearMat);
            for(int side=-1;side<=1;side+=2){
                part("Bras",PrimitiveType.Capsule,new Vector3(side*.39f,1.13f,.02f),new Vector3(.16f,.42f,.16f),jacketMat).transform.localRotation=Quaternion.Euler(0,0,side*12f);
                part("Gant",PrimitiveType.Sphere,new Vector3(side*.50f,.83f,.02f),Vector3.one*.15f,hasBrightGloves ? accentMat : gearMat);
                part("Jambe",PrimitiveType.Capsule,new Vector3(side*.18f,.35f,0),new Vector3(.18f,.42f,.18f),pantsMat);
                part("Chaussure",PrimitiveType.Cube,new Vector3(side*.18f,.10f,.08f),new Vector3(.24f,.16f,.48f),gearMat);
                var ski=part("Ski",PrimitiveType.Cube,new Vector3(side*.20f,.025f,.18f),new Vector3(.105f,.045f,1.85f),jacketMat);
                ski.transform.localRotation=Quaternion.Euler(0,side*1.5f,0);
                part("Fixation",PrimitiveType.Cube,new Vector3(side*.20f,.075f,.10f),new Vector3(.13f,.07f,.26f),gearMat);
                part("Spatule",PrimitiveType.Cube,new Vector3(side*.20f,.065f,1.08f),new Vector3(.11f,.055f,.18f),accentMat).transform.localRotation=Quaternion.Euler(-10f,side*1.5f,0);
                var pole=part("Baton",PrimitiveType.Cylinder,new Vector3(side*.55f,.70f,-.03f),new Vector3(.025f,.62f,.025f),gearMat);
                pole.transform.localRotation=Quaternion.Euler(8f,0,side*7f);
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
