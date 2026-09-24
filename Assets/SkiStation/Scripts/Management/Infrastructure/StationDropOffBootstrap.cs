using UnityEngine;

namespace SkiStation
{
    public sealed class StationDropOffBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntime()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            var go = GameObject.Find("SkiStation_StationArrivalInfrastructure");
            if (go == null) { go = new GameObject("SkiStation_StationArrivalInfrastructure"); Object.DontDestroyOnLoad(go); }
            if (StationParkingRuntime.I == null) go.AddComponent<StationParkingRuntime>();
            if (StationDropOffRuntime.I == null) go.AddComponent<StationDropOffRuntime>();
        }
    }
}
