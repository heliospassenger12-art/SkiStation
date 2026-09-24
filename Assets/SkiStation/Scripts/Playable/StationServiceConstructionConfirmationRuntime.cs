using UnityEngine;

namespace SkiStation
{
    /// <summary>2.01 - recap before committing any service/building construction.</summary>
    public sealed class StationServiceConstructionConfirmationRuntime : MonoBehaviour
    {
        public static StationServiceConstructionConfirmationRuntime I { get; private set; }
        public bool IsOpen { get; private set; }
        private StationServiceConstructionTool owner;
        private Vector3 position;

        private void Awake() { if (I != null && I != this) { Destroy(gameObject); return; } I = this; }
        public static StationServiceConstructionConfirmationRuntime Ensure()
        {
            if (I != null) return I;
            var existing = Object.FindFirstObjectByType<StationServiceConstructionConfirmationRuntime>();
            if (existing != null) return existing;
            return new GameObject("Service Construction Confirmation 2.01").AddComponent<StationServiceConstructionConfirmationRuntime>();
        }

        public void Open(StationServiceConstructionTool tool, Vector3 pendingPosition)
        {
            owner = tool; position = pendingPosition; IsOpen = tool != null;
        }
        public void Close() { IsOpen = false; owner = null; }

        private string TypeLabel(StationServiceType type)
        {
            switch (type)
            {
                case StationServiceType.Restaurant: return "Restaurant";
                case StationServiceType.Shop: return "Boutique";
                case StationServiceType.Bar: return "Bar";
                case StationServiceType.EquipmentRental: return "Location de materiel";
                case StationServiceType.SkiSchool: return "Ecole de ski";
                case StationServiceType.Activity: return "Activite";
                case StationServiceType.Accommodation: return "Hebergement";
                default: return type.ToString();
            }
        }

        private void OnGUI()
        {
            if (!IsOpen || owner == null || StationServiceConstructionRuntime.I == null) return;
            float cost = StationServiceConstructionRuntime.I.GetBuildCost(owner.Type, owner.Range);
            float cash = StationRuntime.I != null ? StationRuntime.I.State.money : 0f;
            bool affordable = StationRuntime.I == null || cash >= cost;
            float w = Mathf.Min(470f, Screen.width - 32f), h = 350f;
            Rect r = new Rect((Screen.width-w)*.5f, (Screen.height-h)*.5f, w, h);
            GUI.Box(r, "RECAPITULATIF CONSTRUCTION");
            GUILayout.BeginArea(new Rect(r.x+18,r.y+34,r.width-36,r.height-50));
            GUILayout.Label("Batiment : " + TypeLabel(owner.Type));
            GUILayout.Label("Gamme : " + owner.Range);
            GUILayout.Label("Position : " + position.x.ToString("0") + " / " + position.z.ToString("0"));
            GUILayout.Space(8);
            GUILayout.Label("PRIX ESTIME : " + cost.ToString("N0") + " EUR");
            if (StationRuntime.I != null) GUILayout.Label("Tresorerie apres construction : " + (cash-cost).ToString("N0") + " EUR");
            if (!affordable) GUILayout.Label("Fonds insuffisants - construction impossible.");
            GUILayout.Space(12);
            GUILayout.BeginHorizontal();
            GUI.enabled = affordable;
            if (GUILayout.Button("CONFIRMER", GUILayout.Height(38))) owner.ConfirmPending(position);
            GUI.enabled = true;
            if (GUILayout.Button("MODIFIER", GUILayout.Height(38))) { Close(); owner.ResumePlacement(); }
            if (GUILayout.Button("ANNULER", GUILayout.Height(38))) { Close(); owner.CancelPlacement(); }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
