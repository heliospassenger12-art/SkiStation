using UnityEngine;

namespace SkiStation
{
    /// <summary>Optional 1.00 diagnostics. Toggle with F12.</summary>
    public sealed class SkiDomainRouteDecisionDebugUI : MonoBehaviour
    {
        [SerializeField] private bool visible;
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F12)) visible = !visible;
        }

        private void OnGUI()
        {
            if (!visible || SkiDomainRouteDecisionRuntime.I == null) return;
            var runtime = SkiDomainRouteDecisionRuntime.I;
            GUILayout.BeginArea(new Rect(12, 12, 430, 260), GUI.skin.window);
            GUILayout.Label("SKISTATION 1.00 — DÉCISIONS MONTAGNE");
            GUILayout.Label("Visiteurs réels suivis : " + runtime.Decisions.Count);
            foreach (var pair in runtime.Decisions)
            {
                var d = pair.Value;
                string skier = d.skier != null ? d.skier.name : "Client";
                string lift = d.nextLift != null ? d.nextLift.name : "—";
                string slope = d.nextSlope != null ? d.nextSlope.name : "—";
                GUILayout.Label(skier + " → " + lift + " → " + slope);
            }
            GUILayout.EndArea();
        }
    }
}
