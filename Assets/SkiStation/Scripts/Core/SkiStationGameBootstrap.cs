using UnityEngine;

namespace SkiStation
{
    public sealed class SkiStationGameBootstrap : MonoBehaviour
    {
        [SerializeField] private bool createDiagnosticsUI = true;
        [SerializeField] private bool diagnosticsVisibleOnStart = false;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            EnsureRuntimeObjects();
        }

        private void EnsureRuntimeObjects()
        {
            if (SkiStationRuntimeRegistry.I == null)
                new GameObject("SkiStation_RuntimeRegistry").AddComponent<SkiStationRuntimeRegistry>();

            if (VisitorServicePlannerRuntime.I == null)
                new GameObject("SkiStation_VisitorServicePlanner").AddComponent<VisitorServicePlannerRuntime>();

            if (SkiStationDiagnosticsRuntime.I == null)
                new GameObject("SkiStation_DiagnosticsRuntime").AddComponent<SkiStationDiagnosticsRuntime>();

            if (createDiagnosticsUI && FindObjectOfType<SkiStationDiagnosticsUI>() == null)
            {
                var ui = new GameObject("SkiStation_DiagnosticsUI").AddComponent<SkiStationDiagnosticsUI>();
                ui.Configure(diagnosticsVisibleOnStart);
            }

            if (MountainFoundationBootstrapExists() == false)
            {
                var mountainRoot = new GameObject("SkiStation_MountainFoundation");
                mountainRoot.AddComponent<MountainFoundationBootstrap>();
                mountainRoot.AddComponent<MountainFoundationDebugUI>();
                mountainRoot.AddComponent<MountainTerrainWorkBootstrap>();
            }
        }

        private static bool MountainFoundationBootstrapExists()
        {
            return FindObjectOfType<MountainFoundationBootstrap>() != null;
        }
    }
}
