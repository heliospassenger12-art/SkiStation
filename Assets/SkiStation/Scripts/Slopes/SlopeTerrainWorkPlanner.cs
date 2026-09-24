using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// Analyses the terrain under a slope corridor and proposes a gentle grading profile.
    /// Nothing is modified until ApplyPlan is called.
    /// </summary>
    public sealed class SlopeTerrainWorkPlanner : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] float corridorMargin = 2f;
        [SerializeField, Min(0.5f)] float analysisRadius = 8f;
        [SerializeField, Min(0.1f)] float maxWorkPerPoint = 12f;
        [SerializeField] bool autoAnalyseOnEnable = true;

        readonly List<Vector3> previewPoints = new();
        LineRenderer preview;
        SlopeRuntime slope;
        public bool HasPlan { get; private set; }
        public float EstimatedVolumeIndex { get; private set; }
        public float MaxRequiredWork { get; private set; }
        public int WorkPointCount { get; private set; }
        public IReadOnlyList<Vector3> PreviewPoints => previewPoints;

        void Awake()
        {
            slope = GetComponent<SlopeRuntime>();
            preview = gameObject.AddComponent<LineRenderer>();
            preview.useWorldSpace = true;
            preview.widthMultiplier = 0.22f;
            preview.positionCount = 0;
            preview.material = new Material(Shader.Find("Sprites/Default"));
            preview.enabled = false;
        }

        void OnEnable() { if (autoAnalyseOnEnable) Analyse(); }

        public bool Analyse()
        {
            HasPlan = false; EstimatedVolumeIndex = 0f; MaxRequiredWork = 0f; WorkPointCount = 0;
            previewPoints.Clear();
            if (slope == null) slope = GetComponent<SlopeRuntime>();
            if (slope == null || slope.Points.Count < 2 || MountainRuntime.I == null || !MountainRuntime.I.IsGenerated) return false;

            float start = slope.Points[0].y;
            float end = slope.Points[slope.Points.Count - 1].y;
            float total = Mathf.Max(0.01f, slope.Length);
            float travelled = 0f;
            for (int i = 0; i < slope.Points.Count; i++)
            {
                if (i > 0) travelled += Vector3.Distance(slope.Points[i - 1], slope.Points[i]);
                float t = travelled / total;
                float target = Mathf.Lerp(start, end, t);
                float terrain = MountainRuntime.I.Sample(slope.Points[i]).altitude;
                float required = Mathf.Clamp(target - terrain, -maxWorkPerPoint, maxWorkPerPoint);
                if (Mathf.Abs(required) > 0.25f)
                {
                    WorkPointCount++;
                    EstimatedVolumeIndex += Mathf.Abs(required) * Mathf.Max(1f, slope.GetWidthAt(i) + corridorMargin);
                    MaxRequiredWork = Mathf.Max(MaxRequiredWork, Mathf.Abs(required));
                }
                previewPoints.Add(slope.Points[i] + Vector3.up * 0.25f);
            }
            HasPlan = true;
            preview.positionCount = previewPoints.Count;
            for (int i = 0; i < previewPoints.Count; i++) preview.SetPosition(i, previewPoints[i]);
            preview.enabled = true;
            return true;
        }

        public void SetPreviewVisible(bool visible) { if (preview) preview.enabled = visible && HasPlan; }

        public bool ApplyPlan()
        {
            if (!HasPlan || slope == null || MountainTerrainWorkRuntime.I == null) return false;
            if (slope.Points.Count < 2) return false;
            float start = slope.Points[0].y, end = slope.Points[slope.Points.Count - 1].y;
            float total = Mathf.Max(0.01f, slope.Length), travelled = 0f;
            for (int i = 0; i < slope.Points.Count; i++)
            {
                if (i > 0) travelled += Vector3.Distance(slope.Points[i - 1], slope.Points[i]);
                float t = travelled / total;
                float target = Mathf.Lerp(start, end, t);
                float radius = Mathf.Max(analysisRadius, slope.GetWidthAt(i) * 0.5f + corridorMargin);
                MountainTerrainWorkRuntime.I.ApplyBrush(slope.Points[i], radius, 1f, true, target);
            }
            var conformity = GetComponent<SlopeTerrainConformityRuntime>();
            if (conformity == null) conformity = gameObject.AddComponent<SlopeTerrainConformityRuntime>();
            conformity.Conform();
            var mesh = GetComponent<SlopeMeshGenerator>();
            if (mesh == null) mesh = gameObject.AddComponent<SlopeMeshGenerator>();
            mesh.Generate(slope);
            Analyse();
            return true;
        }

        public void ClearPlan()
        {
            HasPlan = false; previewPoints.Clear();
            if (preview) { preview.positionCount = 0; preview.enabled = false; }
        }
    }
}
