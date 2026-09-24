using UnityEngine;

namespace SkiStation.Arcade
{
    // Optional diagnostics for the first real Unity playtest. No management/visitor dependencies.
    public sealed class ArcadePlaytestDiagnostics : MonoBehaviour
    {
        public ArcadeSkiController skier;
        public Camera followCamera;
        public bool showDiagnostics;
        private string lastEvent = "En attente de la premiere figure";
        private float lastEventAt;
        private GUIStyle label;

        private void Start()
        {
            if (skier == null) return;
            skier.Landed += OnLanded;
            skier.Crashed += OnCrashed;
            skier.Retried += OnRetried;
            skier.TrickLanded += OnTrick;
        }
        private void OnDisable()
        {
            if (skier == null) return;
            skier.Landed -= OnLanded;
            skier.Crashed -= OnCrashed;
            skier.Retried -= OnRetried;
            skier.TrickLanded -= OnTrick;
        }
        private void OnLanded(float impact) { Record("Reception, impact " + impact.ToString("F1")); }
        private void OnCrashed() { Record("Figure ratee : R pour recommencer"); }
        private void OnRetried() { Record("Reprise instantanee"); }
        private void OnTrick(string trick) { Record("Figure plaquee : " + trick); }
        private void Record(string message) { lastEvent = message; lastEventAt = Time.unscaledTime; Debug.Log("[Arcade 2.42] " + message); }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) showDiagnostics = !showDiagnostics;
            if (showDiagnostics && skier != null && !float.IsNaN(skier.Speed) && skier.transform.position.y < -1000f)
                Debug.LogWarning("[Arcade 2.42] Skieur sous le terrain : verifier le collider et la position de depart.");
        }
        private void OnGUI()
        {
            if (!showDiagnostics) return;
            if (label == null) { label = new GUIStyle(GUI.skin.label); label.fontSize = 14; label.normal.textColor = Color.white; }
            GUI.BeginGroup(new Rect(0f, 140f, Screen.width, Screen.height - 140f));
            GUI.Box(new Rect(12f, 70f, 355f, 205f), "Arcade 2.42 - Diagnostic (F1 pour masquer)");
            if (skier == null) { GUI.Label(new Rect(22f, 100f, 330f, 35f), "Skieur non assigne", label); GUI.EndGroup(); return; }
            string mode = skier.IsCrashed ? "CHUTE" : skier.IsAirborne ? "EN L'AIR" : "AU SOL";
            string boost = skier.UnlimitedBoostUnlocked
                ? "Illimite : " + skier.unlimitedBoostTimeRemaining.ToString("F1") + " s"
                : "Jauge : " + (skier.BoostFraction * 100f).ToString("F0") + "%";
            GUI.Label(new Rect(22f, 100f, 335f, 22f), "Etat : " + mode + " | Vitesse : " + skier.Speed.ToString("F1"), label);
            GUI.Label(new Rect(22f, 125f, 335f, 22f), "Boost : " + boost + (skier.IsBoosting ? " (ACTIF)" : ""), label);
            GUI.Label(new Rect(22f, 150f, 335f, 22f), "Combo : " + skier.ComboCount + " | Camera : " + (followCamera != null && followCamera.enabled ? "OK" : "a verifier"), label);
            GUI.Label(new Rect(22f, 175f, 335f, 22f), "Dernier evenement : " + lastEvent, label);
            GUI.Label(new Rect(22f, 210f, 335f, 42f), "Reception : ecart " + skier.LastLandingAngleError.ToString("F1")
                + " / 20 deg | " + (skier.IsSwitch ? "SWITCH" : "FACE"), label);
            GUI.EndGroup();
        }
    }
}
