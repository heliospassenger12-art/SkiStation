using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// 3.28: per-visitor time budget. It does not teleport or move anybody: it only
    /// answers whether a new leisure/ski cycle still fits before the planned return.
    /// Existing lift, slope and pedestrian controllers remain movement authorities.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VisitorDayPlan328 : MonoBehaviour
    {
        [SerializeField, Range(14f, 22f)] private float plannedDepartureHour = 17.5f;
        [SerializeField, Min(0f)] private float returnReserveMinutes = 25f;
        [SerializeField, Min(1f)] private float defaultDescentGameMinutes = 22f;
        [SerializeField, Min(0f)] private float decisionMarginMinutes = 8f;

        public float PlannedDepartureHour => plannedDepartureHour;
        public float MinutesRemaining => GameClock.I == null ? 9999f : Mathf.Max(0f, (plannedDepartureHour - GameClock.I.CurrentHour) * 60f);
        public bool ReturnWindowStarted => MinutesRemaining <= returnReserveMinutes;
        public float ReservedMinutes => returnReserveMinutes + decisionMarginMinutes;

        private void Awake()
        {
            // Stable variation without consuming UnityEngine.Random: visitors do not all leave at once.
            int bucket = Mathf.Abs(GetInstanceID()) % 9; // -40 .. +40 min around 17:30.
            plannedDepartureHour = 17.5f + (bucket - 4) * (10f / 60f);
        }

        public float EstimateLiftCycleGameMinutes(LiftRuntime lift, SlopeRuntime likelySlope = null)
        {
            if (lift == null) return float.PositiveInfinity;
            float liftMinutes = lift.TravelGameMinutes;
            if (float.IsInfinity(liftMinutes)) return liftMinutes;
            float descent = likelySlope != null ? EstimateSlopeGameMinutes(likelySlope) : defaultDescentGameMinutes;
            return liftMinutes + descent + decisionMarginMinutes + returnReserveMinutes;
        }

        public float EstimateSlopeGameMinutes(SlopeRuntime slope)
        {
            if (slope == null || GameClock.I == null) return defaultDescentGameMinutes;
            // Management skier baseline is 6 m/s. Convert the physical descent duration
            // through the same central 3.27 clock used by lifts.
            float realSeconds = slope.Length / 6f;
            return Mathf.Max(3f, GameClock.I.RealSecondsToGameMinutes(realSeconds));
        }

        public bool CanStartLiftCycle(LiftRuntime lift, SlopeRuntime likelySlope = null)
        {
            if (GameClock.I == null) return true;
            return MinutesRemaining >= EstimateLiftCycleGameMinutes(lift, likelySlope);
        }

        public bool ShouldHeadHomeNow()
        {
            return GameClock.I != null && MinutesRemaining <= returnReserveMinutes;
        }

        public static VisitorDayPlan328 Ensure(SkierRuntime skier)
        {
            if (skier == null) return null;
            return skier.GetComponent<VisitorDayPlan328>() ?? skier.gameObject.AddComponent<VisitorDayPlan328>();
        }
    }
}
