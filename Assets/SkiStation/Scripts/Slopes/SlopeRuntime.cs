using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public sealed class SlopeRuntime : MonoBehaviour
    {
        public IReadOnlyList<Vector3> Points => points;
        public IReadOnlyList<float> Widths => widths;
        public Vector3 TopPoint => points.Count > 0 ? points[0] : transform.position;
        public Vector3 BottomPoint => points.Count > 0 ? points[points.Count - 1] : transform.position;
        public float Width { get; private set; }
        public float Length { get; private set; }
        public float AverageAngle { get; private set; }
        public float MinAngle { get; private set; }
        public float MaxAngle { get; private set; }
        public float AverageWidth { get; private set; }
        public float NarrowestWidth { get; private set; }
        public float WidestWidth { get; private set; }
        public SlopeDifficulty Difficulty { get; private set; } = SlopeDifficulty.Green;
        public bool HasDesignedDifficulty { get; private set; }
        public int CompletedRuns { get; private set; }
        public float PopularityScore { get; private set; }
        public bool Open { get; private set; } = true;
        public bool Groomed { get; private set; }
        public int TrafficCount { get; private set; }
        public float TrafficDensity => Length <= 0.01f ? 0f : TrafficCount / Length;
        public SlopeEndConnection TopConnection { get; private set; }
        public SlopeEndConnection BottomConnection { get; private set; }

        private readonly List<Vector3> points = new List<Vector3>();
        private readonly List<float> widths = new List<float>();

        private void Awake()
        {
            if (SlopeNetwork.I == null) new GameObject("SlopeNetwork").AddComponent<SlopeNetwork>();
            SlopeNetwork.I.Register(this);
            if (ResortRouteNetwork.I == null) new GameObject("ResortRouteNetwork").AddComponent<ResortRouteNetwork>();
            ResortRouteNetwork.I.RegisterSlope(this);
        }

        public void Build(IReadOnlyList<Vector3> route, IReadOnlyList<float> routeWidths)
        {
            points.Clear(); widths.Clear();
            if (route != null) points.AddRange(route);
            if (routeWidths != null && routeWidths.Count == points.Count) widths.AddRange(routeWidths);
            while (widths.Count < points.Count) widths.Add(widths.Count == 0 ? 10f : widths[widths.Count - 1]);

            // A ski run is represented from its upper terminal to its lower terminal.
            // Players may draw in either direction, so normalize the route when the
            // last point is clearly higher than the first one. Width samples follow it.
            if (points.Count >= 2 && points[points.Count - 1].y > points[0].y + 0.5f)
            {
                points.Reverse();
                widths.Reverse();
            }
            Width = widths.Count > 0 ? widths[0] : 10f;
            Recalculate();
        }

        public void Build(IReadOnlyList<Vector3> route, float width)
        {
            var w = new List<float>();
            if (route != null) for (int i = 0; i < route.Count; i++) w.Add(width);
            Build(route, w);
        }

        public void SetConnections(SlopeEndConnection top, SlopeEndConnection bottom)
        { TopConnection = top; BottomConnection = bottom; }
        public void SetGroomed(bool value) => Groomed = value;
        public void RegisterSkier() => TrafficCount++;
        public void UnregisterSkier() => TrafficCount = Mathf.Max(0, TrafficCount - 1);
        public void ResetTraffic() => TrafficCount = 0;
        public void SetOpen(bool value) => Open = value;
        public float GetTrafficPenalty() => Mathf.Min(25f, TrafficDensity * 1000f);

        public void SetDesignedDifficulty(SlopeDifficulty value)
        {
            Difficulty = value;
            HasDesignedDifficulty = true;
        }

        public void RegisterCompletedRun(float experienceScore)
        {
            CompletedRuns++;
            PopularityScore = Mathf.Clamp(PopularityScore * .92f + Mathf.Clamp01(experienceScore) * 8f, 0f, 100f);
        }

        public float GetWidthAt(int index)
        { return index >= 0 && index < widths.Count ? widths[index] : Width; }

        private void Recalculate()
        {
            Length = 0f; float angleSum = 0f; int angleCount = 0;
            MinAngle = 999f; MaxAngle = 0f;
            AverageWidth = 0f; NarrowestWidth = 999f; WidestWidth = 0f;
            for (int i = 0; i < points.Count; i++)
            {
                float w = GetWidthAt(i);
                AverageWidth += w;
                NarrowestWidth = Mathf.Min(NarrowestWidth, w);
                WidestWidth = Mathf.Max(WidestWidth, w);
            }
            if (points.Count > 0) AverageWidth /= points.Count;
            if (points.Count == 0) NarrowestWidth = WidestWidth = AverageWidth = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                Vector3 delta = points[i] - points[i - 1];
                Length += delta.magnitude;
                if (delta.sqrMagnitude > 0.01f)
                {
                    float angle = Vector3.Angle(delta, Vector3.ProjectOnPlane(delta, Vector3.up));
                    angleSum += angle; angleCount++;
                    MinAngle = Mathf.Min(MinAngle, angle); MaxAngle = Mathf.Max(MaxAngle, angle);
                }
            }
            AverageAngle = angleCount > 0 ? angleSum / angleCount : 0f;
            if (!HasDesignedDifficulty)
            {
                Difficulty = AverageAngle < 8f ? SlopeDifficulty.Green :
                    AverageAngle < 16f ? SlopeDifficulty.Blue :
                    AverageAngle < 25f ? SlopeDifficulty.Red :
                    AverageAngle < 35f ? SlopeDifficulty.Black : SlopeDifficulty.Extreme;
            }
        }
    }
}
