using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// 2.14 strict player-construction visual loop. One real SkierRuntime is temporarily given a single,
    /// explicit world-space movement authority so we can validate what the player sees:
    /// bottom station -> lift line -> top station -> piste -> bottom.
    /// This deliberately bypasses the older competing transfer/route movers for ONE skier.
    /// </summary>
    public sealed class VisibleSkiLoopAuthorityRuntime : MonoBehaviour
    {
        private enum Phase { Acquire, ToLift, LiftRide, ToSlope, SkiSlope, ReturnToLift }

        public SkierRuntime TrackedSkier => skier;
        private SkierRuntime skier;
        private LiftRuntime lift;
        private SlopeRuntime slope;
        private Phase phase = Phase.Acquire;
        private float phaseT;
        private readonly List<Vector3> liftPath = new List<Vector3>();
        private readonly List<Vector3> slopePath = new List<Vector3>();
        private float nextAcquire;
        private bool boardingAligned;
        private int liftWaypoint;
        private int slopeWaypoint;
        private const float ArrivalTolerance = .35f;
        private GameObject marker;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (FindFirstObjectByType<VisibleSkiLoopAuthorityRuntime>() == null)
                new GameObject("SkiStation_2.14_PLAYER_PATH_AUTHORITY").AddComponent<VisibleSkiLoopAuthorityRuntime>();
        }

        private void Update()
        {
            if (skier == null || lift == null || slope == null)
            {
                if (Time.unscaledTime >= nextAcquire)
                {
                    nextAcquire = Time.unscaledTime + .5f;
                    TryAcquire();
                }
                return;
            }

            if (!lift.Open || lift.Faulted || !slope.Open) return;

            // Absolute authority for the tracked proof skier.
            skier.StopMoving();

            switch (phase)
            {
                case Phase.ToLift:
                    skier.SetState(SkierState.WaitingForLift);
                    MoveToBoarding(Phase.LiftRide);
                    break;

                case Phase.LiftRide:
                    skier.SetState(SkierState.RidingLift);
                    MoveWaypointByWaypoint(liftPath, ref liftWaypoint, Mathf.Max(2f, lift.EffectiveSpeedKmh / 3.6f), Phase.ToSlope, true);
                    break;

                case Phase.ToSlope:
                    skier.SetState(SkierState.Skiing);
                    MoveLinear(skier.transform.position, slope.TopPoint, 14f, Phase.SkiSlope);
                    break;

                case Phase.SkiSlope:
                    skier.SetState(SkierState.Skiing);
                    MoveWaypointByWaypoint(slopePath, ref slopeWaypoint, 18f, Phase.ReturnToLift, false);
                    break;

                case Phase.ReturnToLift:
                    skier.SetState(SkierState.WaitingForLift);
                    MoveToBoarding(Phase.LiftRide);
                    break;
            }

            if (marker != null) marker.transform.position = skier.transform.position + Vector3.up * 4f;
        }

        private void TryAcquire()
        {
            var lifts = FindObjectsByType<LiftRuntime>(FindObjectsSortMode.None);
            var slopes = FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None);
            var skiers = FindObjectsByType<SkierRuntime>(FindObjectsSortMode.None);
            if (lifts.Length == 0 || slopes.Length == 0 || skiers.Length == 0) return;

            // 2.13: construction objects are the source of truth.
            // The bootstrap also creates a demonstration lift/slope. 2.12 could accidentally
            // lock onto that hidden/demo pair, which explains why the state loop was correct
            // while the marker was nowhere near the player's visible construction.
            // Player-built slopes are named Piste_* by SlopeConstructionMode and player-built
            // lifts are root objects created by LiftConstructionPlanner. Prefer those strictly.
            LiftRuntime builtLift = null;
            for (int i = 0; i < lifts.Length; i++)
            {
                var candidate = lifts[i];
                if (candidate == null || !candidate.Open || candidate.Faulted) continue;
                bool demo = candidate.name.Contains("Démonstration") || candidate.name.Contains("Demonstration") ||
                            candidate.LiftId == "starter_panorama_6seat";
                if (!demo && candidate.transform.parent == null) { builtLift = candidate; break; }
            }

            SlopeRuntime builtSlope = null;
            for (int j = 0; j < slopes.Length; j++)
            {
                var candidate = slopes[j];
                if (candidate == null || !candidate.Open || candidate.Points.Count < 2) continue;
                if (candidate.name.StartsWith("Piste_") && builtLift != null && Vector3.Distance(builtLift.Top, candidate.TopPoint) <= ManagementJourneyAccess.SlopeConnectionRadius) { builtSlope = candidate; break; }
            }

            // 2.14: NO fallback to starter/demo geometry. Until BOTH player constructions exist,
            // there is deliberately no proof skier and no ski loop.
            if (builtLift == null || builtSlope == null) return;
            lift = builtLift;
            slope = builtSlope;

            // Follow a real arrival, never an arbitrary agent already on a slope or lift.
            skier = null;
            for (int k = 0; k < skiers.Length; k++)
                if (skiers[k] != null && skiers[k].State == SkierState.Resting &&
                    VisitorSkiAdmission.CanSki(skiers[k]) &&
                    (skiers[k].GetComponent<VisitorLifecycleRuntime>() == null || !skiers[k].GetComponent<VisitorLifecycleRuntime>().OwnsMovement) &&
                    (skiers[k].GetComponent<SkierTerminalDecisionRuntime>() == null || !skiers[k].GetComponent<SkierTerminalDecisionRuntime>().Active) &&
                    ManagementJourneyAccess.CanWalk(skiers[k].transform.position, builtLift.Bottom, ManagementJourneyAccess.LiftApproachRadius))
                { skier = skiers[k]; break; }
            if (skier == null) { lift = null; slope = null; return; }
            // Remove it from any vehicle and stop older movement components from fighting us.
            skier.transform.SetParent(null, true);
            Disable<SkierRouteAgent>();
            Disable<SkierMountainTransferRuntime>();
            Disable<LiftPostArrivalRoutingRuntime>();
            Disable<LiftPassengerDisembarkRuntime>();
            Disable<SkierAlternateRouteRuntime>();

            liftPath.Clear();
            for (int i = 0; i < lift.Route.Count; i++) liftPath.Add(lift.Route[i]);
            if (liftPath.Count < 2) { liftPath.Add(lift.Bottom); liftPath.Add(lift.Top); }

            slopePath.Clear();
            for (int i = 0; i < slope.Points.Count; i++) slopePath.Add(slope.Points[i]);

            BuildMarker();
            SetPhase(Phase.ToLift);
            Debug.Log("SkiStation 2.14 PLAYER PATH LOCKED | lift=" + lift.name +
                      " | slope=" + slope.name + " | top gap=" +
                      Vector3.Distance(lift.Top, slope.TopPoint).ToString("0.0") + " m");
        }

        private void Disable<T>() where T : Behaviour
        {
            var c = skier != null ? skier.GetComponent<T>() : null;
            if (c != null) c.enabled = false;
        }

        private void SetPhase(Phase next)
        {
            if (next == Phase.LiftRide) VisitorSkiAdmission.RecordBoarding(skier);
            phase = next;
            phaseT = 0f;
            boardingAligned = false;
            if (next == Phase.LiftRide) liftWaypoint = 0;
            if (next == Phase.SkiSlope) slopeWaypoint = 0;
            Debug.Log("SkiStation 2.14 PLAYER PATH => " + phase);
        }

        // Boarding must begin at the physical lower station. Never jump to route[0]
        // while the agent is still approaching from the village.
        private void MoveToBoarding(Phase next)
        {
            Vector3 station = lift.Bottom + Vector3.up * .7f;
            Vector3 here = skier.transform.position;
            Vector3 flat = new Vector3(station.x - here.x, 0f, station.z - here.z);
            if (flat.magnitude > 1.25f)
            {
                Vector3 step = Vector3.MoveTowards(here, new Vector3(station.x, here.y, station.z), 6f * Time.deltaTime);
                if (ManagementSurface.TryGet(step, out var ground)) step.y = ground.y + .7f;
                skier.transform.position = step;
                Face(flat);
                return;
            }
            // Approach the actual first cable point continuously, including any station offset.
            Vector3 entry = liftPath.Count > 0 ? liftPath[0] + Vector3.up * .7f : station;
            if (!boardingAligned) { boardingAligned = true; return; }
            skier.transform.position = Vector3.MoveTowards(here, entry, 3f * Time.deltaTime);
            Face(entry - here);
            if (Vector3.Distance(skier.transform.position, entry) <= ArrivalTolerance)
                SetPhase(next);
        }

        private void MoveLinear(Vector3 fromNow, Vector3 target, float speed, Phase next)
        {
            float d = Vector3.Distance(fromNow, target);
            if (d <= 1f)
            {
                skier.transform.position = target + Vector3.up * .7f;
                SetPhase(next);
                return;
            }
            Vector3 targetRaised = target + Vector3.up * .7f;
            skier.transform.position = Vector3.MoveTowards(skier.transform.position, targetRaised, speed * Time.deltaTime);
            Face(targetRaised - skier.transform.position);
        }

        // Every frame moves towards the NEXT physical waypoint, never assigns a remote
        // interpolated position. This makes a discontinuous route visible and testable.
        private void MoveWaypointByWaypoint(List<Vector3> path, ref int waypoint,
            float speed, Phase next, bool airborne)
        {
            if (path == null || path.Count < 2) { SetPhase(Phase.Acquire); return; }
            waypoint = Mathf.Clamp(waypoint, 0, path.Count - 1);
            Vector3 target = path[waypoint] + Vector3.up * .7f;
            Vector3 before = skier.transform.position;
            skier.transform.position = Vector3.MoveTowards(before, target, speed * Time.deltaTime);
            Face(target - before);
            if (Vector3.Distance(skier.transform.position, target) > ArrivalTolerance) return;
            if (waypoint < path.Count - 1) { waypoint++; return; }
            SetPhase(next);
        }

        private void Face(Vector3 dir)
        {
            if (dir.sqrMagnitude > .01f)
                skier.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        private void BuildMarker()
        {
            if (marker != null) return;
            marker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            marker.name = "VISIBLE_LOOP_SKIER_MARKER";
            marker.transform.localScale = new Vector3(2.2f, 3.5f, 2.2f);
            var col = marker.GetComponent<Collider>(); if (col != null) Destroy(col);
            var r = marker.GetComponent<Renderer>();
            var shader = Shader.Find("Standard");
            if (r != null && shader != null)
            {
                var m = new Material(shader);
                m.color = new Color(1f, .08f, .35f);
                r.material = m;
            }
        }

        private void OnGUI()
        {
            if (skier == null) return;
            var style = new GUIStyle(GUI.skin.box) { fontSize = 17, alignment = TextAnchor.MiddleCenter };
            GUI.Box(new Rect(Screen.width * .5f - 180f, 105f, 360f, 42f),
                "2.14 • TES CONSTRUCTIONS : " + phase.ToString().ToUpperInvariant(), style);
        }
    }
}
