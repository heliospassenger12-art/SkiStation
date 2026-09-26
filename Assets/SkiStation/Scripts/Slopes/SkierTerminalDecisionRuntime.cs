using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// 1.04: gives a real visitor a short free-circulation decision at a plateau.
    /// The visitor may return to a lift, use a nearby facility/activity, or leave.
    /// No corridor or artificial traffic is created.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkierTerminalDecisionRuntime : MonoBehaviour
    {
        private enum Choice { None, Lift, Leisure, Activity, Leave }

        [SerializeField] private float decisionDelay = 1.2f;
        [SerializeField] private float facilityRadius = 35f;
        [SerializeField] private float leaveAtHour = 16.5f;
        [SerializeField, Range(0f, 1f)] private float baseLeaveChance = .12f;

        private SkierRuntime skier;
        private SkierRouteAgent routeAgent;
        private SlopeTerminalAreaRuntime area;
        private Choice choice;
        private float timer;
        private bool active;
        private bool consumed;
        private Transform target;

        public bool Active => active;

        private void Awake()
        {
            skier = GetComponent<SkierRuntime>();
            routeAgent = GetComponent<SkierRouteAgent>();
        }

        private void Update()
        {
            if (!active || skier == null) return;
            if (skier.State == SkierState.RidingLift || skier.State == SkierState.WaitingForLift) { active = false; return; }

            timer -= Time.deltaTime;
            if (choice == Choice.None)
            {
                if (timer <= 0f) Decide();
                return;
            }

            if (choice == Choice.Leave) { skier.SetState(SkierState.Leaving); active = false; return; }
            if (choice == Choice.Lift) { active = false; routeAgent?.QueueNextAvailableLift(transform.position); return; }

            if (target == null) { active = false; routeAgent?.QueueNextAvailableLift(transform.position); return; }
            if (Vector3.Distance(transform.position, target.position) > 2f)
            {
                skier.SetState(SkierState.Resting);
                skier.MoveTo(target.position);
                return;
            }

            if (!consumed)
            {
                consumed = TryConsume();
                if (consumed) timer = Mathf.Max(2f, decisionDelay * 2.5f);
                else { choice = Choice.Lift; timer = .2f; }
                return;
            }

            if (timer <= 0f)
            {
                active = false;
                if (UnityEngine.Random.value < .08f && area != null && area.IsBaseArea && ManagementJourneyAccess.CanExitToVillage(transform.position))
                    skier.SetState(SkierState.Leaving);
                else
                    routeAgent?.QueueNextAvailableLift(transform.position);
            }
        }

        public bool Begin(SlopeTerminalAreaRuntime terminal)
        {
            if (skier == null || routeAgent == null || terminal == null || active) return false;
            area = terminal;
            active = true;
            choice = Choice.None;
            consumed = false;
            target = null;
            timer = decisionDelay;
            skier.SetState(SkierState.Resting);
            return true;
        }

        private void Decide()
        {
            var dayPlan = VisitorDayPlan328.Ensure(skier);
            bool late = (dayPlan != null && dayPlan.ShouldHeadHomeNow()) ||
                        (GameClock.I != null && GameClock.I.CurrentHour >= leaveAtHour);
            if (area != null && area.IsBaseArea && ManagementJourneyAccess.CanExitToVillage(transform.position) && (late || UnityEngine.Random.value < baseLeaveChance))
            {
                choice = Choice.Leave;
                return;
            }

            var lift = FindNearbyLift(area != null ? area.Center : transform.position);
            var leisure = FindNearbyLeisure(area != null ? area.Center : transform.position);
            var activity = FindNearbyActivity(area != null ? area.Center : transform.position);

            float liftScore = lift != null ? 1f : 0f;
            float leisureScore = leisure != null ? .45f : 0f;
            float activityScore = activity != null ? .45f : 0f;
            if (VisitorIntentRuntime.I != null)
            {
                var intent = VisitorIntentRuntime.I.GetIntent(skier);
                if (intent == VisitorIntent.MountainDiscovery || intent == VisitorIntent.Relaxation || intent == VisitorIntent.ApresSki)
                { leisureScore += .45f; activityScore += .35f; }
                if (intent == VisitorIntent.FamilyDay) leisureScore += .35f;
                if (intent == VisitorIntent.SportPerformance || intent == VisitorIntent.Training) liftScore += .30f;
            }

            float total = liftScore + leisureScore + activityScore;
            if (total <= .01f) { choice = Choice.Lift; return; }
            float r = UnityEngine.Random.value * total;
            if ((r -= liftScore) <= 0f) { choice = Choice.Lift; return; }
            if ((r -= leisureScore) <= 0f) { choice = Choice.Leisure; target = leisure != null ? leisure.transform : null; return; }
            choice = Choice.Activity;
            target = activity != null ? activity.transform : null;
        }

        private bool TryConsume()
        {
            if (choice == Choice.Leisure && target != null)
            {
                var facility = target.GetComponent<LeisureFacilityRuntime>();
                if (facility != null && facility.TryEnter())
                {
                    VisitorIntentRuntime.I?.RegisterServiceChoice(skier, facility.definition != null ? facility.definition.facilityName : facility.name);
                    return true;
                }
            }
            if (choice == Choice.Activity && target != null)
            {
                var activity = target.GetComponent<MountainActivityRuntime>();
                if (activity != null && activity.TryEnter())
                {
                    VisitorIntentRuntime.I?.RegisterActivityChoice(skier, activity);
                    return true;
                }
            }
            return false;
        }

        private LeisureFacilityRuntime FindNearbyLeisure(Vector3 center)
        {
            LeisureFacilityRuntime best = null; float bestScore = float.MinValue;
            foreach (var f in LeisureFacilityRuntime.All)
            {
                if (f == null || f.definition == null || f.AvailableCapacity <= 0) continue;
                float d = Vector3.Distance(center, f.transform.position);
                if (d > facilityRadius) continue;
                float score = -d * .02f;
                if (VisitorBehaviorRuntime.I != null) score += VisitorIntentRuntime.I != null ? VisitorIntentRuntime.I.ScoreServiceIntent(skier, f) : 0f;
                if (score > bestScore) { bestScore = score; best = f; }
            }
            return best;
        }

        private MountainActivityRuntime FindNearbyActivity(Vector3 center)
        {
            MountainActivityRuntime best = null; float bestScore = float.MinValue;
            foreach (var a in MountainActivityRuntime.All)
            {
                if (a == null || a.definition == null || a.AvailableCapacity <= 0) continue;
                float d = Vector3.Distance(center, a.transform.position);
                if (d > facilityRadius) continue;
                float score = -d * .02f;
                if (VisitorIntentRuntime.I != null) score += VisitorIntentRuntime.I.ScoreActivityIntent(skier, a);
                if (score > bestScore) { bestScore = score; best = a; }
            }
            return best;
        }

        private LiftRuntime FindNearbyLift(Vector3 center)
        {
            LiftRuntime best = null; float bestScore = float.MinValue;
            var lifts = Object.FindObjectsOfType<LiftRuntime>();
            foreach (var lift in lifts)
            {
                if (lift == null || !lift.Open || lift.Faulted) continue;
                float d = Vector3.Distance(center, lift.Bottom);
                if (d > facilityRadius) continue;
                float score = -d * .02f;
                if (VisitorIntentRuntime.I != null) score += VisitorIntentRuntime.I.ScoreLiftIntent(skier, lift);
                if (score > bestScore) { bestScore = score; best = lift; }
            }
            return best;
        }
    }
}
