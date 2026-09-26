using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// Keeps the playable visitor loop finite: real visitors can leave the resort and are
    /// removed from the scene once they reach the base departure area. This prevents a
    /// prototype session from filling permanently with old visitors.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VisitorLifecycleRuntime : MonoBehaviour
    {
        [SerializeField] private float departureHour = 18.5f;
        [SerializeField] private float forcedDepartureHour = 20f;
        [SerializeField] private float destroyDistance = 3f;
        [SerializeField] private float maxLifetimeMinutes = 720f;

        private SkierRuntime skier;
        private float lifetimeMinutes;
        private bool departing;
        private bool departureRequested;
        private float nextDepartureRecovery;
        public bool OwnsMovement => departing || goingToLodging || overnight;
        private Vector3 departurePoint;
        private StationVisitorStay249 stay;
        private Vector3[] lodgingPath;
        private int lodgingWaypoint;
        private bool goingToLodging;
        private bool overnight;
        private int overnightDay;
        private bool routeAgentWasEnabled;
        private bool postArrivalWasEnabled;
        private bool arrivalChecked;
        private float nextLodgingAttempt;

        private void Awake()
        {
            skier = GetComponent<SkierRuntime>();
            stay = GetComponent<StationVisitorStay249>();
        }

        private void Update()
        {
            if (skier == null || GameClock.I == null) return;
            var dayChoice = GetComponent<VisitorDayDecision329>();
            if (dayChoice != null && dayChoice.OwnsMovement) return;

            // The road-arrival controller owns movement until the visitor reaches the resort.
            var arrival = GetComponent<StationVisitorRoadArrival248>();
            if (arrival != null && arrival.IsArriving) return;
            if (!arrivalChecked) { arrivalChecked = true; stay = GetComponent<StationVisitorStay249>(); }
            if (!departing && skier.State == SkierState.Leaving)
            {
                if (ManagementJourneyAccess.CanExitToVillage(transform.position)) BeginDeparture();
                else
                {
                    departureRequested = true;
                    skier.StopMoving();
                    skier.SetState(SkierState.Skiing);
                }
            }
            // A pending departure may only commit at the foot of the mountain.
            // No MoveTo(entrance) is ever issued from an upper station.
            if (departureRequested && !departing && Time.time >= nextDepartureRecovery)
            {
                nextDepartureRecovery = Time.time + 2f;
                var route = GetComponent<SkierRouteAgent>();
                var activeTransfer = GetComponent<SkierMountainTransferRuntime>();
                var disembark = GetComponent<LiftPassengerDisembarkRuntime>();
                if (route != null && route.CurrentSlope == null &&
                    (activeTransfer == null || !activeTransfer.isActiveAndEnabled) &&
                    (disembark == null || !disembark.IsDisembarking) &&
                    skier.State != SkierState.RidingLift && skier.State != SkierState.WaitingForLift)
                {
                    var post = GetComponent<LiftPostArrivalRoutingRuntime>();
                    if (post != null && post.SourceLift != null) post.Begin(post.SourceLift);
                }
            }
            if (overnight)
            {
                if (GameClock.I.CurrentDay > overnightDay)
                {
                    overnight = false;
                    if (stay == null || GameClock.I.CurrentDay >= stay.CheckoutDay || !stay.HasReservation)
                        BeginDeparture();
                    else ResumeSkiDay();
                }
                return;
            }
            if (goingToLodging) { FollowLodgingPath(); return; }
            lifetimeMinutes += Time.deltaTime / Mathf.Max(.1f, GameClock.I.RealSecondsPerGameHour / 60f);

            // Never interrupt a passenger who is physically riding a lift or queued in a lift.
            // The existing lift systems remain authoritative over those states.
            // 2.10: do not let the day/lifetime departure logic overwrite an active ski loop.
            // Departure is only allowed while the visitor is idle at the resort, never mid-run.
            var proof = FindFirstObjectByType<VisibleSkiLoopAuthorityRuntime>();
            var upperExit = GetComponent<LiftPassengerDisembarkRuntime>();
            var transfer = GetComponent<SkierMountainTransferRuntime>();
            bool protectedByLift = skier.State == SkierState.RidingLift || skier.State == SkierState.WaitingForLift || skier.State == SkierState.Skiing ||
                (proof != null && proof.TrackedSkier == skier) ||
                (upperExit != null && upperExit.IsDisembarking) ||
                (transfer != null && transfer.isActiveAndEnabled);
            float hour = GameClock.I.CurrentHour;

            if (!departing && !protectedByLift && (departureRequested || hour >= departureHour || lifetimeMinutes >= maxLifetimeMinutes) && ManagementJourneyAccess.CanExitToVillage(transform.position))
            {
                if (stay != null && stay.HasReservation && GameClock.I.CurrentDay < stay.CheckoutDay)
                    BeginLodgingReturn();
                else BeginDeparture();
            }

            if (!departing && hour >= forcedDepartureHour && !protectedByLift && ManagementJourneyAccess.CanExitToVillage(transform.position))
            {
                if (stay != null && stay.HasReservation && GameClock.I.CurrentDay < stay.CheckoutDay)
                    BeginLodgingReturn();
                else BeginDeparture();
            }

            if (departing)
            {
                if (Vector3.Distance(transform.position, departurePoint) <= destroyDistance)
                {
                    Destroy(gameObject);
                }
            }
        }

        public void RequestDayReturn329()
        {
            departureRequested = true;
            if (!ManagementJourneyAccess.CanExitToVillage(transform.position)) return;
            if (stay != null && stay.HasReservation && GameClock.I != null && GameClock.I.CurrentDay < stay.CheckoutDay)
                BeginLodgingReturn();
            else BeginDeparture();
        }

        private void BeginLodgingReturn()
        {
            if (Time.time < nextLodgingAttempt) return;
            if (stay == null || stay.BookedAccommodation == null) { BeginDeparture(); return; }
            var network = StationPedestrianNetwork249.I;
            Vector3[] path;
            if (network == null || !network.TryGetAccess(stay.BookedAccommodation.gameObject, out path)
                || path == null || path.Length < 2)
            {
                // A missing pedestrian connection must not trap visitors in the resort.
                nextLodgingAttempt = Time.time + 10f;
                return;
            }
            lodgingPath = path;
            lodgingWaypoint = 0;
            goingToLodging = true;
            departureRequested = false;
            SuspendSkiRouting();
            skier.SetState(SkierState.Resting);
            skier.MoveTo(lodgingPath[lodgingWaypoint]);
        }

        private void FollowLodgingPath()
        {
            if (stay == null || stay.BookedAccommodation == null) { goingToLodging = false; BeginDeparture(); return; }
            if (Vector3.Distance(transform.position, lodgingPath[lodgingWaypoint]) > destroyDistance) return;
            lodgingWaypoint++;
            if (lodgingWaypoint < lodgingPath.Length)
            {
                skier.MoveTo(lodgingPath[lodgingWaypoint]);
                return;
            }
            goingToLodging = false;
            overnight = true;
            overnightDay = GameClock.I.CurrentDay;
            skier.StopMoving();
            skier.SetState(SkierState.Resting);
        }

        private void SuspendSkiRouting()
        {
            var agent = GetComponent<SkierRouteAgent>();
            if (agent != null) { routeAgentWasEnabled = agent.enabled; agent.enabled = false; }
            var transfer = GetComponent<SkierMountainTransferRuntime>(); if (transfer != null) transfer.Cancel();
            var post = GetComponent<LiftPostArrivalRoutingRuntime>();
            if (post != null) { postArrivalWasEnabled = post.enabled; post.enabled = false; }
        }

        private void ResumeSkiDay()
        {
            lifetimeMinutes = 0f;
            departureRequested = false;
            skier.SetState(SkierState.Resting);
            var agent = GetComponent<SkierRouteAgent>();
            if (agent != null) { agent.enabled = routeAgentWasEnabled; if (agent.enabled) agent.QueueNextAvailableLift(transform.position); }
            var post = GetComponent<LiftPostArrivalRoutingRuntime>();
            if (post != null) post.enabled = postArrivalWasEnabled;
        }

        private void BeginDeparture()
        {
            if (!ManagementJourneyAccess.CanExitToVillage(transform.position)) { departureRequested = true; return; }
            departureRequested = false;
            departing = true;
            // 2.11: departure owns the skier. Stop route/lift handoff systems from issuing
            // competing MoveTo commands after the visitor has decided to leave.
            var routeAgent = GetComponent<SkierRouteAgent>(); if (routeAgent != null) routeAgent.enabled = false;
            var transfer = GetComponent<SkierMountainTransferRuntime>(); if (transfer != null) transfer.Cancel();
            var postArrival = GetComponent<LiftPostArrivalRoutingRuntime>(); if (postArrival != null) postArrival.enabled = false;
            departurePoint = FindDeparturePoint();
            skier.SetState(SkierState.Leaving);
            skier.MoveTo(departurePoint);
        }

        private Vector3 FindDeparturePoint()
        {
            return ManagementSurface.Entrance;
        }
    }
}
