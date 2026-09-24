using UnityEngine;

namespace SkiStation
{
    // Resolves nearby piste/lift endpoints after construction while keeping explicit connections authoritative.
    public sealed class SlopeEndpointAutoConnector : MonoBehaviour
    {
        [SerializeField] private float slopeSnapRadius = 24f;
        [SerializeField] private float liftSnapRadius = 32f;

        public void Resolve(SlopeRuntime slope)
        {
            if (slope == null) return;
            var c = slope.GetComponent<SlopeConnectionRuntime>();
            if (c == null) c = slope.gameObject.AddComponent<SlopeConnectionRuntime>();
            ResolveEnd(slope, c, true, slope.TopPoint);
            ResolveEnd(slope, c, false, slope.BottomPoint);
        }

        private void ResolveEnd(SlopeRuntime own, SlopeConnectionRuntime c, bool top, Vector3 point)
        {
            SlopeRuntime nearestSlope = null; float nearestSlopeDistance = slopeSnapRadius;
            var slopes = FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None);
            for (int i = 0; i < slopes.Length; i++)
            {
                var other = slopes[i]; if (other == null || other == own) continue;
                float dTop = Vector3.Distance(point, other.TopPoint); float dBottom = Vector3.Distance(point, other.BottomPoint);
                float d = Mathf.Min(dTop, dBottom);
                if (d < nearestSlopeDistance) { nearestSlopeDistance = d; nearestSlope = other; }
            }
            if (nearestSlope != null)
            {
                if (top) c.SetTopSlope(nearestSlope, nearestSlopeDistance); else c.SetBottomSlope(nearestSlope, nearestSlopeDistance);
                return;
            }
            LiftRuntime nearestLift = null; float nearestLiftDistance = liftSnapRadius; bool nearTop = false;
            var lifts = FindObjectsByType<LiftRuntime>(FindObjectsSortMode.None);
            for (int i = 0; i < lifts.Length; i++)
            {
                var lift = lifts[i]; if (lift == null) continue;
                float dt = Vector3.Distance(point, lift.Top); float db = Vector3.Distance(point, lift.Bottom);
                if (dt < nearestLiftDistance) { nearestLiftDistance = dt; nearestLift = lift; nearTop = true; }
                if (db < nearestLiftDistance) { nearestLiftDistance = db; nearestLift = lift; nearTop = false; }
            }
            if (nearestLift != null)
            {
                SlopeEndpointKind kind = nearTop ? SlopeEndpointKind.LiftTop : SlopeEndpointKind.LiftBottom;
                if (top) c.SetTopTarget(kind, nearestLift.gameObject, nearestLiftDistance); else c.SetBottomTarget(kind, nearestLift.gameObject, nearestLiftDistance);
            }
        }
    }
}
