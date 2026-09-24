using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public struct SlopeDesignAssessment
    {
        public bool valid;
        public float score;
        public float skiableFraction;
        public float maximumSlope;
        public float averageSlope;
        public float verticalDrop;
        public float length;
        public SlopeDifficulty recommendedDifficulty;
        public string message;
    }

    // 1.75 — design-time bridge between a player-drawn piste and the Alpine terrain authority.
    // It evaluates the complete polyline (not only its endpoints) and can resample it onto the massif.
    public sealed class SlopeDesignIntelligenceRuntime : MonoBehaviour
    {
        public SlopeDesignAssessment LastAssessment { get; private set; }

        public List<Vector3> ConformAndResample(IReadOnlyList<Vector3> controlPoints, float spacing)
        {
            var result = new List<Vector3>();
            if (controlPoints == null || controlPoints.Count == 0) return result;
            float step = Mathf.Clamp(spacing, 3f, 30f);
            for (int segment = 0; segment < controlPoints.Count - 1; segment++)
            {
                Vector3 a = controlPoints[segment];
                Vector3 b = controlPoints[segment + 1];
                float planar = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
                int samples = Mathf.Max(1, Mathf.CeilToInt(planar / step));
                for (int i = 0; i < samples; i++)
                {
                    if (segment > 0 && i == 0) continue;
                    Vector3 p = Vector3.Lerp(a, b, (float)i / samples);
                    result.Add(Surface(p));
                }
            }
            result.Add(Surface(controlPoints[controlPoints.Count - 1]));
            return result;
        }

        public SlopeDesignAssessment Assess(IReadOnlyList<Vector3> route, SlopeDifficulty designedDifficulty)
        {
            var a = new SlopeDesignAssessment { valid = false, recommendedDifficulty = SlopeDifficulty.Green, message = "Trace trop court" };
            if (route == null || route.Count < 2) { LastAssessment = a; return a; }
            float length = 0f, slopeSum = 0f, maxSlope = 0f; int samples = 0, skiable = 0;
            for (int i = 1; i < route.Count; i++)
            {
                Vector3 p0 = Surface(route[i - 1]);
                Vector3 p1 = Surface(route[i]);
                Vector3 d = p1 - p0;
                float planar = new Vector2(d.x, d.z).magnitude;
                if (planar < .1f) continue;
                float angle = Mathf.Atan2(Mathf.Abs(d.y), planar) * Mathf.Rad2Deg;
                length += d.magnitude; slopeSum += angle; maxSlope = Mathf.Max(maxSlope, angle); samples++;
                if (AlpineTerrainIntelligenceRuntime.I == null || AlpineTerrainIntelligenceRuntime.I.DifficultyAt(Vector3.Lerp(p0, p1, .5f)) != AlpineSkiDifficulty.Unsuitable) skiable++;
            }
            float avg = samples > 0 ? slopeSum / samples : 0f;
            float fraction = samples > 0 ? (float)skiable / samples : 0f;
            float drop = Mathf.Abs(Surface(route[0]).y - Surface(route[route.Count - 1]).y);
            SlopeDifficulty recommended = maxSlope <= 14f ? SlopeDifficulty.Green : maxSlope <= 24f ? SlopeDifficulty.Blue : maxSlope <= 34f ? SlopeDifficulty.Red : maxSlope <= 43f ? SlopeDifficulty.Black : SlopeDifficulty.Extreme;
            float difficultyPenalty = Mathf.Abs((int)recommended - (int)designedDifficulty) * 8f;
            float score = Mathf.Clamp(fraction * 72f + Mathf.Clamp01(drop / 300f) * 16f + Mathf.Clamp01(length / 1200f) * 12f - difficultyPenalty, 0f, 100f);
            bool valid = length >= 40f && fraction >= .72f && maxSlope <= 48f;
            string message = !valid ? (length < 40f ? "Piste trop courte" : fraction < .72f ? "Terrain non skiable (" + Mathf.RoundToInt(fraction * 100f) + "% du trace autorise)" : "Pente localement excessive") : recommended == designedDifficulty ? "Trace coherent" : "Difficulte conseillee: " + recommended;
            a = new SlopeDesignAssessment { valid = valid, score = score, skiableFraction = fraction, maximumSlope = maxSlope, averageSlope = avg, verticalDrop = drop, length = length, recommendedDifficulty = recommended, message = message };
            LastAssessment = a; return a;
        }

        private static Vector3 Surface(Vector3 p)
        {
            if (ManagementSurface.TryGet(p, out var renderedSurface)) return renderedSurface;
            var foundation = AlpineTerrainFoundationRuntime.I;
            if (foundation != null) return foundation.Sample(p).position;
            if (MountainRuntime.I != null && MountainRuntime.I.IsGenerated && MountainRuntime.I.TryGetSurface(p, out var surface)) return surface;
            return p;
        }
    }
}
