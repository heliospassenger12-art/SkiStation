using UnityEngine;

namespace SkiStation
{
    public static class LiftStationFlowBootstrap
    {
        public static void Ensure(LiftRuntime lift)
        {
            if (lift == null) return;
            var slotBuilder = lift.GetComponent<LiftStationSlotBuilder>() ?? lift.gameObject.AddComponent<LiftStationSlotBuilder>();
            slotBuilder.Build();
            var vehicles = lift.GetComponentsInChildren<LiftVehicleRuntime>(true);
            for (int i = 0; i < vehicles.Length; i++)
            {
                if (vehicles[i] == null) continue;
                var docking = vehicles[i].GetComponent<LiftVehicleStationDockingRuntime>() ?? vehicles[i].gameObject.AddComponent<LiftVehicleStationDockingRuntime>();
                docking.Build();
                var flow = vehicles[i].GetComponent<LiftStationFlowRuntime>() ?? vehicles[i].gameObject.AddComponent<LiftStationFlowRuntime>();
                flow.Bind(vehicles[i]);
            }
        }
    }
}
