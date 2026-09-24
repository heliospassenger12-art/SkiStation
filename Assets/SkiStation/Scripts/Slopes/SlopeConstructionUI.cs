using UnityEngine;

namespace SkiStation
{
    public sealed class SlopeConstructionUI : MonoBehaviour
    {
        void OnGUI()
        {
            var mode = SlopeConstructionMode.Instance;
            if (mode == null) return;
            GUILayout.BeginArea(new Rect(16, 16, 330, 145), GUI.skin.box);
            GUILayout.Label("SKISTATION — CONSTRUCTION DE PISTE");
            GUILayout.Label(mode.IsDrawing ? "MODE TRACE ACTIF" : "B pour tracer une piste");
            GUILayout.Label("Clic souris : ajouter des points");
            GUILayout.Label("[ / ] : largeur   Entrée : terminer   Échap : annuler");
            GUILayout.Label("Largeur variable : chaque point mémorise sa largeur");
            GUILayout.EndArea();
        }
    }
}
