using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum SkierTransferState { None, Travelling, Arrived, Cancelled }

    /// <summary>0.84: physical surface transfer between a lift arrival and the next ski departure.</summary>
    public sealed class SkierMountainTransferRuntime : MonoBehaviour
    {
        public SkierTransferState State { get; private set; }
        public SlopeRuntime TargetSlope { get; private set; }
        public LiftRuntime SourceLift { get; private set; }
        public int WaypointIndex => waypointIndex;
        public IReadOnlyList<Vector3> Waypoints => waypoints;
        public event Action Changed;

        private SkierRuntime skier;
        private SkierRouteAgent agent;
        private readonly List<Vector3> waypoints = new List<Vector3>();
        private int waypointIndex;
        private float reachDistance = 0.8f;
        private bool handedOff;

        public void Begin(SkierRuntime target, LiftRuntime sourceLift, SlopeRuntime targetSlope)
        {
            enabled = true;
            skier = target;
            SourceLift = sourceLift;
            TargetSlope = targetSlope;
            agent = GetComponent<SkierRouteAgent>() ?? gameObject.AddComponent<SkierRouteAgent>();
            waypoints.Clear();
            waypointIndex = 0;
            handedOff = false;

            Vector3 start = sourceLift != null ? sourceLift.Top : transform.position;
            if (Vector3.Distance(transform.position, start) > reachDistance)
                waypoints.Add(start);

            if (targetSlope != null)
            {
                Vector3 top = targetSlope.TopPoint;
                Vector3 pathStart = waypoints.Count > 0 ? waypoints[waypoints.Count - 1] : transform.position;
                var access = GetComponent<SkierAccessPathRuntime>() ?? gameObject.AddComponent<SkierAccessPathRuntime>();
                var networkPath = new List<Vector3>();
                if (!access.TryBuildPath(pathStart, top, networkPath))
                    AppendTransferSegments(pathStart, top);
                else
                    AppendUnique(networkPath);
            }

            State = waypoints.Count > 0 ? SkierTransferState.Travelling : SkierTransferState.Arrived;
            if (skier != null) skier.SetState(SkierState.Skiing);
            if (VisitorIntentRuntime.I != null && targetSlope != null)
                VisitorIntentRuntime.I.SetStage(skier, VisitorJourneyStage.Traveling, targetSlope.name);
            GoNext();
            Changed?.Invoke();

            if (State == SkierTransferState.Arrived)
                HandoffToSlope();
        }

        private void Update()
        {
            if (State != SkierTransferState.Travelling || skier == null || handedOff) return;
            if (waypointIndex >= waypoints.Count)
            {
                State = SkierTransferState.Arrived;
                HandoffToSlope();
                return;
            }

            Vector3 target = waypoints[waypointIndex];
            if (Vector3.Distance(transform.position, target) <= reachDistance)
            {
                waypointIndex++;
                if (waypointIndex >= waypoints.Count)
                {
                    State = SkierTransferState.Arrived;
                    HandoffToSlope();
                }
                else GoNext();
                Changed?.Invoke();
            }
        }

        private void AppendTransferSegments(Vector3 from, Vector3 to)
        {
            float distance = Vector3.Distance(from, to);
            int segments = Mathf.Clamp(Mathf.CeilToInt(distance / 12f), 1, 8);
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 p = Vector3.Lerp(from, to, t);
                // Keep the transfer close to the world surface without requiring a NavMesh.
                p.y = Mathf.Lerp(from.y, to.y, t);
                waypoints.Add(p);
            }
        }


        private void AppendUnique(List<Vector3> points)
        {
            if (points == null) return;
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 p = points[i];
                if (waypoints.Count == 0 || Vector3.Distance(waypoints[waypoints.Count - 1], p) > reachDistance)
                    waypoints.Add(p);
            }
        }

        private void GoNext()
        {
            if (skier != null && waypointIndex < waypoints.Count)
                skier.MoveTo(waypoints[waypointIndex]);
        }

        private void HandoffToSlope()
        {
            if (handedOff) return;
            handedOff = true;
            if (TargetSlope != null)
                agent.BeginFromLift(SourceLift, TargetSlope);
            enabled = false;
            Changed?.Invoke();
        }

        public void Cancel()
        {
            handedOff = true;
            State = SkierTransferState.Cancelled;
            Changed?.Invoke();
            enabled = false;
        }
    }
}
