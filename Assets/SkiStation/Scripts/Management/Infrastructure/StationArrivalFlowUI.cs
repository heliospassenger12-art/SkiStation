using UnityEngine;

namespace SkiStation
{
    public sealed class StationArrivalFlowUI : MonoBehaviour
    {
        private bool visible;
        private Vector2 scroll;
        private GUIStyle title, label, state;
        private void Update() { if (Input.GetKeyDown(KeyCode.F11)) visible = !visible; }
        private void OnGUI()
        {
            if (!visible) return;
            EnsureStyles();
            var flow = StationArrivalFlowRuntime.I;
            var drop = StationDropOffRuntime.I;
            GUILayout.BeginArea(new Rect(30, 30, 760, 600), GUI.skin.box);
            GUILayout.Label("ARRIVÉE STATION — 0.91", title);
            GUILayout.Label("Monde extérieur → portail → file dépose → dépose-minute → parking → réseau piéton → station", label);
            if (drop != null) GUILayout.Label(string.Format("Dépose-minute : {0}/{1} occupées • file : {2}", drop.OccupiedSlots, drop.Capacity, drop.QueueCount), label);
            GUILayout.Space(8);
            scroll = GUILayout.BeginScrollView(scroll);
            if (flow != null)
            {
                foreach (var skier in FindObjectsOfType<SkierRuntime>())
                {
                    StationArrivalFlowSnapshot s;
                    if (!flow.TryGetSnapshot(skier, out s)) continue;
                    GUILayout.BeginVertical(GUI.skin.box);
                    string transport = "—";
                    StationDropOffSnapshot d;
                    if (drop != null && drop.TryGetSnapshot(skier, out d)) transport = d.transportType.ToString();
                    GUILayout.Label(string.Format("{0} • {1} • {2}", s.visitorName, s.state, transport), state);
                    if (drop != null && drop.TryGetSnapshot(skier, out d))
                        GUILayout.Label(string.Format("Dépose : {0} | file #{1} | reste {2:0}s | parking : {3}", d.state, d.queuePosition, d.remainingSeconds, s.parked ? "OK" : "non"), label);
                    else GUILayout.Label(string.Format("Parking : {0} | chemin : {1}/{2} | distance : {3:0.0} m", s.parked ? "OK" : "non", s.pathIndex, s.pathPoints, s.distanceToTarget), label);
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("Fermer (F11)")) visible = false;
            GUILayout.EndArea();
        }
        private void EnsureStyles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            label = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            state = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
        }
    }
}
