using UnityEngine;

namespace SkiStation
{
    /// <summary>Beginner workflow: P analyses the selected/hovered slope, Enter applies, Escape hides.</summary>
    public sealed class SlopeTerrainWorkPlannerMode : MonoBehaviour
    {
        [SerializeField] Camera buildCamera;
        [SerializeField] KeyCode analyseKey = KeyCode.P;
        SlopeTerrainWorkPlanner activePlanner;

        public SlopeTerrainWorkPlanner ActivePlanner => activePlanner;

        void Awake() { if (!buildCamera) buildCamera = Camera.main; }

        void Update()
        {
            if (!buildCamera) buildCamera = Camera.main;
            if (Input.GetKeyDown(analyseKey)) AnalyseHovered();
            if (activePlanner == null) return;
            if (Input.GetKeyDown(KeyCode.Return)) activePlanner.ApplyPlan();
            if (Input.GetKeyDown(KeyCode.Escape)) { activePlanner.ClearPlan(); activePlanner = null; }
        }

        void AnalyseHovered()
        {
            if (!buildCamera) return;
            Ray ray = buildCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 5000f, ~0, QueryTriggerInteraction.Ignore)) return;
            var slope = hit.collider.GetComponentInParent<SlopeRuntime>();
            if (slope == null) return;
            activePlanner = slope.GetComponent<SlopeTerrainWorkPlanner>();
            if (activePlanner == null) activePlanner = slope.gameObject.AddComponent<SlopeTerrainWorkPlanner>();
            activePlanner.Analyse();
        }
    }
}
