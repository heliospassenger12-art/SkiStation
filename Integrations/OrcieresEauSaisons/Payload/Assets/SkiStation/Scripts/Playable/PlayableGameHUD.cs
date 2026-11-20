using UnityEngine;

namespace SkiStation
{
    /// <summary>Ergonomic HUD with progressive disclosure: one primary menu and one submenu at a time.</summary>
    public sealed class PlayableGameHUD : MonoBehaviour
    {
        private enum PrimaryMenu { None, Construction, Management, Domain, Help, Visitors, Career, Weather, Journal }
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
        private GUISkin stationSkin;private int comfortRevision=-1;
        private static readonly Color32 Ink = new Color32(238, 243, 249, 255);
        private static readonly Color32 Muted = new Color32(159, 176, 195, 255);
        private static readonly Color32 Amber = new Color32(255, 178, 56, 255);
        private static readonly Color32 Panel = new Color32(16, 24, 36, 238);
        private static readonly string[] ToolbarLabels =
            { "Construire", "Domaine", "Finances", "Visiteurs", "Recherche", "Carrière", "Météo", "Journal", "Options" };
        private GUIStyle toolbarStyle, selectedToolbarStyle, selectedSpeedStyle, metricStyle, metricCaptionStyle;
        private Texture2D toolbarIcons;
        private Vector2 panelScroll;
        private bool closePanelRequested;
        private const float ToolbarHeight = 64f;
        private PlayableCameraController managementCamera;
        public static bool PointerOverCameraControls => ConstructionInput349.PointerBlocked;
        private void BeginArea349(Rect rect, GUIStyle style)
        { ConstructionInput349.PanelArea(this, "hud-" + rect.x + "-" + rect.y, rect); GUILayout.BeginArea(rect, style); }

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
            // The live Arcade session owns gameplay input and its own pause surface.
            if(Arcade.ArcadeSkiSession.ActiveSession!=null&&Arcade.ArcadeSkiSession.ActiveSession.IsPlaying)return;
            if(DemoModalSurface263.BlocksLowerUi||Arcade.RiderHub262.IsOpen||SkiStationStartMenu249.HomeOpen263||DemoManagementMenu263.Paused263||Arcade.DemoRuntime263.OptionsOpen)return;
            if (Input.GetKeyDown(KeyCode.Escape)) CloseMenus();
            if (Input.GetKeyDown(KeyCode.F1)) TogglePrimary(PrimaryMenu.Help);
            if (!StationVillageConstruction249.IsPlacing && Input.GetKeyDown(KeyCode.Alpha1)) SetSpeed(GameSpeed.Normal);
            if (!StationVillageConstruction249.IsPlacing && Input.GetKeyDown(KeyCode.Alpha2)) SetSpeed(GameSpeed.Fast);
            if (!StationVillageConstruction249.IsPlacing && Input.GetKeyDown(KeyCode.Alpha3)) SetSpeed(GameSpeed.VeryFast);
            if (Input.GetKeyDown(KeyCode.Space)) GameSpeedController.I?.TogglePause();
            if (SlopeConditionUI.IsOpen) return;
            if (Input.GetKeyDown(KeyCode.B)) ToggleSlope();
            if (Input.GetKeyDown(KeyCode.L)) ToggleLift();
        }

