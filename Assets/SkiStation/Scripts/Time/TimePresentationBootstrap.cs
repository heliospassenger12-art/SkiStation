using UnityEngine;

namespace SkiStation
{
    public static class TimePresentationBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (DayNightModeRuntime.I == null)
                new GameObject("DayNightModeRuntime").AddComponent<DayNightModeRuntime>();
            if (TimePresentationRuntime.I == null)
                new GameObject("TimePresentationRuntime").AddComponent<TimePresentationRuntime>();
            if (Object.FindObjectOfType<DayNightModeUI>() == null)
                new GameObject("DayNightModeUI").AddComponent<DayNightModeUI>();
        }
    }
}
