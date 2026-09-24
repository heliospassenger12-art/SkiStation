using UnityEngine;

namespace SkiStation.Arcade
{
    // Unparented camera: follows travel direction, independent of trick/body yaw.
    // Independent from piste waypoints and the visitor simulation.
    [RequireComponent(typeof(Camera))]
    public sealed class ArcadeThirdPersonCamera : MonoBehaviour
    {
        public Transform target;
        public ArcadeSkiController controller;
        public Vector3 lookOffset = new Vector3(0f, 0.1f, 0f);
        public float distance = 6.8f;
        public float height = 3.2f;
        public float followSharpness = 7f;
        public float normalFieldOfView = 72f;
        public float maximumFieldOfView = 80f;
        public float maximumSpeedForEffects = 40f;
        public float extraCameraDistanceAtSpeed = 0.4f;
        public float collisionRadius = 0.35f;
        public LayerMask occlusionMask = ~0;
        public float extraBigAirDistance = 1.2f;
        public float extraBigAirHeight = 0.8f;
        public float bigAirThreshold = 12f;
        float takeoffHeight;
        float airborneCameraBlend;
        bool wasAirborne;
        public float headingDeadZone = 12f;
        public float maximumOrbitDegreesPerSecond = 35f;
        float cameraYaw;
        Vector3 previousTargetPosition;
        Camera attachedCamera;
        void Awake() { attachedCamera = GetComponent<Camera>(); }
        void Start() { if (controller != null) controller.Retried += SnapBehind; }
        void OnDestroy() { if (controller != null) controller.Retried -= SnapBehind; }
        public void SnapBehind()
        {
            if (target == null) return;
            airborneCameraBlend = 0f; wasAirborne = false;
            Vector3 heading = controller != null && controller.TravelVelocity.sqrMagnitude > .1f
                ? controller.TravelVelocity : target.forward;
            cameraYaw = Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg;
            previousTargetPosition = target.position;
            transform.position = DesiredPosition();
            transform.LookAt(target.position + Vector3.up * 0.7f + lookOffset);
        }
        Vector3 DesiredPosition()
        {
            float speed = controller == null ? 0f : controller.Speed;
            float speedRecoil = Mathf.Clamp01(speed / Mathf.Max(1f, maximumSpeedForEffects)) * extraCameraDistanceAtSpeed;
            float airborneRise = controller != null && controller.IsAirborne ? 1.6f : 0f;
            float bigAirRecoil = airborneCameraBlend * extraBigAirDistance;
            Vector3 focus = target.position + Vector3.up * (0.8f + airborneRise * 0.25f) + lookOffset;
            Vector3 viewHeading = Quaternion.Euler(0f, cameraYaw, 0f) * Vector3.forward;
            Vector3 desired = focus - viewHeading * (distance + speedRecoil + bigAirRecoil)
                + Vector3.up * (height + airborneRise + airborneCameraBlend * extraBigAirHeight);
            return ConstrainToVisible(focus, desired);
        }
        Vector3 ConstrainToVisible(Vector3 focus, Vector3 desired)
        {
            Vector3 ray = desired - focus;
            RaycastHit[] hits = Physics.SphereCastAll(focus, collisionRadius, ray.normalized,
                ray.magnitude, occlusionMask, QueryTriggerInteraction.Ignore);
            float allowed = ray.magnitude;
            foreach (RaycastHit hit in hits)
            {
                // Never let the player's own skis or avatar push the camera into first person.
                if (hit.collider.transform == target || hit.collider.transform.IsChildOf(target)) continue;
                if (hit.distance < allowed) allowed = Mathf.Max(0.1f, hit.distance - 0.3f);
            }
            return focus + ray.normalized * allowed;
        }
        void LateUpdate()
        {
            if (target == null) return;
            // Follow translation directly: speed no longer creates metres of follow lag.
            transform.position += target.position - previousTargetPosition;
            previousTargetPosition = target.position;
            if (controller != null && controller.Speed > 2f && !controller.IsCrashed)
            {
                Vector3 trajectory = controller.TravelVelocity;
                float targetYaw = Mathf.Atan2(trajectory.x,trajectory.z)*Mathf.Rad2Deg;
                cameraYaw = ArcadeSkiRules.CameraYaw(cameraYaw,targetYaw,headingDeadZone,
                    maximumOrbitDegreesPerSecond,Time.deltaTime);
            }
            bool airborne = controller != null && controller.IsAirborne;
            if (airborne && !wasAirborne) takeoffHeight = target.position.y;
            float heightAboveTakeoff = airborne ? Mathf.Max(0f, target.position.y - takeoffHeight) : 0f;
            float desiredBigAir = airborne ? Mathf.Clamp01(heightAboveTakeoff / Mathf.Max(1f, bigAirThreshold)) : 0f;
            airborneCameraBlend = Mathf.MoveTowards(airborneCameraBlend, desiredBigAir, Time.deltaTime * 2.5f);
            wasAirborne = airborne;
            float blend = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, DesiredPosition(), blend);
            Vector3 focus = target.position + Vector3.up * 0.7f + lookOffset;
            transform.position = ConstrainToVisible(focus, transform.position);
            transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
            if (attachedCamera != null)
            {
                float intensity = controller == null ? 0f : Mathf.Clamp01(controller.Speed / Mathf.Max(1f, maximumSpeedForEffects));
                float fov = Mathf.Lerp(normalFieldOfView, maximumFieldOfView, intensity * intensity);
                attachedCamera.fieldOfView = Mathf.Lerp(attachedCamera.fieldOfView, fov, blend);
            }
        }
    }
}
