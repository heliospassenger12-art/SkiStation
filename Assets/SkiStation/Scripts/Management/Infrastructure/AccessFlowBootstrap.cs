using UnityEngine;

namespace SkiStation
{
    public sealed class AccessFlowBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntime()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            var go = GameObject.Find("SkiStation_AccessFlowRuntime");
            if (go == null) { go = new GameObject("SkiStation_AccessFlowRuntime"); Object.DontDestroyOnLoad(go); }
            if (AccessFlowDirectionRuntime.I == null) go.AddComponent<AccessFlowDirectionRuntime>();
            if (FindObjectOfType<AccessFlowDirectionUI>() == null) go.AddComponent<AccessFlowDirectionUI>();
        }
    }
}
