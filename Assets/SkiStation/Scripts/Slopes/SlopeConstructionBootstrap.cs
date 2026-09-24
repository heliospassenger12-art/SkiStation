using UnityEngine;

namespace SkiStation
{
    public sealed class SlopeConstructionBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (FindFirstObjectByType<SlopeConstructionMode>() != null) return;
            var go = new GameObject("SlopeConstructionMode");
            go.AddComponent<SlopeConstructionMode>();
        }
    }
}
