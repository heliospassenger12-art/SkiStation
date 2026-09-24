using UnityEngine;

namespace SkiStation
{
    public enum SlopeEndpointKind { None, LiftTop, LiftBottom, SlopeTop, SlopeBottom, ResortPoint, Building }

    [System.Serializable]
    public struct SlopeEndpointConnection
    {
        public SlopeEndpointKind kind;
        public SlopeRuntime slope;
        public GameObject target;
        public float distance;

        public bool IsValid => kind != SlopeEndpointKind.None && (slope != null || target != null || kind == SlopeEndpointKind.ResortPoint);
    }

    public sealed class SlopeConnectionRuntime : MonoBehaviour
    {
        public SlopeRuntime Slope { get; private set; }
        public SlopeEndpointConnection Top { get; private set; }
        public SlopeEndpointConnection Bottom { get; private set; }

        private void Awake()
        {
            if (ResortSlopeGraph.I == null) new GameObject("ResortSlopeGraph").AddComponent<ResortSlopeGraph>();
            ResortSlopeGraph.I.Register(this);
        }

        public void Initialize(SlopeRuntime slope)
        {
            Slope = slope;
        }

        public void SetTopSlope(SlopeRuntime target, float distance)
        {
            Top = new SlopeEndpointConnection { kind = target == null ? SlopeEndpointKind.None : SlopeEndpointKind.SlopeTop, slope = target, distance = distance };
            RefreshLegacyFlags();
        }

        public void SetBottomSlope(SlopeRuntime target, float distance)
        {
            Bottom = new SlopeEndpointConnection { kind = target == null ? SlopeEndpointKind.None : SlopeEndpointKind.SlopeBottom, slope = target, distance = distance };
            RefreshLegacyFlags();
        }

        public void SetTopTarget(SlopeEndpointKind kind, GameObject target, float distance)
        {
            Top = new SlopeEndpointConnection { kind = kind, target = target, distance = distance };
            RefreshLegacyFlags();
        }

        public void SetBottomTarget(SlopeEndpointKind kind, GameObject target, float distance)
        {
            Bottom = new SlopeEndpointConnection { kind = kind, target = target, distance = distance };
            RefreshLegacyFlags();
        }

        public void ClearTop() { Top = default; RefreshLegacyFlags(); }
        public void ClearBottom() { Bottom = default; RefreshLegacyFlags(); }

        private void RefreshLegacyFlags()
        {
            if (Slope == null) return;
            Slope.SetConnections(ToLegacy(Top.kind), ToLegacy(Bottom.kind));
        }

        private static SlopeEndConnection ToLegacy(SlopeEndpointKind kind)
        {
            switch (kind)
            {
                case SlopeEndpointKind.LiftTop:
                case SlopeEndpointKind.LiftBottom: return SlopeEndConnection.Lift;
                case SlopeEndpointKind.SlopeTop:
                case SlopeEndpointKind.SlopeBottom: return SlopeEndConnection.Slope;
                case SlopeEndpointKind.ResortPoint: return SlopeEndConnection.ResortPoint;
                case SlopeEndpointKind.Building: return SlopeEndConnection.Building;
                default: return SlopeEndConnection.None;
            }
        }
    }
}
