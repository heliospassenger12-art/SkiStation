using UnityEngine;

namespace SkiStation
{
    /// <summary>Beginner-friendly terrain work tool. T toggles, mouse edits, +/- changes radius.</summary>
    public sealed class MountainTerrainWorkMode : MonoBehaviour
    {
        [SerializeField] Camera buildCamera;
        [SerializeField, Min(1f)] float brushRadius = 18f;
        [SerializeField] float strengthPerSecond = 3f;
        [SerializeField] float minRadius = 4f;
        [SerializeField] float maxRadius = 60f;
        bool active;
        int operation; // 0 lower, 1 raise, 2 flatten
        float targetHeight;

        public bool IsActive => active;
        public float BrushRadius => brushRadius;

        void Awake() { if (!buildCamera) buildCamera = Camera.main; }

        void Update()
        {
            if (!buildCamera) buildCamera = Camera.main;
            if (Input.GetKeyDown(KeyCode.T)) active = !active;
            if (!active) return;
            if (Input.GetKeyDown(KeyCode.Alpha1)) operation = 0;
            if (Input.GetKeyDown(KeyCode.Alpha2)) operation = 1;
            if (Input.GetKeyDown(KeyCode.Alpha3)) { operation = 2; CaptureTarget(); }
            if (Input.GetKeyDown(KeyCode.Minus)) brushRadius = Mathf.Max(minRadius, brushRadius - 2f);
            if (Input.GetKeyDown(KeyCode.Equals)) brushRadius = Mathf.Min(maxRadius, brushRadius + 2f);
            if (Input.GetKeyDown(KeyCode.Backspace) && MountainTerrainWorkRuntime.I != null) MountainTerrainWorkRuntime.I.ClearAll();
            if (Input.GetMouseButton(0)) Apply(false);
            if (Input.GetMouseButton(1)) Apply(true);
        }

        void CaptureTarget()
        {
            if (TrySurface(out var p)) targetHeight = p.y;
        }

        void Apply(bool erase)
        {
            if (!TrySurface(out var p) || MountainTerrainWorkRuntime.I == null) return;
            float signed = (operation == 0 ? -1f : 1f) * strengthPerSecond * Time.deltaTime;
            if (erase) signed = -signed;
            MountainTerrainWorkRuntime.I.ApplyBrush(p, brushRadius, signed, operation == 2, targetHeight);
        }

        bool TrySurface(out Vector3 p)
        {
            p = default;
            if (!buildCamera || MountainRuntime.I == null || !MountainRuntime.I.IsGenerated) return false;
            Ray ray = buildCamera.ScreenPointToRay(Input.mousePosition);
            return Physics.Raycast(ray, out RaycastHit hit, 5000f, ~0, QueryTriggerInteraction.Ignore) && hit.collider == MountainRuntime.I.GetComponent<Collider>() && (p = hit.point) != default;
        }
    }
}
