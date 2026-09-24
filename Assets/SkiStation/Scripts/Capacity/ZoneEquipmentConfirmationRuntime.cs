using UnityEngine;
namespace SkiStation
{
    /// <summary>Confirmation step for zone equipment. No placement happens before player confirmation.</summary>
    public sealed class ZoneEquipmentConfirmationRuntime : MonoBehaviour
    {
        public static ZoneEquipmentConfirmationRuntime I { get; private set; }
        public bool Pending { get; private set; }
        public ManagedZoneType ZoneType { get; private set; }
        public ManagedZoneEquipmentType EquipmentType { get; private set; }
        public Vector3 Position { get; private set; }
        public float CapacityPerHour { get; private set; }
        public float QualityBonus { get; private set; }
        public float EstimatedCost { get; private set; }
        public string ValidationMessage { get; private set; }
        private void Awake(){ if(I!=null&&I!=this){Destroy(gameObject);return;} I=this; }
        public void Prepare(ManagedZoneType zone, ManagedZoneEquipmentType type, Vector3 position)
        {
            ZoneType=zone; EquipmentType=type; Position=position;
            CapacityPerHour=ManagedZoneEquipmentRuntime.I!=null?ManagedZoneEquipmentRuntime.I.Capacity(type):0f;
            QualityBonus=ManagedZoneEquipmentRuntime.I!=null?ManagedZoneEquipmentRuntime.I.Bonus(type):0f;
            EstimatedCost=Cost(type); ValidationMessage=""; Pending=true;
        }
        public void Confirm()
        {
            if(!Pending)return;
            if(ManagedZoneEquipmentRuntime.I==null || !ManagedZoneEquipmentRuntime.I.CanPlace(ZoneType,Position))
            {
                ValidationMessage="Placement invalide : placez cet equipement dans la zone selectionnee.";
                return;
            }
            if(StationRuntime.I==null)
            {
                ValidationMessage="Economie indisponible : construction annulee.";
                return;
            }
            if(!StationRuntime.I.Spend(EstimatedCost,FinanceCategory.Construction,"zone_equipment_"+EquipmentType,"Construction equipement de zone"))
            {
                ValidationMessage="Fonds insuffisants pour confirmer cette construction.";
                return;
            }
            if(!ManagedZoneEquipmentRuntime.I.Place(ZoneType,EquipmentType,Position))
            {
                StationRuntime.I.Earn(EstimatedCost,FinanceCategory.Construction,CustomerProfile.Tourist,"zone_equipment_refund","Remboursement placement equipement refuse");
                ValidationMessage="Placement refuse : aucun cout conserve.";
                return;
            }
            ValidationMessage=""; Pending=false;
        }
        public void Cancel(){ ValidationMessage=""; Pending=false; }
        private static float Cost(ManagedZoneEquipmentType type){ switch(type){case ManagedZoneEquipmentType.MagicCarpet:return 85000f;case ManagedZoneEquipmentType.BeginnerTow:return 125000f;case ManagedZoneEquipmentType.LearningArea:return 12000f;case ManagedZoneEquipmentType.RestArea:return 18000f;default:return 4500f;} }
        private void OnGUI()
        {
            if(!Pending)return; float w=430f,h=230f,x=(Screen.width-w)*.5f,y=(Screen.height-h)*.5f;
            GUI.Box(new Rect(x,y,w,h),"RECAPITULATIF AVANT VALIDATION");
            GUI.Label(new Rect(x+22,y+38,w-44,22),"Equipement : "+EquipmentType+"   Zone : "+ZoneType);
            GUI.Label(new Rect(x+22,y+66,w-44,22),"Capacite : "+(CapacityPerHour>0?CapacityPerHour.ToString("0")+" pers./h":"—"));
            GUI.Label(new Rect(x+22,y+92,w-44,22),"Bonus qualite : +"+(QualityBonus*100f).ToString("0")+" %");
            GUI.Label(new Rect(x+22,y+118,w-44,22),"Prix estime : "+EstimatedCost.ToString("N0")+" EUR");
            if(StationRuntime.I!=null) GUI.Label(new Rect(x+22,y+140,w-44,22),"Tresorerie : "+StationRuntime.I.State.money.ToString("N0")+" EUR");
            if(!string.IsNullOrEmpty(ValidationMessage)) GUI.Label(new Rect(x+22,y+151,w-44,20),ValidationMessage);
            if(GUI.Button(new Rect(x+22,y+170,120,34),"CONFIRMER")) Confirm();
            if(GUI.Button(new Rect(x+155,y+170,120,34),"MODIFIER")) Cancel();
            if(GUI.Button(new Rect(x+288,y+170,120,34),"ANNULER")) Cancel();
        }
    }
    public static class ZoneEquipmentConfirmationBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot(){
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;
 if(ZoneEquipmentConfirmationRuntime.I==null)new GameObject("Zone Equipment Confirmation 2.04").AddComponent<ZoneEquipmentConfirmationRuntime>(); }
    }
}
