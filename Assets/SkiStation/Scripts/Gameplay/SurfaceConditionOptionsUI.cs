using UnityEngine;

namespace SkiStation
{
    /// <summary>Simple optional-settings panel. F8 opens/closes it.</summary>
    public sealed class SurfaceConditionOptionsUI : MonoBehaviour
    {
        private bool visible;
        private GUIStyle header;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8)) visible = !visible;
        }

        private void OnGUI()
        {
            if (!visible || SurfaceConditionOptionsRuntime.I == null) return;
            if (header == null) header = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 16 };

            GUILayout.BeginArea(new Rect(Screen.width * .5f - 190f, 80f, 380f, 220f), GUI.skin.box);
            GUILayout.Label("OPTIONS — CONDITIONS DE NEIGE", header);
            GUILayout.Space(8f);
            bool advanced = SurfaceConditionOptionsRuntime.I.AdvancedSurfaceConditions;
            GUILayout.Label("Mode actuel : " + SurfaceConditionOptionsRuntime.I.ModeLabel);
            GUILayout.Label("Les détails verglas / neige lourde sont optionnels.");
            GUILayout.Space(8f);
            if (GUILayout.Button(advanced ? "✓ Conditions réalistes" : "Activer les conditions réalistes", GUILayout.Height(38f)))
                SurfaceConditionOptionsRuntime.I.SetAdvancedSurfaceConditions(!advanced);
            GUILayout.Space(6f);
            GUILayout.Label("Mode simplifié : météo visuelle sans pénalité\ncomplexe liée au verglas ou à la neige 'soupe'.");
            GUILayout.Label("F8 : fermer");
            GUILayout.EndArea();
        }
    }
}
