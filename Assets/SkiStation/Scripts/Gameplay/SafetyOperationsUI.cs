using UnityEngine;

namespace SkiStation
{
    public sealed class SafetyOperationsUI : MonoBehaviour
    {
        private bool open;
        private void Update() { if (Input.GetKeyDown(KeyCode.F7)) open = !open; }
        private void OnGUI()
        {
            if (!open || SafetyOperationsRuntime.I == null) return;
            var s = SafetyOperationsRuntime.I;
            GUILayout.BeginArea(new Rect(18, 210, 390, 330), GUI.skin.box);
            GUILayout.Label("SÉCURITÉ DU DOMAINE — F7");
            GUILayout.Label($"Équipes de pisteurs : {s.PatrolTeams}");
            GUILayout.Label($"Hélicoptères disponibles : {s.Helicopters}");
            GUILayout.Label($"Interventions en cours : {s.ActiveIncidents}");
            GUILayout.Label($"Interventions résolues aujourd'hui : {s.ResolvedToday}");
            GUILayout.Label($"Indice de sécurité : {s.SafetyScore01:P0}");
            foreach (var i in s.Incidents)
            {
                string heli = i.helicopterRequired ? " — HÉLICOPTÈRE" : "";
                GUILayout.Label($"• {i.Description} — {i.slope?.name}{heli}");
            }
            GUILayout.Space(8);
            GUILayout.Label("Les coussins de protection des pylônes sont automatiques : aucun entretien ni gestion requis.");
            GUILayout.Label("Mineure : 1 équipe • Modérée : 1 équipe • Importante : plusieurs équipes • Critique : hélicoptère possible");
            GUILayout.EndArea();
        }
    }
}
