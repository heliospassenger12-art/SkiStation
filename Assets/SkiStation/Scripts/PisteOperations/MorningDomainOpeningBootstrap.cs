using UnityEngine;
namespace SkiStation.PisteOperations
{
    public static class MorningDomainOpeningBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (Object.FindObjectOfType<MorningDomainOpeningRuntime>() == null)
                new GameObject("SkiStation_MorningDomainOpening").AddComponent<MorningDomainOpeningRuntime>();
            if (Object.FindObjectOfType<MorningDomainOpeningUI>() == null)
                new GameObject("SkiStation_MorningDomainOpeningUI").AddComponent<MorningDomainOpeningUI>();
        }
    }
}
