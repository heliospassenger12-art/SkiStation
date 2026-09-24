using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// 1.47: lightweight rerouting for real skiers.
    /// When a piste becomes unavailable, the skier first looks for a nearby open
    /// alternative. No teleportation: the skier physically moves to the alternative
    /// piste's top/entry point. Congestion and visitor intent influence the choice.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkierAlternateRouteRuntime : MonoBehaviour
    {
        [SerializeField] private float searchRadius = 32f;
        [SerializeField] private float retryDelay = 2f;
        [SerializeField] private float transferTimeout = 12f;
        [SerializeField] private float maximumUphillTransfer = 1.5f;
        [SerializeField] private float congestionWeight = 8f;
        [SerializeField] private float distanceWeight = .035f;

        private SkierRuntime skier;
        private SkierRouteAgent agent;
        private SlopeRuntime target;
        private float retryTimer;
        private float transferElapsed;
        private Vector3 lastPosition;
        private float stalledTime;
        private bool moving;

        public bool IsRerouting => moving;
        public SlopeRuntime TargetSlope => target;

        private void Awake()
        {
            skier = GetComponent<SkierRuntime>();
            agent = GetComponent<SkierRouteAgent>();
        }

        public bool TryReroute(Vector3 from, SlopeRuntime excludeSlope)
        {
            if (skier == null || agent == null || moving) return false;
            var candidate = FindBest(from, excludeSlope);
            if (candidate == null) return false;

            // Only transfer to a piste whose entry is genuinely adjacent. A direct
            // MoveTo across a valley, cliff or another piste is not pathfinding.
            if (!IsSafeTransfer(from, candidate)) return false;
            target = candidate;
            moving = true;
            retryTimer = retryDelay;
            transferElapsed = 0f;
            stalledTime = 0f;
            lastPosition = transform.position;
            skier.SetState(SkierState.Skiing);
            skier.MoveTo(candidate.TopPoint);
            if (VisitorIntentRuntime.I != null)
                VisitorIntentRuntime.I.SetStage(skier, VisitorJourneyStage.Skiing, candidate.name);
            return true;
        }

        public bool TryRerouteToOpenSlope(Vector3 from, SlopeRuntime excludeSlope)
        {
            return TryReroute(from, excludeSlope);
        }

        private void Update()
        {
            if (!moving) return;
            if (target == null || skier == null || agent == null || !target.Open)
            {
                AbortTransfer();
                return;
            }

            // A reroute is a short, local piste junction, never an unlimited
            // cross-mountain walk. Recover if movement is blocked or another
            // controller unexpectedly interrupts the transfer.
            transferElapsed += Time.deltaTime;
            if (Vector3.Distance(transform.position, lastPosition) < .05f)
                stalledTime += Time.deltaTime;
            else
                stalledTime = 0f;
            lastPosition = transform.position;
            if (transferElapsed > transferTimeout || stalledTime > 4f ||
                !IsSafeTransfer(transform.position, target))
            {
                AbortTransfer();
                return;
            }

            if (Vector3.Distance(transform.position, target.TopPoint) <= 2f)
            {
                var next = target;
                moving = false;
                target = null;
                skier.StopMoving();
                agent.BeginFromReroute(next);
                return;
            }

            retryTimer -= Time.deltaTime;
            if (retryTimer <= 0f)
            {
                retryTimer = retryDelay;
                skier.MoveTo(target.TopPoint);
            }
        }

        private void AbortTransfer()
        {
            moving = false;
            target = null;
            if (skier != null)
            {
                skier.StopMoving();
                skier.SetState(SkierState.Resting);
            }
            // SkierRouteAgent regains control on its next update and can
            // select an accessible lift rather than walking uphill.
        }

        private bool IsSafeTransfer(Vector3 from, SlopeRuntime slope)
        {
            if (slope == null) return false;
            Vector3 entry = slope.TopPoint;
            float horizontal = Vector2.Distance(new Vector2(from.x, from.z), new Vector2(entry.x, entry.z));
            // Nearby entry only: this is a local junction, not a cross-mountain route.
            float localReach = Mathf.Min(searchRadius, Mathf.Max(3f, slope.GetWidthAt(0) * .5f));
            return horizontal <= localReach && entry.y <= from.y + maximumUphillTransfer &&
                   Mathf.Abs(entry.y - from.y) <= 3f;
        }

        private SlopeRuntime FindBest(Vector3 from, SlopeRuntime excludeSlope)
        {
            if (SlopeNetwork.I == null) return null;
            SlopeRuntime best = null;
            float bestScore = float.MinValue;
            for (int i = 0; i < SlopeNetwork.I.Slopes.Count; i++)
            {
                var slope = SlopeNetwork.I.Slopes[i];
                if (slope == null || slope == excludeSlope || !slope.Open || slope.Points.Count < 2) continue;
                float d = Vector3.Distance(from, slope.TopPoint);
                if (d > searchRadius || !IsSafeTransfer(from, slope)) continue;
                // A piste alternative is reachable on skis only if its start is
                // at approximately the same altitude (or below) the skier.
                // Never walk a visitor uphill to a summit: use a lift instead.
                if (slope.TopPoint.y > from.y + 2f) continue;

                float score = -d * distanceWeight;
                score -= slope.TrafficDensity * congestionWeight;
                score += slope.PopularityScore * .01f;
                if (VisitorIntentRuntime.I != null)
                    score += VisitorIntentRuntime.I.ScoreSlopeIntent(skier, slope) * .35f;

                // Prefer a real alternative, not the exact same terminal connection.
                var connection = slope.GetComponent<SlopeConnectionRuntime>();
                if (connection != null && connection.Top.kind == SlopeEndpointKind.LiftBottom)
                    score += .15f;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = slope;
                }
            }
            return best;
        }
    }
}
