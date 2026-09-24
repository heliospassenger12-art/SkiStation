using UnityEngine;

namespace SkiStation
{
    public sealed class AccessIntersectionBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntime()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            var go = GameObject.Find("SkiStation_AccessFlowRuntime");
            if (go == null)
            {
                go = new GameObject("SkiStation_AccessFlowRuntime");
                DontDestroyOnLoad(go);
            }
            if (AccessIntersectionTrafficRuntime.I == null) go.AddComponent<AccessIntersectionTrafficRuntime>();
            if (AccessTrafficHotspotRuntime.I == null) go.AddComponent<AccessTrafficHotspotRuntime>();
            if (FindObjectOfType<AccessTrafficHotspotUI>() == null) go.AddComponent<AccessTrafficHotspotUI>();
        }
    }
}
