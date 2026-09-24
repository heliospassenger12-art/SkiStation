using UnityEngine;

namespace SkiStation
{
    /// <summary>Optional lightweight diagnostic overlay for 0.96.</summary>
    public sealed class SkiDomainFlowUI : MonoBehaviour
    {
        [SerializeField] private bool visible = true;
        private GUIStyle title, body;

        private void Awake()
        {
            title = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            body = new GUIStyle(GUI.skin.label) { fontSize = 14 };
        }

        private void OnGUI()
        {
            if (!visible || SkiDomainFlowRuntime.I == null) return;
            var s = SkiDomainFlowRuntime.I.Snapshot;
            GUILayout.BeginArea(new Rect(18, 18, 360, 240), GUI.skin.box);
            GUILayout.Label("DOMAINE SKIABLE — 0.96", title);
            GUILayout.Label("Visiteurs réels : " + s.skiers, body);
            GUILayout.Label("En descente : " + s.skiing + "   |   Attente : " + s.waitingForLift, body);
            GUILayout.Label("Remontée : " + s.ridingLift + "   |   Repos : " + s.resting, body);
            GUILayout.Label("Arrivées : " + s.arriving + "   |   Départs : " + s.leaving, body);
            GUILayout.Label("Pistes ouvertes : " + s.openSlopes + "   |   Remontées : " + s.openLifts, body);
            GUILayout.Label("Véhicules réels : " + s.activeVehicles, body);
            GUILayout.Label("Satisfaction moyenne : " + s.averageSatisfaction.ToString("0") + "%", body);
            GUILayout.EndArea();
        }
    }
}
