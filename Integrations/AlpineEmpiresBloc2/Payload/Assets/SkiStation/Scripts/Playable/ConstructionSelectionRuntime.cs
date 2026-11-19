using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>Shared world/list selection, visible outline and a single-use demolition recap.</summary>
    public sealed class ConstructionSelectionRuntime : MonoBehaviour
    {
        public static ConstructionSelectionRuntime I { get; private set; }
        public Component Selected { get; private set; }
        public string SelectedLabel => Selected == null ? string.Empty : BuildLabel(Selected);
        public bool HasSelection => Selected != null;
        public bool IsRemovalPending => removalTarget != null;
        Component removalTarget;
        LineRenderer outline;
        Material outlineMaterial;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }
        void Update()
        {
            if (Arcade.ArcadeSceneIsolation.IsActive) return;
            // A preview owns consultation clicks, never demolition of an earlier real selection.
            if (NetworkRecommendationPreviewRuntime388.I != null && NetworkRecommendationPreviewRuntime388.I.OverlayVisible) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { ClearSelection349(); return; }
            if (IsRemovalPending) return;
            if (ConstructionInput349.AnyToolActive) return;
            if (VisitorDetailsPanel255.HasSelectedVisitor) { ClearSelection349(); return; }
            if (Input.GetMouseButtonDown(0) && !ConstructionInput349.PointerBlocked && !Input.GetKey(KeyCode.LeftShift)) SelectAtMouse();
            if (!ConstructionInput349.PointerBlocked && (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))) RemoveSelected();
            if (Selected == null && outline != null) outline.enabled = false;
        }

        public void Select349(Component target)
        {
            if (target != null && !(target is SlopeRuntime) && !(target is LiftRuntime) && !(target is StationStructureStatus249)) return;
            removalTarget = null; Selected = target;
            RefreshOutline(); StationStructureStatusPanel249.Show349(target);
        }
        public void ClearSelection349()
        {
            Selected = removalTarget = null;
            if (outline != null) outline.enabled = false;
            StationStructureStatusPanel249.Hide349();
        }
        public void SelectAtMouse()
        {
            if (ConstructionInput349.AnyToolActive || ConstructionInput349.PointerBlocked) return;
            var camera = Camera.main; if (camera == null) return;
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            float groundDistance = ManagementSurface.TryRaycast349(ray, out var ground) ? Vector3.Distance(ray.origin, ground) + 2f : float.MaxValue;
            Component best = null; float closest = groundDistance;
            foreach (var status in Object.FindObjectsByType<StationStructureStatus249>(FindObjectsSortMode.None))
            {
                if (!status.gameObject.activeInHierarchy) continue;
                if (ConstructionPlacement349.BoundsOf(status.gameObject).IntersectRay(ray, out float distance) && distance >= 0 && distance < closest)
                { best = status; closest = distance; }
            }
            if (Physics.Raycast(ray, out var hit, closest, ~0, QueryTriggerInteraction.Ignore))
            {
                var slope = hit.collider.GetComponentInParent<SlopeRuntime>();
                var lift = hit.collider.GetComponentInParent<LiftRuntime>();
                if (slope != null) best = slope; else if (lift != null) best = lift;
            }
            if (best == null)
            {
                float bestPixels = 16f;
                foreach (var slope in Object.FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None))
                    PickRoute(camera, slope, slope.Points, ref best, ref bestPixels);
                foreach (var lift in Object.FindObjectsByType<LiftRuntime>(FindObjectsSortMode.None))
                    PickRoute(camera, lift, lift.Route, ref best, ref bestPixels);
            }
            Select349(best);
        }
        static void PickRoute(Camera camera, Component target, IReadOnlyList<Vector3> route, ref Component best, ref float bestPixels)
        {
            if (route == null) return;
            Vector2 mouse = Input.mousePosition;
            for (int i = 1; i < route.Count; i++)
            {
                Vector3 a = camera.WorldToScreenPoint(route[i - 1]), b = camera.WorldToScreenPoint(route[i]);
                if (a.z <= 0 || b.z <= 0) continue;
                Vector2 ab = (Vector2)(b - a);
                float t = ab.sqrMagnitude < .001f ? 0 : Mathf.Clamp01(Vector2.Dot(mouse - (Vector2)a, ab) / ab.sqrMagnitude);
                float pixels = Vector2.Distance(mouse, (Vector2)a + ab * t);
                if (pixels >= bestPixels) continue;
                Vector3 point = Vector3.Lerp(route[i - 1], route[i], t);
                Vector3 delta = point - camera.transform.position;
                if (ManagementSurface.TryRaycast349(new Ray(camera.transform.position, delta.normalized), out var ground) &&
                    Vector3.Distance(camera.transform.position, ground) < delta.magnitude - 3f) continue;
                bestPixels = pixels; best = target;
            }
        }
        void RefreshOutline()
        {
            if (Selected == null) { if (outline != null) outline.enabled = false; return; }
            if (outline == null)
            {
                var go = new GameObject("349_Selection"); go.transform.SetParent(transform, false);
                outline = go.AddComponent<LineRenderer>(); outline.useWorldSpace = true;
                outlineMaterial = RuntimeMaterialFactory.Create(new Color(.1f, .9f, 1f));
                outline.sharedMaterial = outlineMaterial; outline.numCapVertices = 4;
            }
            var points = new List<Vector3>();
            if (Selected is SlopeRuntime slope) points.AddRange(slope.Points);
            else if (Selected is LiftRuntime lift) points.AddRange(lift.Route);
            else
            {
                Bounds b = ConstructionPlacement349.BoundsOf(Selected.gameObject);
                points.Add(new Vector3(b.min.x, b.min.y, b.min.z)); points.Add(new Vector3(b.max.x, b.min.y, b.min.z));
                points.Add(new Vector3(b.max.x, b.min.y, b.max.z)); points.Add(new Vector3(b.min.x, b.min.y, b.max.z)); points.Add(points[0]);
            }
            outline.positionCount = points.Count; outline.widthMultiplier = 1.4f;
            outline.startColor = outline.endColor = new Color(.1f, .9f, 1f);
            for (int i = 0; i < points.Count; i++) outline.SetPosition(i, points[i] + Vector3.up * 1.2f);
            outline.enabled = true;
        }
        public void RemoveSelected()
        {
            if (Selected == null || ConstructionInput349.AnyToolActive) return;
            removalTarget = Selected; ConstructionInput349.ConsumeClick();
        }
        public bool CanRemove349(Component target, out string reason)
        {
            reason = "Selection disparue";
            if (target == null) return false;
            if (target.GetComponent<StationServiceRuntime>()?.UsesAlliedAccount == true)
            { reason = "Ce bâtiment appartient à une autre entreprise."; return false; }
            if (target is SlopeRuntime slope && slope.TrafficCount > 0)
            { reason = "Des skieurs descendent encore cette piste. Attendez leur sortie."; return false; }
            if (target is LiftRuntime lift)
            {
                // 3.78.3: destruction is impossible while the lift is accepting passengers.
                // Close first, release the queue, then let every onboard passenger reach the top.
                if (lift.Open)
                { reason = "Fermez d'abord la remontee. Elle doit etre vide avant destruction."; return false; }
                if (lift.StationLogic != null && lift.StationLogic.QueueSize > 0)
                { reason = "Fermeture en cours : attendez que la file soit entierement liberee."; return false; }
                if (lift.HasOnboardPassengers)
                { reason = "Vidage en cours : les passagers doivent atteindre la gare d'arrivee."; return false; }
            }
            var lodging = target.GetComponent<StationAccommodation249>();
            if (lodging != null && lodging.CurrentGuests > 0)
            { reason = "L'hebergement accueille encore des clients."; return false; }
            if (!(target is SlopeRuntime) && !(target is LiftRuntime) && !(target is StationStructureStatus249))
            { reason = "Cette infrastructure ne peut pas etre supprimee."; return false; }
            reason = ""; return true;
        }
        public void ConfirmRemoval349()
        {
            Component target = removalTarget;
            if (target == null || target != Selected || !CanRemove349(target, out _)) return;
            string label = BuildLabel(target);
            ClearSelection349(); ConstructionInput349.ConsumeClick();
            var service = target.GetComponent<StationServiceRuntime>();
            if (service != null) { service.CancelAll349(); StationServiceConstructionRuntime.BuiltServices.Remove(service); }
            if (target is SlopeRuntime slope) SlopeNetwork.I?.Unregister(slope);
            ResortRouteNetwork.I?.Unregister349(target);
            StationPedestrianNetwork249.I?.Unregister349(target.gameObject);
            target.gameObject.SetActive(false); Destroy(target.gameObject);
            PlayableLoopRuntime.I?.SetMessage(label + " supprime.");
        }
        void OnGUI()
        {
            if (!IsRemovalPending) return;
            Rect panel = new Rect((Screen.width - 450f) * .5f, (Screen.height - 240f) * .5f, 450f, 240f);
            GUI.Box(panel, "SUPPRIMER CETTE CONSTRUCTION ?");
            GUILayout.BeginArea(new Rect(panel.x + 18, panel.y + 35, panel.width - 36, panel.height - 48));
            GUILayout.Label(BuildLabel(removalTarget));
            GUILayout.Label("Suppression definitive, sans remboursement. Les services et cours de ce batiment seront annules.");
            bool valid = CanRemove349(removalTarget, out string reason);
            if (!valid) GUILayout.Label(reason);
            GUILayout.FlexibleSpace(); GUILayout.BeginHorizontal();
            bool enabled = GUI.enabled; GUI.enabled = enabled && valid;
            if (GUILayout.Button("SUPPRIMER", GUILayout.Height(36))) ConfirmRemoval349();
            GUI.enabled = enabled;
            if (GUILayout.Button("CONSERVER", GUILayout.Height(36))) { removalTarget = null; ConstructionInput349.ConsumeClick(); }
            GUILayout.EndHorizontal(); GUILayout.EndArea();
        }
        void OnDestroy() { if (I == this) I = null; if (outlineMaterial != null) Destroy(outlineMaterial); }
        static string BuildLabel(Component component)
        {
            if (component is SlopeRuntime slope) return "Piste " + slope.Difficulty + " - " + slope.Length.ToString("N0") + " m";
            if (component is LiftRuntime lift) return "Remontee - " + lift.Length.ToString("N0") + " m";
            return component.name;
        }
    }
    public static class ConstructionSelectionBootstrap191
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Ensure()
        {
            if (Arcade.ArcadeSceneIsolation.IsActive) return;
            if (Object.FindFirstObjectByType<ConstructionSelectionRuntime>() == null)
                new GameObject("Construction Selection 3.49").AddComponent<ConstructionSelectionRuntime>();
        }
    }
}
