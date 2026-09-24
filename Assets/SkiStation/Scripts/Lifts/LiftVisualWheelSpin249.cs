using UnityEngine;

namespace SkiStation
{
    /// <summary>Wheel rotation follows the owning lift speed and operating state. Visual only.</summary>
    public sealed class LiftVisualWheelSpin249 : MonoBehaviour
    {
        Vector3 worldAxis = Vector3.right;
        float fallbackSpeedMetersPerSecond;
        float wheelRadius;
        LiftRuntime owningLift;

        public void Configure(Vector3 axis, float radius, float cableSpeed)
        {
            worldAxis = axis.sqrMagnitude > .001f ? axis.normalized : Vector3.right;
            wheelRadius = Mathf.Max(.001f, radius);
            fallbackSpeedMetersPerSecond = Mathf.Max(0f, cableSpeed);
            owningLift = GetComponentInParent<LiftRuntime>();
        }

        void Update()
        {
            // A stopped, closed or faulted lift must not have spinning wheels.
            float speed = owningLift != null
                ? (owningLift.Open && !owningLift.Faulted ? Mathf.Max(0f, owningLift.EffectiveSpeedKmh) / 3.6f : 0f)
                : fallbackSpeedMetersPerSecond;
            if (speed > .0001f && Time.deltaTime > 0f)
                transform.Rotate(worldAxis, speed / wheelRadius * Mathf.Rad2Deg * Time.deltaTime, Space.World);
        }
    }
}
