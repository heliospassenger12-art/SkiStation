using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>0.89: management view of directional access traffic.</summary>
    public sealed class AccessFlowDirectionUI : MonoBehaviour
    {
        private bool visible; private Vector2 scroll; private GUIStyle title, label, hot;
        private void Update() { if (Input.GetKeyDown(KeyCode.F10)) visible = !visible; }
        private void OnGUI()
        {
            if (!visible) return; Styles();
            GUILayout.BeginArea(new Rect(610, 30, 610, 520), GUI.skin.box);
            GUILayout.Label("CIRCULATION — FLUX & SENS", title);
            GUILayout.Label("Analyse basée uniquement sur les visiteurs réels.", label);
            var rt = AccessFlowDirectionRuntime.I;
            if (rt == null || rt.Snapshots.Count == 0) GUILayout.Label("Aucune voie piétonne observée.", label);
            else
            {
                scroll = GUILayout.BeginScrollView(scroll);
                foreach (var kv in rt.Snapshots)
                {
                    var s = kv.Value;
                    GUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.Label((s.isBottleneck ? "⚠ " : "") + s.name + "  •  " + s.configuredDirection, s.isBottleneck ? hot : label);
                    GUILayout.Label(string.Format("Total {0} | sens + {1} | sens - {2} | pression {3:0}% | délai x{4:0.0}", s.totalVisitors, s.forwardVisitors, s.reverseVisitors, s.pressure01*100f, s.delayMultiplier), label);
                    GUILayout.Label("Déséquilibre directionnel : " + (s.directionalImbalance01*100f).ToString("0") + "%", label);
                    GUILayout.EndVertical();
                }
                GUILayout.EndScrollView();
            }
            if (GUILayout.Button("Actualiser")) rt?.Refresh();
            if (GUILayout.Button("Fermer (F10)")) visible = false;
            GUILayout.EndArea();
        }
        private void Styles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label){fontSize=20,fontStyle=FontStyle.Bold};
            label = new GUIStyle(GUI.skin.label){fontSize=13};
            hot = new GUIStyle(GUI.skin.label){fontSize=14,fontStyle=FontStyle.Bold};
        }
    }
}
