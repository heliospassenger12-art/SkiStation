using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// Optional scene adapter. Assign the sun light and optional ambient light.
    /// In Tycoon mode the scene stays in a stable daytime presentation.
    /// </summary>
    public sealed class DayNightLightingAdapter : MonoBehaviour
    {
        [SerializeField] private Light sun;
        [SerializeField] private Light[] additionalLights;
        [SerializeField] private float daytimeSunIntensity = 1.1f;
        [SerializeField] private float daytimeSunPitch = 45f;
        [SerializeField] private float immersiveSunPitch = 45f;
        [SerializeField] private AnimationCurve immersiveIntensity = AnimationCurve.Linear(0f, 0.15f, 1f, 1f);

        private void Update()
        {
            var presentation = TimePresentationRuntime.I;
            if (presentation == null) return;

            float hour = presentation.VisualHour;
            bool tycoon = DayNightModeRuntime.I != null && DayNightModeRuntime.I.IsTycoonDayOnly;
            if (tycoon)
            {
                ApplyDaytime();
                return;
            }

            float angle = (hour / 24f) * 360f - 90f;
            float dayFactor = Mathf.Clamp01(Mathf.Sin((hour - 6f) / 12f * Mathf.PI));
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(immersiveSunPitch - angle, -35f, 0f);
                sun.intensity = Mathf.Lerp(immersiveIntensity.Evaluate(0f), immersiveIntensity.Evaluate(1f), dayFactor);
            }
        }

        private void ApplyDaytime()
        {
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(daytimeSunPitch, -35f, 0f);
                sun.intensity = daytimeSunIntensity;
            }
        }
    }
}
