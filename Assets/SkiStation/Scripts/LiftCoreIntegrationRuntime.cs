using System;
using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// 0.77 integration layer: keeps lift performance, maintenance and technology
    /// synchronized without creating visitors or replacing the management source of truth.
    /// Wind is intentionally absent from all technical wear/failure calculations.
    /// </summary>
    public sealed class LiftCoreIntegrationRuntime : MonoBehaviour
    {
        public static LiftCoreIntegrationRuntime I { get; private set; }
        [SerializeField] private float refreshSeconds = 1f;
        private float timer;
        public event Action Changed;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < Mathf.Max(.2f, refreshSeconds)) return;
            timer = 0f;
            RefreshLifts();
        }

        public void RefreshLifts()
        {
            var network = ResortRouteNetwork.I;
            if (network == null) return;

            foreach (var lift in network.Lifts)
            {
                if (lift == null || lift.Definition == null) continue;
                bool maintenanceBlocked = LiftMaintenanceIntegrationRuntime.Instance != null &&
                    LiftMaintenanceIntegrationRuntime.Instance.IsUnderMaintenance(lift.LiftId);
                bool maintenanceCanOperate = true;
                if (LiftMaintenanceRuntime.Instance != null)
                {
                    var state = LiftMaintenanceRuntime.Instance.GetOrCreate(lift.LiftId, lift.Definition.displayName);
                    maintenanceCanOperate = state == null || state.canOperate;
                }

                if (maintenanceBlocked || !maintenanceCanOperate)
                {
                    if (lift.Open) lift.SetOpen(false);
                }
                else if (!lift.Faulted && lift.CurrentSpeedKmh <= 0.01f && lift.Open)
                {
                    lift.SetFaulted(false);
                }

                if (LiftMaintenanceRuntime.Instance != null)
                {
                    float pressure = InfrastructureCongestionRuntime.I != null
                        ? InfrastructureCongestionRuntime.I.GetLiftPressure01(lift) : 0f;
                    float intensity = Mathf.Clamp01(.35f + pressure * .65f);
                    LiftMaintenanceRuntime.Instance.SetOperatingStress(lift.LiftId, intensity, 0f);
                }
            }
            Changed?.Invoke();
        }
    }
}
