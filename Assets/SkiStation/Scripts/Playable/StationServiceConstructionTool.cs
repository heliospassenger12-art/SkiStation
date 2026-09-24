using UnityEngine;

namespace SkiStation
{
    /// <summary>Click-to-place bridge with mandatory recap before committing service construction.</summary>
    public sealed class StationServiceConstructionTool : MonoBehaviour
    {
        public static StationServiceConstructionTool I { get; private set; }
        public StationServiceType Type { get; private set; } = StationServiceType.Restaurant;
        public ServiceRange Range { get; private set; } = ServiceRange.Standard;
        public bool Active { get; private set; }

        private Camera cam;
        private void Awake() { if (I != null && I != this) { Destroy(gameObject); return; } I = this; StationServiceConstructionConfirmationRuntime.Ensure(); }
        public void Configure(StationServiceType type, ServiceRange range) { Type = type; Range = range; Active = true; }
        public void Stop() { Active = false; }
        public void ResumePlacement() { Active = true; }
        public void CancelPlacement() { Active = false; }

        public void ConfirmPending(Vector3 position)
        {
            if (StationServiceConstructionRuntime.I == null) return;
            var built = StationServiceConstructionRuntime.I.Build(Type, position, Range);
            if (built != null)
            {
                Active = false;
                StationServiceConstructionConfirmationRuntime.Ensure().Close();
            }
        }

        private void Update()
        {
            if (!Active) return;
            var confirmation = StationServiceConstructionConfirmationRuntime.Ensure();
            if (confirmation.IsOpen) return;
            if (cam == null) cam = Camera.main;
            if (Input.GetMouseButtonDown(1)) { Stop(); return; }
            if (cam == null || !Input.GetMouseButtonDown(0) || StationServiceConstructionRuntime.I == null || PlayableGameHUD.PointerOverCameraControls) return;
            RaycastHit hit;
            if (!Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out hit, 5000f)) return;
            Vector3 position = hit.point;
            if (MountainRuntime.I != null)
            {
                Vector3 surface;
                if (MountainRuntime.I.TryGetSurface(position, out surface)) position = surface;
            }
            confirmation.Open(this, position);
        }
    }
}
