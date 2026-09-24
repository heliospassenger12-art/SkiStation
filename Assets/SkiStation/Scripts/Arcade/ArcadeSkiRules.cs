using UnityEngine;
namespace SkiStation.Arcade
{
    public static class ArcadeSkiRules
    {
        public const float LandingTolerance = 20f;
        public static float CameraYaw(float current, float target, float deadZone, float rate, float dt)
        {
            float delta = Mathf.DeltaAngle(current,target);
            return current + Mathf.Sign(delta)*Mathf.Min(Mathf.Max(0f,Mathf.Abs(delta)-deadZone),rate*dt);
        }
        // Signed speed: uphill momentum decreases through zero, then slides back.
        public static float StepSpeed(float speed, Vector3 axis, float gravity, float drag, float dt)
        {
            return (speed + Vector3.Dot(Vector3.down * gravity, axis.normalized) * dt)
                * Mathf.Exp(-Mathf.Max(0f, drag) * dt);
        }
        public static float LandingError(Vector3 skis, Vector3 trajectory, Vector3 normal)
        {
            Vector3 a = Vector3.ProjectOnPlane(skis, normal);
            Vector3 b = Vector3.ProjectOnPlane(trajectory, normal);
            if (a.sqrMagnitude < .0001f || b.sqrMagnitude < .0001f) return 90f;
            float angle = Vector3.Angle(a,b);
            return Mathf.Min(angle,180f-angle);
        }
        public static bool CanLand(Vector3 skis, Vector3 trajectory, Vector3 normal)
        {
            return LandingError(skis,trajectory,normal) <= LandingTolerance + .0001f;
        }
    }
}
