using System;
using System.Collections.Generic;
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
        public event Action<StationServiceRuntime> ServiceBuilt;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
        }

        public StationServiceRuntime Build(StationServiceType type, Vector3 position, ServiceRange range = ServiceRange.Standard, string customName = null)
        {
            float cost = GetBuildCost(type, range);
            if (StationRuntime.I != null && !StationRuntime.I.Spend(cost, FinanceCategory.Construction, type.ToString(), "Construction service 1.30")) return null;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = string.IsNullOrWhiteSpace(customName) ? DefaultName(type) : customName;
            // Sample the actual management mesh, not the older procedural mountain.
            if (ConstructionTerrainAdaptation251.TrySampleHeight(position.x, position.z, out float ground))
                position.y = ground;
            else if (MountainRuntime.I != null && MountainRuntime.I.TryGetSurface(position, out var surface))
                position = surface;
            ConstructionTerrainAdaptation251.Flatten(position, 16f, 16f, 6f);
            ConstructionClearance249.ClearTrees(new Vector3[] { position }, 14f);
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
            definition.serviceId = "svc_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            definition.serviceName = go.name;
            definition.type = type;
            definition.range = range;
            ApplyPreset(definition);
            if (type == StationServiceType.SkiSchool) definition.skiSchoolZoneType = SkiSchoolZoneType.StandardSchool;

            var runtime = go.AddComponent<StationServiceRuntime>();
            runtime.definition = definition;
            if (type == StationServiceType.SkiSchool) {
                var school = go.AddComponent<SkiSchoolRuntime>();
                school.service = runtime;
                school.zoneType = definition.skiSchoolZoneType;
                school.maxGroupSize = definition.lessonMaxGroupSize;
            }
            BuiltServices.Add(runtime);
            ServiceBuilt?.Invoke(runtime);
            return runtime;
        }

        public void Configure(StationServiceRuntime service, ServiceRange range, int capacity, int staff, float price, float quality, bool familyFriendly)
        {
            if (service == null || service.definition == null) return;
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
            if (service == null || service.definition == null || service.definition.type != StationServiceType.SkiSchool) return;
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
                case StationServiceType.Shop: d.capacity = 12; d.maxQueue = 18; d.serviceMinutes = 7f; d.basePrice = 35f; d.quality = 76f; d.stock = 500; d.unlimitedStock = false; break;
                case StationServiceType.Restaurant: d.capacity = 24; d.maxQueue = 30; d.serviceMinutes = 35f; d.basePrice = 45f; d.quality = 78f; d.familyFriendly = true; break;
                case StationServiceType.Bar: d.capacity = 18; d.maxQueue = 24; d.serviceMinutes = 25f; d.basePrice = 28f; d.quality = 76f; d.openAtNight = true; d.closingHour = 23f; break;
                case StationServiceType.EquipmentRental: d.capacity = 10; d.maxQueue = 20; d.serviceMinutes = 12f; d.basePrice = 32f; d.quality = 77f; d.stock = 120; d.unlimitedStock = false; break;
                case StationServiceType.SkiSchool: d.capacity = 8; d.maxQueue = 16; d.serviceMinutes = 60f; d.basePrice = 75f; d.quality = 82f; d.staffRequired = 2; d.lessonFormat = SkiSchoolServiceFormat.Flexible; d.lessonMaxGroupSize = 8; d.youthSpecialization = .9f; d.beginnerSpecialization = .9f; break;
                case StationServiceType.Activity: d.capacity = 16; d.maxQueue = 20; d.serviceMinutes = 30f; d.basePrice = 25f; d.quality = 78f; break;
                case StationServiceType.Accommodation: d.capacity = 20; d.maxQueue = 12; d.serviceMinutes = 10f; d.basePrice = 140f; d.quality = 82f; d.staffRequired = 2; d.openAtNight = true; d.closingHour = 24f; break;
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
