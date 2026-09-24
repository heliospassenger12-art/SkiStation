using System;
using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// Converts authoritative GameClock time into presentation time.
    /// Tycoon mode locks visual time to midday while the real calendar keeps moving.
    /// </summary>
    public sealed class TimePresentationRuntime : MonoBehaviour
    {
        public static TimePresentationRuntime I { get; private set; }
        public const float TycoonVisualHour = 12f;

        public float VisualHour
        {
            get
            {
                if (DayNightModeRuntime.I != null && DayNightModeRuntime.I.IsTycoonDayOnly)
                    return TycoonVisualHour;
                return GameClock.I != null ? GameClock.I.CurrentHour : TycoonVisualHour;
            }
        }

        public bool IsVisualNight => VisualHour < 6f || VisualHour >= 24f;
        public bool IsVisualDay => !IsVisualNight;
        public event Action<float> VisualTimeChanged;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update() => VisualTimeChanged?.Invoke(VisualHour);
        private void OnDestroy() { if (I == this) I = null; }
    }
}
