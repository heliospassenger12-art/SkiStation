using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// Minimal runtime assembly required to make the prototype genuinely playable in a fresh scene.
    /// It wires existing systems together; it does not replace their gameplay logic.
    /// </summary>
    public static class PlayableCoreBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            Ensure<StationRuntime>("SkiStation_StationRuntime");
            Ensure<GameClock>("SkiStation_GameClock");
            Ensure<GameSpeedController>("SkiStation_GameSpeed");
            Ensure<FinanceRuntime>("SkiStation_Finance");
            Ensure<PricingRuntime>("SkiStation_Pricing");
            Ensure<StationStatisticsRuntime>("SkiStation_Statistics");
            Ensure<VisitorIntentRuntime>("SkiStation_VisitorIntent");
            Ensure<VisitorBehaviorRuntime>("SkiStation_VisitorBehavior");
            Ensure<SlopeVisitorExperienceRuntime>("SkiStation_SlopeExperience");
            Ensure<SlopeGameplayDebugUI>("SkiStation_SlopeUI");
            Ensure<VisitorLoyaltyRuntime>("SkiStation_VisitorLoyalty");
            Ensure<ResortRouteNetwork>("SkiStation_ResortRouteNetwork");
            Ensure<SlopeNetwork>("SkiStation_SlopeNetwork");
            Ensure<StationVisitorDemandRuntime>("SkiStation_VisitorDemand");
            Ensure<PlayableCameraController>("SkiStation_CameraController");
            Ensure<PlayableGameHUD>("SkiStation_PlayableHUD");
            Ensure<PlayableLoopRuntime>("SkiStation_PlayableLoop");
            Ensure<LegacyUIPanelCoordinator>("SkiStation_UICoordinator");
            Ensure<DomainMapUI>("SkiStation_DomainMapUI");
            Ensure<MountainWeatherRuntime>("SkiStation_MountainWeather");
            Ensure<SurfaceConditionOptionsRuntime>("SkiStation_SurfaceConditionOptions");
            Ensure<SurfaceConditionOptionsUI>("SkiStation_SurfaceConditionOptionsUI");
            Ensure<SlopeConditionManagerRuntime>("SkiStation_SlopeConditionManager");
            Ensure<SlopeConditionUI>("SkiStation_SlopeConditionUI");
            Ensure<SnowGroomingFleetRuntime>("SkiStation_SnowGroomingFleet");
            Ensure<SnowGroomingFleetUI>("SkiStation_SnowGroomingFleetUI");
            Ensure<SkiStation.PisteOperations.MorningDomainOpeningRuntime>("SkiStation_MorningDomainOpening");
            Ensure<SkiStation.PisteOperations.MorningDomainOpeningUI>("SkiStation_MorningDomainOpeningUI");
            Ensure<SafetyOperationsRuntime>("SkiStation_SafetyOperations");
            Ensure<SafetyOperationsUI>("SkiStation_SafetyOperationsUI");
            Ensure<SlopeCongestionRuntime>("SkiStation_SlopeCongestion");
            Ensure<DomainSectorRuntime>("SkiStation_DomainSectors");
            Ensure<DomainSectorUI>("SkiStation_DomainSectorUI");
            Ensure<ManagedZoneRuntime>("SkiStation_ManagedZones");
            Ensure<ManagedZoneConstructionMode>("SkiStation_ManagedZoneConstruction");
            Ensure<ManagedZoneEquipmentRuntime>("SkiStation_ManagedZoneEquipment");
            Ensure<ManagedZoneEquipmentConstructionMode>("SkiStation_ManagedZoneEquipmentConstruction");
            Ensure<ManagedZoneSpecializationRuntime>("SkiStation_ManagedZoneSpecialization");
            Ensure<ManagedZoneSpecializationUI>("SkiStation_ManagedZoneSpecializationUI");
            Ensure<ManagedZoneReputationRuntime>("SkiStation_ManagedZoneReputation");
            Ensure<ManagedZoneReputationUI>("SkiStation_ManagedZoneReputationUI");
            Ensure<PiouPiouZoneRuntime>("SkiStation_PiouPiouZones");
            Ensure<PiouPiouConstructionMode>("SkiStation_PiouPiouConstruction");
            Ensure<SkiSchoolUI>("SkiStation_SkiSchoolUI");
            Ensure<SkiLessonProgressUI>("SkiStation_SkiLessonProgressUI");
            Ensure<PlayableStartupGuard>("SkiStation_PlayableStartupGuard");

            var liftPlanner = Object.FindFirstObjectByType<LiftConstructionPlanner>();
            if (liftPlanner == null)
            {
                var go = new GameObject("SkiStation_LiftConstructionPlanner");
                liftPlanner = go.AddComponent<LiftConstructionPlanner>();
                var definition = ScriptableObject.CreateInstance<LiftDefinition>();
                LiftCareerCatalog249.Configure(definition, "chair_fixed_2");
                liftPlanner.SetDefinition(definition);
                liftPlanner.enabled = true;
                liftPlanner.SetActive(false);
            }
            Ensure<LiftCareerCatalog249>("SkiStation_LiftCareerCatalog");
        }

        private static T Ensure<T>(string name) where T : Component
        {
            var existing = Object.FindFirstObjectByType<T>();
            if (existing != null) return existing;
            return new GameObject(name).AddComponent<T>();
        }
    }
}
