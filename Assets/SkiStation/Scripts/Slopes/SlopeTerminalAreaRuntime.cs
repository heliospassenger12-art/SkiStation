using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum SlopeTerminalEndpointKind { Top, Bottom }

    /// <summary>
    /// 1.04: logical free-circulation plateau at a slope endpoint.
    /// It has no rendered path. Visitors can naturally disperse toward a lift,
    /// a service/activity, or leave the ski domain from the area.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SlopeTerminalAreaRuntime : MonoBehaviour
    {
        public static readonly List<SlopeTerminalAreaRuntime> All = new List<SlopeTerminalAreaRuntime>();

        public SlopeRuntime SourceSlope { get; private set; }
        public SlopeTerminalEndpointKind Kind { get; private set; }
        public Vector3 Center { get; private set; }
        public float Radius { get; private set; } = 16f;
        public bool IsBaseArea { get; private set; }

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() { All.Remove(this); }

        public void Configure(SlopeRuntime slope, SlopeTerminalEndpointKind kind, Vector3 center, float radius, bool isBaseArea)
        {
            SourceSlope = slope;
            Kind = kind;
            Center = center;
            Radius = Mathf.Clamp(radius, 5f, 45f);
            IsBaseArea = isBaseArea;
            transform.position = center;
            transform.name = (slope != null ? slope.name : "Slope") + "_FreePlateau_" + kind;
        }

        public bool Contains(Vector3 position)
        {
            return (position - Center).sqrMagnitude <= Radius * Radius;
        }

        public static SlopeTerminalAreaRuntime FindForSlope(SlopeRuntime slope, SlopeTerminalEndpointKind kind, Vector3 position)
        {
            SlopeTerminalAreaRuntime best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < All.Count; i++)
            {
                var area = All[i];
                if (area == null || area.SourceSlope != slope || area.Kind != kind) continue;
                float d = (area.Center - position).sqrMagnitude;
                if (d < bestDistance) { bestDistance = d; best = area; }
            }
            return best;
        }
    }
}
