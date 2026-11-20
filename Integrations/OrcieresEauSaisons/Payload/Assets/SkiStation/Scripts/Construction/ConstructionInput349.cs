using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SkiStation
{
    public enum ConstructionTool349 { Slope, Lift, Service, Village, Road, Zone, Children, WaterCrossing }

    /// <summary>One active tool; IMGUI rectangles protect the world beneath them.</summary>
    public static class ConstructionInput349
    {
        struct Panel { public Rect rect; public int frame; public Component owner; }
        static readonly Dictionary<string, Panel> panels = new Dictionary<string, Panel>();
        static int consumedFrame = -1;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset349() { panels.Clear(); consumedFrame = -1; }
        public static bool ModalOpen => SlopeConditionUI.IsOpen || Arcade.RiderHub262.IsOpen || Arcade.DemoRuntime263.OptionsOpen || SkiStationStartMenu249.HomeOpen263 || DemoManagementMenu263.Paused263 || Arcade.ArcadeDomainEntry.TimeChoiceOpen || StationVillageConstruction249.IsConfirming349 || StationRoadConstruction249.IsConfirming349 ||
            (ManagedZoneConfirmationRuntime.I != null && ManagedZoneConfirmationRuntime.I.Pending) ||
            (ZoneEquipmentConfirmationRuntime.I != null && ZoneEquipmentConfirmationRuntime.I.Pending) ||
            (SlopeConstructionConfirmationRuntime.I != null && SlopeConstructionConfirmationRuntime.I.IsOpen) ||
            (LiftConstructionConfirmationRuntime.I != null && LiftConstructionConfirmationRuntime.I.IsOpen) ||
            (StationServiceConstructionConfirmationRuntime.I != null && StationServiceConstructionConfirmationRuntime.I.IsOpen) ||
            (ConstructionSelectionRuntime.I != null && ConstructionSelectionRuntime.I.IsRemovalPending);

        public static bool AnyToolActive => ModalOpen || StationVillageConstruction249.IsPlacing || StationRoadConstruction249.IsDrawing ||
            (SlopeConstructionMode.Instance != null && SlopeConstructionMode.Instance.IsDrawing) ||
            (StationServiceConstructionTool.I != null && StationServiceConstructionTool.I.Active) || LiftActive() || AuxiliaryActive() || Object.FindFirstObjectByType<TerrainGeneration.WaterCrossingTool>()?.Active == true;

        static bool LiftActive()
        { var lift = Object.FindFirstObjectByType<LiftConstructionPlanner>(); return lift != null && lift.IsActive; }
        static bool AuxiliaryActive()
        {
            var zone = Object.FindFirstObjectByType<ManagedZoneConstructionMode>();
            var children = Object.FindFirstObjectByType<PiouPiouConstructionMode>();
            return (zone != null && zone.IsDrawing) || (children != null && children.IsDrawing);
        }

        public static void PanelArea(Component owner, string key, Rect rect)
        {
            if (panels.Count > 64)
            {
                var expired = new List<string>();
                foreach (var item in panels) if (item.Value.owner == null || item.Value.frame < Time.frameCount - 1) expired.Add(item.Key);
                foreach (string old in expired) panels.Remove(old);
            }
            panels[key] = new Panel { owner = owner, frame = Time.frameCount, rect = rect };
        }
        public static void ConsumeClick() { consumedFrame = Time.frameCount; }
        public static bool PointerBlocked
        {
            get
            {
                if (Time.frameCount == consumedFrame || ModalOpen || VisitorDetailsPanel255.BlocksWorldPointer) return true;
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return true;
                Vector2 point = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                foreach (var panel in panels.Values)
                    if (panel.owner != null && panel.frame >= Time.frameCount - 1 && panel.rect.Contains(point)) return true;
                return false;
            }
        }

        public static void Activate(ConstructionTool349 tool)
        {
            ConsumeClick();
            if (tool != ConstructionTool349.WaterCrossing) Object.FindFirstObjectByType<TerrainGeneration.WaterCrossingTool>()?.Cancel();
            ZoneEquipmentConfirmationRuntime.I?.Cancel();
            if (tool != ConstructionTool349.Slope) SlopeConstructionMode.Instance?.CancelPendingSlope();
            if (tool != ConstructionTool349.Lift) Object.FindFirstObjectByType<LiftConstructionPlanner>()?.SetActive(false);
            if (tool != ConstructionTool349.Service) StationServiceConstructionTool.I?.CancelPlacement();
            if (tool != ConstructionTool349.Village) StationVillageConstruction249.Stop349();
            if (tool != ConstructionTool349.Road) StationRoadConstruction249.Stop349();
            if (tool != ConstructionTool349.Zone) Object.FindFirstObjectByType<ManagedZoneConstructionMode>()?.Stop349();
            if (tool != ConstructionTool349.Children) Object.FindFirstObjectByType<PiouPiouConstructionMode>()?.Stop349();
            ConstructionSelectionRuntime.I?.ClearSelection349();
            StationStructureStatusPanel249.Hide349();
        }

        public static bool TryGround(Camera camera, out Vector3 point)
        {
            point = default;
            if (camera == null || !camera.pixelRect.Contains((Vector2)Input.mousePosition)) return false;
            return ManagementSurface.TryRaycast349(camera.ScreenPointToRay(Input.mousePosition), out point);
        }
    }
}
