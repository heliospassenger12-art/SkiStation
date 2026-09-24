using UnityEngine;
using SkiStation.PisteOperations;

namespace SkiStation
{
    public sealed class GroomingPolicyRuntime : MonoBehaviour
    {
        public static GroomingPolicyRuntime I { get; private set; }
        public bool preferNight = true;
        public bool allowDayEmergency = false;
        [Range(18f, 23.99f)] public float nightWindowStart = 20f;
        [Range(0f, 6f)] public float nightWindowEnd = 6f;

        public bool IsTycoonDayOnly => DayNightModeRuntime.I != null && DayNightModeRuntime.I.IsTycoonDayOnly;
        public GroomingPresentationMode PresentationMode => IsTycoonDayOnly
            ? GroomingPresentationMode.OffscreenTycoonOperation
            : GroomingPresentationMode.VisibleNightOperation;

        public bool IsPreferredWindow
        {
            get
            {
                if (IsTycoonDayOnly) return true;
                var c = GameClock.I;
                if (c == null || !preferNight) return true;
                return c.CurrentHour >= nightWindowStart || c.CurrentHour < nightWindowEnd;
            }
        }

        public bool CanDispatch(bool emergency = false)
        {
            if (IsTycoonDayOnly) return false; // Tycoon uses ResolveOffscreen instead of visible dispatch.
            if (!preferNight) return true;
            return IsPreferredWindow || (emergency && allowDayEmergency);
        }

        public void Awake() { I = this; }
        private void OnDestroy() { if (I == this) I = null; }
    }
}
