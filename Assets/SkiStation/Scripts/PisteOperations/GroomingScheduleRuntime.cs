using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum GroomingPriority { Low, Normal, High, Critical }

    /// <summary>
    /// Schedules real piste maintenance. In Tycoon mode it resolves maintenance in the
    /// management layer instead of spawning a visible grooming operation during the day.
    /// </summary>
    public sealed class GroomingScheduleRuntime : MonoBehaviour
    {
        public static GroomingScheduleRuntime I { get; private set; }
        public bool automatic = true;
        public bool instantConstructionStyle = false;
        public bool emergencyDayDispatch = false;
        public float dispatchInterval = 2f;
        public int maxConcurrent = 1;
        public readonly List<SlopeRuntime> Queue = new List<SlopeRuntime>();
        public SlopeRuntime Active { get; private set; }
        public int OffscreenOperationsToday { get; private set; }

        private float timer;

        private void Awake()
        {
            I = this;
            if (GameClock.I != null) GameClock.I.DayStarted += OnDayStarted;
        }

        private void OnDestroy()
        {
            if (GameClock.I != null) GameClock.I.DayStarted -= OnDayStarted;
            if (I == this) I = null;
        }

        private void OnDayStarted(int day)
        {
            OffscreenOperationsToday = 0;
            if (GroomingPolicyRuntime.I != null && GroomingPolicyRuntime.I.IsTycoonDayOnly)
                ResolveOffscreenTycoonMaintenance();
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < dispatchInterval) return;
            timer = 0f;
            EvaluateAndDispatch();
        }

        public void Request(SlopeRuntime slope)
        {
            if (slope == null || Queue.Contains(slope) || Active == slope) return;
            if (GroomingPolicyRuntime.I != null && GroomingPolicyRuntime.I.IsTycoonDayOnly)
            {
                ResolveOffscreenSlope(slope);
                return;
            }
            Queue.Add(slope);
            SortQueue();
        }

        public void Cancel(SlopeRuntime slope) { if (slope != null) Queue.Remove(slope); }

        public void ResolveOffscreenTycoonMaintenance()
        {
            for (int i = Queue.Count - 1; i >= 0; i--)
            {
                var slope = Queue[i];
                Queue.RemoveAt(i);
                ResolveOffscreenSlope(slope);
            }
        }

        private void ResolveOffscreenSlope(SlopeRuntime slope)
        {
            if (slope == null) return;
            var maintenance = slope.GetComponent<SlopeMaintenanceRuntime>() ?? slope.gameObject.AddComponent<SlopeMaintenanceRuntime>();
            maintenance.GroomInstant();
            OffscreenOperationsToday++;
        }

        private void SortQueue() => Queue.Sort((a, b) => Score(b).CompareTo(Score(a)));

        private float Score(SlopeRuntime slope)
        {
            if (slope == null) return float.MinValue;
            var maintenance = slope.GetComponent<SlopeMaintenanceRuntime>();
            float snow = maintenance != null ? maintenance.snowCondition : 1f;
            float grooming = maintenance != null ? maintenance.grooming : 1f;
            return (1f - snow) * 70f + (1f - grooming) * 40f;
        }

        public void EvaluateAndDispatch()
        {
            if (!automatic || Active != null || Queue.Count == 0) return;
            var policy = GroomingPolicyRuntime.I;
            if (policy != null && policy.IsTycoonDayOnly)
            {
                ResolveOffscreenTycoonMaintenance();
                return;
            }
            if (policy != null && !policy.CanDispatch(emergencyDayDispatch)) return;

            SortQueue();
            var slope = Queue[0];
            if (!GroomingSlopeAccessRuntime.CanCloseForGrooming(slope))
            {
                Queue.RemoveAt(0);
                Queue.Add(slope);
                SortQueue();
                return;
            }

            Queue.RemoveAt(0);
            Active = slope;
            GroomingSlopeAccessRuntime.CloseForGrooming(slope);
            var maintenance = slope.GetComponent<SlopeMaintenanceRuntime>() ?? slope.gameObject.AddComponent<SlopeMaintenanceRuntime>();
            maintenance.BeginGrooming();
            var vehicle = FindObjectOfType<GroomingVehicleRuntime>();
            if (vehicle == null)
                vehicle = new GameObject("GroomingVehicle").AddComponent<GroomingVehicleRuntime>();
            vehicle.StartWork(slope, instantConstructionStyle);
        }

        public void NotifyFinished(SlopeRuntime slope)
        {
            if (Active != slope) return;
            Active = null;
            GroomingSlopeAccessRuntime.ReopenAfterGrooming(slope);
        }
    }
}
