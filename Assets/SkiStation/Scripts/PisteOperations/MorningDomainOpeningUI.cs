using UnityEngine;
namespace SkiStation.PisteOperations
{
    public sealed class MorningDomainOpeningUI : MonoBehaviour
    {
        void OnGUI()
        {
            var r = MorningDomainOpeningRuntime.I;
            if (r == null) return;
            GUI.Box(new Rect(Screen.width - 390, 10, 375, 108), "OUVERTURE DU DOMAINE");
            GUI.Label(new Rect(Screen.width - 378, 36, 350, 22), r.LastReport);
            GUI.Label(new Rect(Screen.width - 378, 58, 350, 22), "État : " + (r.DomainReadiness >= 70f ? "Prêt à accueillir les skieurs" : "Préparation incomplète"));
            if (GUI.Button(new Rect(Screen.width - 378, 82, 160, 24), "Réévaluer")) r.ForceRecheck();
        }
    }
}
