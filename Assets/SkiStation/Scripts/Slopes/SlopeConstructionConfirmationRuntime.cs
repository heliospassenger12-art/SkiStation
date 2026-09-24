using UnityEngine;

namespace SkiStation
{
    // 1.99 - confirmation sheet between drawing and committing a piste.
    public sealed class SlopeConstructionConfirmationRuntime : MonoBehaviour
    {
        public static SlopeConstructionConfirmationRuntime I { get; private set; }
        public bool IsOpen { get; private set; }
        public SlopeDesignAssessment Assessment { get; private set; }
        public float EstimatedCost { get; private set; }
        public SlopeConstructionDifficulty Difficulty { get; private set; }
        SlopeConstructionMode owner;

        void Awake() { if (I != null && I != this) { Destroy(gameObject); return; } I = this; }
        public void Open(SlopeConstructionMode mode, SlopeDesignAssessment assessment, float cost, SlopeConstructionDifficulty difficulty)
        { owner = mode; Assessment = assessment; EstimatedCost = cost; Difficulty = difficulty; IsOpen = true; }
        public void Close() { IsOpen = false; owner = null; }

        void OnGUI()
        {
            if (!IsOpen) return;
            float w = Mathf.Min(440f, Screen.width - 32f), h = 310f;
            Rect r = new Rect((Screen.width-w)*.5f, (Screen.height-h)*.5f, w, h);
            GUI.Box(r, "CONFIRMER LA PISTE"); GUILayout.BeginArea(new Rect(r.x+18,r.y+34,r.width-36,r.height-50));
            GUILayout.Label("Difficulte choisie : " + Difficulty);
            GUILayout.Label("Distance : " + Assessment.length.ToString("N0") + " m");
            GUILayout.Label("Denivele : " + Assessment.verticalDrop.ToString("N0") + " m");
            GUILayout.Label("Pente moyenne : " + Assessment.averageSlope.ToString("0.0") + " deg");
            GUILayout.Label("Pente maximale : " + Assessment.maximumSlope.ToString("0.0") + " deg");
            GUILayout.Label("Difficulte conseillee : " + Assessment.recommendedDifficulty);
            GUILayout.Label("Qualite du trace : " + Assessment.score.ToString("0") + "/100");
            GUILayout.Space(8); GUILayout.Label("PRIX ESTIME : " + EstimatedCost.ToString("N0") + " EUR");
            GUILayout.Space(12); GUILayout.BeginHorizontal();
            if (GUILayout.Button("CONFIRMER", GUILayout.Height(38))) owner?.ConfirmPendingSlope();
            if (GUILayout.Button("MODIFIER", GUILayout.Height(38))) owner?.ResumePendingSlope();
            if (GUILayout.Button("ANNULER", GUILayout.Height(38))) owner?.CancelPendingSlope();
            GUILayout.EndHorizontal(); GUILayout.EndArea();
        }
    }
}
