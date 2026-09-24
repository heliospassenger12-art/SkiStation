using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// Snaps an existing slope centerline to the generated mountain surface before mesh generation.
    /// This keeps construction data and the visual ribbon aligned with the actual terrain.
    /// </summary>
    public sealed class SlopeTerrainConformityRuntime : MonoBehaviour
    {
        [SerializeField] private float surfaceOffset = 0.06f;
        [SerializeField] private bool conformOnEnable = true;
        public bool LastConformSucceeded { get; private set; }
        public int SnappedPointCount { get; private set; }

        private void OnEnable()
        {
            if (conformOnEnable) Conform();
        }

        public bool Conform()
        {
            var slope = GetComponent<SlopeRuntime>();
            if (slope == null || slope.Points.Count < 2)
            {
                LastConformSucceeded = false;
                return false;
            }

            var points = new List<Vector3>(slope.Points.Count);
            var widths = new List<float>(slope.Widths.Count);
            SnappedPointCount = 0;
            for (int i = 0; i < slope.Points.Count; i++)
            {
                Vector3 p = slope.Points[i];
                if (ManagementSurface.TryGet(p, out var surface))
                {
                    p = surface + Vector3.up * surfaceOffset;
                    SnappedPointCount++;
                }
                points.Add(p);
                widths.Add(slope.GetWidthAt(i));
            }

            slope.Build(points, widths);
            LastConformSucceeded = SnappedPointCount == points.Count;
            return LastConformSucceeded;
        }
    }
}
