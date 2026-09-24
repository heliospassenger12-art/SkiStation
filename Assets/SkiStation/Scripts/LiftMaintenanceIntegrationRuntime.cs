using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum LiftMaintenanceAction
    {
        Preventive,
        Corrective,
        SlowOperation,
        Close
    }

    [Serializable]
    public class LiftMaintenanceRecord
    {
        public string liftId;
        public LiftMaintenanceAction action;
        public int day;
        public float hour;
        public float cost;
        public float durationHours;
        public string note;
    }

    public class LiftMaintenanceIntegrationRuntime : MonoBehaviour
    {
        public static LiftMaintenanceIntegrationRuntime Instance { get; private set; }

        public float preventiveCost = 3500f;
        public float correctiveCost = 9000f;
        public float preventiveDurationHours = 4f;
        public float correctiveDurationHours = 10f;
        public float slowOperationWearMultiplier = 0.55f;

        private readonly Dictionary<string, float> remainingHours = new();
        private readonly Dictionary<string, LiftMaintenanceAction> activeActions = new();
        private readonly List<LiftMaintenanceRecord> history = new();

        public event Action Changed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            var clock = GameClock.I;
            if (clock == null) return;

            foreach (var id in new List<string>(remainingHours.Keys))
            {
                if (remainingHours[id] <= 0f) continue;
                remainingHours[id] -= Time.deltaTime * Mathf.Max(0.01f, GameSpeedController.I != null ? GameSpeedController.I.CurrentScale : 1f);
                if (remainingHours[id] <= 0f)
                    CompleteAction(id);
            }
        }

        public bool StartPreventive(string liftId)
        {
            return StartAction(liftId, LiftMaintenanceAction.Preventive,
                preventiveCost, preventiveDurationHours, "Maintenance préventive");
        }

        public bool StartCorrective(string liftId)
        {
            return StartAction(liftId, LiftMaintenanceAction.Corrective,
                correctiveCost, correctiveDurationHours, "Maintenance corrective");
        }

        public bool StartAction(string liftId, LiftMaintenanceAction action,
            float cost, float durationHours, string note)
        {
            if (string.IsNullOrEmpty(liftId) || remainingHours.ContainsKey(liftId))
                return false;

            if (StationRuntime.Instance != null &&
                !StationRuntime.Instance.Spend(cost))
                return false;

            remainingHours[liftId] = Mathf.Max(0.1f, durationHours);
            activeActions[liftId] = action;

            var clock = GameClock.I;
            history.Add(new LiftMaintenanceRecord {
                liftId = liftId, action = action,
                day = clock != null ? clock.CurrentDay : 0,
                hour = clock != null ? clock.CurrentHour : 0f,
                cost = cost, durationHours = durationHours, note = note
            });

            Changed?.Invoke();
            return true;
        }

        public bool IsUnderMaintenance(string liftId)
        {
            return !string.IsNullOrEmpty(liftId) && remainingHours.ContainsKey(liftId);
        }

        public float GetRemainingHours(string liftId)
        {
            float v;
            return remainingHours.TryGetValue(liftId, out v) ? Mathf.Max(0f, v) : 0f;
        }

        public LiftMaintenanceAction GetActiveAction(string liftId)
        {
            LiftMaintenanceAction a;
            return activeActions.TryGetValue(liftId, out a) ? a : LiftMaintenanceAction.Close;
        }

        public IReadOnlyList<LiftMaintenanceRecord> GetHistory() => history;

        private void CompleteAction(string liftId)
        {
            remainingHours.Remove(liftId);
            activeActions.Remove(liftId);

            if (LiftMaintenanceRuntime.Instance != null)
                LiftMaintenanceRuntime.Instance.Reopen(liftId);

            Changed?.Invoke();
        }
    }
}
