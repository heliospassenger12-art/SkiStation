using UnityEngine;

namespace SkiStation
{
    public sealed class SlopeTerrainWorkPlannerUI : MonoBehaviour
    {
        [SerializeField] SlopeTerrainWorkPlannerMode mode;
        public void Bind(SlopeTerrainWorkPlannerMode value) { mode = value; }
        void OnGUI()
        {
            if (!mode) return;
            GUILayout.BeginArea(new Rect(16, Screen.height - 145, 430, 125), GUI.skin.box);
            GUILayout.Label("TRAVAUX DE PISTE");
            GUILayout.Label("P : analyser la piste sous la souris | Entrée : appliquer | Échap : annuler");
            var p = mode.ActivePlanner;
            if (p != null && p.HasPlan)
                GUILayout.Label($"Travaux estimés : {p.WorkPointCount} zones | intensité max : {p.MaxRequiredWork:0.0} m | indice volume : {p.EstimatedVolumeIndex:0}");
            else GUILayout.Label("Aucune analyse active.");
            GUILayout.EndArea();
        }
    }
}
