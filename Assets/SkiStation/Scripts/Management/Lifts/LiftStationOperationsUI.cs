using UnityEngine;

namespace SkiStation
{
    /// <summary>0.78 specialist station panel. Read-only: operational commands stay in existing lift controls.</summary>
    public sealed class LiftStationOperationsUI : MonoBehaviour
    {
        public bool Visible { get; private set; }
        private LiftStationOperationsRuntime selected;
        private Rect window = new Rect(340, 80, 390, 280);

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F7)) Visible = !Visible;
            if (!Visible) return;
            if (LiftManagementRuntime.I != null && LiftManagementRuntime.I.SelectedLift != null)
                selected = LiftStationOperationsRuntime.Find(LiftManagementRuntime.I.SelectedLift);
        }

        private void OnGUI()
        {
            if (!Visible) return;
            window = GUI.Window(GetInstanceID(), window, DrawWindow, "GESTION STATIONS — REMONTÉE");
        }

        private void DrawWindow(int id)
        {
            if (selected == null)
            {
                GUILayout.Label("Sélectionnez une remontée dans l'écran F4.");
            }
            else
            {
                var s = selected.Snapshot;
                GUILayout.Label("Remontée : " + s.liftName);
                GUILayout.Label("État : " + s.state);
                GUILayout.Space(6);
                GUILayout.Label("Aval : " + (s.bottomReady ? "station opérationnelle" : "indisponible"));
                GUILayout.Label("Amont : " + (s.topReady ? "station opérationnelle" : "indisponible"));
                GUILayout.Label("File aval : " + s.queue);
                GUILayout.Label("Passagers sur la ligne : " + s.passengersOnLine);
                GUILayout.Label("Capacité / véhicule : " + s.capacityPerVehicle);
                GUILayout.Label("Vitesse effective : " + s.effectiveSpeedKmh.ToString("0.0") + " km/h");
                GUILayout.Label("Trajet : " + s.travelMinutes.ToString("0.0") + " min");
            }
            if (GUILayout.Button("Actualiser")) selected?.Rebuild();
            GUI.DragWindow();
        }
    }
}
