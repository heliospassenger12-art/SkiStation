using UnityEngine;

namespace SkiStation.PisteOperations
{
    public class GroomingGarageUI : MonoBehaviour
    {
        bool show;
        void Update() { if (Input.GetKeyDown(KeyCode.F6)) show = !show; }

        void OnGUI()
        {
            if (!show) return;
            var garage = GroomingGarageRuntime.I;
            var fleet = GroomingFleetRuntime.I;
            var team = GroomingTeamRuntime.I;
            if (garage == null || fleet == null) return;

            GUI.Box(new Rect(455, 270, 420, 300), "GARAGE & ÉQUIPES — F6");
            GUI.Label(new Rect(470, 300, 390, 22), $"Garage : capacité {garage.Capacity} dameuses");
            GUI.Label(new Rect(470, 324, 390, 22), $"Flotte : {fleet.Vehicles.Count}   Conducteurs : {(team != null ? team.AvailableDrivers : 0)}");
            GUI.Label(new Rect(470, 348, 390, 22), $"Équipes : {(team != null ? team.teamCount : 0)}   Efficacité : {(team != null ? team.EfficiencyMultiplier : 1f):0.00}x");
            GUI.Label(new Rect(470, 372, 390, 22), "Carburant : non simulé — gestion volontairement simplifiée");

            int y = 405;
            int i = 1;
            foreach (var v in fleet.Vehicles)
            {
                if (v == null) continue;
                string state = v.working ? "EN DAMAGE" : (v.returning ? "RETOUR GARAGE" : "PRÊTE");
                GUI.Label(new Rect(470, y, 390, 20), $"Dameuse {i:00}  •  {state}  {(v.target != null ? v.target.name : "")}");
                y += 21;
                i++;
                if (y > 545) break;
            }
        }
    }
}
