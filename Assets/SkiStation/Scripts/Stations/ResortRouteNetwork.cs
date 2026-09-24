using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// Runtime topology of the ski resort. It connects lift tops to slope tops and
    /// slope bottoms to lift bottoms using proximity, while allowing explicit links later.
    /// This is the first real graph layer for the complete skier loop.
    /// </summary>
    public class ResortRouteNetwork : MonoBehaviour
    {
        public static ResortRouteNetwork I { get; private set; }

        public IReadOnlyList<LiftRuntime> Lifts => lifts;
        public IReadOnlyList<SlopeRuntime> Slopes => slopes;

        [SerializeField] private float connectionRadius = 28f;
        [SerializeField] private float preferredDestinationWeight = 0.25f;
        [SerializeField] private float congestionWeight = 0.5f;
        [SerializeField] private float hotspotPenalty = 35f;

        private readonly List<LiftRuntime> lifts = new List<LiftRuntime>();
        private readonly List<SlopeRuntime> slopes = new List<SlopeRuntime>();

        private void Awake()
        {
            I = this;
        }

        public void RegisterLift(LiftRuntime lift)
        {
            if (lift != null && !lifts.Contains(lift)) lifts.Add(lift);
        }

        public void RegisterSlope(SlopeRuntime slope)
        {
            if (slope != null && !slopes.Contains(slope)) slopes.Add(slope);
        }

        public SlopeRuntime FindSlopeFromLift(LiftRuntime lift)
        {
            if (lift == null) return null;
            return FindBestSlope(lift.Top, lift.Bottom);
        }

        public SlopeRuntime FindBestSlope(Vector3 start, Vector3 preferredEnd)
        {
            SlopeRuntime best = null;
            float bestScore = float.MaxValue;

            Cleanup();
            foreach (var slope in slopes)
            {
                if (slope == null || !slope.Open || slope.Points.Count < 2) continue;

                float entry = Vector3.Distance(start, slope.TopPoint);
                if (entry > connectionRadius) continue;

                float exit = Vector3.Distance(slope.BottomPoint, preferredEnd);
                float traffic = InfrastructureCongestionRuntime.I != null
                    ? InfrastructureCongestionRuntime.I.GetSlopePressure01(slope)
                    : Mathf.Clamp01(slope.GetTrafficPenalty() / 25f);
                float score = entry + exit * preferredDestinationWeight + traffic * hotspotPenalty * congestionWeight;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = slope;
                }
            }
            return best;
        }

        public LiftRuntime FindLiftFromSlope(SlopeRuntime slope)
        {
            if (slope == null) return null;
            return FindBestLift(slope.BottomPoint, slope.TopPoint);
        }

        public LiftRuntime FindBestLift(Vector3 start, Vector3 preferredTop)
        {
            LiftRuntime best = null;
            float bestScore = float.MaxValue;

            Cleanup();
            foreach (var lift in lifts)
            {
                if (lift == null || !lift.Open || lift.Faulted || lift.Definition == null) continue;

                float entry = Vector3.Distance(start, lift.Bottom);
                if (entry > connectionRadius) continue;

                float destination = Vector3.Distance(lift.Top, preferredTop);
                float pressure = InfrastructureCongestionRuntime.I != null
                    ? InfrastructureCongestionRuntime.I.GetLiftPressure01(lift)
                    : Mathf.Clamp01((lift.StationLogic != null ? lift.StationLogic.QueueSize : 0f) / Mathf.Max(1f, lift.CapacityPerHour * .20f));
                float score = entry + destination * preferredDestinationWeight + pressure * hotspotPenalty * congestionWeight;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = lift;
                }
            }
            return best;
        }

        public bool AreConnected(Vector3 a, Vector3 b) => Vector3.Distance(a, b) <= connectionRadius;

        private void Cleanup()
        {
            lifts.RemoveAll(l => l == null);
            slopes.RemoveAll(s => s == null);
        }
    }
}
