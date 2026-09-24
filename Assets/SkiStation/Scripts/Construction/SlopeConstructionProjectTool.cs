using UnityEngine;

namespace SkiStation
{
    /// <summary>Press K then click a piste to launch its construction project.</summary>
    public sealed class SlopeConstructionProjectTool : MonoBehaviour
    {
        [SerializeField] Camera buildCamera;
        bool active;
        SlopeConstructionInvestment investment = SlopeConstructionInvestment.Standard;
        void Awake() { if (!buildCamera) buildCamera = Camera.main; }
        void Update()
        {
            if (!buildCamera) buildCamera = Camera.main;
            if (Input.GetKeyDown(KeyCode.K)) active = !active;
            if (Input.GetKeyDown(KeyCode.J)) investment = (SlopeConstructionInvestment)(((int)investment + 1) % 3);
            if (!active || !Input.GetMouseButtonDown(0) || SlopeConstructionProjectManager.I == null) return;
            Ray ray = buildCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 5000f)) return;
            var slope = hit.collider.GetComponentInParent<SlopeRuntime>();
            if (slope == null) return;
            SlopeConstructionProjectManager.I.BeginProject(slope, investment);
            active = false;
        }
    }
}
