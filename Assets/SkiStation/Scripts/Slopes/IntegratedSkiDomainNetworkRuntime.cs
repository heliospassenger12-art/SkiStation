using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum SkiDomainLinkKind { Slope, Lift, Transfer }

    public struct SkiDomainLink
    {
        public SkiDomainLinkKind kind;
        public SlopeRuntime slope;
        public LiftRuntime lift;
        public Vector3 from;
        public Vector3 to;
        public float cost;
        public bool usable;
    }

    /// <summary>
    /// 1.76: unified terrain-aware ski-domain graph. It joins real pistes and lifts
    /// through their physical endpoints, while keeping transfers short and downhill-safe.
    /// It is deliberately read-only: existing construction/runtime systems stay authoritative.
    /// </summary>
    public sealed class IntegratedSkiDomainNetworkRuntime : MonoBehaviour
    {
        public static IntegratedSkiDomainNetworkRuntime I { get; private set; }
        [Min(2f)] public float endpointRadius = 28f;
        [Min(0f)] public float maxTransferClimb = 8f;
        [Min(0f)] public float transferCostMultiplier = 2.2f;
        [Min(0f)] public float congestionWeight = 35f;

        private readonly List<SkiDomainLink> links = new List<SkiDomainLink>();
        public IReadOnlyList<SkiDomainLink> Links => links;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        private void Start() { Rebuild(); }

        public void Rebuild()
        {
            links.Clear();
            SlopeRuntime[] slopes = Object.FindObjectsOfType<SlopeRuntime>();
            LiftRuntime[] lifts = Object.FindObjectsOfType<LiftRuntime>();

            for (int i = 0; i < slopes.Length; i++)
            {
                SlopeRuntime s = slopes[i];
                if (s == null || s.Points == null || s.Points.Count < 2) continue;
                links.Add(new SkiDomainLink
                {
                    kind = SkiDomainLinkKind.Slope, slope = s, lift = null,
                    from = s.TopPoint, to = s.BottomPoint,
                    cost = Mathf.Max(1f, s.Length) * (1f + s.TrafficDensity * congestionWeight / 100f),
                    usable = s.Open
                });
            }

            for (int i = 0; i < lifts.Length; i++)
            {
                LiftRuntime l = lifts[i];
                if (l == null) continue;
                links.Add(new SkiDomainLink
                {
                    kind = SkiDomainLinkKind.Lift, slope = null, lift = l,
                    from = l.Bottom, to = l.Top,
                    cost = float.IsInfinity(l.TravelSeconds) ? 100000f : Mathf.Max(1f, l.TravelSeconds),
                    usable = l.Open && !l.Faulted
                });
            }

            BuildTransfers(slopes, lifts);
        }

        private void BuildTransfers(SlopeRuntime[] slopes, LiftRuntime[] lifts)
        {
            float r2 = endpointRadius * endpointRadius;
            for (int s = 0; s < slopes.Length; s++)
            {
                SlopeRuntime slope = slopes[s];
                if (slope == null || slope.Points.Count < 2) continue;

                for (int l = 0; l < lifts.Length; l++)
                {
                    LiftRuntime lift = lifts[l];
                    if (lift == null) continue;
                    TryAddTransfer(slope.BottomPoint, lift.Bottom, r2);
                    TryAddTransfer(lift.Top, slope.TopPoint, r2);
                }

                for (int other = 0; other < slopes.Length; other++)
                {
                    if (other == s || slopes[other] == null || slopes[other].Points.Count < 2) continue;
                    TryAddTransfer(slope.BottomPoint, slopes[other].TopPoint, r2);
                }
            }
        }

        private void TryAddTransfer(Vector3 from, Vector3 to, float radiusSquared)
        {
            Vector3 delta = to - from;
            if (delta.sqrMagnitude > radiusSquared || delta.y > maxTransferClimb) return;
            float distance = delta.magnitude;
            links.Add(new SkiDomainLink
            {
                kind = SkiDomainLinkKind.Transfer, slope = null, lift = null,
                from = from, to = to,
                cost = Mathf.Max(1f, distance * transferCostMultiplier), usable = true
            });
        }

        public LiftRuntime FindReachableLiftFromSlope(SlopeRuntime slope)
        {
            if (slope == null) return null;
            LiftRuntime best = null;
            float bestScore = float.MaxValue;
            LiftRuntime[] lifts = Object.FindObjectsOfType<LiftRuntime>();
            for (int i = 0; i < lifts.Length; i++)
            {
                LiftRuntime lift = lifts[i];
                if (lift == null || !lift.Open || lift.Faulted) continue;
                Vector3 delta = lift.Bottom - slope.BottomPoint;
                if (delta.y > maxTransferClimb || delta.sqrMagnitude > endpointRadius * endpointRadius) continue;
                float score = delta.magnitude + (float.IsInfinity(lift.TravelSeconds) ? 10000f : lift.TravelSeconds * 0.15f);
                if (score < bestScore) { bestScore = score; best = lift; }
            }
            return best;
        }

        public SlopeRuntime FindReachableSlopeFromLift(LiftRuntime lift)
        {
            if (lift == null) return null;
            SlopeRuntime best = null;
            float bestScore = float.MaxValue;
            SlopeRuntime[] slopes = Object.FindObjectsOfType<SlopeRuntime>();
            for (int i = 0; i < slopes.Length; i++)
            {
                SlopeRuntime slope = slopes[i];
                if (slope == null || !slope.Open || slope.Points.Count < 2) continue;
                Vector3 delta = slope.TopPoint - lift.Top;
                if (delta.y > maxTransferClimb || delta.sqrMagnitude > endpointRadius * endpointRadius) continue;
                float score = delta.magnitude + slope.TrafficDensity * congestionWeight;
                if (score < bestScore) { bestScore = score; best = slope; }
            }
            return best;
        }
    }
}
