using UnityEngine;

namespace SkiStation
{
    [CreateAssetMenu(menuName = "SkiStation/Project Config", fileName = "SkiStationProjectConfig")]
    public sealed class SkiStationProjectConfig : ScriptableObject
    {
        [Header("Project")]
        public string projectVersion = SkiStationVersion.Current;
        public string unityTarget = SkiStationVersion.ProjectTarget;

        [Header("Gameplay policy")]
        [Tooltip("Management simulation remains the source of truth. Do not generate fake visitors.")]
        public bool managementSimulationIsAuthoritative = true;
        [Tooltip("Player simulation remains isolated from management consequences.")]
        public bool playerSimulationIsIsolated = true;
    }
}
