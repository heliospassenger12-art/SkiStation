using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public sealed class SlopeNetwork : MonoBehaviour
    {
        public static SlopeNetwork I { get; private set; }
        public IReadOnlyList<SlopeRuntime> Slopes => slopes;
        private readonly List<SlopeRuntime> slopes = new List<SlopeRuntime>();

        private void Awake() { if (I != null && I != this) { Destroy(gameObject); return; } I = this; }
        public void Register(SlopeRuntime slope) { if (slope != null && !slopes.Contains(slope)) slopes.Add(slope); }
        public void Unregister(SlopeRuntime slope) { if (slope != null) slopes.Remove(slope); }
        public SlopeRuntime FindNearestTop(Vector3 point, float radius = 35f)
        {
            SlopeRuntime best = null; float bestD = radius * radius;
            for (int i = 0; i < slopes.Count; i++) { var s = slopes[i]; if (s == null || !s.Open) continue; float d = (s.TopPoint - point).sqrMagnitude; if (d < bestD) { bestD = d; best = s; } }
            return best;
        }
        public SlopeRuntime FindNearestBottom(Vector3 point, float radius = 35f)
        {
            SlopeRuntime best = null; float bestD = radius * radius;
            for (int i = 0; i < slopes.Count; i++) { var s = slopes[i]; if (s == null || !s.Open) continue; float d = (s.BottomPoint - point).sqrMagnitude; if (d < bestD) { bestD = d; best = s; } }
            return best;
        }

        public SlopeRuntime FindBestSlope(Vector3 start, Vector3 preferredEnd)
        {
            SlopeRuntime best = null; float bestScore = float.MaxValue;
            foreach (var slope in slopes)
            {
                if (slope == null || !slope.Open) continue;
                float score = Vector3.Distance(start, slope.TopPoint) + Vector3.Distance(preferredEnd, slope.BottomPoint) * .35f;
                if (score < bestScore) { bestScore = score; best = slope; }
            }
            return best;
        }
    }
}
