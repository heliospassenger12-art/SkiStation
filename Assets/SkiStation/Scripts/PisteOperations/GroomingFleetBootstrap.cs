using UnityEngine;
using SkiStation;

namespace SkiStation.PisteOperations
{
    public static class GroomingFleetBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (GroomingGarageOperationsRuntime.I == null)
                new GameObject("SkiStation_GroomingGarageOperations").AddComponent<GroomingGarageOperationsRuntime>();
            GroomingGarageOperationsRuntime.I.EnsureStarterGarage();

            if (GroomingTeamRuntime.I == null)
                new GameObject("SkiStation_GroomingTeams").AddComponent<GroomingTeamRuntime>();
            if (GroomingFleetStrategyRuntime.I == null)
                new GameObject("SkiStation_GroomingFleetStrategy").AddComponent<GroomingFleetStrategyRuntime>();
            if (GroomingFleetRuntime.I == null)
                new GameObject("SkiStation_GroomingFleet").AddComponent<GroomingFleetRuntime>();
            if (Object.FindObjectOfType<GroomingGarageUI>() == null)
                new GameObject("SkiStation_GroomingGarageUI").AddComponent<GroomingGarageUI>();
        }
    }
}
