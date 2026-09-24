using UnityEngine;

namespace SkiStation
{
    public enum SkierState { Arriving, WaitingForLift, RidingLift, Skiing, Resting, Leaving }

    public class SkierRuntime : MonoBehaviour
    {
        public SkierState State { get; private set; } = SkierState.Arriving;
        public float Satisfaction { get; private set; } = 75f;
        public SkierPopulationProfile PopulationProfile => GetComponent<SkierPopulationProfile>();

        [SerializeField] private float moveSpeed = 6f;
        private Vector3 destination;
        private bool moving;

        public void SetState(SkierState state) => State = state;

        public void MoveTo(Vector3 target)
        {
            destination = target;
            moving = true;
        }

        // 2.11: movement controllers can explicitly relinquish authority (boarding, handoffs).
        public void StopMoving() => moving = false;

        private void Update()
        {
            // While seated on a lift the vehicle transform is the sole movement authority.
            // A stale MoveTo destination must never pull the skier away from its chair/gondola.
            if (State == SkierState.RidingLift) { moving = false; return; }
            if (!moving) return;

            Vector3 delta = destination - transform.position;
            if (delta.magnitude < 0.5f)
            {
                moving = false;
                return;
            }

            Vector3 next = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
            var route = GetComponent<SkierRouteAgent>();
            if ((State == SkierState.Arriving || State == SkierState.Leaving || (route != null && route.IsApproachingLift)) && ManagementSurface.TryGet(next, out var ground)) next = ground + (State == SkierState.Arriving && GetComponent<ArrivalIdentity250>() != null ? Vector3.up * .36f : Vector3.zero);
            transform.position = next;
            if (delta.sqrMagnitude > 0.1f)
                transform.forward = Vector3.Slerp(transform.forward, delta.normalized, 8f * Time.deltaTime);
        }

        public void ChangeSatisfaction(float amount)
        {
            Satisfaction = Mathf.Clamp(Satisfaction + amount, 0f, 100f);
        }
    }
}
