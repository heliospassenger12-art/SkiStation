using System;
using System.Collections.Generic;
using SkiStation.AlpineEmpires;
using UnityEngine;

namespace SkiStation
{
    /// <summary>Builds and configures concrete 1.30 service buildings. It creates real runtime services; no visitors are spawned.</summary>
    public sealed class StationServiceConstructionRuntime : MonoBehaviour
    {
        public static StationServiceConstructionRuntime I { get; private set; }
        public static readonly List<StationServiceRuntime> BuiltServices = new();
        [SerializeField] private Transform serviceRoot;
        [SerializeField] private float defaultBuildCost = 50000f;
        private static readonly Vector2 ServiceSize = new Vector2(16f, 16f);
        public event Action<StationServiceRuntime> ServiceBuilt;
        public string LastFailure349 { get; private set; }
        public bool ValidateBuild349(Vector3 point, float cost, out string reason)
            => ValidateBuild349(point, cost, out reason, out _, out _);

        internal static bool ValidatePlacement349(Vector3 point, out string reason)
            => ValidatePlacement349(point, out reason, out _, out _);

        private static bool ValidatePlacement349(Vector3 point, out string reason, out string companyId, out string sectorId)
        {
            companyId = sectorId = null;
            if (!ConstructionPlacement349.ValidateSite(point, ServiceSize, out reason)) return false;
            var menu = SkiStationStartMenu249.Owner263;
            if (menu != null && menu.IsAlpineSlot263)
                return menu.TryAuthorizeAlpineService263(point, ServiceSize, out companyId, out sectorId, out reason);
            return true;
        }

        private bool ValidateBuild349(Vector3 point, float cost, out string reason, out string companyId, out string sectorId)
        {
            if (!ValidatePlacement349(point, out reason, out companyId, out sectorId)) return false;
            reason = ConstructionPlacement349.BudgetReason(cost);
            return reason.Length == 0;
        }

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
        }

        public StationServiceRuntime Build(StationServiceType type, Vector3 position, ServiceRange range = ServiceRange.Standard, string customName = null)
        {
            float cost = GetBuildCost(type, range);
            if (!ValidateBuild349(position, cost, out var reason, out var companyId, out var sectorId)) { LastFailure349 = reason; return null; }
            if (!StationRuntime.I.Spend(cost, FinanceCategory.Construction, type.ToString(), "Construction service"))
            { LastFailure349 = "Fonds insuffisants"; return null; }
            LastFailure349 = "";
            var service = Materialize(type, position, range, customName, companyId, sectorId);
            Publish(service, cost);
            return service;
        }

