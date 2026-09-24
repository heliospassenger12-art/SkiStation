using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>Moves one lift vehicle and simulates simple passenger boarding/unloading.</summary>
    public class LiftVehicleRuntime : MonoBehaviour
    {
        public float NormalizedPosition => normalizedPosition;
        public bool GoingUp => direction > 0;
        public int OccupantCount => occupants.Count;
        public IReadOnlyList<SkierRuntime> Occupants => occupants;
        public int Capacity => lift != null ? lift.EffectiveVehicleCapacity : (definition != null ? Mathf.Max(1, definition.seatsPerVehicle) : 1);
        public LiftDefinition Definition => definition;
        public bool IsRailVehicle => railVehicle;
        public float TravelProgress => normalizedPosition;
        public bool CanBoardSimulationPlayer => definition != null && occupants.Count + simulationPassengers.Count < Capacity;

        public event Action<Transform, SimulationLiftRideSession> SimulationPlayerArrived;

        [SerializeField] private float normalizedPosition;
        [SerializeField] private int direction = 1;
        private readonly List<Vector3> route = new List<Vector3>();
        private readonly List<SkierRuntime> occupants = new List<SkierRuntime>();
        private readonly List<Transform> simulationPassengers = new List<Transform>();
        private readonly List<SimulationLiftRideSession> simulationSessions = new List<SimulationLiftRideSession>();
        private LiftDefinition definition;
        private bool railVehicle;
        private float waitTimer;
        private LiftRuntime lift;

        public void Configure(LiftDefinition def, Vector3 a, Vector3 b, float start, bool isRailVehicle)
            => Configure(def, new[] { a, b }, start, isRailVehicle);

        public void Configure(LiftDefinition def, IReadOnlyList<Vector3> points, float start, bool isRailVehicle)
        {
            definition = def;
            railVehicle = isRailVehicle;
            route.Clear();
            if (points != null) route.AddRange(points);
            normalizedPosition = Mathf.Clamp01(start);
            direction = normalizedPosition <= .001f ? 1 : (normalizedPosition >= .999f ? -1 : 1);
            lift = GetComponentInParent<LiftRuntime>();
            ApplyTransform();
        }

        public bool TryBoardSimulationPlayer(Transform player, SimulationLiftRideSession session)
        {
            if (player == null || session == null || !CanBoardSimulationPlayer || simulationPassengers.Contains(player)) return false;
            simulationPassengers.Add(player);
            simulationSessions.Add(session);
            player.SetParent(transform, true);
            player.localPosition = Vector3.up * 0.8f + Vector3.right * 0.0f;
            return true;
        }

        public void RemoveSimulationPlayer(Transform player, SimulationLiftRideSession session)
        {
            int index = simulationPassengers.IndexOf(player);
            if (index < 0) return;
            simulationPassengers.RemoveAt(index);
            simulationSessions.RemoveAt(index);
            if (player != null) player.SetParent(null, true);
        }

        public bool TryBoard(SkierRuntime skier)
        {
            if (!VisitorSkiAdmission.CanSki(skier) || occupants.Count + simulationPassengers.Count >= Capacity || occupants.Contains(skier)) return false;
            VisitorSkiAdmission.RecordBoarding(skier);
            occupants.Add(skier);
            skier.StopMoving();
            skier.SetState(SkierState.RidingLift);
            skier.transform.SetParent(transform, true);
            Vector3 seat = Vector3.up * 0.7f + Vector3.right * ((occupants.Count - 1) - (Capacity - 1) * .5f) * .45f;
            skier.transform.localPosition = seat;
            return true;
        }

        public void UnloadAll()
        {
            if (simulationPassengers.Count > 0)
            {
                for (int i = simulationPassengers.Count - 1; i >= 0; i--)
                {
                    Transform passenger = simulationPassengers[i];
                    SimulationLiftRideSession session = simulationSessions[i];
                    if (passenger != null) passenger.SetParent(null, true);
                    SimulationPlayerArrived?.Invoke(passenger, session);
                }
                simulationPassengers.Clear();
                simulationSessions.Clear();
            }

            if (occupants.Count == 0) return;
            var slotBuilder = lift != null ? lift.GetComponent<LiftStationSlotBuilder>() : null;
            var unloadSlots = slotBuilder != null
                ? slotBuilder.GetSlots(LiftStationSlotType.Unloading, LiftStationEndpointKind.Top)
                : System.Array.Empty<LiftStationSlotRuntime>();
            var exitSlots = slotBuilder != null
                ? slotBuilder.GetSlots(LiftStationSlotType.Exit, LiftStationEndpointKind.Top)
                : System.Array.Empty<LiftStationSlotRuntime>();
            Vector3 fallbackExit = transform.position + transform.forward * 2f + Vector3.up * .1f;
            for (int i = occupants.Count - 1; i >= 0; i--)
            {
                SkierRuntime skier = occupants[i];
                if (skier != null)
                {
                    Vector3 unloadPos = (unloadSlots.Length > 0 ? unloadSlots[i % unloadSlots.Length].transform.position : fallbackExit);
                    Vector3 exitPos = (exitSlots.Length > 0 ? exitSlots[i % exitSlots.Length].transform.position : unloadPos + transform.forward * 1.5f);
                    skier.transform.SetParent(null, true);
                    skier.transform.position = unloadPos;
                    skier.SetState(SkierState.Skiing);
                    skier.StopMoving(); // Disembark controller exclusively owns the exit animation.
                    LiftStationSlotRuntime unloadSlot = unloadSlots.Length > 0 ? unloadSlots[i % unloadSlots.Length] : null;
                    LiftStationSlotRuntime exitSlot = exitSlots.Length > 0 ? exitSlots[i % exitSlots.Length] : null;
                    unloadSlot?.TryReserve(skier);
                    exitSlot?.TryReserve(skier);
                    var disembark = skier.GetComponent<LiftPassengerDisembarkRuntime>() ?? skier.gameObject.AddComponent<LiftPassengerDisembarkRuntime>();
                    disembark.Begin(skier, lift, unloadSlot, exitSlot);
                }
            }
            occupants.Clear();
        }

        private void Update()
        {
            if (definition == null || route.Count < 2) return;
            if (lift != null && (!lift.Open || lift.Faulted)) return;
            if (waitTimer > 0f) { waitTimer -= Time.deltaTime; ApplyTransform(); return; }

            float speed = Mathf.Max(.1f, lift != null ? lift.EffectiveSpeedKmh : definition.speedKmh) / 3.6f;
            float length = RouteLength();
            float duration = Mathf.Max(3f, length / speed);
            float delta = Time.deltaTime / duration;
            normalizedPosition += delta * direction;

            if (normalizedPosition >= 1f)
            {
                normalizedPosition = 1f;
                // Passengers disembark only at the upper terminal.
                // The descending vehicle is intentionally empty and returns to the
                // lower terminal where new passengers can board.
                if (direction > 0) { UnloadAll(); waitTimer = definition.unloadingSeconds; direction = -1; }
            }
            else if (normalizedPosition <= 0f)
            {
                normalizedPosition = 0f;
                // Never unload management passengers at the bottom: if a passenger
                // is still attached here it remains on board until the next upper
                // terminal cycle. The normal loop keeps the vehicle empty on descent.
                if (direction < 0) { waitTimer = definition.loadingSeconds; direction = 1; }
            }

            ApplyTransform();
        }

        private void ApplyTransform()
        {
            Vector3 p = Evaluate(normalizedPosition);
            Vector3 ahead = Evaluate(Mathf.Clamp01(normalizedPosition + .004f * direction));
            transform.position = p;
            Vector3 look = direction > 0 ? ahead - p : p - ahead;
            if (look.sqrMagnitude > .0001f) transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
        }

        private float RouteLength()
        {
            float length = 0f;
            for (int i = 1; i < route.Count; i++) length += Vector3.Distance(route[i - 1], route[i]);
            return length;
        }

        private Vector3 Evaluate(float t)
        {
            if (route.Count == 0) return transform.position;
            if (route.Count == 1) return route[0];
            float total = RouteLength();
            float distance = Mathf.Clamp01(t) * total;
            for (int i = 1; i < route.Count; i++)
            {
                float segment = Vector3.Distance(route[i - 1], route[i]);
                if (distance <= segment)
                    return Vector3.Lerp(route[i - 1], route[i], distance / Mathf.Max(.001f, segment));
                distance -= segment;
            }
            return route[route.Count - 1];
        }
    }
}
