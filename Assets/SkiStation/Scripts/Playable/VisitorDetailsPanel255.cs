using UnityEngine;
using UnityEngine.EventSystems;

namespace SkiStation
{
    // Selection only: no movement, purchase, queue or simulation state is changed here.
    [DefaultExecutionOrder(-200)]
    public sealed class VisitorDetailsPanel255 : MonoBehaviour
    {
        private static VisitorDetailsPanel255 instance;
        private bool visible;
        private SkierRuntime selected;
        private SkierRuntime[] visitors = new SkierRuntime[0];
        private float refreshAt;
        private Vector2 scroll;
        private GUIStyle body, heading;
        private int consumedFrame = -1;
        public static bool IsOpen => instance != null && instance.visible;
        public static bool HasSelectedVisitor => IsOpen && instance.selected != null;
        private Rect Panel => new Rect(Screen.width - Mathf.Min(410f, Screen.width - 24f) - 12f,
            78f, Mathf.Min(410f, Screen.width - 24f), Mathf.Max(150f, Screen.height - 155f));
        public static bool BlocksWorldPointer => instance != null &&
            (instance.consumedFrame == Time.frameCount || (instance.visible && instance.Panel.Contains(
                new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y))));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Arcade.ArcadeSceneIsolation.IsActive || FindFirstObjectByType<VisitorDetailsPanel255>() != null) return;
            new GameObject("SkiStation_255_Visiteurs").AddComponent<VisitorDetailsPanel255>();
        }
        private void Awake() { instance = this; }
        private void OnDestroy() { if (instance == this) instance = null; }
        private void RefreshVisitors()
        {
            visitors = FindObjectsByType<SkierRuntime>(FindObjectsSortMode.InstanceID);
            refreshAt = Time.unscaledTime + 1f;
        }
        private void Update()
        {
            if (Arcade.ArcadeSceneIsolation.IsActive) { Destroy(gameObject); return; }
            if (Input.GetKeyDown(KeyCode.V)) { visible = !visible; RefreshVisitors(); }
            if (Input.GetKeyDown(KeyCode.F6) || Input.GetKeyDown(KeyCode.Escape)) { visible = false; selected = null; }
            if (visible && Time.unscaledTime >= refreshAt) RefreshVisitors();
            if (!Input.GetMouseButtonDown(0) || BlocksWorldPointer) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (PlayableGameHUD.PointerOverCameraControls || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) return;
            // Existing menus occupy the left, top and bottom of the management view.
            Vector3 mouse = Input.mousePosition;
            if (mouse.x < 375f || mouse.y < 100f || mouse.y > Screen.height - 75f || ConstructionActive()) return;
            RefreshVisitors();
            var hit = PickVisitor();
            if (hit != null) { selected = hit; visible = true; consumedFrame = Time.frameCount; scroll = Vector2.zero; }
            else { selected = null; visible = false; }
        }
        private static bool ConstructionActive()
        {
            if (StationVillageConstruction249.IsPlacing || StationRoadConstruction249.IsDrawing) return true;
            if (SlopeConstructionMode.Instance != null && SlopeConstructionMode.Instance.IsDrawing) return true;
            if (StationServiceConstructionTool.I != null && StationServiceConstructionTool.I.Active) return true;
            var lift = FindFirstObjectByType<LiftConstructionPlanner>();
            if (lift != null && lift.IsActive) return true;
            var zone = FindFirstObjectByType<ManagedZoneConstructionMode>();
            if (zone != null && zone.IsDrawing) return true;
            var children = FindFirstObjectByType<PiouPiouConstructionMode>();
            return children != null && children.IsDrawing;
        }
        private SkierRuntime PickVisitor()
        {
            var cam = Camera.main;
            if (cam == null) return null;
            SkierRuntime best = null;
            float bestPixels = 24f, bestDepth = float.MaxValue;
            foreach (var visitor in visitors)
            {
                if (visitor == null || !visitor.isActiveAndEnabled) continue;
                Bounds bounds = new Bounds(visitor.transform.position, Vector3.zero);
                bool rendered = false;
                foreach (var renderer in visitor.GetComponentsInChildren<Renderer>())
                {
                    if (!renderer.enabled || renderer is TrailRenderer || renderer is ParticleSystemRenderer) continue;
                    if (!rendered) bounds = renderer.bounds; else bounds.Encapsulate(renderer.bounds);
                    rendered = true;
                }
                if (!rendered) continue;
                Vector3 screen = cam.WorldToScreenPoint(bounds.center);
                if (screen.z <= cam.nearClipPlane || screen.x < 0 || screen.x > Screen.width || screen.y < 0 || screen.y > Screen.height) continue;
                float pixels = Vector2.Distance(new Vector2(screen.x, screen.y), (Vector2)Input.mousePosition);
                if (pixels > bestPixels || (Mathf.Approximately(pixels, bestPixels) && screen.z >= bestDepth)) continue;
                // Do not select a visitor hidden by terrain or a solid structure.
                Vector3 delta = bounds.center - cam.transform.position;
                if (Physics.Raycast(cam.transform.position, delta.normalized, out var block,
                    Mathf.Max(0, delta.magnitude - bounds.extents.magnitude), ~0, QueryTriggerInteraction.Ignore)
                    && block.collider.GetComponentInParent<SkierRuntime>() != visitor) continue;
                best = visitor; bestPixels = pixels; bestDepth = screen.z;
            }
            return best;
        }
        private static string StateLabel(SkierRuntime visitor)
        {
            var arrival = visitor.GetComponent<StationVisitorRoadArrival248>();
            if (arrival != null && arrival.IsArriving) return arrival.DiagnosticGoal;
            var route = visitor.GetComponent<SkierRouteAgent>();
            if (route != null && route.IsGpsWalking) return "Liaison vers la prochaine etape";
            if (route != null && route.IsApproachingLift) return "Rejoint la remontee";
            switch (visitor.State)
            {
                case SkierState.Arriving: return "Arrive dans la station";
                case SkierState.WaitingForLift: return "Attend l'embarquement";
                case SkierState.RidingLift: return "Monte en remontee";
                case SkierState.Skiing: return "Descend la piste";
                case SkierState.Leaving: return "Quitte la station";
                default: return "Au repos / choix du prochain trajet";
            }
        }
        private void Row(string label, string value) { GUILayout.Label(label + " : " + value, body); }
        private void DrawSelected()
        {
            var identity = selected.GetComponent<ArrivalIdentity250>();
            var arrival = selected.GetComponent<StationVisitorRoadArrival248>();
            var route = selected.GetComponent<SkierRouteAgent>();
            var journey = selected.GetComponent<SkierMountainJourneyRuntime>();
            GUILayout.Label(selected.name, heading);
            Row("Objectif", StateLabel(selected));
            var planner = MultiLiftDestinationPlanner301.I;
            if (planner != null && planner.Targets.TryGetValue(selected, out var destinationSlope))
                Row("Piste souhaitee", destinationSlope != null ? destinationSlope.name : "Aucune");
            if (route != null && !string.IsNullOrEmpty(route.GpsItinerary))
                Row("GPS du domaine", route.enabled ? route.GpsItinerary : "Suspendu : boucle temoin ou autre activite");
            Row("Arrivee", identity == null ? "Non renseignee" : identity.Mode == ArrivalMode250.Car ? "Voiture" : identity.Mode == ArrivalMode250.Bus ? "Autocar" : "A pied");
            Row("Materiel", arrival != null ? (arrival.HasEquipment ? "Equipe" : "Location necessaire") : identity != null ? (identity.OwnEquipment ? "Equipe" : "Sans equipement") : "Non renseigne");
            Row("Forfait du jour", VisitorSkiAdmission.HasPass(selected) ? "Valide" : "Aucun forfait valide enregistre");
            Row("Satisfaction", selected.Satisfaction.ToString("0") + " / 100");
            string destination = "Aucune destination confirmee";
            if (arrival != null && arrival.IsArriving) destination = arrival.DiagnosticDestination;
            else if (route != null && route.ApproachingLift != null) destination = route.ApproachingLift.name;
            else if (selected.State == SkierState.Skiing && route != null && route.CurrentSlope != null) destination = route.CurrentSlope.name;
            else if ((selected.State == SkierState.RidingLift || selected.State == SkierState.WaitingForLift) && journey != null && journey.CurrentLift != null) destination = journey.CurrentLift.name;
            Row("Destination", destination);
            string wait = arrival != null && arrival.IsArriving ? arrival.DiagnosticWait : "";
            if (selected.State == SkierState.WaitingForLift) wait = "En file pour embarquer";
            if (wait.Length == 0 && selected.State == SkierState.Resting && (route == null || !route.IsApproachingLift))
                wait = "Repos ou recherche d'un trajet ; cause detaillee non exposee";
            Row("Attente", wait.Length > 0 ? wait : "Aucune attente signalee");
            if (Time.timeScale == 0) GUILayout.Label("Simulation en pause", body);
        }
        private void OnGUI()
        {
            if (!visible || Arcade.ArcadeSceneIsolation.IsActive) return;
            if (body == null)
            {
                body = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
                body.normal.textColor = Color.white;
                heading = new GUIStyle(body) { fontStyle = FontStyle.Bold, fontSize = 16 };
            }
            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;
            GUILayout.BeginArea(Panel, GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("VISITEURS | V", heading);
            if (GUILayout.Button("X", GUILayout.Width(30))) { visible = false; selected = null; }
            GUILayout.EndHorizontal();
            scroll = GUILayout.BeginScrollView(scroll);
            if (selected != null) DrawSelected();
            else GUILayout.Label("Cliquez sur un visiteur ou choisissez-le ci-dessous.", body);
            GUILayout.Space(12);
            GUILayout.Label("Visiteurs presents : " + visitors.Length, heading);
            foreach (var visitor in visitors)
            {
                if (visitor == null) continue;
                if (GUILayout.Button(visitor.name + " — " + StateLabel(visitor), GUILayout.MinHeight(30))) selected = visitor;
            }
            if (visitors.Length == 0) GUILayout.Label("Aucun visiteur present pour le moment.", body);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUI.matrix = oldMatrix;
        }
    }
}
