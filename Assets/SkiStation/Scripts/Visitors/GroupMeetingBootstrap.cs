using UnityEngine;

namespace SkiStation
{
    public static class GroupMeetingBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (GroupMeetingRuntime.I != null) return;
            GameObject go = new GameObject("Group Meeting Runtime");
            go.AddComponent<GroupMeetingRuntime>();
        }
    }
}
