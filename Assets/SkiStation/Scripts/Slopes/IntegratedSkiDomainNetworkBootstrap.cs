using UnityEngine;

namespace SkiStation
{
    public static class IntegratedSkiDomainNetworkBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Ensure()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (IntegratedSkiDomainNetworkRuntime.I != null) return;
            Object.FindObjectOfType<IntegratedSkiDomainNetworkRuntime>();
            if (IntegratedSkiDomainNetworkRuntime.I == null)
                new GameObject("IntegratedSkiDomainNetwork").AddComponent<IntegratedSkiDomainNetworkRuntime>();

            if (Object.FindObjectOfType<IntegratedSkierDomainBootstrap>() == null)
                new GameObject("IntegratedSkierDomainBootstrap").AddComponent<IntegratedSkierDomainBootstrap>();
        }
    }
}
