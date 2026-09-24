using UnityEngine;

namespace SkiStation
{
    public sealed class ManagedZoneSpecializationUI : MonoBehaviour
    {
        private bool visible;
        private Vector2 scroll;

        private void Update() { if (Input.GetKeyDown(KeyCode.F5)) visible = !visible; }

        private void OnGUI()
        {
            if (!visible || ManagedZoneRuntime.I == null) return;
            GUI.Box(new Rect(560, 100, 470, 430), "SPÉCIALISATION DES ZONES — F5");
            var zones = ManagedZoneRuntime.I.Zones;
            scroll = GUI.BeginScrollView(new Rect(572, 135, 446, 380), scroll, new Rect(0, 0, 420, Mathf.Max(350, zones.Count * 112)));
            int y = 0;
            foreach (var z in zones)
            {
                GUI.Box(new Rect(0, y, 410, 102), "");
                GUI.Label(new Rect(10, y + 7, 390, 20), z.name + " — " + z.specializationLabel);
                GUI.Label(new Rect(10, y + 29, 390, 18), "Spécialisation : " + Mathf.RoundToInt(z.specializationScore01 * 100f) + "%");
                GUI.Label(new Rect(10, y + 49, 390, 18), "Public dominant : " + z.dominantVisitorProfile);
                GUI.Label(new Rect(10, y + 69, 390, 18), "Débutants/Jeunes : " + z.beginnerVisitors + "   Familles : " + z.familyVisitors + "   Sportifs : " + z.sportVisitors);
                y += 112;
            }
            GUI.EndScrollView();
        }
    }
}
