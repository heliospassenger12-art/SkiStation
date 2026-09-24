using UnityEngine;

namespace SkiStation
{
    public sealed class SkiLessonProgressUI : MonoBehaviour
    {
        private bool visible;
        private void Update() { if (Input.GetKeyDown(KeyCode.F10)) visible = !visible; }
        private void OnGUI()
        {
            if (!visible) return;
            GUI.Box(new Rect(Screen.width - 430, 270, 420, 190), "Progression école de ski — F10");
            float y = 298f;
            int shown = 0;
            foreach (var skier in FindObjectsOfType<SkierRuntime>())
            {
                if (skier == null || skier.GetComponent<SkiLessonProgressRuntime>() == null) continue;
                var p = skier.GetComponent<SkiLessonProgressRuntime>();
                var profile = skier.PopulationProfile;
                string name = profile != null ? profile.DisplayName : "Élève";
                GUI.Label(new Rect(Screen.width - 415, y, 390, 22), $"{name} — Badge : {p.CurrentBadgeName} — {p.LessonsCompleted} cours — {p.Skill01:P0}");
                if (profile != null && profile.IsChild && p.PiouPiouGraduated)
                    GUI.Label(new Rect(Screen.width - 415, y + 18, 390, 22), "→ Prêt à tester les zones débutants du front de neige");
                y += profile != null && profile.IsChild && p.PiouPiouGraduated ? 40 : 24;
                if (++shown >= 7) break;
            }
            if (shown == 0) GUI.Label(new Rect(Screen.width - 415, y, 390, 22), "Aucun élève suivi pour le moment.");
        }
    }
}
