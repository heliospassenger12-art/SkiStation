using UnityEngine;

namespace SkiStation
{
    public sealed class SlopeConstructionProjectUI : MonoBehaviour
    {
        void OnGUI()
        {
            var manager = SlopeConstructionProjectManager.I;
            var policy = ConstructionPolicyRuntime.I;
            if (manager == null && policy == null) return;
            GUILayout.BeginArea(new Rect(16, Screen.height - 190, 430, 170), GUI.skin.box);
            GUILayout.Label("SKISTATION — CHANTIER & QUALITÉ");
            GUILayout.Label("Construction immédiate : " + ((policy != null && policy.InstantConstruction) ? "OUI" : "NON") + " (I pour basculer)");
            GUILayout.Label("J : investissement Économie → Standard → Premium");
            if (manager != null && manager.ActiveProject != null)
            {
                var p = manager.ActiveProject;
                var quality = p.Slope != null ? p.Slope.GetComponent<SlopeConstructionQualityRuntime>() : null;
                GUILayout.Label(p.State == SlopeConstructionProjectState.Building ? "Travaux en cours : " + Mathf.RoundToInt(p.Progress01 * 100f) + "%" : "Chantier terminé");
                GUILayout.Label("Coût : " + Mathf.RoundToInt(p.Cost).ToString("N0") + " €");
                GUILayout.Label("Durée estimée : " + Mathf.RoundToInt(p.DurationGameMinutes) + " min de jeu");
                if (quality != null && quality.Evaluated)
                    GUILayout.Label("Qualité : " + quality.GetTierLabel() + " — " + Mathf.RoundToInt(quality.QualityScore) + "/100");
            }
            else GUILayout.Label("K : activer le lancement de chantier puis cliquer une piste.");
            GUILayout.EndArea();
        }
    }
}
