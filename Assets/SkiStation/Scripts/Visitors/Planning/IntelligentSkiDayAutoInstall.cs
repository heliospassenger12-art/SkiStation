using UnityEngine;

namespace SkiStation
{
    public static class IntelligentSkiDayAutoInstall
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (Object.FindObjectOfType<IntelligentSkiDayBootstrap>() != null) return;
            GameObject go = new GameObject("SkiStation_IntelligentSkiDay_1_78");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<IntelligentSkiDayBootstrap>();
        }
    }
}
