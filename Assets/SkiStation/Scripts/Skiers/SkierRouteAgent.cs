using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// Complete resort navigation: lift top -> compatible slope -> slope bottom -> next lift.
    /// The route network is proximity based today, ready for explicit graph links later.
    /// </summary>
    public class SkierRouteAgent : MonoBehaviour
    {
        [SerializeField] private SkierRuntime skier;
        [SerializeField] private float waypointReachDistance = 1.5f;
        [SerializeField] private float liftSearchRadius = ManagementJourneyAccess.LiftApproachRadius;
        [SerializeField] private float retryDelay = 2f;

        public SlopeRuntime CurrentSlope => currentSlope;
        public LiftRuntime ApproachingLift => approachingLift;
        public bool IsApproachingLift => approachingLift != null;
        public float SlopeProgress01 => currentSlope == null || route.Count <= 1 ? 0f : Mathf.Clamp01(index / (float)(route.Count - 1));

        private readonly List<Vector3> route = new List<Vector3>();
        private int index;
        private LiftRuntime lastLift;
        private SlopeRuntime currentSlope;
        private float retryTimer;
        private SlopeLiftConnectorRuntime activeConnector;
        private SkierAlternateRouteRuntime alternateRoute;
        private int connectorIndex;
        private LiftRuntime approachingLift;
        // Phase 29: an arriving road visitor must board a lift before the first descent.
        private bool hasBoardedLift;
        public bool HasCompletedLiftRide => hasBoardedLift;
        public bool IsGpsWalking => gpsWalking;
        public string GpsItinerary { get; private set; } = "";
        private bool gpsWalking, gpsApproachedStart, gpsQueued;
        private SkiDomainLink gpsStep;
        public bool TryFollowDomainPlan()
        {
            var planner = MultiLiftDestinationPlanner301.I;
            if (planner == null || !isActiveAndEnabled || skier == null || !VisitorSkiAdmission.CanSki(skier)) return false;
            var life = GetComponent<VisitorLifecycleRuntime>();
            if (life != null && life.OwnsMovement) return false;
            if (skier.State == SkierState.RidingLift || skier.State == SkierState.WaitingForLift) return false;
            gpsWalking = false; gpsQueued = false; approachingLift = null;
            skier.StopMoving(); skier.SetState(SkierState.Resting); retryTimer = retryDelay;
            string itinerary;
            bool found = planner.TryGetNextStep(skier, transform.position, out gpsStep, out itinerary);
            GpsItinerary = itinerary;
            if (!found) return true;
            if (gpsStep.kind == SkiDomainLinkKind.Lift)
            {
                approachingLift = gpsStep.lift;
                skier.MoveTo(approachingLift.Bottom);
            }
            else
            {
                gpsWalking = true; gpsApproachedStart = false;
                skier.MoveTo(gpsStep.from);
            }
            return true;
        }
        private void UpdateGpsWalk()
        {
            if (gpsStep.kind == SkiDomainLinkKind.Slope && (gpsStep.slope == null || !gpsStep.slope.Open))
            { gpsWalking = false; TryFollowDomainPlan(); return; }
            Vector3 target = gpsApproachedStart ? gpsStep.to : gpsStep.from;
            Vector3 delta = target - transform.position;
            if (new Vector2(delta.x, delta.z).magnitude > 1.5f) return;
            if (gpsStep.kind == SkiDomainLinkKind.Slope)
            {
                gpsWalking = false; skier.StopMoving(); BeginFromReroute(gpsStep.slope); return;
            }
            if (!gpsApproachedStart)
            {
                if (!ManagementJourneyAccess.CanWalk(transform.position, gpsStep.to, 90f))
                { gpsWalking = false; TryFollowDomainPlan(); return; }
                gpsApproachedStart = true; skier.MoveTo(gpsStep.to); return;
            }
            gpsWalking = false; skier.StopMoving(); TryFollowDomainPlan();
        }


        // Called only by the actual lift unloading controller, never by a route request.
        public void ConfirmCompletedLiftRide(LiftRuntime lift)
        {
            if (lift == null) return;
            hasBoardedLift = true; gpsWalking = false; gpsQueued = false;
            // A completed ride invalidates any previously registered slope occupancy.
            // ClearRoute alone does not unregister it.
            if (currentSlope != null) currentSlope.UnregisterSkier();
            lastLift = lift;
            approachingLift = null;
            ClearRoute();
        }

        private void Awake()
        {
            if (skier == null) skier = GetComponent<SkierRuntime>();
            alternateRoute = GetComponent<SkierAlternateRouteRuntime>() ?? gameObject.AddComponent<SkierAlternateRouteRuntime>();
        }

        private void OnDestroy()
        {
            if (currentSlope != null) currentSlope.UnregisterSkier();
            MultiLiftDestinationPlanner301.I?.Forget(skier);
        }

        public void BeginFromLift(LiftRuntime lift)
        {
            BeginFromLift(lift, null);
        }

        public void BeginFromLift(LiftRuntime lift, SlopeRuntime forcedSlope)
        {
            // A lift-top handoff is valid only after a real ride for road-arriving visitors.
            var arrival = GetComponent<StationVisitorRoadArrival248>();
            if (arrival != null && (!arrival.ReadyToSki || !hasBoardedLift))
            {
                QueueNearestLift(transform.position);
                return;
            }
            if (currentSlope != null) currentSlope.UnregisterSkier();
            lastLift = lift;
            approachingLift = null;
            ClearRoute();

            var network = ResortRouteNetwork.I;
            var integratedNavigator = skier != null ? (skier.GetComponent<IntegratedSkierDomainNavigator>() ?? skier.gameObject.AddComponent<IntegratedSkierDomainNavigator>()) : null;
            SlopeRuntime plannedChoice = null;
            if (forcedSlope == null && MultiLiftDestinationPlanner301.I != null)
                MultiLiftDestinationPlanner301.I.TryGetNextSlope(skier, lift, out plannedChoice);
            SlopeRuntime integratedChoice = forcedSlope == null && plannedChoice == null && integratedNavigator != null ? integratedNavigator.ChooseSlopeFromLift(lift) : null;
            currentSlope = forcedSlope != null ? forcedSlope : (plannedChoice != null ? plannedChoice : (integratedChoice != null ? integratedChoice :
                (VisitorBehaviorRuntime.I != null ? VisitorBehaviorRuntime.I.ChooseSlope(skier, lift) :
                (network != null ? network.FindSlopeFromLift(lift) :
                (SlopeNetwork.I != null && lift != null ? SlopeNetwork.I.FindBestSlope(lift.Top, lift.Bottom) : null)))));

            if (currentSlope != null)
            {
                for (int i = 0; i < currentSlope.Points.Count; i++) route.Add(currentSlope.Points[i]);
                index = 0;
                currentSlope.RegisterSkier();
                var journey = skier != null ? (skier.GetComponent<SkierMountainJourneyRuntime>() ?? skier.gameObject.AddComponent<SkierMountainJourneyRuntime>()) : null;
                journey?.BindSlope(currentSlope);
                skier?.SetState(SkierState.Skiing);
                if (VisitorIntentRuntime.I != null) VisitorIntentRuntime.I.SetStage(skier, VisitorJourneyStage.Skiing, currentSlope.name);
                GoNext();
                return;
            }

            QueueNearestLift(lift != null ? lift.Bottom : transform.position);
        }

        private void Update()
        {
            if (skier == null || skier.State == SkierState.Leaving) return;
            if (skier.State == SkierState.RidingLift) { hasBoardedLift = true; return; }
            // The unloading animation and post-arrival transfer own movement until
            // the visitor has physically exited the upper station.
            var disembark = GetComponent<LiftPassengerDisembarkRuntime>();
            if (disembark != null && disembark.IsDisembarking) return;
            var transfer = GetComponent<SkierMountainTransferRuntime>();
            if (transfer != null && transfer.isActiveAndEnabled) return;
            var post = GetComponent<LiftPostArrivalRoutingRuntime>();
            if (post != null && post.isActiveAndEnabled &&
                (post.State == LiftPostArrivalState.ChoosingDestination || post.State == LiftPostArrivalState.WaitingForLift)) return;
            // 2.48: the road arrival owns movement until the visitor reaches the front de neige.
            var roadArrival = GetComponent<StationVisitorRoadArrival248>();
            if (roadArrival != null && roadArrival.IsArriving) return;

            if (alternateRoute != null && alternateRoute.IsRerouting) return;
            var terminalDecision = GetComponent<SkierTerminalDecisionRuntime>();
            if (terminalDecision != null && terminalDecision.Active) return;

            if (gpsQueued && skier.State == SkierState.WaitingForLift && lastLift == null)
            { gpsQueued = false; skier.StopMoving(); skier.SetState(SkierState.Resting); TryFollowDomainPlan(); return; }
            if (gpsWalking) { UpdateGpsWalk(); return; }

            if (approachingLift != null)
            {
                UpdateLiftApproach();
                return;
            }

            if (activeConnector != null)
            {
                UpdateConnectorMovement();
                return;
            }

            if (route.Count > 0 && currentSlope != null)
            {
                if (!currentSlope.Open)
                {
                    var closedSlope = currentSlope;
                    currentSlope.UnregisterSkier();
                    ClearRoute();
                    if (TryFollowDomainPlan()) return;
                    if (alternateRoute == null) alternateRoute = GetComponent<SkierAlternateRouteRuntime>() ?? gameObject.AddComponent<SkierAlternateRouteRuntime>();
                    if (!alternateRoute.TryRerouteToOpenSlope(transform.position, closedSlope))
                        QueueNearestLift(transform.position);
                    return;
                }
                if (index >= route.Count)
                {
                    FinishSlope();
                    return;
                }

                var connector = SlopeLiftConnectorNetwork.I != null ? SlopeLiftConnectorNetwork.I.FindUsable(transform.position, 4f) : null;
                if (MultiLiftDestinationPlanner301.I == null && connector != null && connector.SourceSlope == currentSlope && connector.TargetLift != null)
                {
                    ActivateConnector(connector);
                    return;
                }

                if (Vector3.Distance(transform.position, route[index]) <= waypointReachDistance)
                {
                    index++;
                    if (index >= route.Count) FinishSlope();
                    else GoNext();
                }
                return;
            }

            retryTimer -= Time.deltaTime;
            if (retryTimer <= 0f && skier.State != SkierState.WaitingForLift)
            {
                retryTimer = retryDelay;
                QueueNearestLift(transform.position);
            }
        }

        private void ActivateConnector(SlopeLiftConnectorRuntime connector)
        {
            if (connector == null || connector.Route.Count < 2) return;
            if (currentSlope != null) currentSlope.UnregisterSkier();
            currentSlope = null;
            route.Clear(); index = 0;
            activeConnector = connector;
            connectorIndex = 0;
            skier?.SetState(SkierState.Skiing);
            GoNextConnector();
        }

        private void UpdateConnectorMovement()
        {
            if (activeConnector == null || activeConnector.TargetLift == null || !activeConnector.TargetLift.Open || activeConnector.TargetLift.Faulted)
            {
                activeConnector = null;
                QueueNearestLift(transform.position);
                return;
            }

            if (connectorIndex >= activeConnector.Route.Count)
            {
                var lift = activeConnector.TargetLift;
                activeConnector = null;
                connectorIndex = 0;
                QueueNearestLift(lift.Bottom);
                return;
            }

            if (Vector3.Distance(transform.position, activeConnector.Route[connectorIndex]) <= waypointReachDistance)
            {
                connectorIndex++;
                if (connectorIndex >= activeConnector.Route.Count)
                {
                    var lift = activeConnector.TargetLift;
                    activeConnector = null;
                    connectorIndex = 0;
                    QueueNearestLift(lift.Bottom);
                }
                else GoNextConnector();
            }
        }

        private void GoNextConnector()
        {
            if (activeConnector != null && connectorIndex >= 0 && connectorIndex < activeConnector.Route.Count)
                skier?.MoveTo(activeConnector.Route[connectorIndex]);
        }

        private void FinishSlope()
        {
            SlopeRuntime completedSlope = currentSlope;
            Vector3 bottom = completedSlope != null ? completedSlope.BottomPoint : transform.position;
            if (completedSlope != null) completedSlope.UnregisterSkier();
            if (SlopeVisitorExperienceRuntime.I != null) SlopeVisitorExperienceRuntime.I.RecordCompletion(skier, completedSlope);
            if (VisitorIntentRuntime.I != null) VisitorIntentRuntime.I.RegisterSlopeCompleted(skier, completedSlope);
            ClearRoute();
            MultiLiftDestinationPlanner301.I?.RegisterCompletedSlope(skier, completedSlope);
            // Continue the same destination through the ordered graph before legacy local choices.
            if (TryFollowDomainPlan()) return;
            if (alternateRoute == null) alternateRoute = GetComponent<SkierAlternateRouteRuntime>() ?? gameObject.AddComponent<SkierAlternateRouteRuntime>();
            if (alternateRoute.TryRerouteToOpenSlope(bottom, completedSlope)) return;
            var terminal = SlopeTerminalAreaRuntime.FindForSlope(completedSlope, SlopeTerminalEndpointKind.Bottom, bottom);
            if (terminal != null)
            {
                var terminalDecision = skier != null ? (skier.GetComponent<SkierTerminalDecisionRuntime>() ?? skier.gameObject.AddComponent<SkierTerminalDecisionRuntime>()) : null;
                if (terminalDecision != null && terminalDecision.Begin(terminal)) return;
            }
            var connector = SlopeLiftConnectorNetwork.I != null ? SlopeLiftConnectorNetwork.I.FindUsable(bottom, 4f) : null;
            if (connector != null && connector.SourceSlope == completedSlope && connector.TargetLift != null)
            {
                ActivateConnector(connector);
                return;
            }
            QueueNearestLift(bottom);
        }

        private void GoNext()
        {
            if (index >= 0 && index < route.Count)
                skier?.MoveTo(route[index]);
        }

        public void BeginFromReroute(SlopeRuntime slope)
        {
            var arrival = GetComponent<StationVisitorRoadArrival248>();
            if (arrival != null && (!arrival.ReadyToSki || !hasBoardedLift))
            {
                QueueNearestLift(transform.position);
                return;
            }
            if (slope == null || !slope.Open)
            {
                QueueNearestLift(transform.position);
                return;
            }
            if (currentSlope != null) currentSlope.UnregisterSkier();
            ClearRoute();
            currentSlope = slope;
            for (int i = 0; i < currentSlope.Points.Count; i++) route.Add(currentSlope.Points[i]);
            index = 0;
            currentSlope.RegisterSkier();
            var journey = skier != null ? (skier.GetComponent<SkierMountainJourneyRuntime>() ?? skier.gameObject.AddComponent<SkierMountainJourneyRuntime>()) : null;
            journey?.BindSlope(currentSlope);
            skier?.SetState(SkierState.Skiing);
            GoNext();
        }

        public void QueueNextAvailableLift(Vector3 from)
        {
            QueueNearestLift(from);
        }

        private void QueueNearestLift(Vector3 from)
        {
            if (!isActiveAndEnabled || !VisitorSkiAdmission.CanSki(skier) || skier.State == SkierState.WaitingForLift || skier.State == SkierState.RidingLift) return;

            if (TryFollowDomainPlan()) return;
            LiftRuntime best = null;
            if (MultiLiftDestinationPlanner301.I != null)
                MultiLiftDestinationPlanner301.I.TryGetNextLift(skier, from, out best);
            var network = ResortRouteNetwork.I;
            if (best == null && network != null)
            {
                best = VisitorBehaviorRuntime.I != null
                    ? VisitorBehaviorRuntime.I.ChooseLift(skier, from, lastLift != null ? lastLift.Top : from)
                    : network.FindBestLift(from, lastLift != null ? lastLift.Top : from);
            }

            if (!IsAccessibleLift(best, from)) best = null;
            if (best == null)
            {
                float bestDistance = liftSearchRadius * liftSearchRadius;
                LiftRuntime[] lifts = Object.FindObjectsOfType<LiftRuntime>();
                for (int i = 0; i < lifts.Length; i++)
                {
                    var lift = lifts[i];
                    if (!IsAccessibleLift(lift, from)) continue;
                    float d = (lift.Bottom - from).sqrMagnitude;
                    if (d < bestDistance) { bestDistance = d; best = lift; }
                }
            }

            if (best != null)
            {
                // Do not send a skier straight up a cliff to a remote lower
                // station. Wait for an accessible lift rather than climbing.
                Vector3 deltaToStation = best.Bottom - transform.position;
                if (!IsAccessibleLift(best, transform.position))
                {
                    skier.StopMoving();
                    skier.SetState(SkierState.Resting);
                    retryTimer = retryDelay;
                    return;
                }
                // 2.11: physically go to the lower station before queueing. Do not let the
                // simulation queue a skier from the other side of the mountain.
                float stationGap = Vector3.Distance(transform.position, best.Bottom);
                if (stationGap > 4f)
                {
                    approachingLift = best;
                    skier.SetState(SkierState.Resting);
                    skier.MoveTo(best.Bottom);
                    return;
                }
            }

            if (best != null && best.TryQueueSkier(skier))
            {
                var journey = skier.GetComponent<SkierMountainJourneyRuntime>() ?? skier.gameObject.AddComponent<SkierMountainJourneyRuntime>();
                journey.BindLift(best);
                skier.SetState(SkierState.WaitingForLift);
                lastLift = best;
            }
        }

        // Keep a single movement owner while walking to the lower lift station.
        // Do not repeatedly pick another lift or overwrite the destination mid-approach.
        private void UpdateLiftApproach()
        {
            LiftRuntime target = approachingLift;
            if (target == null || !target.Open || target.Faulted)
            {
                approachingLift = null;
                skier.StopMoving();
                skier.SetState(SkierState.Resting);
                retryTimer = retryDelay;
                return;
            }

            Vector3 offset = target.Bottom - transform.position;
            if (new Vector2(offset.x, offset.z).magnitude > liftSearchRadius)
            {
                approachingLift = null;
                skier.StopMoving();
                skier.SetState(SkierState.Resting);
                retryTimer = retryDelay;
                return;
            }
            if (offset.sqrMagnitude > 4f * 4f) return;

            approachingLift = null;
            skier.StopMoving();
            if (target.TryQueueSkier(skier))
            {
                var journey = skier.GetComponent<SkierMountainJourneyRuntime>() ?? skier.gameObject.AddComponent<SkierMountainJourneyRuntime>();
                journey.BindLift(target);
                skier.SetState(SkierState.WaitingForLift);
                lastLift = target;
                gpsQueued = MultiLiftDestinationPlanner301.I != null;
            }
            else
            {
                skier.SetState(SkierState.Resting);
                retryTimer = retryDelay;
            }
        }

        private bool IsAccessibleLift(LiftRuntime lift, Vector3 from)
        {
            if (lift == null || !lift.Open || lift.Faulted || lift.Definition == null) return false;
            Vector3 delta = lift.Bottom - from;
            if (!ManagementJourneyAccess.CanWalk(from, lift.Bottom, liftSearchRadius)) return false;
            return ManagementJourneyAccess.SlopeForLift(lift) != null;
        }

        private void ClearRoute()
        {
            route.Clear();
            index = 0;
            currentSlope = null;
        }
    }
}
