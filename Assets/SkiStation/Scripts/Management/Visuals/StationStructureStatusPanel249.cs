using UnityEngine;
using UnityEngine.EventSystems;

namespace SkiStation
{
    // F6: select a structure to open its detail card. Status is aligned at the far right of the title row.
    public sealed class StationStructureStatusPanel249 : MonoBehaviour
    {
        private bool visible;
        private Vector2 scroll;
        private Component selected;
        private GUIStyle titleStyle;
        private GUIStyle badgeStyle;
        private GUIStyle bodyStyle;
        private readonly Color panelColor = new Color(.13f, .19f, .25f, .96f);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;
            new GameObject("SkiStation_249_Statuts").AddComponent<StationStructureStatusPanel249>();
        }
        private void Update()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) { Destroy(gameObject); return; }
            if (Input.GetKeyDown(KeyCode.F6)) visible = !visible;
            if (Input.GetMouseButtonDown(0) && !PointerOverInterface() && !ConstructionInProgress())
                TrySelectFromWorld();
        }
        // IMGUI has no EventSystem pointer registration: protect the visible right-hand card
        // explicitly, as well as Unity UI and the existing top/left HUD controls.
        private bool PointerOverInterface()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return true;
            if (PlayableGameHUD.PointerOverCameraControls) return true;
            Vector3 mouse = Input.mousePosition;
            if (visible && mouse.x >= Screen.width - Mathf.Min(430f, Screen.width - 24f) - 12f
                && mouse.y >= Screen.height - 75f - Mathf.Min(580f, Screen.height - 90f)) return true;
            return mouse.y >= Screen.height - 72f || mouse.x <= 375f;
        }
        private static bool ConstructionInProgress()
        {
            if (SlopeConstructionMode.Instance != null && SlopeConstructionMode.Instance.IsDrawing) return true;
            var liftPlanner = FindFirstObjectByType<LiftConstructionPlanner>();
            if (liftPlanner != null && liftPlanner.IsActive) return true;
            return StationVillageConstruction249.IsPlacing;
        }
        private void TrySelectFromWorld()
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            Component hitTarget = null;
            float closest = float.MaxValue;
            // Constructed buildings currently have no colliders on their visual children.
            // Intersect their renderer bounds instead of changing the physics of construction.
            foreach (var status in FindObjectsOfType<StationStructureStatus249>())
            {
                if (status == null || !status.gameObject.activeInHierarchy) continue;
                var renderers = status.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer == null || !renderer.enabled) continue;
                    float distance;
                    if (renderer.bounds.IntersectRay(ray, out distance) && distance >= 0f && distance < closest)
                    { closest = distance; hitTarget = status; }
                }
            }
            RaycastHit physicsHit;
            if (Physics.Raycast(ray, out physicsHit, 5000f, ~0, QueryTriggerInteraction.Ignore))
            {
                var slope = physicsHit.collider.GetComponentInParent<SlopeRuntime>();
                var lift = physicsHit.collider.GetComponentInParent<LiftRuntime>();
                Component target = slope != null ? (Component)slope : lift;
                if (target != null && physicsHit.distance < closest)
                { closest = physicsHit.distance; hitTarget = target; }
            }
            if (hitTarget == null) return; // Clicking empty terrain must not close the card.
            selected = hitTarget;
            visible = true;
        }
        private void Styles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, wordWrap = false };
            titleStyle.normal.textColor = Color.white;
            badgeStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 12 };
            badgeStyle.normal.textColor = Color.white;
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true };
            bodyStyle.normal.textColor = new Color(.88f, .93f, .97f);
        }
        private void OnGUI()
        {
            if (!visible || SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;
            Styles();
            float width = Mathf.Min(430f, Screen.width - 24f);
            Rect area = new Rect(Screen.width - width - 12f, 75f, width, Mathf.Min(580f, Screen.height - 90f));
            Color old = GUI.backgroundColor;
            GUI.backgroundColor = panelColor;
            GUILayout.BeginArea(area, GUI.skin.box);
            GUI.backgroundColor = old;
            GUILayout.BeginHorizontal();
            GUILayout.Label("STRUCTURES  |  F6", titleStyle);
            if (GUILayout.Button("X", GUILayout.Width(28))) { visible = false; selected = null; }
            GUILayout.EndHorizontal();
            if (selected != null) DrawDetails(selected);
            GUILayout.Space(7);
            GUILayout.Label("Selectionner une structure :", bodyStyle);
            scroll = GUILayout.BeginScrollView(scroll);
            foreach (var item in FindObjectsOfType<StationStructureStatus249>())
                if (item != null) DrawEntry(item, item.name);
            foreach (var slope in FindObjectsOfType<SlopeRuntime>())
                if (slope != null) DrawEntry(slope, "Piste : " + slope.name);
            foreach (var lift in FindObjectsOfType<LiftRuntime>())
                if (lift != null) DrawEntry(lift, "Remontee : " + lift.name);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        private void DrawEntry(Component target, string label)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(label, GUILayout.ExpandWidth(true))) selected = target;
            StateBadge(target, 105f);
            GUILayout.EndHorizontal();
        }
        private void DrawDetails(Component target)
        {
            string name = target is SlopeRuntime ? "Piste : " + target.name : target is LiftRuntime ? "Remontee : " + target.name : target.name;
            GUILayout.BeginVertical(GUI.skin.box);
            // Title consumes the remaining width; the badge stays flush right on the SAME line.
            GUILayout.BeginHorizontal();
            GUILayout.Label(name, titleStyle, GUILayout.ExpandWidth(true), GUILayout.MinWidth(0));
            StateBadge(target, 124f);
            GUILayout.EndHorizontal();
            var accommodation = target.GetComponent<StationAccommodation249>();
            if (accommodation != null)
            {
                GUILayout.Label("Capacite : " + accommodation.Capacity + "  |  Occupation : " + accommodation.CurrentGuests + "  |  Places libres : " + accommodation.AvailablePlaces, bodyStyle);
                GUILayout.Label("Tarif indicatif : " + accommodation.NightlyRate() + " EUR / nuit", bodyStyle);
            }
            var shop = target.GetComponent<StationEquipmentShop249>();
            if (shop != null) GUILayout.Label("Magasin de materiel de glisse", bodyStyle);
            bool blocked = IsIntervention(target);
            if (blocked) GUILayout.Label("Intervention en cours : reouverture indisponible.", bodyStyle);
            else if (GUILayout.Button(IsOpen(target) ? "Fermer" : "Ouvrir")) SetOpen(target, !IsOpen(target));
            GUILayout.EndVertical();
        }
        private static bool IsIntervention(Component target)
        {
            var structure = target as StationStructureStatus249;
            if (structure != null) return structure.Current == StationStructureStatus249.State.Intervention;
            var slope = target as SlopeRuntime;
            if (slope != null) { var maintenance = slope.GetComponent<SlopeMaintenanceRuntime>(); return maintenance != null && maintenance.groomingInProgress; }
            var lift = target as LiftRuntime;
            return lift != null && (lift.Faulted || (LiftMaintenanceIntegrationRuntime.Instance != null && LiftMaintenanceIntegrationRuntime.Instance.IsUnderMaintenance(lift.LiftId)));
        }
        private static bool IsOpen(Component target)
        {
            var structure = target as StationStructureStatus249;
            if (structure != null) return structure.Current == StationStructureStatus249.State.Open;
            var slope = target as SlopeRuntime;
            if (slope != null) return slope.Open;
            var lift = target as LiftRuntime;
            return lift != null && lift.Open;
        }
        private static void SetOpen(Component target, bool open)
        {
            if (IsIntervention(target)) return;
            var structure = target as StationStructureStatus249;
            if (structure != null) { structure.SetOpen(open); return; }
            var slope = target as SlopeRuntime;
            if (slope != null) { slope.SetOpen(open); return; }
            var lift = target as LiftRuntime;
            if (lift != null) lift.SetOpen(open);
        }
        private void StateBadge(Component target, float width)
        {
            bool blocked = IsIntervention(target);
            bool open = IsOpen(target);
            string label = blocked ? "● DEPANNAGE" : open ? "● OUVERT" : "● FERME";
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = blocked ? new Color(.9f, .49f, .12f) : open ? new Color(.18f, .65f, .37f) : new Color(.76f, .24f, .27f);
            GUILayout.Box(label, badgeStyle, GUILayout.Width(width), GUILayout.Height(25));
            GUI.backgroundColor = previous;
        }
    }
}
