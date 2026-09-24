using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public sealed class ResortSlopeGraph : MonoBehaviour
    {
        public static ResortSlopeGraph I { get; private set; }
        [Min(1f)] public float autoConnectRadius = 22f;
        [Min(0f)] public float maxVerticalMismatch = 18f;
        [Range(-1f, 1f)] public float minimumDirectionDot = -0.15f;

        private readonly List<SlopeConnectionRuntime> nodes = new List<SlopeConnectionRuntime>();

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        public void Register(SlopeConnectionRuntime node)
        {
            if (node != null && !nodes.Contains(node)) nodes.Add(node);
        }

        public void Unregister(SlopeConnectionRuntime node) { if (node != null) nodes.Remove(node); }

        public int AutoConnect(SlopeRuntime slope, float radius = -1f)
        {
            if (slope == null) return 0;
            if (radius <= 0f) radius = autoConnectRadius;
            EnsureNode(slope);
            int links = 0;
            var own = slope.GetComponent<SlopeConnectionRuntime>();

            SlopeRuntime best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < SlopeNetwork.I.Slopes.Count; i++)
            {
                var candidate = SlopeNetwork.I.Slopes[i];
                if (candidate == null || candidate == slope || !candidate.Open) continue;
                if (TryScore(slope.BottomPoint, candidate.TopPoint, slope, candidate, radius, out float score) && score < bestScore)
                { bestScore = score; best = candidate; }
            }
            if (best != null)
            {
                var other = EnsureNode(best);
                float d = Vector3.Distance(slope.BottomPoint, best.TopPoint);
                own.SetBottomSlope(best, d);
                other.SetTopSlope(slope, d);
                links++;
            }
            return links;
        }

        public int AutoConnectAll(float radius = -1f)
        {
            if (SlopeNetwork.I == null) return 0;
            int count = 0;
            for (int i = 0; i < SlopeNetwork.I.Slopes.Count; i++) count += AutoConnect(SlopeNetwork.I.Slopes[i], radius);
            return count;
        }

        public bool TryConnect(SlopeRuntime from, SlopeRuntime to, float radius = -1f)
        {
            if (from == null || to == null || from == to) return false;
            if (radius <= 0f) radius = autoConnectRadius;
            if (!TryScore(from.BottomPoint, to.TopPoint, from, to, radius, out _)) return false;
            var a = EnsureNode(from); var b = EnsureNode(to);
            float d = Vector3.Distance(from.BottomPoint, to.TopPoint);
            a.SetBottomSlope(to, d); b.SetTopSlope(from, d);
            return true;
        }

        private bool TryScore(Vector3 fromPoint, Vector3 toPoint, SlopeRuntime from, SlopeRuntime to, float radius, out float score)
        {
            score = float.MaxValue;
            float d = Vector3.Distance(fromPoint, toPoint);
            if (d > radius) return false;
            if (Mathf.Abs(fromPoint.y - toPoint.y) > maxVerticalMismatch) return false;
            if (from.Points.Count < 2 || to.Points.Count < 2) return false;
            Vector3 a = (from.Points[from.Points.Count - 1] - from.Points[from.Points.Count - 2]).normalized;
            Vector3 b = (to.Points[1] - to.Points[0]).normalized;
            if (Vector3.Dot(a, b) < minimumDirectionDot) return false;
            score = d + Mathf.Abs(fromPoint.y - toPoint.y) * 0.5f;
            return true;
        }

        private SlopeConnectionRuntime EnsureNode(SlopeRuntime slope)
        {
            var node = slope.GetComponent<SlopeConnectionRuntime>();
            bool created = node == null;
            if (created) node = slope.gameObject.AddComponent<SlopeConnectionRuntime>();
            node.Initialize(slope);
            Register(node);
            return node;
        }
    }
}
