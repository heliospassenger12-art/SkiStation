using UnityEngine;

namespace SkiStation.Arcade
{
    // Optional visuals: large landings create larger snow bursts, never forced crashes.
    public sealed class ArcadeSkiImpactFeedback : MonoBehaviour
    {
        public ParticleSystem landingSnow;
        public float maximumBurst = 180f;
        public void OnArcadeLanding(float impactSpeed)
        {
            if (landingSnow == null) return;
            int particles = Mathf.RoundToInt(Mathf.Clamp(impactSpeed * 2.5f, 8f, maximumBurst));
            landingSnow.transform.position = transform.position;
            landingSnow.Emit(particles);
            // Temporary snow crater / terrain deformation is intentionally deferred.
        }
    }
}
