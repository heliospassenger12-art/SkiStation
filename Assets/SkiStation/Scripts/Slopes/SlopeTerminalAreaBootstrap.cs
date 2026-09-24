using UnityEngine;

namespace SkiStation
{
    public static class SlopeTerminalAreaBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (Object.FindObjectOfType<SlopeTerminalAreaBuilder>() != null) return;
            new GameObject("SlopeTerminalAreaBuilder").AddComponent<SlopeTerminalAreaBuilder>();
        }
    }
}
