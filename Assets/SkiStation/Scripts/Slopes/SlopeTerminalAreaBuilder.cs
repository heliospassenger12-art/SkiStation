using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// Creates invisible logical free-circulation areas where a slope meets a sufficiently
    /// flat plateau. No NavMesh, road or snow ribbon is generated.
    /// </summary>
    public sealed class SlopeTerminalAreaBuilder : MonoBehaviour
    {
        [SerializeField] private float maxTerminalAngle = 8f;
        [SerializeField] private float defaultRadius = 16f;
        [SerializeField] private float baseAltitudeThreshold = 80f;
        [SerializeField] private bool buildTopAreas = true;
        [SerializeField] private bool buildBottomAreas = true;
        [SerializeField] private float refreshSeconds = 1.5f;
        private float nextRefresh;

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + Mathf.Max(.25f, refreshSeconds);
            RebuildMissingAreas();
        }

        public void RebuildMissingAreas()
        {
            var slopes = Object.FindObjectsOfType<SlopeRuntime>();
            for (int i = 0; i < slopes.Length; i++)
            {
                var slope = slopes[i];
                if (slope == null || slope.Points.Count < 2) continue;
                if (buildTopAreas && IsFlatAtEndpoint(slope, true)) EnsureArea(slope, SlopeTerminalEndpointKind.Top, slope.TopPoint);
                if (buildBottomAreas && IsFlatAtEndpoint(slope, false)) EnsureArea(slope, SlopeTerminalEndpointKind.Bottom, slope.BottomPoint);
            }
        }

        private bool IsFlatAtEndpoint(SlopeRuntime slope, bool top)
        {
            int i = top ? 0 : slope.Points.Count - 1;
            int j = top ? 1 : slope.Points.Count - 2;
            Vector3 delta = slope.Points[j] - slope.Points[i];
            if (delta.sqrMagnitude < .01f) return true;
            float angle = Vector3.Angle(delta, Vector3.ProjectOnPlane(delta, Vector3.up));
            return angle <= maxTerminalAngle;
        }

        private void EnsureArea(SlopeRuntime slope, SlopeTerminalEndpointKind kind, Vector3 point)
        {
            var existing = SlopeTerminalAreaRuntime.FindForSlope(slope, kind, point);
            if (existing != null) return;
            var go = new GameObject(slope.name + "_FreePlateau_" + kind);
            var area = go.AddComponent<SlopeTerminalAreaRuntime>();
            bool baseArea = kind == SlopeTerminalEndpointKind.Bottom && point.y <= baseAltitudeThreshold;
            area.Configure(slope, kind, point, defaultRadius, baseArea);
        }
    }
}
