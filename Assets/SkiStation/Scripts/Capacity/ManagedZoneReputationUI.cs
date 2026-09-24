using UnityEngine;

namespace SkiStation
{
    public sealed class ManagedZoneReputationUI : MonoBehaviour
    {
        private bool visible;
        private Vector2 scroll;
        private void Update() { if (Input.GetKeyDown(KeyCode.F4)) visible = !visible; }

        private void OnGUI()
        {
            if (!visible || ManagedZoneReputationRuntime.I == null) return;
            var runtime = ManagedZoneReputationRuntime.I;
            GUI.Box(new Rect(520, 100, 510, 470), "RÉPUTATION & ATTRACTIVITÉ — F4");
            GUI.Label(new Rect(540, 130, 450, 22), "Réputation de la station : " + Mathf.RoundToInt(runtime.StationReputation01 * 100f) + "% — " + runtime.StationReputationLabel);
            GUI.Label(new Rect(540, 153, 450, 34), "La réputation émerge de la spécialisation, de l'expérience, de la capacité et de la fluidité.");
            scroll = GUI.BeginScrollView(new Rect(535, 195, 475, 350), scroll, new Rect(0, 0, 445, Mathf.Max(320, runtime.Zones.Count * 105)));
            int y = 0;
            foreach (var z in runtime.Zones)
            {
                GUI.Box(new Rect(0, y, 440, 94), "");
                GUI.Label(new Rect(10, y + 7, 420, 20), z.name + " — " + z.type);
                GUI.Label(new Rect(10, y + 29, 420, 18), "Réputation : " + Mathf.RoundToInt(z.reputation01 * 100f) + "% — " + z.reputationLabel);
                GUI.Label(new Rect(10, y + 49, 420, 18), "Attractivité : " + Mathf.RoundToInt(z.attraction01 * 100f) + "% — " + z.attractionLabel);
                GUI.Label(new Rect(10, y + 69, 420, 18), "Visiteurs présents : " + z.visitorsInside);
                y += 105;
            }
            GUI.EndScrollView();
        }
    }
}
