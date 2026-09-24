using UnityEngine;

namespace SkiStation
{
    public sealed class SkiStationDiagnosticsUI : MonoBehaviour
    {
        [SerializeField] private bool visibleOnStart;
        private bool visible;
        private Vector2 scroll;

        public void Configure(bool startVisible) => visible = startVisible;

        private void Awake() => visible = visibleOnStart;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) visible = !visible;
            if (Input.GetKeyDown(KeyCode.F2) && SkiStationDiagnosticsRuntime.I != null)
                SkiStationDiagnosticsRuntime.I.RunStartupDiagnostics();
        }

        private void OnGUI()
        {
            if (!visible || SkiStationDiagnosticsRuntime.I == null) return;

            float width = Mathf.Min(720f, Screen.width - 32f);
            float height = Mathf.Min(520f, Screen.height - 32f);
            GUILayout.BeginArea(new Rect(16f, 16f, width, height), GUI.skin.window);
            GUILayout.Label("SkiStation — Diagnostic runtime 1.06");
            GUILayout.Label("F1 : afficher/masquer    F2 : relancer les diagnostics");

            var runtime = SkiStationDiagnosticsRuntime.I;
            GUILayout.Label(runtime.HasErrors ? "État : ERREUR" : runtime.HasWarnings ? "État : AVERTISSEMENT" : "État : OK");
            scroll = GUILayout.BeginScrollView(scroll);
            foreach (var entry in runtime.Entries)
                GUILayout.Label($"[{entry.severity}] {entry.code} — {entry.message}");
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
