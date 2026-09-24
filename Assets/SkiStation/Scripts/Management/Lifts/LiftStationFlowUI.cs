using UnityEngine;

namespace SkiStation
{
    /// <summary>F8 diagnostic view of real vehicle docking and passenger flow.</summary>
    public sealed class LiftStationFlowUI : MonoBehaviour
    {
        private bool visible;
        private GUIStyle title;
        private void Update() { if (Input.GetKeyDown(KeyCode.F8)) visible = !visible; }
        private void OnGUI()
        {
            if (!visible) return;
            title = title ?? new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            GUILayout.BeginArea(new Rect(24, 24, 560, 620), GUI.skin.box);
            GUILayout.Label("GESTION DES FLUX — STATIONS", title);
            var lifts = FindObjectsOfType<LiftRuntime>();
            for (int i = 0; i < lifts.Length; i++)
            {
                var lift = lifts[i];
                if (lift == null) continue;
                GUILayout.Label((lift.Definition != null ? lift.Definition.displayName : lift.name) + "  [" + lift.LiftId + "]");
                var flows = lift.GetComponentsInChildren<LiftStationFlowRuntime>(true);
                int boarding = 0, releasing = 0;
                for (int j = 0; j < flows.Length; j++)
                {
                    if (flows[j] == null) continue;
                    if (flows[j].Snapshot.state == LiftStationFlowState.Boarding) boarding++;
                    if (flows[j].Snapshot.state == LiftStationFlowState.Releasing) releasing++;
                }
                GUILayout.Label("  Véhicules: " + flows.Length + " | Embarquement: " + boarding + " | Débarquement: " + releasing);
            }
            GUILayout.Label("F8 : fermer");
            GUILayout.EndArea();
        }
    }
}
