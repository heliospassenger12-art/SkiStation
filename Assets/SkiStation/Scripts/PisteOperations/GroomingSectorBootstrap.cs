using UnityEngine;

namespace SkiStation.PisteOperations
{
    public static class GroomingSectorBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if(GroomingSectorDispatchRuntime.I==null)
            {
                var go=new GameObject("GroomingSectorDispatch");
                go.AddComponent<GroomingSectorDispatchRuntime>();
                go.AddComponent<GroomingSectorDebugUI>();
            }
        }
    }
}
