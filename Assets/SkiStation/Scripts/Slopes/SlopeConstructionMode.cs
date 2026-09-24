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

    public sealed class SlopeConstructionMode : MonoBehaviour
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
        Vector3 hoverPoint;
        bool hasHover;
        Vector3[] pendingPositions;
        float[] pendingWidths;
        SlopeDesignAssessment pendingAssessment;

        public bool IsDrawing => drawing;
        public void SetActive(bool value) { if (!value) Cancel(); else drawing = true; }
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
            if (!buildCamera) buildCamera = Camera.main;
            if (!drawing) return;
            if (Input.GetKeyDown(KeyCode.LeftBracket)) currentWidth = Mathf.Max(minWidth, currentWidth - 1f);
            if (Input.GetKeyDown(KeyCode.RightBracket)) currentWidth = Mathf.Min(maxWidth, currentWidth + 1f);
            if (Input.GetKeyDown(finishKey)) Finish();
            if (Input.GetKeyDown(cancelKey)) Cancel();
            UpdateHover();
            if (Input.GetMouseButtonDown(0) && hasHover && !PlayableGameHUD.PointerOverCameraControls) AddPoint(hoverPoint);
            RefreshPreview();
        }

        void UpdateHover()
        {
            hasHover = false;
            if (!buildCamera) return;
            Ray ray = buildCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 10000f, terrainMask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0) return;
            System.Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
            for (int i=0;i<hits.Length;i++)
            {
                if (hits[i].collider == null) continue;
                if (hits[i].collider.transform.IsChildOf(transform)) continue;
                hoverPoint = hits[i].point; hasHover = true; return;
            }
        }

        void AddPoint(Vector3 p)
        {
            if (points.Count > 0 && Vector3.Distance(points[points.Count-1].position, p) < sampleSpacing) return;
            points.Add(new SlopeControlPoint(p, currentWidth));
            Debug.Log("SkiStation 2.06: Piste point " + points.Count + " placed.");
        }

        void RefreshPreview()
        {
            if (!preview) return;
            int extra = hasHover && points.Count > 0 ? 1 : 0;
            preview.positionCount = points.Count + extra;
            for (int i=0;i<points.Count;i++) preview.SetPosition(i, points[i].position + Vector3.up * 6f);
            if (extra == 1) preview.SetPosition(points.Count, hoverPoint + Vector3.up * 6f);
            preview.widthMultiplier = 14f;
        }

        void OnGUI()
        {
            if (!drawing) return;
            string step = points.Count == 0 ? "PISTE : cliquez sur le POINT DE DÉPART" : "PISTE : cliquez pour tracer (" + points.Count + " point" + (points.Count>1?"s":"") + ")  —  ENTRÉE : récapitulatif";
            GUI.Box(new Rect((Screen.width-680f)*.5f, 100f, 680f, 42f), step + "   |   Échap : annuler");
        }

        public void SetDifficulty(SlopeConstructionDifficulty value) => difficulty = value;
        public void SetWidth(float width) => currentWidth = Mathf.Clamp(width, minWidth, maxWidth);

        public void Finish()
        {
            if (points.Count < 2) { Cancel(); return; }
            var temp = new GameObject("SlopeDesignPreview");
            var design = temp.AddComponent<SlopeDesignIntelligenceRuntime>();
            var raw = new List<Vector3>(); foreach (var p in points) raw.Add(p.position);
            pendingPositions = design.ConformAndResample(raw, sampleSpacing).ToArray();
            pendingAssessment = design.Assess(pendingPositions, (SlopeDifficulty)difficulty);
            Destroy(temp);
            if (!pendingAssessment.valid) { PlayableFeedbackRuntime.I?.Show("Construction impossible : " + pendingAssessment.message + ".", 4.5f); return; }
            if (ConstructionClearance249.ConflictsWithSurfaceLift(pendingPositions, maxWidth * .5f, out string crossingReason)) { PlayableFeedbackRuntime.I?.Show(crossingReason, 4.5f); return; }
            pendingWidths = new float[pendingPositions.Length];
            for (int i=0;i<pendingWidths.Length;i++) { float t=pendingWidths.Length<=1?0f:(float)i/(pendingWidths.Length-1); float src=t*(points.Count-1); int lo=Mathf.Clamp(Mathf.FloorToInt(src),0,points.Count-1); int hi=Mathf.Clamp(lo+1,0,points.Count-1); pendingWidths[i]=Mathf.Lerp(points[lo].width,points[hi].width,src-lo); }
            drawing=false;
            if (SlopeConstructionConfirmationRuntime.I == null) new GameObject("Slope Construction Confirmation 1.99").AddComponent<SlopeConstructionConfirmationRuntime>();
            float cost = PlayableLoopRuntime.I != null ? PlayableLoopRuntime.I.EstimateSlopeCost(pendingPositions) : 0f;
            SlopeConstructionConfirmationRuntime.I.Open(this, pendingAssessment, cost, difficulty);
        }

        public void ConfirmPendingSlope()
        {
            if (pendingPositions == null || pendingPositions.Length < 2) return;
            if (PlayableLoopRuntime.I != null && !PlayableLoopRuntime.I.PayForSlope(pendingPositions)) return;
            ConstructionClearance249.ClearTrees(pendingPositions, maxWidth * .5f + 2f);
            var go=new GameObject($"Piste_{difficulty}_{Time.frameCount}"); var runtime=go.AddComponent<SlopeRuntime>(); runtime.Build(pendingPositions,pendingWidths);
            go.AddComponent<SlopeTerrainConformityRuntime>().Conform(); runtime.SetDesignedDifficulty((SlopeDifficulty)difficulty); go.AddComponent<SlopeMeshGenerator>().Generate(runtime); go.AddComponent<SlopePlayableVisibilityRuntime>().Build(runtime); go.AddComponent<SlopeSignRuntime>(); go.AddComponent<SlopeSafetySignageRuntime>().Build(runtime); go.AddComponent<SlopeEndpointAutoConnector>().Resolve(runtime);
            SlopeConstructionConfirmationRuntime.I?.Close(); points.Clear(); preview.positionCount=0; pendingPositions=null; pendingWidths=null;
        }
        public void ResumePendingSlope() { SlopeConstructionConfirmationRuntime.I?.Close(); pendingPositions=null; pendingWidths=null; drawing=true; }
        public void CancelPendingSlope() { SlopeConstructionConfirmationRuntime.I?.Close(); Cancel(); pendingPositions=null; pendingWidths=null; }

        public void Cancel()
        {
            drawing = false;
            points.Clear();
            if (preview) preview.positionCount = 0;
        }
    }
}
