using UnityEngine;

namespace SkiStation
{
    /// <summary>0.99: installs the mountain routing/observation layer without creating any simulated visitors.</summary>
    public sealed class SkiDomainCirculationBootstrap : MonoBehaviour
    {
        [SerializeField] private bool createPlanner = true;
        [SerializeField] private bool createRuntime = true;
        [SerializeField] private bool createDebugUI = false;

        private void Awake()
        {
            if (createPlanner && SkiDomainRoutePlanner.I == null)
                new GameObject("SkiDomainRoutePlanner").AddComponent<SkiDomainRoutePlanner>();
            if (createRuntime && SkiDomainCirculationRuntime.I == null)
                new GameObject("SkiDomainCirculationRuntime").AddComponent<SkiDomainCirculationRuntime>();
            if (createDebugUI && FindObjectOfType<SkiDomainCirculationDebugUI>() == null)
                new GameObject("SkiDomainCirculationDebugUI").AddComponent<SkiDomainCirculationDebugUI>();
        }
    }
}
