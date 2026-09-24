using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// Converts physical management zones into readable specializations based on
    /// their declared role, visitor mix, terrain and installed equipment.
    /// The player sets the intended role; the simulation determines how strongly
    /// the zone actually lives up to that role.
    /// </summary>
    public sealed class ManagedZoneSpecializationRuntime : MonoBehaviour
    {
        public static ManagedZoneSpecializationRuntime I { get; private set; }
        public event Action Changed;
        [SerializeField] private float refreshSeconds = 5f;
        private float timer;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < Mathf.Max(.75f, refreshSeconds)) return;
            timer = 0f;
            Recalculate();
        }

        public void Recalculate()
        {
            if (ManagedZoneRuntime.I == null) return;
            ManagedZoneRuntime.I.Recalculate();
            Changed?.Invoke();
        }

        public float GetAttraction(ManagedZoneType type, CustomerProfile profile)
        {
            switch (type)
            {
                case ManagedZoneType.Beginner:
                    return profile == CustomerProfile.Beginner || profile == CustomerProfile.Youth ? 1f :
                           profile == CustomerProfile.Family ? .85f : profile == CustomerProfile.Expert || profile == CustomerProfile.Athlete ? .05f : .25f;
                case ManagedZoneType.Family:
                    return profile == CustomerProfile.Family ? 1f : profile == CustomerProfile.Beginner || profile == CustomerProfile.Youth ? .72f : .35f;
                case ManagedZoneType.Sport:
                    return profile == CustomerProfile.Athlete || profile == CustomerProfile.Expert || profile == CustomerProfile.Advanced ? 1f : .18f;
                case ManagedZoneType.Snowpark:
                    return profile == CustomerProfile.Athlete || profile == CustomerProfile.Youth || profile == CustomerProfile.Expert ? .95f : .18f;
                default:
                    return profile == CustomerProfile.Tourist || profile == CustomerProfile.Premium ? .85f : .4f;
            }
        }
    }
}
