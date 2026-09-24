using UnityEngine;

namespace SkiStation
{
    /// <summary>Lightweight construction selection/removal layer for the playable slice.</summary>
    public sealed class ConstructionSelectionRuntime : MonoBehaviour
    {
        public static ConstructionSelectionRuntime I { get; private set; }
        public Component Selected { get; private set; }
        public string SelectedLabel => Selected == null ? string.Empty : BuildLabel(Selected);
        public bool HasSelection => Selected != null;
        [SerializeField] private float routePickRadius = 16f;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        private void Update()
        {
            if (SlopeConstructionMode.Instance != null && SlopeConstructionMode.Instance.IsDrawing) return;
            var planner = Object.FindFirstObjectByType<LiftConstructionPlanner>();
            if (planner != null && planner.IsActive) return;

            if (Input.GetMouseButtonDown(0) && !PlayableGameHUD.PointerOverCameraControls) SelectAtMouse();
            if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace)) RemoveSelected();
            if (Input.GetKeyDown(KeyCode.Escape)) Selected = null;
        }

        public void SelectAtMouse()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 5000f, ~0, QueryTriggerInteraction.Ignore))
            {
                var slope = hit.collider.GetComponentInParent<SlopeRuntime>();
                if (slope != null) { Selected = slope; return; }
                var lift = hit.collider.GetComponentInParent<LiftRuntime>();
                if (lift != null) { Selected = lift; return; }
                SelectNearestRoute(hit.point);
            }
        }

        private void SelectNearestRoute(Vector3 point)
        {
            Component best = null; float bestDistance = routePickRadius;
            foreach (var slope in Object.FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None))
            {
                float d = DistanceToPolyline(point, slope.Points);
                if (d < bestDistance) { bestDistance = d; best = slope; }
            }
            foreach (var lift in Object.FindObjectsByType<LiftRuntime>(FindObjectsSortMode.None))
            {
                float d = DistanceToPolyline(point, lift.Route);
                if (d < bestDistance) { bestDistance = d; best = lift; }
            }
            Selected = best;
        }

        private static float DistanceToPolyline(Vector3 p, System.Collections.Generic.IReadOnlyList<Vector3> route)
        {
            if (route == null || route.Count == 0) return float.MaxValue;
            float best = Vector3.Distance(p, route[0]);
            for (int i = 1; i < route.Count; i++)
            {
                Vector3 a = route[i - 1], b = route[i];
                Vector3 ab = b - a;
                float t = ab.sqrMagnitude < .001f ? 0f : Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, Vector3.Distance(p, a + ab * t));
            }
            return best;
        }

        public void RemoveSelected()
        {
            if (Selected == null) return;
            string label = BuildLabel(Selected);
            var slope = Selected as SlopeRuntime;
            if (slope != null) SlopeNetwork.I?.Unregister(slope);
            GameObject go = Selected.gameObject;
            Selected = null;
            Destroy(go);
            PlayableLoopRuntime.I?.SetMessage(label + " supprime.");
        }

        private static string BuildLabel(Component component)
        {
            var slope = component as SlopeRuntime;
            if (slope != null) return "Piste " + slope.Difficulty + " - " + slope.Length.ToString("N0") + " m";
            var lift = component as LiftRuntime;
            if (lift != null) return "Remontee - " + lift.Length.ToString("N0") + " m";
            return component.name;
        }
    }

    public static class ConstructionSelectionBootstrap191
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Ensure()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (Object.FindFirstObjectByType<ConstructionSelectionRuntime>() == null)
                new GameObject("Construction Selection 1.91").AddComponent<ConstructionSelectionRuntime>();
        }
    }
}
