using UnityEngine;
namespace SkiStation
{
    public static class DayNightModeUIBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (Object.FindObjectOfType<DayNightModeUI>() == null)
                new GameObject("DayNightModeUI").AddComponent<DayNightModeUI>();
        }
    }
}
