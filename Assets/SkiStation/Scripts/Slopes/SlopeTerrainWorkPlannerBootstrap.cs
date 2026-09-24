using UnityEngine;

namespace SkiStation
{
    public sealed class SlopeTerrainWorkPlannerBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Start()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            var mode = FindObjectOfType<SlopeTerrainWorkPlannerMode>();
            if (mode == null) mode = new GameObject("SkiStation_SlopeTerrainWorkPlannerMode").AddComponent<SlopeTerrainWorkPlannerMode>();
            var ui = FindObjectOfType<SlopeTerrainWorkPlannerUI>();
            if (ui == null) ui = new GameObject("SkiStation_SlopeTerrainWorkPlannerUI").AddComponent<SlopeTerrainWorkPlannerUI>();
            ui.Bind(mode);
        }
    }
}
