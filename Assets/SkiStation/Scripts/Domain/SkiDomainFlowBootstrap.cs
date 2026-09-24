using UnityEngine;

namespace SkiStation
{
    public static class SkiDomainFlowBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Ensure()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (SkiDomainFlowRuntime.I != null) return;
            var go = new GameObject("SkiDomainFlowRuntime");
            go.AddComponent<SkiDomainFlowRuntime>();
        }
    }
}
