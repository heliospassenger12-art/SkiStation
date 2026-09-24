using UnityEngine;

namespace SkiStation
{
    /// <summary>Global accessibility switch for detailed snow-surface simulation.</summary>
    public sealed class SurfaceConditionOptionsRuntime : MonoBehaviour
    {
        public static SurfaceConditionOptionsRuntime I { get; private set; }
        public bool AdvancedSurfaceConditions { get; private set; } = false;
        public string ModeLabel => AdvancedSurfaceConditions ? "Conditions réalistes" : "Conditions simplifiées";

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetAdvancedSurfaceConditions(bool enabled)
        {
            AdvancedSurfaceConditions = enabled;
            foreach (var slope in Object.FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None))
                slope.GetComponent<SlopeConditionRuntime>()?.RefreshGameplayImpact();
        }
    }
}
