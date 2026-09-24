using UnityEngine;

namespace SkiStation
{
    public static class GroupSkiCoordinatorBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (GroupSkiCoordinatorRuntime.I != null) return;
            GameObject go = new GameObject("Group Ski Coordinator 1.81");
            go.AddComponent<GroupSkiCoordinatorRuntime>();
        }
    }
}
