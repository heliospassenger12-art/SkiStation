using UnityEngine;

namespace SkiStation
{
    /// <summary>0.88: lightweight diagnostic view for access bottlenecks.</summary>
    public sealed class AccessTrafficHotspotUI : MonoBehaviour
    {
        private bool visible;
        private Vector2 scroll;
        private GUIStyle title, label, warning;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9)) visible = !visible;
        }

        private void OnGUI()
        {
            if (!visible) return;
            EnsureStyles();
            var data = AccessTrafficHotspotRuntime.I;
            GUILayout.BeginArea(new Rect(30, 30, 560, 500), GUI.skin.box);
            GUILayout.Label("CIRCULATION — POINTS CHAUDS", title);
            GUILayout.Label("Trafic issu uniquement des visiteurs réels de la simulation.", label);
            GUILayout.Space(8);
            if (data == null || data.Hotspots.Count == 0)
            {
                GUILayout.Label("Aucun point chaud détecté.", label);
            }
            else
            {
                scroll = GUILayout.BeginScrollView(scroll);
                foreach (var h in data.Hotspots)
                {
                    GUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.Label(string.Format("{0}  •  {1}", h.name, h.locationType), warning);
                    GUILayout.Label(string.Format("Pression {0:0}%  |  visiteurs réels {1}  |  voies {2}", h.pressure01 * 100f, h.realVisitors, h.connectedRoads), label);
                    GUILayout.Label(h.recommendation, label);
                    GUILayout.EndVertical();
                    GUILayout.Space(4);
                }
                GUILayout.EndScrollView();
            }
            if (GUILayout.Button("Actualiser"))
            {
                if (AccessRoadTrafficRuntime.I != null) AccessRoadTrafficRuntime.I.Refresh();
                if (AccessIntersectionTrafficRuntime.I != null) AccessIntersectionTrafficRuntime.I.Refresh();
                if (AccessTrafficHotspotRuntime.I != null) AccessTrafficHotspotRuntime.I.Rebuild();
            }
            if (GUILayout.Button("Fermer (F9)")) visible = false;
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            label = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            warning = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
        }
    }
}
