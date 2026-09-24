using UnityEngine;

namespace SkiStation.Arcade
{
    // Visual-only carving, snow trails and landing compression. Never affects gameplay physics.
    public sealed class ArcadeCarvingVisual : MonoBehaviour
    {
        public ArcadeSkiController controller;
        public Transform body;
        public Transform leftSki;
        public Transform rightSki;
        public ParticleSystem carvingSnow;
        public TrailRenderer leftTrack;
        public TrailRenderer rightTrack;
        public float maximumLeanDegrees = 23f;
        public float maximumSpeedPostureDegrees = 16f;
        public float brakingPostureDegrees = -9f;
        public float airbornePostureDegrees = 7f;
        public Color normalTrackColor = new Color(0.78f, 0.92f, 1f);
        public Color regularBoostColor = new Color(0.05f, 1f, 0.75f);
        public Color unlimitedBoostColor = new Color(1f, 0.07f, 0.08f);
        public Color unlimitedPowderOrange = new Color(1f, 0.32f, 0.015f);
        float lean;
        float posturePitch;
        float compression;
        bool previousAirborne;
        Quaternion initialBodyRotation;
        Vector3 initialBodyPosition;

        void Start()
        {
            if (body != null) { initialBodyRotation = body.localRotation; initialBodyPosition = body.localPosition; }
            if (controller != null) { controller.Landed += OnLanding; controller.Crashed += OnCrash; controller.Retried += OnRetry; }
            SetTracks(false);
        }
        void OnDestroy()
        {
            if (controller == null) return;
            controller.Landed -= OnLanding; controller.Crashed -= OnCrash; controller.Retried -= OnRetry;
        }
        void OnLanding(float impact)
        {
            // A high landing compresses the knees visually, not the collider or the terrain.
            compression = Mathf.Clamp(impact * 0.012f, 0.08f, 0.48f);
            if (carvingSnow != null)
            {
                carvingSnow.transform.position = transform.position + transform.forward * 0.5f;
                carvingSnow.Emit(Mathf.RoundToInt(Mathf.Clamp(impact * 2.2f, 8f, 120f)));
            }
        }
        void OnCrash() { SetTracks(false); if (carvingSnow != null) carvingSnow.Stop(); }
        void OnRetry() { lean = 0f; posturePitch = 0f; compression = 0f; SetTracks(false); if (carvingSnow != null) carvingSnow.Stop(); }
        void SetTracks(bool active)
        {
            if (leftTrack != null) { leftTrack.emitting = active; if (!active) leftTrack.Clear(); }
            if (rightTrack != null) { rightTrack.emitting = active; if (!active) rightTrack.Clear(); }
        }
        void LateUpdate()
        {
            if (controller == null) return;
            bool onSnow = !controller.IsAirborne && !controller.IsCrashed;
            float steer = controller.SteeringInput;
            float speed = Mathf.Clamp01(controller.Speed / 24f);
            float targetLean = onSnow ? -steer * maximumLeanDegrees * speed : 0f;
            lean = Mathf.Lerp(lean, targetLean, 1f - Mathf.Exp(-8f * Time.deltaTime));
            float targetPitch = controller.IsAirborne ? airbornePostureDegrees
                : Input.GetKey(KeyCode.LeftControl) ? brakingPostureDegrees
                : speed * maximumSpeedPostureDegrees;
            if (controller.IsChargingJump) targetPitch += controller.JumpCharge01 * 8f;
            posturePitch = Mathf.Lerp(posturePitch, targetPitch,
                1f - Mathf.Exp(-6.5f * Time.deltaTime));
            compression = Mathf.MoveTowards(compression, 0f, Time.deltaTime * 1.3f);
            if (body != null && !controller.IsCrashed)
            {
                // Trick animation also owns body rotation: add carving as a separate child pivot
                // where available; otherwise preserve the existing trick angle.
                float trickAngle = controller.IsAirborne ? controller.TrickAngle : 0f;
                Vector3 localNormal = transform.InverseTransformDirection(controller.GroundNormal);
                Quaternion slope = Quaternion.FromToRotation(Vector3.up, localNormal);
                // Skis rotate with the rig; grounded lean is confined to the torso.
                body.localRotation = onSnow ? slope
                    : initialBodyRotation * Quaternion.Euler(0f, trickAngle, 0f);
                body.localPosition = onSnow ? slope * initialBodyPosition : initialBodyPosition;
            }
            if (controller.IsCrashed) return;
            if (leftTrack != null) leftTrack.emitting = onSnow && controller.Speed > 2f;
            if (rightTrack != null) rightTrack.emitting = onSnow && controller.Speed > 2f;
            if (carvingSnow != null)
            {
                var emission = carvingSnow.emission;
                emission.rateOverTime = onSnow ? Mathf.Abs(steer) * speed * 65f : 0f;
                carvingSnow.transform.position = transform.position - transform.forward * 0.65f;
            }
            Color tint = controller.IsUnlimitedBoosting ? unlimitedBoostColor
                : controller.IsRegularBoosting ? regularBoostColor : normalTrackColor;
            if (leftTrack != null) { leftTrack.startColor = tint; leftTrack.endColor = new Color(tint.r, tint.g, tint.b, 0f); }
            if (rightTrack != null) { rightTrack.startColor = tint; rightTrack.endColor = new Color(tint.r, tint.g, tint.b, 0f); }
            if (carvingSnow != null)
            {
                var main = carvingSnow.main;
                // Stronger red/orange particles during the 30-second unlimited boost.
                // Only the snow particles are tinted; the skier remains clearly visible.
                if (controller.IsUnlimitedBoosting)
                {
                    ParticleSystem.MinMaxGradient flashy = new ParticleSystem.MinMaxGradient(unlimitedBoostColor, unlimitedPowderOrange);
                    main.startColor = flashy;
                    main.startSize = 0.25f;
                    main.startSpeed = 5.2f;
                }
                else
                {
                    main.startColor = controller.IsBoosting ? tint : Color.white;
                    main.startSize = 0.15f;
                    main.startSpeed = 2.8f;
                }
                var emission = carvingSnow.emission;
                if (controller.IsBoosting && onSnow) emission.rateOverTime = (controller.IsUnlimitedBoosting ? 185f : 85f) + Mathf.Abs(steer) * speed * 90f;
            }
            previousAirborne = controller.IsAirborne;
        }
    }
}
