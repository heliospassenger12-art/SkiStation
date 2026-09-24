using System;
using UnityEngine;

namespace SkiStation
{
    public enum LiftPostArrivalState { None, ChoosingDestination, HeadingToSlope, WaitingForLift }

    /// <summary>0.84: chooses the next destination, then physically transfers the real skier to its departure point.</summary>
    public sealed class LiftPostArrivalRoutingRuntime : MonoBehaviour
    {
        public LiftPostArrivalState State { get; private set; }
        public LiftRuntime SourceLift { get; private set; }
        public SlopeRuntime SelectedSlope { get; private set; }
        public event Action Changed;

        private SkierRuntime skier;
        private SkierRouteAgent agent;
        private SkierMountainTransferRuntime transfer;
        private float retryTimer;

        private void Awake()
        {
            skier = GetComponent<SkierRuntime>();
            agent = GetComponent<SkierRouteAgent>();
        }

        public void Begin(LiftRuntime lift)
        {
            SourceLift = lift;
            SelectedSlope = null;
            State = LiftPostArrivalState.ChoosingDestination;
            retryTimer = 0f;
            if (skier != null)
            {
                skier.SetState(SkierState.Skiing);
                var journey = skier.GetComponent<SkierMountainJourneyRuntime>() ?? skier.gameObject.AddComponent<SkierMountainJourneyRuntime>();
                journey.BindLift(lift);
            }
            Resolve();
        }

        private void Update()
        {
            if (State == LiftPostArrivalState.None || State == LiftPostArrivalState.HeadingToSlope) return;
            retryTimer -= Time.deltaTime;
            if (retryTimer <= 0f && (State == LiftPostArrivalState.ChoosingDestination || State == LiftPostArrivalState.WaitingForLift))
            {
                retryTimer = 1.5f;
                Resolve();
            }
        }

        private void Resolve()
        {
            if (skier == null) return;
            if (agent == null) agent = GetComponent<SkierRouteAgent>() ?? gameObject.AddComponent<SkierRouteAgent>();

            var network = ResortRouteNetwork.I;
            var existingDecision = SkiDomainRouteDecisionRuntime.I != null
                ? SkiDomainRouteDecisionRuntime.I.Decisions.TryGetValue(skier, out var routeDecision) ? routeDecision.nextSlope : null
                : null;
            // 2.10: player-built pistes are authoritative after a real lift ride. Older demo/starter
            // routes may still exist in the bootstrap scene and must never steal the handoff.
            SlopeRuntime slope = FindPlayerBuiltSlopeForLift(SourceLift);
            if (slope == null)
            {
                slope = existingDecision != null ? existingDecision : (VisitorBehaviorRuntime.I != null
                    ? VisitorBehaviorRuntime.I.ChooseSlope(skier, SourceLift)
                    : (network != null ? network.FindSlopeFromLift(SourceLift) :
                        (SlopeNetwork.I != null && SourceLift != null ? SlopeNetwork.I.FindBestSlope(SourceLift.Top, SourceLift.Bottom) : null)));
            }

            if (slope != null)
            {
                SelectedSlope = slope;
                var journey = skier.GetComponent<SkierMountainJourneyRuntime>() ?? skier.gameObject.AddComponent<SkierMountainJourneyRuntime>();
                journey.BindSlope(slope);
                State = LiftPostArrivalState.HeadingToSlope;
                Changed?.Invoke();
                transfer = GetComponent<SkierMountainTransferRuntime>() ?? gameObject.AddComponent<SkierMountainTransferRuntime>();
                transfer.Begin(skier, SourceLift, slope);
                return;
            }

            State = LiftPostArrivalState.WaitingForLift;
            Changed?.Invoke();
            agent.QueueNextAvailableLift(SourceLift != null ? SourceLift.Top : transform.position);
        }
        private SlopeRuntime FindPlayerBuiltSlopeForLift(LiftRuntime sourceLift)
        {
            if (sourceLift == null || SlopeNetwork.I == null) return null;
            SlopeRuntime best = null;
            float bestScore = float.MaxValue;
            foreach (var candidate in SlopeNetwork.I.Slopes)
            {
                if (candidate == null || !candidate.Open || candidate.Points == null || candidate.Points.Count < 2) continue;
                // SlopeConstructionMode names committed player pistes Piste_<difficulty>_<frame>.
                if (!candidate.gameObject.name.StartsWith("Piste_", StringComparison.OrdinalIgnoreCase)) continue;
                float topDistance = Vector3.Distance(sourceLift.Top, candidate.TopPoint);
                float bottomDistance = Vector3.Distance(sourceLift.Bottom, candidate.BottomPoint);
                // A piste hundreds of metres away is not connected to this lift. The old
                // prototype selected it anyway, producing the near-vertical pink wandering.
                const float maxDirectAccessGap = ManagementJourneyAccess.SlopeConnectionRadius;
                if (topDistance > maxDirectAccessGap) continue;
                float score = topDistance + bottomDistance * 0.20f;
                if (score < bestScore) { bestScore = score; best = candidate; }
            }
            if (best != null)
                Debug.Log("SkiStation 2.10 REAL LOOP: lift " + sourceLift.name + " -> player piste " + best.name +
                    " | top gap " + Vector3.Distance(sourceLift.Top, best.TopPoint).ToString("0.0") + " m");
            return best;
        }

    }
}
