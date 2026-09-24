using UnityEngine;

namespace SkiStation
{
    /// <summary>0.87 bootstrap for the real visitor access-traffic observer.</summary>
    public sealed class AccessTrafficBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntime()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (AccessRoadTrafficRuntime.I != null) return;
            var go = new GameObject("SkiStation_AccessTrafficRuntime");
            DontDestroyOnLoad(go);
            go.AddComponent<AccessRoadTrafficRuntime>();
        }
    }
}