        private StationServiceRuntime Materialize(StationServiceType type, Vector3 position, ServiceRange range,
            string customName, string companyId, string sectorId, string serviceId = null, Action<GameObject> track = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            track?.Invoke(go);
            go.name = string.IsNullOrWhiteSpace(customName) ? DefaultName(type) : customName;
            if (track == null)
            {
                ConstructionTerrainAdaptation251.Flatten(position, 16f, 16f, 6f);
                ConstructionClearance249.ClearTrees(new Vector3[] { position }, 14f);
            }
            go.transform.position = position + Vector3.up * 3f;
            go.transform.localScale = new Vector3(12f, 6f, 12f);
            if (serviceRoot != null && serviceRoot.gameObject.activeInHierarchy)
                go.transform.SetParent(serviceRoot, true);
            go.SetActive(true);
            // Detailed placeholder is generated after confirmation; never modify the working lift flow.
            StationServiceVisual278.Apply(go, type, go.name);
            Debug.Log("[SkiStation Restaurant Visibilite] Batiment cree : " + go.name +
                      " | position=" + go.transform.position + " | actif=" + go.activeInHierarchy);

            var definition = ScriptableObject.CreateInstance<StationServiceDefinition>();
            definition.serviceId = serviceId ?? ("svc_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            definition.serviceName = go.name;
            definition.type = type;
            definition.range = range;
            ApplyPreset(definition);
            if (type == StationServiceType.SkiSchool) definition.skiSchoolZoneType = SkiSchoolZoneType.StandardSchool;

            var runtime = go.AddComponent<StationServiceRuntime>();
            runtime.definition = definition;
            runtime.AlpineCompanyId = companyId;
            runtime.AlpineTerritoryId = sectorId;
            go.AddComponent<StationStructureStatus249>();
            if (type == StationServiceType.SkiSchool) {
                var school = go.AddComponent<SkiSchoolRuntime>();
                school.service = runtime;
                school.zoneType = definition.skiSchoolZoneType;
                school.maxGroupSize = definition.lessonMaxGroupSize;
            }
            BuiltServices.Add(runtime);
            // The same confirmed access is visible and available to real pedestrians.
            if (StationPedestrianAccess249.TryPlan(StationRoadNetwork247.I, position, 16f, out var entrance, out var roadside, out _))
            {
                var access = StationPedestrianAccess249.CreateVisual(entrance, roadside,
                    RuntimeMaterialFactory.Create(new Color(.66f, .61f, .51f)), "Acces_pieton_" + go.name);
                StationPedestrianNetwork249.GetOrCreate().Register(go, access, entrance, roadside);
            }
            else if (track != null)
                throw new InvalidOperationException("Le raccordement piéton du chantier allié a échoué.");
            return runtime;
        }

        private void Publish(StationServiceRuntime runtime, float cost)
        {
            ServiceBuilt?.Invoke(runtime);
            var site376 = ConstructionSiteRuntime376.BeginService(runtime, cost);
            if (site376 != null)
                PlayableFeedbackRuntime.I?.Show("Chantier bâtiment lancé • " + Mathf.RoundToInt(site376.DurationGameMinutes) + " min de jeu", 4f);
        }

        internal AlpineActionResult BuildAlliedRestaurant(AlpineSession world, string managerId, string companyId,
            string operationId, Vector3 point)
        {
            if (world == null || SkiStationStartMenu249.Owner263?.ActiveAlpineWorld263 != world
                || !world.TryGetManager(companyId, out var company) || company.IsHuman || company.ManagerId != managerId
                || string.IsNullOrWhiteSpace(operationId))
                return AlpineActionResult.Invalid;
            var footprint = AlpineManagementLand.ServiceFootprint(point, new Vector2(28f, 28f));
            if (world.TryBuildRetry(operationId, managerId, companyId, footprint, out var prior)) return prior;
            if (ConstructionPolicyRuntime.I != null && !ConstructionPolicyRuntime.I.InstantConstruction)
                return AlpineActionResult.Forbidden;

            var rights = world.CheckWorldConstruction(managerId, companyId, footprint, out var territoryId);
            if (rights != AlpineActionResult.Applied) return rights;
            if (!ConstructionPlacement349.ValidateSite(point, ServiceSize, out _)
                || !StationPedestrianAccess249.TryPlan(StationRoadNetwork247.I, point, 16f, out _, out _, out _))
                return AlpineActionResult.Forbidden;
            // This first allied construction uses an already level site and therefore does not mutate terrain
            // before its payment is committed.
            for (int x = -1; x <= 1; x++)
                for (int z = -1; z <= 1; z++)
                    if (!ManagementSurface.TryGet(point + new Vector3(x * 14f, 0, z * 14f), out var sample)
                        || Mathf.Abs(sample.y - point.y) > .1f)
                        return AlpineActionResult.Forbidden;

            const StationServiceType type = StationServiceType.Restaurant;
            const ServiceRange range = ServiceRange.Standard;
            float cost = GetBuildCost(type, range);
            if (!AlpineSession.Finite(cost) || cost <= 0f || cost > 20000000f) return AlpineActionResult.Invalid;
            var order = new AlpineCommand
            {
                kind = AlpineActionKind.ServiceBuilt, companyId = companyId, managerId = managerId,
                operationId = operationId, subjectId = "svc_" + ManagementDomainFileStore261.Hash(operationId).Substring(0, 20),
                cents = Mathf.RoundToInt(cost * 100f), sectorId = territoryId, footprint = footprint,
                category = FinanceCategory.Construction, customer = CustomerProfile.Tourist, day = GameClock.I != null ? GameClock.I.CurrentDay : 1,
                hour = GameClock.I != null ? GameClock.I.CurrentHour : 0f
            };
            StationServiceRuntime built = null;
            GameObject staged = null;
            var result = world.BuildService(order, () =>
            {
                try
                {
                    built = Materialize(type, point, range, "Restaurant · " + company.StationId,
                        companyId, territoryId, order.subjectId, go => staged = go);
                    return built != null;
                }
                catch (Exception ex)
                {
                    if (staged != null)
                    {
                        StationPedestrianNetwork249.I?.Unregister349(staged);
                        var service = staged.GetComponent<StationServiceRuntime>();
                        if (service != null)
                        {
                            BuiltServices.Remove(service);
                            if (service.definition != null) Destroy(service.definition);
                        }
                        staged.SetActive(false);
                        Destroy(staged);
                    }
                    Debug.LogException(ex);
                    return false;
                }
            });
            if (result == AlpineActionResult.Applied)
            {
                try { Publish(built, cost); }
                catch (Exception ex) { Debug.LogException(ex); } // The registered building and payment are already committed.
            }
            return result;
        }

#if UNITY_EDITOR
        internal string TestAlpineAlliedRestaurant(AlpineSession world)
        {
            const string operation = "bloc2.allied.restaurant";
            if (world == null) return "Ouvre d’abord la station Alpine Empires.";
            if (world.HasBuiltService(operation)) return "L’entreprise B a déjà financé ce restaurant.";
            if (ConstructionPolicyRuntime.I != null && !ConstructionPolicyRuntime.I.InstantConstruction)
                return "Passe en construction instantanée pour cet essai.";
            var route = StationRoadNetwork247.I?.GetPermanentRoadPath();
            if (route == null) return "Route permanente indisponible.";
            foreach (var roadPoint in route)
                foreach (var offset in new[] { -42f, 42f, -58f, 58f })
                {
                    if (!ManagementSurface.TryGet(roadPoint + new Vector3(offset, 0, 0), out var point)) continue;
                    var result = BuildAlliedRestaurant(world, "manager-2", "company-2", operation, point);
                    if (result == AlpineActionResult.Applied) return "Restaurant construit et payé par l’entreprise B.";
                    if (result == AlpineActionResult.InsufficientFunds) return "Fonds insuffisants pour l’entreprise B.";
                    if (result != AlpineActionResult.Forbidden && result != AlpineActionResult.Undiscovered) return "Chantier refusé : " + result;
                }
            return "Aucune emprise plane et raccordable sur la concession B de cette partie.";
        }
#endif

        public void Configure(StationServiceRuntime service, ServiceRange range, int capacity, int staff, float price, float quality, bool familyFriendly)
        {
            if (service == null || service.definition == null || service.UsesAlliedAccount) return;
            var d = service.definition;
            d.range = range;
            d.capacity = Mathf.Max(1, capacity);
            d.staffRequired = Mathf.Max(0, staff);
            d.staffAvailable = Mathf.Max(0, staff);
            d.basePrice = Mathf.Max(0f, price);
            d.quality = Mathf.Clamp(quality, 0f, 100f);
            d.familyFriendly = familyFriendly;
        }

        public void ConfigureSkiSchool(StationServiceRuntime service, SkiSchoolZoneType zoneType, SkiSchoolServiceFormat format, int maxGroupSize = 8)
        {
            if (service == null || service.definition == null || service.UsesAlliedAccount || service.definition.type != StationServiceType.SkiSchool) return;
            service.definition.skiSchoolZoneType = zoneType;
            service.definition.lessonFormat = format;
            service.definition.lessonMaxGroupSize = Mathf.Max(2, maxGroupSize);
            var school = service.GetComponent<SkiSchoolRuntime>();
            if (school != null) { school.zoneType = zoneType; school.maxGroupSize = service.definition.lessonMaxGroupSize; }
        }

        public float GetBuildCost(StationServiceType type, ServiceRange range)
        {
            float typeFactor = type == StationServiceType.Accommodation ? 4f : type == StationServiceType.Restaurant ? 1.5f : type == StationServiceType.SkiSchool ? 1.25f : 1f;
            float rangeFactor = range == ServiceRange.Luxury ? 2.8f : range == ServiceRange.Premium ? 1.9f : range == ServiceRange.Pro || range == ServiceRange.Expert ? 1.7f : range == ServiceRange.Economy ? .75f : 1f;
            return defaultBuildCost * typeFactor * rangeFactor;
        }

        private static void ApplyPreset(StationServiceDefinition d)
        {
            switch (d.type)
            {
                case StationServiceType.Shop: d.capacity = 12; d.maxQueue = 18; d.serviceMinutes = 7f; d.basePrice = 350f; d.quality = 76f; d.stock = 500; d.unlimitedStock = false; d.sellsEquipment = true; break;
                case StationServiceType.Restaurant: d.capacity = 24; d.maxQueue = 30; d.serviceMinutes = 35f; d.basePrice = 45f; d.quality = 78f; d.familyFriendly = true; break;
                case StationServiceType.Bar: d.capacity = 18; d.maxQueue = 24; d.serviceMinutes = 25f; d.basePrice = 28f; d.quality = 76f; d.openAtNight = true; d.closingHour = 23f; break;
                case StationServiceType.EquipmentRental: d.capacity = 10; d.maxQueue = 20; d.serviceMinutes = 12f; d.basePrice = 32f; d.quality = 77f; d.stock = 120; d.unlimitedStock = false; break;
                case StationServiceType.SkiSchool: d.capacity = 8; d.maxQueue = 16; d.serviceMinutes = 60f; d.basePrice = 75f; d.quality = 82f; d.staffRequired = d.staffAvailable = 2; d.lessonFormat = SkiSchoolServiceFormat.Flexible; d.lessonMaxGroupSize = 8; d.youthSpecialization = .9f; d.beginnerSpecialization = .9f; break;
                case StationServiceType.Activity: d.capacity = 16; d.maxQueue = 20; d.serviceMinutes = 30f; d.basePrice = 25f; d.quality = 78f; break;
                case StationServiceType.Accommodation: d.capacity = 20; d.maxQueue = 12; d.serviceMinutes = 10f; d.basePrice = 140f; d.quality = 82f; d.staffRequired = d.staffAvailable = 2; d.openAtNight = true; d.closingHour = 24f; break;
            }
        }

        private static string DefaultName(StationServiceType type) => type switch
        {
            StationServiceType.Shop => "Boutique", StationServiceType.Restaurant => "Restaurant", StationServiceType.Bar => "Bar",
            StationServiceType.EquipmentRental => "Location de matériel", StationServiceType.SkiSchool => "École de ski",
            StationServiceType.Activity => "Activité", StationServiceType.Accommodation => "Hébergement", _ => "Service"
        };
    }
}