        private void SetSpeed(GameSpeed speed) => GameSpeedController.I?.SetSpeed(speed);
        private void CloseMenus() { primary = PrimaryMenu.None; construction = ConstructionMenu.None; management = ManagementMenu.None; domain = DomainMenu.None; panelScroll = Vector2.zero; }
        private void TogglePrimary(PrimaryMenu menu)
        {
            var next = primary == menu ? PrimaryMenu.None : menu;
            CloseMenus();
            primary = next;
        }

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
            if(titleStyle!=null){if(comfortRevision!=Arcade.DemoSettings263.Revision)ApplyComfortFonts263();return;}
            // A local skin keeps the management HUD consistent without changing other game overlays.
            stationSkin = Object.Instantiate(GUI.skin);
            stationSkin.box = MakePanelStyle(stationSkin.box, Panel);
            stationSkin.window = MakePanelStyle(stationSkin.window, Panel);
            stationSkin.button = MakePanelStyle(stationSkin.button, new Color32(30, 42, 60, 246));
            stationSkin.button.hover.background = RoundedTexture(new Color32(48, 64, 86, 255));
            stationSkin.button.active.background = RoundedTexture(new Color32(64, 77, 96, 255));
            stationSkin.button.normal.textColor = Ink;
            stationSkin.button.hover.textColor = Ink;
            stationSkin.button.active.textColor = Ink;
            stationSkin.label = new GUIStyle(stationSkin.label);
            stationSkin.label.normal.textColor = Ink;
            stationSkin.label.fontSize = 12;
            stationSkin.button.fontSize = 12;
            stationSkin.button.padding = new RectOffset(9, 9, 5, 5);
            titleStyle = new GUIStyle(stationSkin.label) { fontStyle = FontStyle.Bold, fontSize = 15, alignment = TextAnchor.MiddleLeft };
            sectionStyle = new GUIStyle(stationSkin.label) { fontStyle = FontStyle.Bold, fontSize = 12 };
            smallStyle = new GUIStyle(stationSkin.label) { fontSize = 11, wordWrap = true };
            mutedStyle = new GUIStyle(smallStyle); mutedStyle.normal.textColor = Muted;
            toolbarIcons = Resources.Load<Texture2D>("ManagementToolbarClaude") ?? Resources.Load<Texture2D>("ManagementToolbarIcons");
            toolbarStyle = new GUIStyle(stationSkin.button) { alignment = TextAnchor.LowerCenter, padding = new RectOffset(1, 1, 0, 4) };
            toolbarStyle.normal.background = null;
            selectedToolbarStyle = new GUIStyle(toolbarStyle);
            selectedToolbarStyle.normal.background = RoundedTexture(new Color32(70, 54, 32, 255));
            selectedToolbarStyle.normal.textColor = Amber;
            selectedSpeedStyle = new GUIStyle(stationSkin.button);
            selectedSpeedStyle.normal.background = RoundedTexture(Amber);
            selectedSpeedStyle.normal.textColor = Panel;
            metricStyle = new GUIStyle(titleStyle) { clipping = TextClipping.Clip, wordWrap = false };
            metricCaptionStyle = new GUIStyle(mutedStyle) { wordWrap = false, clipping = TextClipping.Clip };
            ApplyComfortFonts263();
        }

        private void ApplyComfortFonts263()
        {
            float scale = Arcade.DemoSettings263.UiScale;
            stationSkin.label.fontSize = stationSkin.button.fontSize = selectedSpeedStyle.fontSize = Mathf.RoundToInt(12 * scale);
            titleStyle.fontSize = metricStyle.fontSize = Mathf.RoundToInt(15 * scale);
            sectionStyle.fontSize = Mathf.RoundToInt(12 * scale);
            smallStyle.fontSize = mutedStyle.fontSize = metricCaptionStyle.fontSize = Mathf.RoundToInt(11 * scale);
            comfortRevision = Arcade.DemoSettings263.Revision;
        }

        private readonly System.Collections.Generic.List<Texture2D> skinTextures = new();
        private Texture2D RoundedTexture(Color color)
        {
            const int size = 20, radius = 8;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0f, radius - Mathf.Min(x + .5f, size - x - .5f));
                    float dy = Mathf.Max(0f, radius - Mathf.Min(y + .5f, size - y - .5f));
                    pixels[y * size + x] = new Color(color.r, color.g, color.b,
                        color.a * Mathf.Clamp01(radius + .5f - Mathf.Sqrt(dx * dx + dy * dy)));
                }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            skinTextures.Add(texture);
            return texture;
        }

        private void OnDestroy()
        {
            foreach (var texture in skinTextures) Destroy(texture);
            if (stationSkin != null) Destroy(stationSkin);
        }

        private GUIStyle MakePanelStyle(GUIStyle source, Color color)
        {
            var style = new GUIStyle(source);
            style.normal.background = RoundedTexture(color);
            style.normal.textColor = Ink;
            style.border = new RectOffset(9, 9, 9, 9);
            style.padding = new RectOffset(10, 10, 7, 7);
            return style;
        }

        private void OnGUI()
        {
            if(SlopeConditionUI.IsOpen)return;
            // The live Arcade session owns gameplay input and its own pause surface.
            if(Arcade.ArcadeSkiSession.ActiveSession!=null&&Arcade.ArcadeSkiSession.ActiveSession.IsPlaying)return;
            if(DemoModalSurface263.BlocksLowerUi||Arcade.RiderHub262.IsOpen||SkiStationStartMenu249.HomeOpen263||DemoManagementMenu263.Paused263||Arcade.DemoRuntime263.OptionsOpen)return;
            EnsureStyles();
            var previousSkin = GUI.skin;
            GUI.skin = stationSkin;
            try
            {
                DrawTopBar(); DrawDayEndSummary360(); DrawCareerCelebration359(); DrawPlayableFeedback();
                if (!ConstructionInput349.AnyToolActive && !ManagementOnboarding263.Installed && Arcade.DemoSettings263.Data.hints != Arcade.DemoHints263.Off)
                { DrawScenarioObjective(); DrawLoopDiagnostic351(); DrawSafetyNet(); }
                DrawPrimaryPanel(); DrawBottomBar();
            }
            finally { GUI.skin = previousSkin; }
        }

        private void DrawVisitorBar()
        {
            GUILayout.Label("VISITEURS", titleStyle);
            var demand = StationVisitorDemandRuntime.I;
            GUILayout.Label(demand == null ? "Arrivées en cours d’initialisation…" :
                $"{demand.ActiveVisitors} sur le domaine · {demand.VisitorsOnRoad} en véhicule", sectionStyle);
            if (demand != null) GUILayout.Label(demand.ArrivalDiagnostic, smallStyle);
            if (ValAlpin.ValAlpinScene.IsActive)
                GUILayout.Label(StationRoadNetwork247.I?.FrontFootwayEnd != null
                    ? "Accès piéton au front de neige ouvert."
                    : "Accès piéton au front de neige indisponible.", mutedStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Front de neige")) managementCamera?.FocusEntrance();
            if (GUILayout.Button("Vue générale")) managementCamera?.Overview();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Zoom +", GUILayout.Height(28))) managementCamera?.ZoomBy(2f);
            if (GUILayout.Button("Zoom -", GUILayout.Height(28))) managementCamera?.ZoomBy(-2f);
            if (GUILayout.Button("Piste", GUILayout.Height(28))) managementCamera?.FocusSlope();
            if (GUILayout.Button("Visiteur", GUILayout.Height(28))) managementCamera?.FocusVisitor();
            if (GUILayout.Button("Arrivees", GUILayout.Height(28))) managementCamera?.FocusArrivals();
            if (GUILayout.Button("Rochers", GUILayout.Height(28))) managementCamera?.FocusRocks();
            GUILayout.EndHorizontal();
        }

        private void DrawReadinessHint()
        {
            var readiness = FirstSessionReadinessRuntime.I;
            if (readiness == null) return;
            float w = Mathf.Min(Screen.width - 24f, 520f);
            BeginArea349(new Rect(12f, 112f, w, 54f), GUI.skin.box);
            GUILayout.Label(readiness.Status + "  •  " + readiness.Hint, smallStyle);
            GUILayout.EndArea();
        }

        private void DrawLoopDiagnostic351()
        {
            var diag = FirstPlayableLoopDiagnostics351.I;
            if (diag == null) { DrawReadinessHint(); return; }
            float w = Mathf.Min(Screen.width - 24f, 560f);
            BeginArea349(new Rect(12f, 112f, w, 78f), GUI.skin.box);
            GUILayout.Label((diag.Blocking ? "BLOQUE  •  " : "ETAT  •  ") + diag.Status, sectionStyle);
            GUILayout.Label(diag.Detail, smallStyle);
            GUILayout.Label("→ " + diag.Action, mutedStyle);
            GUILayout.EndArea();
        }

        private void DrawSafetyNet()
        {
            var safety = FirstSessionSafetyNetRuntime.I;
            if (safety == null || !safety.NeedsAttention) return;
            float w = Mathf.Min(Screen.width - 24f, 520f);
            BeginArea349(new Rect(12f, 292f, w, 48f), GUI.skin.box);
            GUILayout.Label("ATTENTION  •  " + safety.Message, smallStyle);
            GUILayout.EndArea();
        }

        private void DrawDayEndSummary360()
        {
            var runtime = DayEndSummaryRuntime360.I;
            if (runtime == null || !runtime.Visible) return;

            var s = runtime.LastSummary;
            float w = Mathf.Min(520f, Screen.width - 24f);
            float h = 286f;
            float x = Mathf.Max(12f, (Screen.width - w) * .5f);
            float y = Mathf.Max(68f, Mathf.Min(132f, Screen.height - h - 12f));
            BeginArea349(new Rect(x, y, w, h), GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"BILAN DU JOUR {s.day}", titleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("×", GUILayout.Width(28f))) runtime.Dismiss();
            GUILayout.EndHorizontal();

            GUILayout.Label(s.verdict, sectionStyle);
            GUILayout.Space(3f);
            GUILayout.Label($"Visiteurs  {s.visitors:N0}   |   Satisfaction  {(s.visitors > 0 ? s.satisfaction.ToString("0") + "/100" : "—")}", smallStyle);
            GUILayout.Label($"CA  {s.revenue:N0} EUR   |   Dépenses  {s.expenses:N0} EUR", smallStyle);
            GUILayout.Label($"Résultat  {s.profit:+N0;-N0;0} EUR   |   Prestige  {s.prestigeEnd:0}/100 ({s.prestigeDelta:+0.0;-0.0;0.0})", smallStyle);
            GUILayout.Label($"Sécurité  {s.incidents} incident(s)   |   Attente max  {Mathf.Max(s.worstLiftWaitMinutes, s.worstServiceWaitMinutes):0} min", mutedStyle);
            GUILayout.Space(6f);
            GUILayout.Label("✓ Meilleur point", smallStyle);
            GUILayout.Label(s.bestPoint, mutedStyle);
            GUILayout.Label("△ Point à améliorer", smallStyle);
            GUILayout.Label(s.worstPoint, mutedStyle);
            GUILayout.Space(5f);
            GUILayout.Label("Le bilan reste consultable dans Gestion → Finances.", mutedStyle);
            GUILayout.EndArea();
        }

        private void DrawCareerCelebration359()
        {
            var celebrations = CareerCelebrationRuntime359.I;
            if (celebrations == null || !celebrations.Visible) return;

            var card = celebrations.Active;
            float w = Mathf.Min(430f, Screen.width - 24f);
            float x = Mathf.Max(12f, Screen.width - w - 18f);
            float y = 66f;
            BeginArea349(new Rect(x, y, w, 112f), GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label(card.title, titleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("×", GUILayout.Width(28f))) celebrations.Dismiss();
            GUILayout.EndHorizontal();
            GUILayout.Label(card.message, smallStyle);
            if (!string.IsNullOrEmpty(card.footer)) GUILayout.Label(card.footer, mutedStyle);
            float pct = celebrations.Remaining01;
            Rect progress = GUILayoutUtility.GetRect(10f, 4f, GUILayout.ExpandWidth(true));
            GUI.Box(progress, GUIContent.none);
            Rect fill = new Rect(progress.x, progress.y, progress.width * pct, progress.height);
            GUI.DrawTexture(fill, Texture2D.whiteTexture);
            GUILayout.EndArea();
        }

        private void DrawPlayableFeedback()
        {
            var feedback = PlayableFeedbackRuntime.I;
            if (feedback == null || !feedback.Visible) return;
            float w = Mathf.Min(Screen.width - 24f, 500f);
            float x = (Screen.width - w) * .5f;
            BeginArea349(new Rect(x, 346f, w, 42f), GUI.skin.box);
            GUILayout.Label(feedback.Message, smallStyle);
            GUILayout.EndArea();
        }

        private void DrawTopBar()
        {
            var rect = new Rect(8, 6, Screen.width - 16, 48);
            ConstructionInput349.PanelArea(this, "management-top", rect);
            GUI.Box(rect, GUIContent.none, stationSkin.box);
            var station = StationRuntime.I;
            var clock = GameClock.I;
            var demand = StationVisitorDemandRuntime.I;
            float x = rect.x + 12f;
            float statsWidth = rect.width - 178f;
            int count = statsWidth >= 730f ? 7 : statsWidth >= 480f ? 5 : 3;
            float width = Mathf.Max(35f, Mathf.Min(128f, statsWidth / count));
            if (count >= 5)
            {
                string mapName = ValAlpin.ValAlpinScene.IsActive ? ValAlpin.ValAlpinScene.MapName : "SkiStation";
                if (ValAlpin.ValAlpinScene.IsRealRelief)
                {
                    mapName = ValAlpin.ValAlpinScene.FindMap(ValAlpin.ValAlpinScene.ActiveMapId)?.name ?? mapName;
                    if (mapName == "Orcières Merlette" && metricStyle.CalcSize(new GUIContent(mapName)).x > width - 5f)
                        mapName = "Orcières";
                }
                DrawMetric(x, width, mapName, "Gestion", Amber);
                x += width;
            }
            DrawMetric(x, width, clock?.GetTimeLabel() ?? "—", $"Jour {clock?.CurrentDay ?? 1}", Ink);
            x += width;
            DrawMetric(x, width, EconomyModeRuntime.I?.UnlimitedFunds == true ? "∞" : $"{(station?.State.money ?? 0f):N0} €", "Trésorerie", Ink);
            x += width;
            DrawMetric(x, width, (demand?.ActiveVisitors ?? 0).ToString(),
                demand != null && demand.VisitorsOnRoad > 0 ? $"{demand.VisitorsOnRoad} en route" : "Visiteurs", Ink);
            x += width;
            if (count >= 5)
            {
                var weather = SeasonWeatherRuntime363.I;
                DrawMetric(x, width, weather == null ? "—" : $"{weather.Today.temperatureC:0} °C", "Météo", Ink);
                x += width;
            }
            if (count == 7)
            {
                var statistics = StationStatisticsRuntime.I;
                DrawMetric(x, width, statistics != null && statistics.TotalVisitors > 0 ? $"{statistics.AverageSatisfaction:0} %" : "—", "Satisfaction", Ink);
                DrawMetric(x + width, width, $"{CareerPrestigeRuntime357.I?.Prestige ?? 0f:0} / 100", "Prestige", Amber);
            }
            float speedX = rect.xMax - 157f;
            DrawSpeed(speedX, "Ⅱ", GameSpeed.Paused);
            DrawSpeed(speedX + 38f, "×1", GameSpeed.Normal);
            DrawSpeed(speedX + 76f, "×3", GameSpeed.Fast);
            DrawSpeed(speedX + 114f, "×8", GameSpeed.VeryFast);
        }

        private void DrawMetric(float x, float width, string value, string caption, Color color)
        {
            metricStyle.normal.textColor = color;
            GUI.Label(new Rect(x, 10, width - 5, 23), value, metricStyle);
            GUI.Label(new Rect(x, 33, width - 5, 16), caption, metricCaptionStyle);
        }

        private void DrawSpeed(float x, string label, GameSpeed mode)
        {
            var controller = GameSpeedController.I;
            if (!GUI.Button(new Rect(x, 14, 34, 32), label, controller?.Current == mode ? selectedSpeedStyle : stationSkin.button)) return;
            if (mode == GameSpeed.Paused) controller?.TogglePause();
            else SetSpeed(mode);
        }

        private void DrawScenarioObjective()
        {
            var scenario = FirstPlayableScenarioRuntime.I;
            if (scenario == null) return;
            float w = Mathf.Min(Screen.width - 24f, 520f);
            BeginArea349(new Rect(12, 62, w, 48), GUI.skin.box);
            GUILayout.Label((scenario.Completed ? "OBJECTIF TERMINE  •  " : "OBJECTIF  •  ") + scenario.Objective);
            GUILayout.EndArea();
        }

        private void DrawBottomBar()
        {
            var bar = new Rect(0, Screen.height - ToolbarHeight, Screen.width, ToolbarHeight);
            ConstructionInput349.PanelArea(this, "management-toolbar", bar);
            GUI.Box(bar, GUIContent.none, stationSkin.box);
            float width = Mathf.Min(86f * Arcade.DemoSettings263.UiScale, (Screen.width - 12f) / ToolbarLabels.Length);
            float x = (Screen.width - width * ToolbarLabels.Length) * .5f;
            int fontSize = Mathf.Clamp(Mathf.FloorToInt(width / 6.2f), 8, Mathf.RoundToInt(12 * Arcade.DemoSettings263.UiScale));
            toolbarStyle.fontSize = selectedToolbarStyle.fontSize = fontSize;
            for (int i = 0; i < ToolbarLabels.Length; i++)
            {
                var rect = new Rect(x + i * width + 2, bar.y + 4, width - 4, ToolbarHeight - 8);
                if (GUI.Button(rect, ToolbarLabels[i], ToolbarSelected(i) ? selectedToolbarStyle : toolbarStyle))
                    ActivateToolbar(i);
                if (toolbarIcons != null && Event.current.type == EventType.Repaint)
                {
                    float size = Mathf.Min(29f, rect.width - 4f);
                    var tint = GUI.color;
                    GUI.color = Color.white;
                    GUI.DrawTextureWithTexCoords(new Rect(rect.center.x - size * .5f, rect.y + 4f, size, size),
                        toolbarIcons, new Rect(i / (float)ToolbarLabels.Length, 0, 1f / ToolbarLabels.Length, 1f));
                    GUI.color = tint;
                }
            }
        }

        private bool ToolbarSelected(int index) => index switch
        {
            0 => primary == PrimaryMenu.Construction,
            1 => primary == PrimaryMenu.Domain,
            2 => primary == PrimaryMenu.Management && management != ManagementMenu.Research,
            3 => primary == PrimaryMenu.Visitors,
            4 => primary == PrimaryMenu.Management && management == ManagementMenu.Research,
            5 => primary == PrimaryMenu.Career,
            6 => primary == PrimaryMenu.Weather,
            7 => primary == PrimaryMenu.Journal,
            _ => false
        };

        private void ActivateToolbar(int index)
        {
            bool wasSelected = ToolbarSelected(index);
            CloseMenus();
            if (wasSelected) return;
            switch (index)
            {
                case 0: primary = PrimaryMenu.Construction; construction = ConstructionMenu.SkiArea; break;
                case 1: primary = PrimaryMenu.Domain; domain = DomainMenu.Overview; break;
                case 2: primary = PrimaryMenu.Management; management = ManagementMenu.Finances; break;
                case 3: primary = PrimaryMenu.Visitors; break;
                case 4: primary = PrimaryMenu.Management; management = ManagementMenu.Research; break;
                case 5: primary = PrimaryMenu.Career; break;
                case 6: primary = PrimaryMenu.Weather; break;
                case 7: primary = PrimaryMenu.Journal; break;
                case 8: Arcade.DemoRuntime263.OpenOptions(); break;
            }
        }

        private string PrimaryPanelTitle() => primary switch
        {
            PrimaryMenu.Construction => "Construire",
            PrimaryMenu.Management => management == ManagementMenu.Research ? "Recherche" : "Finances",
            PrimaryMenu.Domain => "Domaine skiable",
            PrimaryMenu.Visitors => "Visiteurs",
            PrimaryMenu.Career => "Carrière et événements",
            PrimaryMenu.Weather => "Météo et neige",
            PrimaryMenu.Journal => "Journal de la station",
            _ => "Aide"
        };

        private void DrawPrimaryPanel()
        {
            if (primary == PrimaryMenu.None) return;
            float w = Mathf.Min(400f, Screen.width - 16f);
            float y = 104f;
            float h = Mathf.Max(124f, Screen.height - ToolbarHeight - y - 10f);
            BeginArea349(new Rect(Screen.width - w - 8f, y, w, h), GUI.skin.window);
            GUILayout.BeginHorizontal();
            GUILayout.Label(PrimaryPanelTitle(), titleStyle);
            if (GUILayout.Button("✕", GUILayout.Width(32), GUILayout.Height(27)))
            {
                // Keep the same GUILayout structure for the rest of this event.
                closePanelRequested = true;
            }
            GUILayout.EndHorizontal();
            panelScroll = GUILayout.BeginScrollView(panelScroll);
            switch (primary)
            {
                case PrimaryMenu.Construction: DrawConstruction(); break;
                case PrimaryMenu.Management: DrawManagement(); break;
                case PrimaryMenu.Domain: DrawDomain(); break;
                case PrimaryMenu.Help: DrawHelp(); break;
                case PrimaryMenu.Visitors: DrawVisitorBar(); break;
                case PrimaryMenu.Career: DrawCareerPanel(); break;
                case PrimaryMenu.Weather: DrawWeatherPanel(); break;
                case PrimaryMenu.Journal: DrawJournal(); break;
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            if (closePanelRequested)
            {
                closePanelRequested = false;
                primary = PrimaryMenu.None;
                panelScroll = Vector2.zero;
            }
        }

        private void DrawCareerPanel()
        {
            var prestige = CareerPrestigeRuntime357.I;
            if (prestige != null)
            {
                GUILayout.Label($"{prestige.LevelName} · Prestige {prestige.Prestige:0} / 100", sectionStyle);
                GUILayout.Label(prestige.NextMilestone, mutedStyle);
            }
            var objectives = CareerObjectivesRuntime358.I;
            if (objectives == null) return;
            GUILayout.Space(12);
            GUILayout.Label(objectives.CurrentTitle, sectionStyle);
            GUILayout.Label(objectives.CurrentDescription, smallStyle);
            GUILayout.Label(objectives.ProgressLabel, mutedStyle);
            if (objectives.CurrentReward > 0f) GUILayout.Label($"Récompense · {objectives.CurrentReward:N0} €", mutedStyle);
            GUILayout.Space(10);
            for (int i = 0; i < objectives.ObjectiveCount; i++)
            {
                bool completed = objectives.AllCompleted || i + 1 < objectives.ObjectiveNumber;
                bool active = !completed && i + 1 == objectives.ObjectiveNumber;
                string status = completed ? "Validé" : active ? "En cours" : "À venir";
                GUILayout.Label($"{i + 1}. {objectives.ObjectiveTitle(i)} · {status}", active ? sectionStyle : mutedStyle);
            }
        }

        private void DrawWeatherPanel()
        {
            GUILayout.Label("MÉTÉO", titleStyle);
            var weather = SeasonWeatherRuntime363.I;
            if (weather != null)
            {
                foreach (var day in weather.GetUpcoming(3))
                {
                    GUILayout.Label($"Jour {day.day} · {day.weatherLabel} · {day.temperatureC:0} °C", sectionStyle);
                    GUILayout.Label($"Vent {day.windKmh:0} km/h · Visibilité {day.visibility01:P0} · {day.snowLabel}", smallStyle);
                }
            }
            if (GUILayout.Button("Neige et damage [F5]")) SlopeConditionUI.Open();
        }

        private void DrawJournal()
        {
            GUILayout.Label("JOURNAL DE LA STATION", titleStyle);
            var loop = PlayableLoopRuntime.I;
            if (loop != null) GUILayout.Label(loop.LastMessage, smallStyle);
            var operations = StationOperationsRuntime390.I;
            if (operations == null) return;
            var events = operations.State?.current?.events;
            if (events != null)
                for (int i = events.Count - 1; i >= Mathf.Max(0, events.Count - 6); i--)
                {
                    var entry = events[i];
                    GUILayout.Label($"{entry.hour:00.0} h · {entry.label}", sectionStyle);
                    if (!string.IsNullOrEmpty(entry.reason)) GUILayout.Label(entry.reason, mutedStyle);
                }
            if (GUILayout.Button("Tableau des opérations [F11]"))
            {
                operations.Visible = true;
                CloseMenus();
            }
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
            if (construction == ConstructionMenu.Buildings)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Restaurant")) BeginServicePlacement(StationServiceType.Restaurant);
                if (GUILayout.Button("Boutique / location")) BeginServicePlacement(StationServiceType.Shop);
                if (GUILayout.Button("Ecole de ski")) BeginServicePlacement(StationServiceType.SkiSchool);
                GUILayout.EndHorizontal();
                if (GUILayout.Button("Chalets, hotels et commerces  [H]")) { StationVillageConstruction249.Begin349(); CloseMenus(); }
            }
            open = construction == ConstructionMenu.Mountain;
            next = Accordion("Montagne", open); if (next != open) construction = next ? ConstructionMenu.Mountain : ConstructionMenu.None;
            if (construction == ConstructionMenu.Mountain)
            {
                var water=TerrainGeneration.RealTerrainWater.Current;
                var crossing=FindFirstObjectByType<TerrainGeneration.WaterCrossingTool>();
                if(water!=null&&crossing!=null)
                {
                    if (GUILayout.Button("Pont de piste · 18 m")) { crossing.Begin(water,false); CloseMenus(); }
                    if (GUILayout.Button("Passerelle VTT · 4 m")) { crossing.Begin(water,true); CloseMenus(); }
                }
                else GUILayout.Label("Aménagements du terrain et neige.", mutedStyle);
            }
        }

        private void DrawManagement()
        {
            GUILayout.Label("GESTION", titleStyle);
            DrawFinanceRow();
            DrawManagementRow("Personnel", ManagementMenu.Staff, "Equipes, moniteurs et operations.");
            DrawManagementRow("Recherche", ManagementMenu.Research, "Technologies et paliers de progression.");
            DrawManagementRow("Ecole de ski", ManagementMenu.SkiSchool, "Cours, groupes et progression.");
            DrawCalendarRow361();
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
            var economy = EconomyBalanceRuntime.I;
            if (economy != null)
            {
                int today = station != null ? station.State.day : 1;
                float todayRevenue = finance != null ? finance.GetRevenueForDay(today) : 0f;
                float todayExpenses = finance != null ? finance.GetExpensesForDay(today) : 0f;
                GUILayout.Label($"Aujourd'hui  +{todayRevenue:N0} / -{todayExpenses:N0} EUR  |  {economy.ProfitabilityLabel}", smallStyle);
                GUILayout.Label($"Exploitation estimee  {economy.EstimatedDailyOperatingCost:N0} EUR/j  |  Reserve conseillee  {economy.SafetyReserve:N0} EUR", mutedStyle);
                GUILayout.Label($"Forfait / demande  x{economy.TicketPriceRatio:0.00} / x{economy.DemandMultiplier:0.00}", mutedStyle);
                var impact = StationInvestmentImpactRuntime.I;
                if (impact != null)
                {
                    GUILayout.Label($"Investissements  {impact.DevelopmentLabel}  |  attractivite x{impact.DemandMultiplier:0.00}", smallStyle);
                    GUILayout.Label($"Debit remontees {impact.TotalLiftCapacityPerHour:N0}/h  |  services {impact.TotalServiceCapacity} places  |  confort {impact.SatisfactionComfort01:P0}", mutedStyle);
                    GUILayout.Label(impact.BottleneckLabel, mutedStyle);
                }
                var queueEconomy = StationQueueEconomyRuntime.I;
                if (queueEconomy != null)
                {
                    GUILayout.Label($"Files  remontees {queueEconomy.LiftQueueVisitors} | services {queueEconomy.ServiceQueueVisitors} | confort {queueEconomy.QueueComfort01:P0}", smallStyle);
                    GUILayout.Label($"Attente max  remontee ~{queueEconomy.WorstLiftWaitMinutes:0} min | service ~{queueEconomy.WorstServiceWaitMinutes:0} min", mutedStyle);
                    GUILayout.Label("Point sensible : " + queueEconomy.WorstQueueLabel, mutedStyle);
                    GUILayout.Label("Investissement conseille : " + queueEconomy.BestInvestmentLabel, smallStyle);
                    GUILayout.Label(queueEconomy.BestInvestmentReason, mutedStyle);
                }
                GUILayout.Label(economy.LastSettlement, mutedStyle);

                var history = EconomyHistoryRuntime.I;
                if (history != null)
                {
                    GUILayout.Space(4);
                    GUILayout.Label("HISTORIQUE - 5 DERNIERS JOURS", smallStyle);
                    var days = history.GetRecentDays(5);
                    for (int i = 0; i < days.Length; i++)
                    {
                        var d = days[i];
                        string sat = d.visitors > 0 ? d.satisfaction.ToString("0") + "/100" : "—";
                        GUILayout.Label($"J{d.day}  CA {d.revenue:N0} | resultat {d.profit:+N0;-N0;0} EUR | visiteurs {d.visitors} | sat. {sat}", mutedStyle);
                    }
                    GUILayout.Label(history.TrendLabel, smallStyle);
                    GUILayout.Label(history.DecisionFeedback, mutedStyle);
                }

                var dayEnd = DayEndSummaryRuntime360.I;
                if (dayEnd != null && dayEnd.LastSummary.HasData)
                {
                    var recap = dayEnd.LastSummary;
                    GUILayout.Space(4);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"BILAN J{recap.day}  |  résultat {recap.profit:+N0;-N0;0} EUR  |  sat. {(recap.visitors > 0 ? recap.satisfaction.ToString("0") + "/100" : "—")}", smallStyle);
                    if (!dayEnd.Visible && GUILayout.Button("Afficher", GUILayout.Width(70f))) dayEnd.ShowLast();
                    GUILayout.EndHorizontal();
                    GUILayout.Label("Meilleur : " + recap.bestPoint, mutedStyle);
                    GUILayout.Label("À améliorer : " + recap.worstPoint, mutedStyle);
                }

                var prestige = CareerPrestigeRuntime357.I;
                if (prestige != null)
                {
                    GUILayout.Space(4);
                    GUILayout.Label($"PRESTIGE  {prestige.Prestige:0}/100  —  {prestige.LevelName}", smallStyle);
                    GUILayout.Label(prestige.CurrentUnlock, mutedStyle);
                    GUILayout.Label($"Prochain palier : {prestige.NextMilestone}  |  progression {prestige.NextMilestoneProgress01:P0}", smallStyle);
                    GUILayout.Label(prestige.DriversLabel, mutedStyle);
                }

                var objectives = CareerObjectivesRuntime358.I;
                if (objectives != null && (CareerProgressionRuntime.I == null || !CareerProgressionRuntime.I.IsSandbox))
                {
                    GUILayout.Space(4);
                    GUILayout.Label($"OBJECTIF CARRIERE  {objectives.ObjectiveNumber}/{objectives.ObjectiveCount} — {objectives.CurrentTitle}", smallStyle);
                    GUILayout.Label(objectives.CurrentDescription, mutedStyle);
                    GUILayout.Label($"Progression {objectives.Progress01:P0}  |  {objectives.ProgressLabel}", smallStyle);
                    if (!objectives.AllCompleted) GUILayout.Label($"Récompense : prime carrière {objectives.CurrentReward:N0} EUR", mutedStyle);
                }

                var progression = CareerProgressionRuntime.I;
                if (progression != null && progression.HasNextTier)
                {
                    float nextInvestment = progression.NextResearchCost;
                    GUILayout.Label($"Prochaine recherche  {nextInvestment:N0} EUR  |  progression budget {economy.UpgradeProgress01(nextInvestment):P0}", smallStyle);
                    GUILayout.Label(economy.DescribeNextInvestment(nextInvestment), mutedStyle);
                }
            }
        }
        private void DrawCalendarRow361()
        {
            bool open = management == ManagementMenu.Calendar;
            bool next = Accordion("Calendrier", open);
            if (next != open) management = next ? ManagementMenu.Calendar : ManagementMenu.None;
            if (management != ManagementMenu.Calendar) return;

            var calendar = SeasonCalendarRuntime361.I;
            if (calendar == null) { GUILayout.Label("Calendrier saisonnier en cours d'initialisation...", mutedStyle); return; }
            var today = calendar.Today;
            GUILayout.Label($"SAISON {calendar.SeasonNumber}  •  JOUR {calendar.SeasonDay}  •  {today.label}", smallStyle);
            GUILayout.Label(today.detail, mutedStyle);
            GUILayout.Label($"Impact fréquentation prévu  x{today.demandMultiplier:0.00}", smallStyle);
            var peakFlow379 = PeakVisitorFlowRuntime379.I;
            if (peakFlow379 != null)
            {
                GUILayout.Label($"FLUX RÉEL  {peakFlow379.FlowLabel}", smallStyle);
                GUILayout.Label(peakFlow379.DetailLabel, mutedStyle);
            }
            GUILayout.Label(calendar.PlanningHint, mutedStyle);
            var weather363 = SeasonWeatherRuntime363.I;
            if (weather363 != null)
            {
                var w = weather363.Today;
                GUILayout.Space(3);
                GUILayout.Label($"MÉTÉO  {w.weatherLabel}  •  {w.temperatureC:0}°C  •  vent {w.windKmh:0} km/h", smallStyle);
                GUILayout.Label($"Neige prévue : {w.snowLabel} ({w.predictedSnowQuality01:P0})  •  fréquentation météo x{w.demandMultiplier:0.00}", mutedStyle);
                GUILayout.Label(w.impactLabel, mutedStyle);
                GUILayout.Label(weather363.PlanningHint, mutedStyle);
            }
            var forecast = CapacityForecastRuntime362.I;
            if (forecast != null)
            {
                GUILayout.Space(3);
                GUILayout.Label("PRÉVISION CAPACITÉ", smallStyle);
                GUILayout.Label(forecast.PlanningAlert, mutedStyle);
            }
            GUILayout.Space(4);
            GUILayout.Label("7 JOURS À VENIR", smallStyle);
            var days = calendar.GetUpcoming(7);
            var capacityDays = forecast != null ? forecast.GetUpcoming(7) : null;
            var weatherDays = weather363 != null ? weather363.GetUpcoming(7) : null;
            for (int i = 0; i < days.Length; i++)
            {
                var d = days[i];
                string prefix = i == 0 ? "Aujourd'hui" : "+" + i + " j";
                string capacity = capacityDays != null && i < capacityDays.Length
                    ? "  •  risque " + capacityDays[i].riskLabel + "  •  ~" + capacityDays[i].projectedVisitors + " visiteurs"
                    : string.Empty;
                string weatherText = weatherDays != null && i < weatherDays.Length
                    ? "  •  " + weatherDays[i].weatherLabel + " " + weatherDays[i].temperatureC.ToString("0") + "°C  •  neige " + weatherDays[i].snowLabel
                    : string.Empty;
                GUILayout.Label($"{prefix}  •  J{d.day}  •  {d.label}  •  demande x{d.demandMultiplier:0.00}{weatherText}{capacity}", i == 0 ? smallStyle : mutedStyle);
                if (capacityDays != null && i < capacityDays.Length && capacityDays[i].risk >= CapacityRisk362.High)
                    GUILayout.Label("   ↳ " + capacityDays[i].bottleneck + " — " + capacityDays[i].recommendation, mutedStyle);
            }
        }

        private void DrawManagementRow(string label, ManagementMenu value, string detail)
        {
            bool open = management == value; bool next = Accordion(label, open);
            if (next != open) management = next ? value : ManagementMenu.None;
            if(management==value){GUILayout.Label(detail,mutedStyle);if(value==ManagementMenu.Research){var lab=CareerProgressionRuntime.I;if(lab!=null){GUILayout.Label(lab.GetTierName(lab.CurrentTier),smallStyle);GUILayout.Label(lab.GetTierUnlocks(lab.CurrentTier),smallStyle);if(lab.HasNextTier&&!lab.CatalogueUnlocked&&GUILayout.Button("Rechercher le prochain palier — "+lab.NextResearchCost.ToString("N0")+" €"))lab.ResearchNextTier();GUILayout.Label(lab.LastMessage,mutedStyle);}if(GUILayout.Button("Catalogue des remontées"))Object.FindFirstObjectByType<LiftCareerCatalog249>()?.Open263();}}
        }

        private void DrawDomain()
        {
            GUILayout.Label("DOMAINE", titleStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Carte [M]")) SendMapToggle();
            if (GUILayout.Button("Commandes [F1]")) TogglePrimary(PrimaryMenu.Help);
            GUILayout.EndHorizontal();
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
            else if(value==DomainMenu.Slopes)
            {
                if(GUILayout.Button("Neige et damage [F5]"))SlopeConditionUI.Open();
            }
            else GUILayout.Label("Les informations detaillees s'afficheront ici a la demande.", mutedStyle);
        }

        private void DrawHelp()
        {
            GUILayout.Label("COMMANDES", titleStyle);
            GUILayout.Label("Flèches : déplacement · Molette : zoom vers le curseur · Molette maintenue : glisser · Maj + molette maintenue : tourner",smallStyle);
            GUILayout.Label("Origine : vue generale | Fin : front de neige", smallStyle);
            GUILayout.Label("B piste  |  L remontee  |  H village  |  M carte  |  Espace pause  |  1/2/3 vitesse", smallStyle);
            GUILayout.Label("Clic : fiche structure | Suppr : confirmation | F6 : liste des structures", smallStyle);
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
