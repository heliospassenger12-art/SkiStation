using UnityEngine;

namespace SkiStation
{
    /// <summary>Lightweight management/debug panel for the 1.30 service network. Replaceable by the final UI later.</summary>
    public sealed class StationServiceManagementUI : MonoBehaviour
    {
        private bool visible = true;
        private int selected = -1;
        private StationServiceType buildType = StationServiceType.Restaurant;
        private ServiceRange buildRange = ServiceRange.Standard;
        private Vector2 scroll;

        private void OnGUI()
        {
            if (!visible) { if (GUI.Button(new Rect(10, 10, 150, 28), "Services (1.30)")) visible = true; return; }
            GUILayout.BeginArea(new Rect(10, 10, 430, Mathf.Min(Screen.height - 20, 720)), GUI.skin.box);
            GUILayout.BeginHorizontal(); GUILayout.Label("SERVICES & COMMERCES", GUILayout.Width(250)); if (GUILayout.Button("×", GUILayout.Width(30))) visible = false; GUILayout.EndHorizontal();
            GUILayout.Label("Réseau réel : capacité • files • personnel • prix • qualité");
            GUILayout.Space(4);
            if (StationServiceConstructionRuntime.I != null)
            {
                GUILayout.BeginHorizontal(); buildType = (StationServiceType)GUILayout.SelectionGrid((int)buildType, new[] { "Shop", "Restaurant", "Bar", "Rental", "School", "Activity", "Hotel" }, 4); GUILayout.EndHorizontal();
                buildRange = (ServiceRange)GUILayout.Toolbar((int)buildRange, new[] { "Eco", "Std", "Premium", "Luxury", "Pro", "Expert" });
                GUILayout.Label("Coût : " + StationServiceConstructionRuntime.I.GetBuildCost(buildType, buildRange).ToString("N0") + " €");
                if (GUILayout.Button("Construire ici (origine de la scène)")) StationServiceConstructionRuntime.I.Build(buildType, Vector3.zero, buildRange);
            }
            GUILayout.Space(8);
            scroll = GUILayout.BeginScrollView(scroll);
            for (int i = 0; i < StationServiceRuntime.All.Count; i++)
            {
                var s = StationServiceRuntime.All[i]; if (s == null || s.definition == null) continue;
                bool isSel = selected == i;
                if (GUILayout.Button((isSel ? "▶ " : "") + s.definition.serviceName + " — " + s.definition.type)) selected = i;
                if (isSel)
                {
                    var d = s.definition; var st = s.Stats;
                    GUILayout.Label($"Ouvert : {s.IsOpen()}   Capacité : {s.ActiveCustomers}/{d.capacity}   File : {s.QueueCount}/{d.maxQueue}");
                    GUILayout.Label($"Prix : {d.basePrice:0.##} €   Qualité : {d.quality:0}   Personnel : {d.staffAvailable}/{d.staffRequired}");
                    GUILayout.Label($"Servis : {st.served}   Refus : {st.refused}   CA : {st.revenue:0.##} €   Pic file : {st.queuePeak}");
                    GUILayout.Label($"Attente cumulée : {st.waitingMinutes:0.0} min   Satisfaction service : {(st.satisfactionSamples > 0 ? st.satisfaction / st.satisfactionSamples : d.quality):0.0}");
                }
            }
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
    }
}
