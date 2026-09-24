using UnityEngine;
namespace SkiStation
{
    public sealed class ManagedZoneEquipmentConstructionMode : MonoBehaviour
    {
        [SerializeField] private ManagedZoneType zoneType = ManagedZoneType.Beginner;
        [SerializeField] private ManagedZoneEquipmentType equipmentType = ManagedZoneEquipmentType.MagicCarpet;
        private Camera cam;
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.X)) equipmentType = (ManagedZoneEquipmentType)(((int)equipmentType + 1) % 5);
            if (Input.GetKeyDown(KeyCode.C)) zoneType = (ManagedZoneType)(((int)zoneType + 1) % 5);
            if (Input.GetMouseButtonDown(0) && Input.GetKey(KeyCode.LeftShift) && !PlayableGameHUD.PointerOverCameraControls)
            {
                cam = cam != null ? cam : Camera.main; if (cam == null || ManagedZoneEquipmentRuntime.I == null) return;
                var ray = cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, 5000f, ~0, QueryTriggerInteraction.Ignore))
                    { if (ZoneEquipmentConfirmationRuntime.I != null) ZoneEquipmentConfirmationRuntime.I.Prepare(zoneType, equipmentType, hit.point); }
            }
        }
        private void OnGUI()
        {
            if (!Input.GetKey(KeyCode.LeftShift) && !LegacyUIPanelCoordinator.DiagnosticsVisible) return;
            GUI.Box(new Rect(18, Screen.height - 205, 500, 45), "ÉQUIPEMENTS DE ZONE — Shift + clic : placer • X : équipement • C : zone");
            GUI.Label(new Rect(32, Screen.height - 184, 465, 20), "Zone : " + zoneType + "   Équipement : " + equipmentType);
        }
    }
}
