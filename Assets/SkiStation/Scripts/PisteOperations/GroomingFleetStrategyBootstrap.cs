using UnityEngine;
using SkiStation.PisteOperations;

public static class GroomingFleetStrategyBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

        if (Object.FindFirstObjectByType<GroomingFleetStrategyRuntime>() != null) return;
        var go = new GameObject("SkiStation_GroomingFleetStrategy");
        go.AddComponent<GroomingFleetStrategyRuntime>();
    }
}
