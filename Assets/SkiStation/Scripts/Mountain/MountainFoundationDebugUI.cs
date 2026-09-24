using UnityEngine;

namespace SkiStation
{
    public sealed class MountainFoundationDebugUI : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleKey = KeyCode.F3;
        private bool visible;
        private void Update() { if (Input.GetKeyDown(toggleKey)) visible = !visible; }
        private void OnGUI()
        {
            if (!visible || MountainRuntime.I == null) return;
            var m = MountainRuntime.I;
            Vector3 p = Camera.main ? Camera.main.transform.position : Vector3.zero;
            MountainSample s = m.Sample(p);
            GUI.Box(new Rect(12, 12, 330, 125), "SKISTATION — MOUNTAIN FOUNDATION");
            GUI.Label(new Rect(24, 40, 300, 20), "Generated: " + m.IsGenerated);
            GUI.Label(new Rect(24, 62, 300, 20), "Resolution: " + m.GeneratedResolution);
            GUI.Label(new Rect(24, 84, 300, 20), "Altitude: " + s.altitude.ToString("0") + " m");
            GUI.Label(new Rect(24, 106, 300, 20), "Slope: " + s.slopeAngle.ToString("0.0") + "°  Zone: " + s.zone);
        }
    }
}
