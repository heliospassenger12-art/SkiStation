using UnityEngine;

namespace SkiStation
{
    public sealed class SkiSchoolUI : MonoBehaviour
    {
        private bool visible;
        private void Update() { if (Input.GetKeyDown(KeyCode.F9)) visible = !visible; }
        private void OnGUI()
        {
            if (!visible) return;
            GUI.Box(new Rect(Screen.width - 410, 10, 400, 250), "Écoles de ski — F9");
            float y = 38f;
            if (SkiSchoolRuntime.All.Count == 0) { GUI.Label(new Rect(Screen.width - 395, y, 370, 22), "Aucune école de ski construite."); return; }
            foreach (var school in SkiSchoolRuntime.All)
            {
                if (school == null) continue;
                var s = school.GetSnapshot();
                GUI.Label(new Rect(Screen.width - 395, y, 370, 22), $"{s.serviceName} — {(s.zoneType == SkiSchoolZoneType.PiouPiou ? "Piou-Piou" : "Adultes / tout âge")}"); y += 22;
                GUI.Label(new Rect(Screen.width - 395, y, 370, 22), $"Cours : {s.activeLessons} | Individuels {s.individualLessons} | Collectifs {s.groupLessons}"); y += 22;
                GUI.Label(new Rect(Screen.width - 395, y, 370, 22), $"Élèves : {s.activeStudents} | Enfants {s.childrenLessons} | Adultes {s.adultLessons}"); y += 25;
            }
            GUI.Label(new Rect(Screen.width - 395, y, 370, 22), "Piou-Piou : réservé aux enfants. Adultes débutants → zones débutants.");
        }
    }
}
