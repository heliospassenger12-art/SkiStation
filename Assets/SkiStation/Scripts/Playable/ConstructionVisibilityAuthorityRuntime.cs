using UnityEngine;

namespace SkiStation
{
    /// <summary>2.17: guarantees construction mode remains readable regardless of presentation/weather scripts.</summary>
    public sealed class ConstructionVisibilityAuthorityRuntime : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (FindFirstObjectByType<ConstructionVisibilityAuthorityRuntime>() == null)
                new GameObject("SkiStation_2.17_ConstructionVisibilityAuthority").AddComponent<ConstructionVisibilityAuthorityRuntime>();
        }

        private void LateUpdate()
        {
            bool buildingSlope = SlopeConstructionMode.Instance != null && SlopeConstructionMode.Instance.IsDrawing;
            var lift = FindFirstObjectByType<LiftConstructionPlanner>();
            bool buildingLift = lift != null && lift.IsActive;
            if (buildingSlope || buildingLift) RenderSettings.fog = false;
        }
    }
}
