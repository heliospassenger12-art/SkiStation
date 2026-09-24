using UnityEngine;
using SkiStation;

/// <summary>2.00 - universal recap/confirmation step for lift construction.</summary>
public sealed class LiftConstructionConfirmationRuntime : MonoBehaviour
{
    public static LiftConstructionConfirmationRuntime I { get; private set; }
    public bool IsOpen { get; private set; }
    LiftConstructionPlanner owner;

    void Awake() { if (I != null && I != this) { Destroy(gameObject); return; } I = this; }
    public static LiftConstructionConfirmationRuntime Ensure()
    {
        if (I != null) return I;
        var existing = Object.FindFirstObjectByType<LiftConstructionConfirmationRuntime>();
        if (existing != null) return existing;
        return new GameObject("Lift Construction Confirmation 2.00").AddComponent<LiftConstructionConfirmationRuntime>();
    }
    public void Open(LiftConstructionPlanner planner) { owner = planner; IsOpen = planner != null; }
    public void Close() { IsOpen = false; owner = null; }

    void OnGUI()
    {
        if (!IsOpen || owner == null) return;
        float w = Mathf.Min(470f, Screen.width - 32f), h = 350f;
        Rect r = new Rect((Screen.width-w)*.5f, (Screen.height-h)*.5f, w, h);
        GUI.Box(r, "CONFIRMER LA REMONTEE");
        GUILayout.BeginArea(new Rect(r.x+18,r.y+34,r.width-36,r.height-50));
        GUILayout.Label("Type : " + owner.PendingDisplayName);
        GUILayout.Label("Longueur : " + owner.PendingLength.ToString("N0") + " m");
        GUILayout.Label("Denivele : " + owner.PendingVerticalRise.ToString("N0") + " m");
        GUILayout.Label("Pente moyenne : " + owner.PendingAverageSlope.ToString("0.0") + " deg");
        GUILayout.Label("Vitesse : " + owner.PendingSpeedKmh.ToString("0.0") + " km/h");
        GUILayout.Label("Capacite theorique : " + owner.PendingCapacityPerHour.ToString("N0") + " pers/h");
        GUILayout.Label("Mode pylones : " + owner.PlacementMode);
        GUILayout.Label("Pylones prevus : " + owner.PendingPylonCount);
        GUILayout.Space(8);
        GUILayout.Label("PRIX ESTIME : " + owner.PendingCost.ToString("N0") + " EUR");
        if (!string.IsNullOrEmpty(owner.PendingRestriction)) GUILayout.Label("ATTENTION : " + owner.PendingRestriction);
        GUILayout.Space(12); GUILayout.BeginHorizontal();
        GUI.enabled = string.IsNullOrEmpty(owner.PendingRestriction);
        if (GUILayout.Button("CONFIRMER", GUILayout.Height(38))) owner.ConfirmPendingLift();
        GUI.enabled = true;
        if (GUILayout.Button("MODIFIER", GUILayout.Height(38))) owner.ResumePendingLift();
        if (GUILayout.Button("ANNULER", GUILayout.Height(38))) owner.CancelPendingLift();
        GUILayout.EndHorizontal(); GUILayout.EndArea();
    }
}
