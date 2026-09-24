using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>Hides prototype OnGUI panels by default so they cannot stack over the playable view. F10 restores them for diagnostics.</summary>
    public sealed class LegacyUIPanelCoordinator : MonoBehaviour
    {
        private readonly List<MonoBehaviour> hidden = new List<MonoBehaviour>();
        private bool legacyVisible;
        public static bool DiagnosticsVisible { get; private set; }

        private void Start() { RefreshAndHide(); }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.F10)) return;
            legacyVisible = !legacyVisible;
            DiagnosticsVisible = legacyVisible;
            if (legacyVisible) SetHidden(true);
            else RefreshAndHide();
        }

        private void RefreshAndHide()
        {
            hidden.Clear();
            var all = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var behaviour in all)
            {
                if (behaviour == null || behaviour == this || behaviour is PlayableGameHUD || behaviour is DomainMapUI) continue;
                Type t = behaviour.GetType();
                string n = t.Name;
                if (!LooksLikeLegacyPanel(n)) continue;
                hidden.Add(behaviour);
                behaviour.enabled = false;
            }
            legacyVisible = false;
            DiagnosticsVisible = false;
        }

        private void SetHidden(bool enabled)
        {
            for (int i = hidden.Count - 1; i >= 0; i--)
            {
                if (hidden[i] == null) { hidden.RemoveAt(i); continue; }
                hidden[i].enabled = enabled;
            }
        }

        private static bool LooksLikeLegacyPanel(string n)
        {
            if (n == "SlopeConstructionProjectUI") return true;
            if (!n.EndsWith("UI", StringComparison.Ordinal)) return false;
            // Construction input components are not UI panels and therefore are unaffected.
            return n.Contains("Debug") || n.Contains("Management") || n.Contains("Operations") ||
                   n.Contains("Condition") || n.Contains("Reputation") || n.Contains("Specialization") ||
                   n.Contains("Sector") || n.Contains("Safety") || n.Contains("Grooming") ||
                   n.Contains("School") || n.Contains("Lesson") || n.Contains("Weather") ||
                   n.Contains("TerrainWork") || n.Contains("Flow") || n.Contains("Traffic") ||
                   n.Contains("Maintenance") || n.Contains("Profile") || n.Contains("Opening");
        }
    }
}
