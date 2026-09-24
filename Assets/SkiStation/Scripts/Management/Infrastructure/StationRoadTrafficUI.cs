using UnityEngine;
namespace SkiStation
{
    public sealed class StationRoadTrafficUI : MonoBehaviour
    {
        private void OnGUI()
        {
            if (StationRoadTrafficRuntime.I == null) return;
            var s = StationRoadTrafficRuntime.I.Summary;
            GUI.Box(new Rect(12, 12, 340, 190), "TRAFIC ROUTIER — 0.94");
            GUI.Label(new Rect(24, 42, 320, 22), "Véhicules réels : " + s.activeVehicles);
            GUI.Label(new Rect(24, 64, 320, 22), "Ralentis : " + s.slowingVehicles + "   Attente : " + s.waitingVehicles);
            GUI.Label(new Rect(24, 86, 320, 22), "Bouchons : " + s.jammedVehicles + "   Croisements : " + s.crossingVehicles);
            GUI.Label(new Rect(24, 108, 320, 22), "Stops : " + s.stoppedVehicles + "   Cédez : " + s.yieldingVehicles);
            GUI.Label(new Rect(24, 130, 320, 22), "Routes saturées : " + s.saturatedRoads);
            GUI.Label(new Rect(24, 152, 320, 22), "Vitesse moy. : " + s.averageSpeedMetersPerSecond.ToString("0.0") + " m/s");
            GUI.Label(new Rect(24, 174, 320, 22), "File dépose-minute : " + s.dropOffQueue);
        }
    }
}
