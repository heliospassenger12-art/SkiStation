using UnityEngine;

namespace SkiStation
{
    public sealed class SlopeConditionUI : MonoBehaviour
    {
        private bool show;
        private Vector2 scroll;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5)) show = !show;
        }

        private void OnGUI()
        {
            if (!show) return;
            var slopes = Object.FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None);
            GUILayout.BeginArea(new Rect(Screen.width - 430, 10, 420, Mathf.Min(Screen.height - 20, 560)), GUI.skin.window);
            GUILayout.Label("PISTES — CONDITIONS & DAMAGE");
            GUILayout.Label("F5 : fermer • La neige, le trafic et le damage évoluent pendant la journée.");
            scroll = GUILayout.BeginScrollView(scroll);
            foreach (var slope in slopes)
            {
                if (slope == null) continue;
                var c = slope.GetComponent<SlopeConditionRuntime>();
                if (c == null) continue;
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label($"{slope.name}  —  {c.ConditionLabel}");
                GUILayout.Label($"Neige : {c.SnowQuality01:P0}   Épaisseur : {c.SnowDepthCm:0} cm");
                GUILayout.Label($"Damage : {c.GroomingProgress01:P0}   Trafic : {slope.TrafficCount}");
                GUILayout.Label($"État : {(slope.Open ? "OUVERTE" : "FERMÉE")}");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Dam­er", GUILayout.Width(110))) c.Groom(1f);
                if (GUILayout.Button("Gros damage", GUILayout.Width(110))) c.Groom(1.6f);
                if (GUILayout.Button("Fermer", GUILayout.Width(90))) slope.SetOpen(false);
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
