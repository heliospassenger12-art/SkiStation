using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum SlopeConstructionDifficulty { Green, Blue, Red, Black }

    [Serializable]
    public struct SlopeControlPoint
    {
        public Vector3 position;
        [Min(1f)] public float width;
        public SlopeControlPoint(Vector3 p, float w) { position = p; width = w; }
    }

    public sealed partial class SlopeConstructionMode : MonoBehaviour
    {
        public static SlopeConstructionMode Instance { get; private set; }
        [SerializeField] Camera buildCamera;
        [SerializeField] LayerMask terrainMask = ~0;
        [SerializeField] float sampleSpacing = 8f;
        [SerializeField] float defaultWidth = 10f;
        [SerializeField] float minWidth = 4f;
        [SerializeField] float maxWidth = 28f;
        [SerializeField] SlopeConstructionDifficulty difficulty = SlopeConstructionDifficulty.Blue;
        [SerializeField] KeyCode finishKey = KeyCode.Return;
        [SerializeField] KeyCode cancelKey = KeyCode.Escape;
        readonly System.Collections.Generic.List<SlopeControlPoint> points = new();
        LineRenderer preview;
        float currentWidth;
        bool drawing;
        SlopeConstructionDifficulty pendingDifficulty;
        Vector3 hoverPoint;
        bool hasHover;
        Vector3[] pendingPositions;
        float[] pendingWidths;
        SlopeDesignAssessment pendingAssessment;
        string valAlpinRefusal;

        public bool IsDrawing => drawing;
        public void SetActive(bool value) { if (!value) CancelPendingSlope(); else { ConstructionInput349.Activate(ConstructionTool349.Slope); drawing = true; } }
        public IReadOnlyList<SlopeControlPoint> Points => points;
        public SlopeConstructionDifficulty Difficulty => difficulty;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            currentWidth = defaultWidth;
            if (!buildCamera) buildCamera = Camera.main;
            preview = gameObject.AddComponent<LineRenderer>();
            preview.positionCount = 0;
            preview.widthMultiplier = 0.12f;
            preview.useWorldSpace = true;
            var previewShader = Shader.Find("Unlit/Color");
            if (previewShader == null) previewShader = Shader.Find("Sprites/Default");
            preview.material = previewShader != null ? new Material(previewShader) : RuntimeMaterialFactory.Create(new Color(0f,.95f,1f,1f));
            if (preview.material != null && preview.material.HasProperty("_Color")) preview.material.color = new Color(0f,.95f,1f,1f);
            preview.startColor = new Color(0f,.95f,1f,1f); preview.endColor = preview.startColor;
            preview.sortingOrder = 100;
            preview.numCapVertices = 8; preview.numCornerVertices = 4;
        }

        void Update()
        {
            if(RecommendationDraft389!=null){StepRecommendation389();return;}
            ReleaseFlexibleDrag();
            if (!buildCamera) buildCamera = Camera.main;
            if (Input.GetKeyDown(cancelKey)) { CancelPendingSlope(); return; }
            // Finishing is a keyboard action, independent of the world pointer's position.
            // Retain real modal protection; only the mouse-to-world clicks remain panel-blocked.
            if (TerrainGeneration.ValAlpinTestScene.IsActive && drawing && !ConstructionInput349.ModalOpen &&
                (Input.GetKeyDown(finishKey) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            { Finish(); return; }
            if(!drawing)return;
            if(ConstructionInput349.PointerBlocked)
            {if(FlexibleAuthoring){hasHover=false;RefreshPreview();}return;}
            if ((Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Backspace)) && points.Count > 0)
            {points.RemoveAt(points.Count - 1);SyncFlexibleSnaps();selectedControl=draggedControl=-1;DirtyFlexible();}
            if (Input.GetKeyDown(KeyCode.LeftBracket)) SetWidth(currentWidth - 1f);
            if (Input.GetKeyDown(KeyCode.RightBracket)) SetWidth(currentWidth + 1f);
            if (Input.GetKeyDown(finishKey)) { Finish(); return; }
            UpdateHover();
            bool editing=EditFlexibleControl();
            if (!editing && Input.GetMouseButtonDown(0) && hasHover && !PlayableGameHUD.PointerOverCameraControls)
            {if(FlexibleAuthoring)AddFlexibleMousePoint();else AddPoint(hoverPoint);}
            RefreshPreview();
        }

        void UpdateHover()
        {
            hasHover = ConstructionInput349.TryGround(buildCamera, out hoverPoint);
            if (hasHover && ManagementSurface.TryTrail(hoverPoint,out var deck,this)) hoverPoint=deck;
            rawFlexibleHover=hoverPoint;
            SnapFlexibleHover();
        }

        void AddPoint(Vector3 p)
        {
            if(FlexibleAuthoring&&points.Count>=SlopeFlexibleDraftGeometry.MaxControls){RefuseSlopeFinish("128 points de contrôle maximum : terminez puis raccordez une autre piste.");return;}
            if (points.Count > 0 && Vector3.Distance(points[points.Count-1].position, p) < sampleSpacing) return;
            points.Add(new SlopeControlPoint(p, currentWidth));SyncFlexibleSnaps();DirtyFlexible();
            Debug.Log("SkiStation 2.06: Piste point " + points.Count + " placed.");
        }

        void RefreshPreview()
        {
            if(RefreshFlexiblePreview())return;
            if (!preview) return;
            int extra = hasHover && points.Count > 0 ? 1 : 0;
            preview.positionCount = points.Count + extra;
            for (int i=0;i<points.Count;i++) preview.SetPosition(i, points[i].position + Vector3.up * 6f);
            if (extra == 1) preview.SetPosition(points.Count, hoverPoint + Vector3.up * 6f);
            preview.widthMultiplier = 14f;
        }

        void OnGUI()
        {
            if(RecommendationDraft389!=null){RecommendationConstructionRuntime389.I?.DrawDraft();return;}
            if (!drawing) return;
            string step = points.Count == 0 ? "PISTE : cliquez sur le POINT DE DÉPART" : "PISTE : cliquez pour tracer (" + points.Count + " point" + (points.Count>1?"s":"") + ")  —  ENTRÉE : récapitulatif";
            float panelWidth=Mathf.Min(680f,Screen.width-24f);
            Rect panel = new Rect((Screen.width-panelWidth)*.5f, 100f, panelWidth, 62f);
            ConstructionInput349.PanelArea(this, "slope-tool", panel);
            GUI.Box(panel, step + "\nLargeur : " + currentWidth.ToString("0") + " m ([ / ]) | Clic droit : dernier point | Échap : annuler");
            if(FlexibleAuthoring){DrawFlexibleToolbar(panel);return;}
            if (TerrainGeneration.ValAlpinTestScene.IsActive)
            {
                Rect action = new Rect(panel.x, panel.yMax + 4f, panel.width, 36f);
                ConstructionInput349.PanelArea(this, "valalpin-slope-finish", action);
                bool prior = GUI.enabled;
                GUI.enabled = prior && points.Count >= 2 && !ConstructionInput349.ModalOpen;
                if (GUI.Button(action, "TERMINER LE TRACÉ → RÉCAPITULATIF (Entrée)")) Finish();
                GUI.enabled = prior;
                if (!string.IsNullOrEmpty(valAlpinRefusal))
                {
                    Rect refusal = new Rect(panel.x, action.yMax + 4f, panel.width, 54f);
                    ConstructionInput349.PanelArea(this, "valalpin-slope-refusal", refusal);
                    GUI.Box(refusal, valAlpinRefusal);
                }
            }
        }

        public void SetDifficulty(SlopeConstructionDifficulty value) { if(RecommendationDraft389!=null){SetRecommendationDifficulty389(value);return;}difficulty = value; if (pendingPositions != null) ResumePendingSlope(); }
        public void SetWidth(float width) {if(RecommendationDraft389!=null){SetRecommendationWidth389(width);return;}currentWidth = Mathf.Clamp(width, minWidth, maxWidth);
            if(drawing&&FlexibleAuthoring&&selectedControl>=0&&selectedControl<points.Count)points[selectedControl]=new SlopeControlPoint(points[selectedControl].position,currentWidth);
            DirtyFlexible();}

        public void Finish()
        {
            if (!drawing) return;
            valAlpinRefusal = null;
            if (points.Count < 2) { RefuseSlopeFinish("Placez au moins deux points."); return; }
            var temp = new GameObject("SlopeDesignPreview");
            var design = temp.AddComponent<SlopeDesignIntelligenceRuntime>();
            var raw = new List<Vector3>(); foreach (var p in points) raw.Add(p.position);
            pendingPositions=null;pendingWidths=null;
            if(FlexibleAuthoring)
            {
                if(!PrepareFlexiblePending(out var flexibleReason)){Destroy(temp);RefuseSlopeFinish(flexibleReason);return;}
            }
            else pendingPositions = design.ConformAndResample(raw, sampleSpacing).ToArray();
            pendingDifficulty = difficulty;
            pendingAssessment = design.Assess(pendingPositions, (SlopeDifficulty)pendingDifficulty);
            Destroy(temp);
            if (!ConstructionRules349.SlopeRoute(pendingPositions, out string routeReason)) { RefuseSlopeFinish(routeReason); pendingPositions = null; return; }
            if (!pendingAssessment.valid) { RefuseSlopeFinish("Construction impossible : " + pendingAssessment.message + "."); return; }
            if (ConstructionClearance249.ConflictsWithSurfaceLift(pendingPositions, maxWidth * .5f, out string crossingReason)) { RefuseSlopeFinish(crossingReason); return; }
            if(pendingWidths==null)
            {
                pendingWidths = new float[pendingPositions.Length];
                for (int i = 0; i < pendingWidths.Length; i++) pendingWidths[i] = ConstructionRules349.WidthAt(pendingPositions[i], points);
            }
            drawing=false;HideFlexiblePreview();
            if (SlopeConstructionConfirmationRuntime.I == null) new GameObject("Slope Construction Confirmation 1.99").AddComponent<SlopeConstructionConfirmationRuntime>();
            float cost = ConstructionRules349.SlopeCost(pendingPositions);
            SlopeConstructionConfirmationRuntime.I.Open(this, pendingAssessment, cost, difficulty);
        }

        void RefuseSlopeFinish(string reason)
        {
            valAlpinRefusal = reason;
            PlayableFeedbackRuntime.I?.Show(reason, 4.5f);
        }

        public bool CanConfirm349(out string reason)
            => ValidatePending349(true,out reason);

        bool ValidatePending349(bool checkBudget,out string reason)
        {
            reason = "Aucun projet en attente";
            if ((drawing && RecommendationDraft389==null) || pendingPositions == null || pendingWidths == null || (RecommendationDraft389==null&&(SlopeConstructionConfirmationRuntime.I == null || !SlopeConstructionConfirmationRuntime.I.IsOpen))) return false;
            if(!ValidateFlexibleConnections(out reason))return false;
            if (!ConstructionRules349.SlopeRoute(pendingPositions, out reason)) return false;
            float halfWidth=RecommendationDraft389!=null?RecommendationDraft389.width*.5f:maxWidth*.5f;
            if(!Altitude.AltitudeConstructionPolicy.ValidateRoute(pendingPositions,halfWidth,out reason,this))return false;
            float routeWidth=0;
            foreach(float width in pendingWidths)routeWidth=Mathf.Max(routeWidth,width);
            if(!TerrainGeneration.RealTerrainWater.AllowsRoute(pendingPositions,routeWidth,out reason,true))return false;
            foreach (Vector3 point in pendingPositions)
                if (!ManagementSurface.TryTrail(point, out var ground,this) || Mathf.Abs(ground.y - point.y) > .5f)
                { reason = "Le terrain a change ou sort du massif : modifiez le trace"; return false; }
            if (ConstructionClearance249.ConflictsWithSurfaceLift(pendingPositions, halfWidth, out reason)) return false;
            if (!ConstructionPlacement349.RouteClearOfBuildings(pendingPositions, halfWidth, out reason)) return false;
            reason = checkBudget?ConstructionPlacement349.BudgetReason(ConstructionRules349.SlopeCost(pendingPositions)):"";
            return reason.Length == 0;
        }

        public void ConfirmPendingSlope()
        {
            if(RecommendationDraft389!=null){RecommendationConstructionRuntime389.I?.Confirm(this);return;}
            if (!CanConfirm349(out string reason)) { PlayableFeedbackRuntime.I?.Show(reason, 4f); return; }
            float cost = ConstructionRules349.SlopeCost(pendingPositions);
            if (!StationRuntime.I.Spend(cost, FinanceCategory.Construction, "slope_route", "Construction piste")) return;
            Vector3[] route = pendingPositions; float[] widths = pendingWidths;
            pendingPositions = null; pendingWidths = null; // Single-use confirmation, even on a second event.
            SlopeConstructionConfirmationRuntime.I.Close(); ConstructionInput349.ConsumeClick();
            var go=BuildInfrastructure389(route,widths,pendingDifficulty,false,null,null,maxWidth*.5f+2f);
            var runtime=go.GetComponent<SlopeRuntime>();
            var site376 = ConstructionSiteRuntime376.BeginSlope(runtime, cost);
            points.Clear();ResetFlexibleDraft();preview.positionCount = 0;
            ConstructionSelectionRuntime.I?.Select349(runtime);
            PlayableLoopRuntime.I?.SetMessage(site376 == null
                ? "Piste construite : " + cost.ToString("N0") + " EUR."
                : "Chantier de piste lancé : " + cost.ToString("N0") + " EUR • " + Mathf.RoundToInt(site376.DurationGameMinutes) + " min de jeu.");
        }
        public void ResumePendingSlope()
        { SlopeConstructionConfirmationRuntime.I?.Close(); pendingPositions = null; pendingWidths = null; drawing = true;DirtyFlexible();ConstructionInput349.ConsumeClick(); }
        public void CancelPendingSlope()
        { if(RecommendationDraft389!=null)ReleaseRecommendationContext389();SlopeConstructionConfirmationRuntime.I?.Close(); Cancel(); pendingPositions = null; pendingWidths = null; ConstructionInput349.ConsumeClick(); }

        private void OnDestroy() { if (Instance == this) Instance = null;DisposeFlexibleDraft();if (preview != null && preview.sharedMaterial != null) Destroy(preview.sharedMaterial); }

        public void Cancel()
        {
            drawing = false; hasHover = false;
            points.Clear();ResetFlexibleDraft();
            if (preview) preview.positionCount = 0;
        }
    }
}
