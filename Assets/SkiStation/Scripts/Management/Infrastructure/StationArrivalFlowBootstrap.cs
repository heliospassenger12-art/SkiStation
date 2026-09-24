using UnityEngine;

namespace SkiStation
{
    /// <summary>0.91: creates the arrival-flow runtime and diagnostic UI once.</summary>
    public sealed class StationArrivalFlowBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntime()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            var go = GameObject.Find("SkiStation_AccessFlowRuntime");
            if (go == null)
            {
                go = new GameObject("SkiStation_AccessFlowRuntime");
                Object.DontDestroyOnLoad(go);
            }
            if (StationArrivalFlowRuntime.I == null) go.AddComponent<StationArrivalFlowRuntime>();
            if (StationArrivalFlowUIExists() == false) go.AddComponent<StationArrivalFlowUI>();
        }
        private static bool StationArrivalFlowUIExists() { return Object.FindObjectOfType<StationArrivalFlowUI>() != null; }
    }
}
