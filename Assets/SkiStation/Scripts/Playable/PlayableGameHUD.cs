using UnityEngine;

namespace SkiStation
{
    /// <summary>Ergonomic HUD with progressive disclosure: one primary menu and one submenu at a time.</summary>
    public sealed class PlayableGameHUD : MonoBehaviour
    {
        private enum PrimaryMenu { None, Construction, Management, Domain, Help }
        private enum ConstructionMenu { None, SkiArea, Buildings, Mountain }
        private enum ManagementMenu { None, Finances, Staff, Research, SkiSchool, Calendar }
        private enum DomainMenu { None, Overview, Slopes, Lifts, Sectors }

        private PrimaryMenu primary;
        private ConstructionMenu construction;
        private ManagementMenu management;
        private DomainMenu domain;
        private SlopeConstructionMode slopeMode;
        private LiftConstructionPlanner liftPlanner;
        private GUIStyle titleStyle, sectionStyle, smallStyle, mutedStyle;
        private GUISkin stationSkin;
        private PlayableCameraController managementCamera;
        public static bool PointerOverCameraControls => new Rect(12f, 170f, Mathf.Min(560f, Screen.width - 24f), 72f)
            .Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));

        private void Start()
        {
            slopeMode = Object.FindFirstObjectByType<SlopeConstructionMode>();
            liftPlanner = Object.FindFirstObjectByType<LiftConstructionPlanner>();
            managementCamera = Object.FindFirstObjectByType<PlayableCameraController>();
            if (Object.FindFirstObjectByType<StationServiceConstructionTool>() == null)
                new GameObject("SkiStation_ServiceConstructionTool").AddComponent<StationServiceConstructionTool>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) CloseMenus();
            if (Input.GetKeyDown(KeyCode.F1)) TogglePrimary(PrimaryMenu.Help);
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetSpeed(GameSpeed.Normal);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetSpeed(GameSpeed.Fast);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetSpeed(GameSpeed.VeryFast);
            if (Input.GetKeyDown(KeyCode.Space)) GameSpeedController.I?.TogglePause();
            if (Input.GetKeyDown(KeyCode.B)) ToggleSlope();
            if (Input.GetKeyDown(KeyCode.L)) ToggleLift();
        }

        private void SetSpeed(GameSpeed speed) => GameSpeedController.I?.SetSpeed(speed);
        private void CloseMenus() { primary = PrimaryMenu.None; construction = ConstructionMenu.None; management = ManagementMenu.None; domain = DomainMenu.None; }
        private void TogglePrimary(PrimaryMenu menu) { primary = primary == menu ? PrimaryMenu.None : menu; construction = ConstructionMenu.None; management = ManagementMenu.None; domain = DomainMenu.None; }

        private void BeginServicePlacement(StationServiceType type)
        {
            if (slopeMode != null) slopeMode.SetActive(false);
            if (liftPlanner != null) liftPlanner.SetActive(false);
            StationServiceConstructionTool.I?.Configure(type, ServiceRange.Standard);
            CloseMenus();
        }

        private void ToggleSlope()
        {
            if (liftPlanner != null) liftPlanner.SetActive(false);
            if (slopeMode == null) slopeMode = Object.FindFirstObjectByType<SlopeConstructionMode>();
            if (slopeMode != null) slopeMode.SetActive(!slopeMode.IsDrawing);
            CloseMenus();
        }

        private void ToggleLift()
        {
            if (slopeMode != null) slopeMode.SetActive(false);
            if (liftPlanner == null) liftPlanner = Object.FindFirstObjectByType<LiftConstructionPlanner>();
            if (liftPlanner == null) return;
            liftPlanner.SetActive(!liftPlanner.IsActive);
            if (liftPlanner.IsActive) liftPlanner.Begin();
            CloseMenus();
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            // A local skin keeps the management HUD consistent without changing other game overlays.
            stationSkin = Object.Instantiate(GUI.skin);
            stationSkin.box = MakePanelStyle(stationSkin.box, new Color(.105f, .17f, .23f, .94f));
            stationSkin.window = MakePanelStyle(stationSkin.window, new Color(.105f, .17f, .23f, .96f));
            stationSkin.button = MakePanelStyle(stationSkin.button, new Color(.17f, .25f, .32f, .97f));
            stationSkin.button.hover.background = SolidTexture(new Color(.24f, .36f, .44f, .98f));
            stationSkin.button.active.background = SolidTexture(new Color(.12f, .42f, .53f, 1f));
            stationSkin.button.normal.textColor = Color.white;
            stationSkin.button.hover.textColor = Color.white;
            stationSkin.button.active.textColor = Color.white;
            stationSkin.label = new GUIStyle(stationSkin.label);
            stationSkin.label.normal.textColor = new Color(.94f, .97f, 1f);
            stationSkin.label.fontSize = 12;
            stationSkin.button.fontSize = 12;
            stationSkin.button.padding = new RectOffset(9, 9, 5, 5);
            titleStyle = new GUIStyle(stationSkin.label) { fontStyle = FontStyle.Bold, fontSize = 15, alignment = TextAnchor.MiddleLeft };
            sectionStyle = new GUIStyle(stationSkin.label) { fontStyle = FontStyle.Bold, fontSize = 12 };
            smallStyle = new GUIStyle(stationSkin.label) { fontSize = 11, wordWrap = true };
            mutedStyle = new GUIStyle(smallStyle); mutedStyle.normal.textColor = new Color(.72f, .81f, .87f);
        }

        private static Texture2D SolidTexture(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }

        private static GUIStyle MakePanelStyle(GUIStyle source, Color color)
        {
            var style = new GUIStyle(source);
            style.normal.background = SolidTexture(color);
            style.normal.textColor = Color.white;
            style.border = new RectOffset(0, 0, 0, 0);
            style.padding = new RectOffset(10, 10, 7, 7);
            return style;
        }

        private void OnGUI()
        {
            EnsureStyles();
            var previousSkin = GUI.skin;
            GUI.skin = stationSkin;
            try
            {
                DrawTopBar(); DrawScenarioObjective(); DrawPlayableFeedback(); DrawReadinessHint();
                DrawSafetyNet(); DrawPrimaryPanel(); DrawSelectionPanel(); DrawBottomBar(); DrawVisitorBar();
            }
            finally { GUI.skin = previousSkin; }
        }

        private void DrawVisitorBar()
        {
            float width = Mathf.Min(560f, Screen.width - 24f);
            GUILayout.BeginArea(new Rect(12f, 170f, width, 94f), GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Visiteurs presents : " + (StationVisitorDemandRuntime.I != null ? StationVisitorDemandRuntime.I.ActiveVisitors : 0));
            if (GUILayout.Button("Front de neige", GUILayout.Width(120))) managementCamera?.FocusEntrance();
            if (GUILayout.Button("Vue generale", GUILayout.Width(110))) managementCamera?.Overview();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Zoom +", GUILayout.Height(28))) managementCamera?.ZoomBy(2f);
            if (GUILayout.Button("Zoom -", GUILayout.Height(28))) managementCamera?.ZoomBy(-2f);
            if (GUILayout.Button("Piste", GUILayout.Height(28))) managementCamera?.FocusSlope();
            if (GUILayout.Button("Visiteur", GUILayout.Height(28))) managementCamera?.FocusVisitor();
            if (GUILayout.Button("Arrivees", GUILayout.Height(28))) managementCamera?.FocusArrivals();
            if (GUILayout.Button("Rochers", GUILayout.Height(28))) managementCamera?.FocusRocks();
            GUILayout.EndHorizontal();
            if (StationVisitorDemandRuntime.I != null)
                GUILayout.Label("ARRIVEES : " + StationVisitorDemandRuntime.I.ArrivalDiagnostic, smallStyle);
            GUILayout.EndArea();
        }

        private void DrawReadinessHint()
        {
            var readiness = FirstSessionReadinessRuntime.I;
            if (readiness == null) return;
            float w = Mathf.Min(Screen.width - 24f, 520f);
            GUILayout.BeginArea(new Rect(12f, 112f, w, 54f), GUI.skin.box);
            GUILayout.Label(readiness.Status + "  •  " + readiness.Hint, smallStyle);
            GUILayout.EndArea();
        }

        private void DrawSafetyNet()
        {
            var safety = FirstSessionSafetyNetRuntime.I;
            if (safety == null || !safety.NeedsAttention) return;
            float w = Mathf.Min(Screen.width - 24f, 520f);
            GUILayout.BeginArea(new Rect(12f, 247f, w, 48f), GUI.skin.box);
            GUILayout.Label("ATTENTION  •  " + safety.Message, smallStyle);
            GUILayout.EndArea();
        }

        private void DrawPlayableFeedback()
        {
            var feedback = PlayableFeedbackRuntime.I;
            if (feedback == null || !feedback.Visible) return;
            float w = Mathf.Min(Screen.width - 24f, 500f);
            float x = (Screen.width - w) * .5f;
            GUILayout.BeginArea(new Rect(x, 302f, w, 42f), GUI.skin.box);
            GUILayout.Label(feedback.Message, smallStyle);
            GUILayout.EndArea();
        }

        private void DrawSelectionPanel()
        {
            var selection = ConstructionSelectionRuntime.I;
            if (selection == null || !selection.HasSelection) return;
            float w = Mathf.Min(330f, Screen.width - 24f);
            GUILayout.BeginArea(new Rect(Screen.width - w - 12f, 66f, w, 82f), GUI.skin.box);
            GUILayout.Label(selection.SelectedLabel, sectionStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Selection", mutedStyle);
            if (GUILayout.Button("Supprimer  [Del]", GUILayout.Width(120))) selection.RemoveSelected();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawTopBar()
        {
            var station = StationRuntime.I; var clock = GameClock.I;
            float w = Mathf.Min(Screen.width - 24f, 820f);
            GUILayout.BeginArea(new Rect(12, 10, w, 46), GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("SKISTATION", titleStyle, GUILayout.Width(110));
            GUILayout.Label($"Jour {clock?.CurrentDay ?? 1}  |  {clock?.GetTimeLabel() ?? "06:00"}", GUILayout.Width(135));
            GUILayout.Label($"{(station?.State.money ?? 0f):N0} EUR", GUILayout.Width(135));
            var loop = PlayableLoopRuntime.I;
            if (loop != null) GUILayout.Label(loop.DomainReady ? "Domaine actif" : $"Pistes {loop.SlopeCount} | Remontees {loop.LiftCount}", GUILayout.Width(150));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Pause", GUILayout.Width(54))) GameSpeedController.I?.TogglePause();
            if (GUILayout.Button("1x", GUILayout.Width(38))) SetSpeed(GameSpeed.Normal);
            if (GUILayout.Button("3x", GUILayout.Width(38))) SetSpeed(GameSpeed.Fast);
            if (GUILayout.Button("8x", GUILayout.Width(38))) SetSpeed(GameSpeed.VeryFast);
            GUILayout.EndHorizontal(); GUILayout.EndArea();
        }

        private void DrawScenarioObjective()
        {
            var scenario = FirstPlayableScenarioRuntime.I;
            if (scenario == null) return;
            float w = Mathf.Min(Screen.width - 24f, 520f);
            GUILayout.BeginArea(new Rect(12, 62, w, 48), GUI.skin.box);
            GUILayout.Label((scenario.Completed ? "OBJECTIF TERMINE  •  " : "OBJECTIF  •  ") + scenario.Objective);
            GUILayout.EndArea();
        }

        private void DrawBottomBar()
        {
            const float h = 50f; float y = Screen.height - h - 10f;
            float w = Mathf.Min(Screen.width - 20f, 760f); float x = (Screen.width - w) * .5f;
            GUILayout.BeginArea(new Rect(x, y, w, h), GUI.skin.box); GUILayout.BeginHorizontal();
            PrimaryButton("Construction", PrimaryMenu.Construction);
            PrimaryButton("Gestion", PrimaryMenu.Management);
            PrimaryButton("Domaine", PrimaryMenu.Domain);
            if (GUILayout.Button("Carte  [M]", GUILayout.Height(30))) SendMapToggle();
            if (GUILayout.Button("?", GUILayout.Width(40), GUILayout.Height(30))) TogglePrimary(PrimaryMenu.Help);
            GUILayout.EndHorizontal(); GUILayout.EndArea();
        }

        private void PrimaryButton(string label, PrimaryMenu menu)
        {
            string arrow = primary == menu ? "  ^" : "  v";
            if (GUILayout.Button(label + arrow, GUILayout.Height(30))) TogglePrimary(menu);
        }

        private void DrawPrimaryPanel()
        {
            if (primary == PrimaryMenu.None) return;
            float w = Mathf.Min(560f, Screen.width - 24f);
            float h = primary == PrimaryMenu.Help ? 142f : 210f;
            float x = (Screen.width - w) * .5f; float y = Screen.height - 68f - h;
            GUILayout.BeginArea(new Rect(x, y, w, h), GUI.skin.window);
            switch (primary)
            {
                case PrimaryMenu.Construction: DrawConstruction(); break;
                case PrimaryMenu.Management: DrawManagement(); break;
                case PrimaryMenu.Domain: DrawDomain(); break;
                case PrimaryMenu.Help: DrawHelp(); break;
            }
            GUILayout.EndArea();
        }

        private bool Accordion(string label, bool open)
        {
            if (GUILayout.Button((open ? "-  " : "+  ") + label, GUILayout.Height(28))) return !open;
            return open;
        }

        private void DrawConstruction()
        {
            GUILayout.Label("CONSTRUCTION", titleStyle);
            bool open = construction == ConstructionMenu.SkiArea;
            bool next = Accordion("Domaine skiable", open); if (next != open) construction = next ? ConstructionMenu.SkiArea : ConstructionMenu.None;
            if (construction == ConstructionMenu.SkiArea)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Piste  [B]\n12 000 EUR + longueur")) ToggleSlope();
                if (GUILayout.Button("Remontées  [L]\nChoisir un modèle")) ToggleLift();
                GUILayout.EndHorizontal();
                var station = StationRuntime.I;
                if (station != null && station.State.money < 65000f)
                    GUILayout.Label("Budget serre : commencez par une piste courte ou augmentez les recettes.", mutedStyle);
            }
            open = construction == ConstructionMenu.Buildings;
            next = Accordion("Batiments et services", open); if (next != open) construction = next ? ConstructionMenu.Buildings : ConstructionMenu.None;
            if (construction == ConstructionMenu.Buildings) { GUILayout.BeginHorizontal(); if (GUILayout.Button("Restaurant")) BeginServicePlacement(StationServiceType.Restaurant); if (GUILayout.Button("Boutique")) BeginServicePlacement(StationServiceType.Shop); GUILayout.EndHorizontal(); }
            open = construction == ConstructionMenu.Mountain;
            next = Accordion("Montagne", open); if (next != open) construction = next ? ConstructionMenu.Mountain : ConstructionMenu.None;
            if (construction == ConstructionMenu.Mountain) GUILayout.Label("Amenagements du terrain et neige seront regroupes ici.", mutedStyle);
        }

        private void DrawManagement()
        {
            GUILayout.Label("GESTION", titleStyle);
            DrawFinanceRow();
            DrawManagementRow("Personnel", ManagementMenu.Staff, "Equipes, moniteurs et operations.");
            DrawManagementRow("Recherche", ManagementMenu.Research, "Technologies et paliers de progression.");
            DrawManagementRow("Ecole de ski", ManagementMenu.SkiSchool, "Cours, groupes et progression.");
            DrawManagementRow("Calendrier", ManagementMenu.Calendar, "Recherches, competitions et evenements.");
        }


        private void DrawFinanceRow()
        {
            bool open = management == ManagementMenu.Finances;
            bool next = Accordion("Finances", open);
            if (next != open) management = next ? ManagementMenu.Finances : ManagementMenu.None;
            if (management != ManagementMenu.Finances) return;

            var finance = FinanceRuntime.I;
            var station = StationRuntime.I;
            float balance = station != null ? station.State.money : 0f;
            float revenue = finance != null ? finance.TotalRevenue : 0f;
            float expenses = finance != null ? finance.TotalExpenses : 0f;
            float net = finance != null ? finance.NetCashFlow : 0f;
            GUILayout.Label($"Tresorerie  {balance:N0} EUR  |  Recettes  {revenue:N0} EUR", smallStyle);
            GUILayout.Label($"Depenses  {expenses:N0} EUR  |  Solde session  {net:N0} EUR", smallStyle);
        }
        private void DrawManagementRow(string label, ManagementMenu value, string detail)
        {
            bool open = management == value; bool next = Accordion(label, open);
            if (next != open) management = next ? value : ManagementMenu.None;
            if (management == value) GUILayout.Label(detail, mutedStyle);
        }

        private void DrawDomain()
        {
            GUILayout.Label("DOMAINE", titleStyle);
            DrawDomainRow("Vue d'ensemble", DomainMenu.Overview);
            DrawDomainRow("Pistes", DomainMenu.Slopes);
            DrawDomainRow("Remontees", DomainMenu.Lifts);
            DrawDomainRow("Secteurs", DomainMenu.Sectors);
        }

        private void DrawDomainRow(string label, DomainMenu value)
        {
            bool open = domain == value; bool next = Accordion(label, open);
            if (next != open) domain = next ? value : DomainMenu.None;
            if (domain != value) return;
            if (value == DomainMenu.Overview)
            {
                var cap = DomainCapacityRuntime.I;
                if (cap == null) GUILayout.Label("Donnees en cours d'initialisation...", mutedStyle);
                else GUILayout.Label($"Capacite {cap.Snapshot.effectiveCapacityPerHour:N0}/h  |  Charge {cap.DomainPressure01:P0}" + (cap.HasBottleneck ? $"  |  Goulot: {cap.BottleneckName}" : ""), smallStyle);
            }
            else GUILayout.Label("Les informations detaillees s'afficheront ici a la demande.", mutedStyle);
        }

        private void DrawHelp()
        {
            GUILayout.Label("COMMANDES", titleStyle);
            GUILayout.Label("Fleches / WASD : camera | Shift rapide | Q/E rotation | Molette zoom", smallStyle);
            GUILayout.Label("Origine : vue generale | Fin : front de neige", smallStyle);
            GUILayout.Label("B piste  |  L remontee  |  M carte  |  Espace pause  |  1/2/3 vitesse", smallStyle);
            GUILayout.Label("Echap ferme les menus  |  F10 affiche les panneaux techniques", smallStyle);
            GUILayout.Space(4); GUILayout.Label("Principe UI : une categorie ouverte a la fois pour garder la montagne lisible.", mutedStyle);
        }

        private void SendMapToggle()
        {
            var map = Object.FindFirstObjectByType<DomainMapUI>();
            if (map != null) map.SendMessage("Toggle", SendMessageOptions.DontRequireReceiver);
        }
    }
}
