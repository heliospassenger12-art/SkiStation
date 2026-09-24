using UnityEngine;

namespace SkiStation
{
    /// <summary>Temporary readable UI. Final settings can call DayNightModeRuntime directly.</summary>
    public sealed class DayNightModeUI : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleKey = KeyCode.F12;
        [SerializeField] private bool visible = true;

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey)) DayNightModeRuntime.I?.Toggle();
        }

        private void OnGUI()
        {
            if (!LegacyUIPanelCoordinator.DiagnosticsVisible) return;
            if (!visible) return;
            var mode = DayNightModeRuntime.I != null
                ? DayNightModeRuntime.I.Mode
                : DayNightPresentationMode.ImmersiveDayNight;
            var clock = GameClock.I;
            string calendar = clock != null ? $"Jour {clock.CurrentDay} • {clock.GetTimeLabel()}" : "Calendrier actif";
            string presentation = mode == DayNightPresentationMode.TycoonDayOnly
                ? "TYCOON — JOUR CONTINU"
                : "IMMERSION — JOUR / NUIT";
            GUI.Box(new Rect(12, 92, 420, 74),
                presentation + "\n" + calendar + "\nF12 : changer • calendrier toujours actif");
        }
    }
}
