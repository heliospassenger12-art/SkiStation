using System;
using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// Single source of truth for the player's day/night presentation preference.
    /// Calendar and management time remain authoritative in GameClock in both modes.
    /// Safe to switch during play; no simulation reset is performed.
    /// </summary>
    public sealed class DayNightModeRuntime : MonoBehaviour
    {
        public static DayNightModeRuntime I { get; private set; }

        [SerializeField] private DayNightPresentationMode mode = DayNightPresentationMode.ImmersiveDayNight;
        public DayNightPresentationMode Mode => mode;
        public bool IsTycoonDayOnly => mode == DayNightPresentationMode.TycoonDayOnly;
        public bool CalendarAlwaysAdvances => true;

        public event Action<DayNightPresentationMode> ModeChanged;

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (I == this) I = null;
        }

        public void SetMode(DayNightPresentationMode newMode)
        {
            if (mode == newMode) return;
            mode = newMode;
            ModeChanged?.Invoke(mode);
        }

        public void SetTycoonDayOnly(bool enabled)
        {
            SetMode(enabled ? DayNightPresentationMode.TycoonDayOnly : DayNightPresentationMode.ImmersiveDayNight);
        }

        public void Toggle() => SetTycoonDayOnly(!IsTycoonDayOnly);
    }
}
