using UnityEngine;
using UnityEngine.EventSystems;

namespace SkiStation
{
    // F6: select a structure to open its detail card. Status is aligned at the far right of the title row.
    public sealed class StationStructureStatusPanel249 : MonoBehaviour
    {
        private static StationStructureStatusPanel249 instance;
        private bool visible;
        void Awake() { instance = this; }
        void OnDestroy() { if (instance == this) instance = null; }
        public static void Hide349() { if (instance != null) { instance.visible = false; instance.selected = null; } }
        public static void Show349(Component target)
        {
            if (instance == null) Install();
            if (instance == null) return;
            instance.selected = target; instance.visible = target != null;
        }
        private Vector2 scroll;
        private Component selected;
        private GUIStyle titleStyle;
        private GUIStyle badgeStyle;
        private GUIStyle bodyStyle;
        private readonly Color panelColor = new Color(.13f, .19f, .25f, .96f);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive || instance != null) return;
            new GameObject("SkiStation_249_Statuts").AddComponent<StationStructureStatusPanel249>();
        }
        private void Update()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) { Destroy(gameObject); return; }
            if (Input.GetKeyDown(KeyCode.F6) && !ConstructionInput349.AnyToolActive) visible = !visible;
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
            if (!visible || ConstructionInput349.AnyToolActive || VisitorDetailsPanel255.IsOpen || SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;
            Styles();titleStyle.fontSize=Mathf.RoundToInt(15*Arcade.DemoSettings263.UiScale);badgeStyle.fontSize=bodyStyle.fontSize=Mathf.RoundToInt(12*Arcade.DemoSettings263.UiScale);
            float width = Mathf.Min(430f, Screen.width - 24f);
            Rect area = new Rect(Screen.width - width - 12f, 75f, width, Mathf.Min(580f, Screen.height - 90f));
            ConstructionInput349.PanelArea(this, "structure-card", area);
            Color old = GUI.backgroundColor;
            GUI.backgroundColor = panelColor;
            GUILayout.BeginArea(area, GUI.skin.box);
            GUI.backgroundColor = old;
            GUILayout.BeginHorizontal();
            GUILayout.Label("STRUCTURES  |  F6", titleStyle);
            if (GUILayout.Button("X", GUILayout.Width(28))) { ConstructionSelectionRuntime.I?.ClearSelection349(); ConstructionInput349.ConsumeClick(); }
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
            if (GUILayout.Button(label, GUILayout.ExpandWidth(true))) { ConstructionSelectionRuntime.I?.Select349(target); ConstructionInput349.ConsumeClick(); }
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
            var service = target.GetComponent<StationServiceRuntime>();
            if (service != null && service.definition != null)
            {
                var d = service.definition; var stats = service.Stats;
                GUILayout.Label("Capacite : " + service.ActiveCustomers + "/" + d.capacity + "  |  File : " + service.QueueCount + "/" + d.maxQueue, bodyStyle);
                GUILayout.Label("Personnel : " + d.staffAvailable + "/" + d.staffRequired + "  |  Prix : " + d.basePrice.ToString("0.##") + " EUR", bodyStyle);
                GUILayout.Label("Aujourd'hui : " + stats.served + " servis  |  " + stats.refused + " refus  |  " + stats.revenue.ToString("0") + " EUR", bodyStyle);
                if (service.IsSuspended348) GUILayout.Label("Service suspendu : clients et cours restent sur place, les durees sont gelees.", bodyStyle);
            }
            var school = target.GetComponent<SkiSchoolRuntime>();
            if (school != null)
            {
                var snapshot = school.GetSnapshot();
                GUILayout.Label("Cours actifs : " + snapshot.activeLessons + "  |  Eleves : " + snapshot.activeStudents + "  |  Moniteurs : " + school.instructors, bodyStyle);
            }
            if (target is StationStructureStatus249)
            {
                bool linked = StationPedestrianNetwork249.I != null && StationPedestrianNetwork249.I.TryGetAccess(target.gameObject, out _);
                GUILayout.Label("Acces pieton : " + (linked ? "relie et balise" : "non relie"), bodyStyle);
            }
            bool blocked = IsIntervention(target);
            if (service != null && service.UsesAlliedAccount)
            {
                GUILayout.Label("Entreprise alliée : " + service.AlpineCompanyId, bodyStyle);
                GUILayout.EndVertical();
                return;
            }
            if (blocked) GUILayout.Label("Maintenance en cours : fonctionnement suspendu automatiquement.", bodyStyle);
            else GUILayout.Label("Cliquez sur le statut pour ouvrir ou fermer.", bodyStyle);
            if (GUILayout.Button("Supprimer…")) { ConstructionSelectionRuntime.I?.Select349(target); ConstructionSelectionRuntime.I?.RemoveSelected(); }
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
            if (IsIntervention(target) || target.GetComponent<StationServiceRuntime>()?.UsesAlliedAccount == true) return;
            var structure = target as StationStructureStatus249;
            if (structure != null) { structure.SetOpen(open); return; }
            var slope = target as SlopeRuntime;
            if (slope != null) { OperationalClosureRegistry381.I?.SetSlopeClosure(slope, OperationalClosureSource381.Manual, !open, "Fermeture manuelle"); return; }
            var lift = target as LiftRuntime;
            if (lift != null) OperationalClosureRegistry381.I?.SetLiftClosure(lift, OperationalClosureSource381.Manual, !open, "Fermeture manuelle");
        }
        private void StateBadge(Component target, float width)
        {
            bool blocked = IsIntervention(target);
            bool open = IsOpen(target);
            bool structureMaintenance = target is StationStructureStatus249;
            string label = blocked ? (structureMaintenance ? "● MAINTENANCE" : "● DEPANNAGE") : open ? "● OUVERT" : "● FERME";
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = blocked ? new Color(.9f, .49f, .12f) : open ? new Color(.18f, .65f, .37f) : new Color(.76f, .24f, .27f);
            bool enabled = GUI.enabled; GUI.enabled = enabled && !blocked && target.GetComponent<StationServiceRuntime>()?.UsesAlliedAccount != true;
            if (GUILayout.Button(label, badgeStyle, GUILayout.Width(width), GUILayout.Height(25)))
            { SetOpen(target, !open); ConstructionInput349.ConsumeClick(); }
            GUI.enabled = enabled;
            GUI.backgroundColor = previous;
        }
    }
}
