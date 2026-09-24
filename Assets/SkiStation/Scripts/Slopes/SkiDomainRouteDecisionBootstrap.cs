using UnityEngine;

namespace SkiStation
{
    /// <summary>1.00 bootstrap for the real skier/lift/slope decision bridge.</summary>
    public static class SkiDomainRouteDecisionBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Ensure()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (SkiDomainRouteDecisionRuntime.I == null)
                new GameObject("SkiDomainRouteDecisionRuntime").AddComponent<SkiDomainRouteDecisionRuntime>();
            if (VisitorBehaviorRuntime.I == null)
                new GameObject("VisitorBehaviorRuntime").AddComponent<VisitorBehaviorRuntime>();
            if (ResortRouteNetwork.I == null)
                new GameObject("ResortRouteNetwork").AddComponent<ResortRouteNetwork>();
        }
    }
}
