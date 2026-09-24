using System.Linq;
using UnityEngine;

namespace SkiStation
{
    /// <summary>Small prototype management panel for validating the new slope personality loop.</summary>
    public sealed class SlopeGameplayDebugUI : MonoBehaviour
    {
        private Vector2 scroll;
        private GUIStyle header;

        private void OnGUI()
        {
            if (header == null)
            {
                header = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 15 };
            }

            var slopes = FindObjectsOfType<SlopeRuntime>()
                .Where(s => s != null)
                .OrderByDescending(s => s.PopularityScore)
                .ThenByDescending(s => s.Length)
                .Take(8)
                .ToList();

            GUILayout.BeginArea(new Rect(Screen.width - 390f, 12f, 378f, 310f), GUI.skin.box);
            GUILayout.Label("PISTES — personnalité & fréquentation", header);
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(275f));
            foreach (var slope in slopes)
            {
                GUILayout.Label(string.Format("{0}  | {1}  | {2:0} m  | {3:0.0}°",
                    slope.name, slope.Difficulty, slope.Length, slope.AverageAngle));
                GUILayout.Label(string.Format("   largeur {0:0.0} m  • trafic {1}  • descentes {2}  • popularité {3:0}",
                    slope.AverageWidth, slope.TrafficCount, slope.CompletedRuns, slope.PopularityScore));
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
