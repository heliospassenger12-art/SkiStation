using UnityEngine;

namespace SkiStation
{
    public static class SlopeProfileBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (Object.FindFirstObjectByType<SlopeProfileTool>() != null) return;
            var go = new GameObject("SkiStation_SlopeProfileTool");
            var tool = go.AddComponent<SlopeProfileTool>();
            var ui = go.AddComponent<SlopeProfileToolUI>();
            typeof(SlopeProfileToolUI).GetField("tool", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(ui, tool);
        }
    }
}
