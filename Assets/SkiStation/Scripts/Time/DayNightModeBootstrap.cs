using UnityEngine;
namespace SkiStation
{
    public static class DayNightModeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (DayNightModeRuntime.I == null)
                new GameObject("DayNightModeRuntime").AddComponent<DayNightModeRuntime>();
        }
    }
}
