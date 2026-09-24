using UnityEngine;

namespace SkiStation
{
    /// <summary>Player-friendly station logic: queue, automatic boarding and capacity feedback.</summary>
    public class LiftStationLogic : MonoBehaviour
    {
        public LiftRuntime Lift { get; private set; }
        public int QueueSize => queue != null ? queue.Count : 0;
        public int WaitingPassengers { get; private set; }
        public float BoardingProgress { get; private set; }
        public bool BoardingOpen => Lift != null && Lift.Open && !Lift.Faulted;

        private LiftQueueRuntime queue;
        private LiftPhysicalPassengerFlowRuntime physicalFlow;
        private float boardingTimer;
        private float scanTimer;
        [SerializeField] private float autoQueueRadius = 12f;

        public void Bind(LiftRuntime lift)
        {
            Lift = lift;
            queue = GetComponent<LiftQueueRuntime>() ?? gameObject.AddComponent<LiftQueueRuntime>();
            physicalFlow = GetComponent<LiftPhysicalPassengerFlowRuntime>() ?? gameObject.AddComponent<LiftPhysicalPassengerFlowRuntime>();
            physicalFlow.Bind(lift);
            boardingTimer = 0f;
            WaitingPassengers = 0;
        }

        public bool Enqueue(SkierRuntime skier) => queue != null && queue.Enqueue(skier);

        public SkierRuntime Dequeue() => queue != null ? queue.Dequeue() : null;

        private void Update()
        {
            if (Lift == null || Lift.Definition == null || !BoardingOpen)
            {
                BoardingProgress = 0f;
                boardingTimer = 0f;
                if (queue != null) while (queue.Count > 0)
                {
                    var waiting = queue.Dequeue();
                    if (waiting == null) continue;
                    physicalFlow?.Release(waiting);
                    waiting.StopMoving();
                    waiting.SetState(SkierState.Resting);
                }
                WaitingPassengers = 0;
                return;
            }
            scanTimer += Time.deltaTime;
            if (scanTimer >= .5f) { scanTimer = 0f; AutoQueueNearbySkiers(); }
            boardingTimer += Time.deltaTime;
            float duration = Mathf.Max(.25f, Lift.Definition.loadingSeconds);
            BoardingProgress = Mathf.Clamp01(boardingTimer / duration);
            if (boardingTimer < duration) return;

            boardingTimer = 0f;
            TryBoardNearestVehicle();
        }

        private void AutoQueueNearbySkiers()
        {
            if (queue == null || Lift == null) return;
            SkierRuntime[] skiers = FindObjectsOfType<SkierRuntime>();
            for (int i = 0; i < skiers.Length; i++)
            {
                SkierRuntime skier = skiers[i];
                if (skier == null || skier.State == SkierState.RidingLift || skier.State == SkierState.WaitingForLift) continue;
                // Managed visitors must finish their road arrival and walk to their chosen lift.
                var arrival = skier.GetComponent<StationVisitorRoadArrival248>();
                if (arrival != null && arrival.IsArriving) continue;
                if (skier.GetComponent<SkierRouteAgent>() != null) continue;
                if (Vector3.Distance(skier.transform.position, Lift.Bottom) <= 4f)
                    queue.Enqueue(skier);
            }
            WaitingPassengers = QueueSize;
        }

        private void TryBoardNearestVehicle()
        {
            LiftVehicleRuntime[] vehicles = GetComponentsInChildren<LiftVehicleRuntime>();
            if (vehicles == null || vehicles.Length == 0) return;

            for (int i = 0; i < vehicles.Length; i++)
            {
                LiftVehicleRuntime vehicle = vehicles[i];
                if (vehicle == null || vehicle.OccupantCount >= vehicle.Capacity) continue;
                // Board only a vehicle physically at the lower terminal, not one
                // already several metres uphill (which visibly teleports skiers).
                // NormalizedPosition also rejects a descending vehicle passing nearby.
                if (!vehicle.GoingUp || vehicle.NormalizedPosition > .025f) continue;
                if (Vector3.Distance(vehicle.transform.position, Lift.Bottom) > 3.5f) continue;

                if (physicalFlow != null) physicalFlow.PromoteHeadToBoarding();
                SkierRuntime skier = queue.Peek();
                if (skier == null) return;
                if (!vehicle.TryBoard(skier)) return;
                Dequeue();
                physicalFlow?.Release(skier);
                WaitingPassengers = QueueSize;
                return;
            }
            WaitingPassengers = QueueSize;
        }
    }
}
