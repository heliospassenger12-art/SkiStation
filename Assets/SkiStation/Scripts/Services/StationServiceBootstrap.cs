using UnityEngine;

namespace SkiStation
{
    public static class StationServiceBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Ensure()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (StationServiceConstructionRuntime.I == null) new GameObject("SkiStation_ServiceConstruction").AddComponent<StationServiceConstructionRuntime>();
            if (Object.FindFirstObjectByType<StationServiceManagementUI>() == null) new GameObject("SkiStation_ServiceManagementUI").AddComponent<StationServiceManagementUI>();
        }
    }
}
