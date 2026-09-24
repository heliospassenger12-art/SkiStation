using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum LiftMaintenanceStatus
    {
        Operational,
        AttentionRequired,
        PreventiveMaintenance,
        CorrectiveMaintenance,
        Closed
    }

    [Serializable]
    public class LiftMaintenanceSnapshot
    {
        public string liftId;
        public string displayName;
        public LiftMaintenanceStatus status;
        public float condition01;
        public float wear01;
        public float breakdownRisk01;
        public float operatingIntensity01;
        public float weatherStress01;
        public float maintenanceProgress01;
        public float estimatedMaintenanceCost;
        public int breakdownCount;
        public int maintenanceCount;
        public int downtimeHours;
        public bool canOperate;
    }

    public class LiftMaintenanceRuntime : MonoBehaviour
    {
        public static LiftMaintenanceRuntime Instance { get; private set; }

        [Header("Wear")]
        [Range(0f, 1f)] public float dailyBaseWear = 0.008f;
        [Range(0f, 1f)] public float queueWearFactor = 0.012f;
        [Range(0f, 1f)] public float weatherWearFactor = 0.010f;
        [Range(0f, 1f)] public float preventiveThreshold = 0.70f;
        [Range(0f, 1f)] public float criticalThreshold = 0.35f;

        [Header("Maintenance")]
        public float preventiveCost = 3500f;
        public float correctiveCost = 9000f;
        public float preventiveHours = 4f;
        public float correctiveHours = 10f;

        private readonly Dictionary<string, LiftMaintenanceSnapshot> states =
            new Dictionary<string, LiftMaintenanceSnapshot>();
        private readonly Dictionary<string, bool> wasOpenBeforeMaintenance =
            new Dictionary<string, bool>();

        public event Action Changed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (Time.frameCount % 60 != 0) return;
            SimulateWear();
        }

        public LiftMaintenanceSnapshot GetOrCreate(string liftId, string displayName)
        {
            if (string.IsNullOrEmpty(liftId)) return null;

            LiftMaintenanceSnapshot state;
            if (!states.TryGetValue(liftId, out state))
            {
                state = new LiftMaintenanceSnapshot
                {
                    liftId = liftId,
                    displayName = string.IsNullOrEmpty(displayName) ? liftId : displayName,
                    status = LiftMaintenanceStatus.Operational,
                    condition01 = 1f,
                    wear01 = 0f,
                    canOperate = true
                };
                states.Add(liftId, state);
            }
            return state;
        }

        public bool RequestPreventiveMaintenance(string liftId)
        {
            var state = GetOrCreate(liftId, liftId);
            if (state == null || state.status == LiftMaintenanceStatus.PreventiveMaintenance) return false;

            var lift = FindLift(liftId);
            if (lift != null && !wasOpenBeforeMaintenance.ContainsKey(liftId))
                wasOpenBeforeMaintenance[liftId] = lift.Open;
            if (lift != null) lift.SetOpen(false);

            state.status = LiftMaintenanceStatus.PreventiveMaintenance;
            state.maintenanceProgress01 = 0f;
            state.estimatedMaintenanceCost = preventiveCost;
            state.canOperate = false;
            state.maintenanceCount++;
            state.downtimeHours += Mathf.CeilToInt(preventiveHours);
            Changed?.Invoke();
            return true;
        }

        public bool RequestCorrectiveMaintenance(string liftId)
        {
            var state = GetOrCreate(liftId, liftId);
            if (state == null) return false;

            var lift = FindLift(liftId);
            if (lift != null && !wasOpenBeforeMaintenance.ContainsKey(liftId))
                wasOpenBeforeMaintenance[liftId] = lift.Open;
            if (lift != null) lift.SetOpen(false);

            state.status = LiftMaintenanceStatus.CorrectiveMaintenance;
            state.maintenanceProgress01 = 0f;
            state.estimatedMaintenanceCost = correctiveCost;
            state.canOperate = false;
            state.maintenanceCount++;
            state.downtimeHours += Mathf.CeilToInt(correctiveHours);
            Changed?.Invoke();
            return true;
        }

        public bool CloseForMaintenance(string liftId)
        {
            var state = GetOrCreate(liftId, liftId);
            if (state == null) return false;
            state.status = LiftMaintenanceStatus.Closed;
            state.canOperate = false;
            var lift = FindLift(liftId);
            if (lift != null) lift.SetOpen(false);
            Changed?.Invoke();
            return true;
        }

        public bool Reopen(string liftId)
        {
            var state = GetOrCreate(liftId, liftId);
            if (state == null) return false;
            state.status = LiftMaintenanceStatus.Operational;
            state.canOperate = true;
            var lift = FindLift(liftId);
            if (lift != null)
            {
                bool reopen = !wasOpenBeforeMaintenance.TryGetValue(liftId, out var previous) || previous;
                if (reopen && !lift.Faulted) lift.SetOpen(true);
                wasOpenBeforeMaintenance.Remove(liftId);
            }
            Changed?.Invoke();
            return true;
        }

        private void SimulateWear()
        {
            foreach (var state in states.Values)
            {
                if (!state.canOperate) continue;

                float pressure = Mathf.Clamp01(state.operatingIntensity01);
                float stress = Mathf.Clamp01(state.weatherStress01);
                float delta = dailyBaseWear * 0.002f +
                              pressure * queueWearFactor * 0.002f +
                              stress * weatherWearFactor * 0.002f;

                if (LiftTechnologyRuntime.Instance != null)
                {
                    float techWear = LiftTechnologyRuntime.Instance.GetWearMultiplier(state.liftId);
                    delta *= Mathf.Max(0.1f, techWear);
                }

                state.wear01 = Mathf.Clamp01(state.wear01 + delta);
                state.condition01 = 1f - state.wear01;
                state.breakdownRisk01 = Mathf.Clamp01(
                    Mathf.Pow(1f - state.condition01, 2.2f) * 0.8f);

                if (state.condition01 <= criticalThreshold)
                    state.status = LiftMaintenanceStatus.AttentionRequired;
                else if (state.condition01 <= preventiveThreshold)
                    state.status = LiftMaintenanceStatus.AttentionRequired;
                else
                    state.status = LiftMaintenanceStatus.Operational;
            }
            Changed?.Invoke();
        }

        public void SetOperatingStress(string liftId, float intensity01, float weatherStress01)
        {
            var state = GetOrCreate(liftId, liftId);
            if (state == null) return;
            state.operatingIntensity01 = Mathf.Clamp01(intensity01);
            state.weatherStress01 = Mathf.Clamp01(weatherStress01);
        }

        private LiftRuntime FindLift(string liftId)
        {
            if (string.IsNullOrEmpty(liftId)) return null;
            var network = ResortRouteNetwork.I;
            if (network != null)
                foreach (var lift in network.Lifts)
                    if (lift != null && lift.LiftId == liftId) return lift;
            var all = FindObjectsOfType<LiftRuntime>();
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && all[i].LiftId == liftId) return all[i];
            return null;
        }

        public IReadOnlyCollection<LiftMaintenanceSnapshot> GetAll()
        {
            return states.Values;
        }
    }
}
