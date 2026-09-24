using UnityEngine;

namespace SkiStation
{
    public sealed class MountainTerrainWorkBootstrap : MonoBehaviour
    {
        void Start()
        {
            var mountain = MountainRuntime.I;
            if (mountain == null || !mountain.IsGenerated) return;
            var works = FindObjectOfType<MountainTerrainWorkRuntime>();
            if (works == null) works = new GameObject("SkiStation_TerrainWorks").AddComponent<MountainTerrainWorkRuntime>();
            works.Initialize(mountain.GeneratedResolution);
            var mode = FindObjectOfType<MountainTerrainWorkMode>();
            if (mode == null) mode = new GameObject("SkiStation_TerrainWorkMode").AddComponent<MountainTerrainWorkMode>();
            var ui = FindObjectOfType<MountainTerrainWorkUI>();
            if (ui == null) { var go = new GameObject("SkiStation_TerrainWorkUI"); ui = go.AddComponent<MountainTerrainWorkUI>(); }
            ui.Bind(mode);
        }
    }
}
