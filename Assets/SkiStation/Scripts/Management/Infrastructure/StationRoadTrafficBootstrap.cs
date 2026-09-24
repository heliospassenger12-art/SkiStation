using UnityEngine;
namespace SkiStation
{
    public sealed class StationRoadTrafficBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (StationRoadTrafficRuntime.I != null) return;
            var go = new GameObject("SkiStation_0.93_RoadTrafficRuntime");
            go.AddComponent<StationRoadTrafficRuntime>();
            DontDestroyOnLoad(go);
        }
    }
}
