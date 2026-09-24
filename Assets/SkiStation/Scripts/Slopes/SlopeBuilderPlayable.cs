using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public sealed class SlopeBuilderPlayable : MonoBehaviour
    {
        [SerializeField] private Camera buildCamera;
        [SerializeField] private LineRenderer preview;
        [SerializeField] private SlopeDefinition definition;
        [SerializeField] private GameObject slopePrefab;
        [SerializeField] private float pointSpacing = 5f;
        [SerializeField] private float minimumWidth = 4f;
        [SerializeField] private float maximumWidth = 24f;
        private readonly List<Vector3> points = new List<Vector3>();
        private readonly List<float> widths = new List<float>();
        private bool drawing;
        private float currentWidth = 10f;

        public void Begin()
        {
            points.Clear(); widths.Clear(); drawing = true;
            currentWidth = definition != null ? definition.defaultWidth : 10f;
            UpdatePreview();
        }

        private void Update()
        {
            if (!drawing) return;
            if (buildCamera == null) buildCamera = Camera.main;
            if (buildCamera != null && Physics.Raycast(buildCamera.ScreenPointToRay(Input.mousePosition), out var hit, 5000f))
            {
                Vector3 p = hit.point;
                if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], p) >= pointSpacing)
                { points.Add(p); widths.Add(currentWidth); UpdatePreview(); }
            }
            if (Input.GetKey(KeyCode.LeftBracket)) currentWidth = Mathf.Max(minimumWidth, currentWidth - 10f * Time.deltaTime);
            if (Input.GetKey(KeyCode.RightBracket)) currentWidth = Mathf.Min(maximumWidth, currentWidth + 10f * Time.deltaTime);
            if (Input.GetMouseButtonDown(1)) Cancel();
        }

        public void SetWidth(float width)
        { currentWidth = Mathf.Clamp(width, minimumWidth, maximumWidth); if (widths.Count > 0) widths[widths.Count - 1] = currentWidth; UpdatePreview(); }

        public SlopeRuntime Finish()
        {
            drawing = false;
            if (points.Count < 2) { UpdatePreview(); return null; }
            var go = slopePrefab != null ? Instantiate(slopePrefab) : new GameObject("SkiSlope");
            var slope = go.GetComponent<SlopeRuntime>() ?? go.AddComponent<SlopeRuntime>();
            IReadOnlyList<Vector3> buildPoints = points;
            if (AlpineDomainTerrainBridgeRuntime.I != null)
            {
                List<Vector3> conformed = AlpineDomainTerrainBridgeRuntime.I.ConformSlopePoints(points, 0.35f);
                AlpineCorridorAssessment assessment = AlpineDomainTerrainBridgeRuntime.I.AssessSlope(conformed);
                buildPoints = conformed;
                Debug.Log("Piste terrain assessment: " + assessment.skiDifficulty + " | score " + assessment.score.ToString("0") + "/100 | skiable " + (assessment.skiableFraction * 100f).ToString("0") + "%");
            }
            slope.Build(buildPoints, widths);
            var conformity = go.GetComponent<SlopeTerrainConformityRuntime>() ?? go.AddComponent<SlopeTerrainConformityRuntime>();
            conformity.Conform();
            var mesh = go.GetComponent<SlopeMeshGenerator>() ?? go.AddComponent<SlopeMeshGenerator>();
            mesh.Generate(slope);
            if (ResortSlopeGraph.I == null) new GameObject("ResortSlopeGraph").AddComponent<ResortSlopeGraph>();
            ResortSlopeGraph.I.AutoConnect(slope);
            points.Clear(); widths.Clear(); UpdatePreview(); return slope;
        }

        public void Cancel() { drawing = false; points.Clear(); widths.Clear(); UpdatePreview(); }
        private void UpdatePreview()
        {
            if (preview == null) return; preview.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++) preview.SetPosition(i, points[i]);
        }
    }
}
