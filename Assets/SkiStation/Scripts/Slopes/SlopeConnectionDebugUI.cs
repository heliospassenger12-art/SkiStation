using UnityEngine;

namespace SkiStation
{
    public sealed class SlopeConnectionDebugUI : MonoBehaviour
    {
        public KeyCode reconnectKey = KeyCode.F9;
        public KeyCode toggleKey = KeyCode.F10;
        public bool visible = true;
        private GUIStyle style;

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey)) visible = !visible;
            if (Input.GetKeyDown(reconnectKey) && ResortSlopeGraph.I != null)
                ResortSlopeGraph.I.AutoConnectAll();
        }

        private void OnGUI()
        {
            if (!visible || SlopeNetwork.I == null) return;
            if (style == null) { style = new GUIStyle(GUI.skin.box); style.fontSize = 14; }
            int links = 0;
            for (int i = 0; i < SlopeNetwork.I.Slopes.Count; i++)
            {
                var s = SlopeNetwork.I.Slopes[i];
                var c = s == null ? null : s.GetComponent<SlopeConnectionRuntime>();
                if (c != null) { if (c.Top.IsValid) links++; if (c.Bottom.IsValid) links++; }
            }
            GUI.Box(new Rect(12, 12, 290, 62), "Réseau des pistes\nPistes : " + SlopeNetwork.I.Slopes.Count + "   Connexions : " + links + "\nF9 : reconnecter   F10 : afficher/masquer", style);
        }
    }
}
