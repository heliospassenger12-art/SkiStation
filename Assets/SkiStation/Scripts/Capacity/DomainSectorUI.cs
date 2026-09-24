using UnityEngine;

namespace SkiStation
{
    public sealed class DomainSectorUI : MonoBehaviour
    {
        private bool visible;
        private Vector2 scroll;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F6)) visible = !visible;
        }

        private void OnGUI()
        {
            if (!visible || DomainSectorRuntime.I == null) return;
            GUI.Box(new Rect(18, 100, 520, 430), "SECTEURS DU DOMAINE — F6");
            scroll = GUI.BeginScrollView(new Rect(30, 135, 495, 380), scroll, new Rect(0, 0, 460, Mathf.Max(350, DomainSectorRuntime.I.Sectors.Count * 92)));
            int y = 0;
            foreach (var s in DomainSectorRuntime.I.Sectors)
            {
                GUI.Box(new Rect(0, y, 450, 82), "");
                GUI.Label(new Rect(12, y + 6, 430, 20), s.name + " — " + s.status);
                GUI.Label(new Rect(12, y + 27, 430, 18), "Pistes : " + s.slopes + "   Remontées : " + s.lifts);
                GUI.Label(new Rect(12, y + 46, 430, 18), "Capacité : " + Mathf.RoundToInt(s.effectiveCapacityPerHour) + "/h   Charge : " + Mathf.RoundToInt(s.load01 * 100f) + "%");
                GUI.Label(new Rect(250, y + 46, 190, 18), "Goulot : " + s.bottleneck);
                y += 92;
            }
            GUI.EndScrollView();
        }
    }
}
