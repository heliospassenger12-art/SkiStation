using UnityEngine;

namespace SkiStation
{
    /// <summary>0.99: lightweight diagnostic display. F11 toggles it.</summary>
    public sealed class SkiDomainCirculationDebugUI : MonoBehaviour
    {
        public KeyCode toggleKey = KeyCode.F11;
        public bool visible = true;
        private GUIStyle style;

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey)) visible = !visible;
        }

        private void OnGUI()
        {
            if (!visible || SkiDomainCirculationRuntime.I == null) return;
            if (style == null) { style = new GUIStyle(GUI.skin.box); style.fontSize = 14; }
            var s = SkiDomainCirculationRuntime.I.Snapshot;
            GUI.Box(new Rect(12, 82, 310, 82),
                "Circulation montagne 0.99\n" +
                "Pistes ouvertes : " + s.openSlopes + "   Connectées : " + s.connectedSlopes + "\n" +
                "Terminaux : " + s.terminalSlopes + "   Skieurs réels : " + s.activeSkiers + "\n" +
                "Densité moyenne : " + s.averageTrafficDensity.ToString("0.000"), style);
        }
    }
}
