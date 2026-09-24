using UnityEngine;

namespace SkiStation
{
    public sealed class ConstructionFoundationBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (ConstructionPolicyRuntime.I == null) new GameObject("SkiStation_ConstructionPolicy").AddComponent<ConstructionPolicyRuntime>();
            if (SlopeConstructionProjectManager.I == null) new GameObject("SkiStation_ConstructionManager").AddComponent<SlopeConstructionProjectManager>();
            if (FindFirstObjectByType<SlopeConstructionProjectTool>() == null) new GameObject("SlopeConstructionProjectTool").AddComponent<SlopeConstructionProjectTool>();
            if (FindFirstObjectByType<SlopeConstructionProjectUI>() == null) new GameObject("SlopeConstructionProjectUI").AddComponent<SlopeConstructionProjectUI>();
        }
    }
}
