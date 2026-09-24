using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// One-click helper: click a point on an existing slope, then press C to create a short branch
    /// to the nearest compatible lift bottom. The branch is not a new piste.
    /// </summary>
    public sealed class SlopeLiftConnectorBuilder : MonoBehaviour
    {
        [SerializeField] private Camera buildCamera;
        [SerializeField] private float liftSearchRadius = 30f;
        [SerializeField] private float connectorWidth = 3.5f;
        [SerializeField] private float maxEntryOffset = 3.5f;

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.C)) return;
            if (buildCamera == null) buildCamera = Camera.main;
            if (buildCamera == null) return;
            if (!Physics.Raycast(buildCamera.ScreenPointToRay(Input.mousePosition), out var hit, 5000f)) return;
            var slope = hit.collider.GetComponentInParent<SlopeRuntime>();
            if (slope == null) return;
            CreateToNearestLift(slope, hit.point);
        }

        public SlopeLiftConnectorRuntime CreateToNearestLift(SlopeRuntime slope, Vector3 clickedPoint)
        {
            if (slope == null || slope.Points.Count < 2) return null;
            LiftRuntime target = FindNearestLift(clickedPoint);
            if (target == null) return null;

            Vector3 entry = FindClosestPointOnSlope(slope, clickedPoint, out int index);
            var route = BuildRoute(entry, target.Bottom, slope, index);
            var go = new GameObject("SlopeLiftConnector");
            var connector = go.AddComponent<SlopeLiftConnectorRuntime>();
            connector.Build(slope, target, entry, route, connectorWidth);
            return connector;
        }

        private LiftRuntime FindNearestLift(Vector3 point)
        {
            LiftRuntime best = null; float bestD = liftSearchRadius * liftSearchRadius;
            var lifts = Object.FindObjectsOfType<LiftRuntime>();
            for (int i = 0; i < lifts.Length; i++)
            {
                var lift = lifts[i];
                if (lift == null || !lift.Open || lift.Faulted) continue;
                float d = (lift.Bottom - point).sqrMagnitude;
                if (d < bestD) { bestD = d; best = lift; }
            }
            return best;
        }

        private static Vector3 FindClosestPointOnSlope(SlopeRuntime slope, Vector3 point, out int index)
        {
            index = 0; float best = float.MaxValue; Vector3 result = slope.TopPoint;
            for (int i = 0; i < slope.Points.Count; i++)
            {
                float d = (slope.Points[i] - point).sqrMagnitude;
                if (d < best) { best = d; index = i; result = slope.Points[i]; }
            }
            return result;
        }

        private static List<Vector3> BuildRoute(Vector3 entry, Vector3 liftBottom, SlopeRuntime slope, int index)
        {
            var route = new List<Vector3> { entry };
            Vector3 mid = Vector3.Lerp(entry, liftBottom, .5f);
            mid.y = Mathf.Max(entry.y, liftBottom.y) + 0.15f;
            route.Add(mid);
            route.Add(liftBottom);
            return route;
        }
    }
}
