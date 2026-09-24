using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// 0.96: observes the existing management simulation and exposes one simple domain-flow snapshot.
    /// It never spawns visitors, vehicles, ghosts or spectators.
    /// </summary>
    public sealed class SkiDomainFlowRuntime : MonoBehaviour
    {
        public static SkiDomainFlowRuntime I { get; private set; }
        public SkiDomainFlowSnapshot Snapshot { get; private set; }
        [SerializeField] private float refreshSeconds = 0.5f;
        private float nextRefresh;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
            Refresh();
        }

        private void OnDestroy() { if (I == this) I = null; }

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + Mathf.Max(0.1f, refreshSeconds);
            Refresh();
        }

        public void Refresh()
        {
            int total = 0, skiing = 0, waiting = 0, riding = 0, resting = 0, arriving = 0, leaving = 0;
            float satisfaction = 0f;
            var skiers = FindObjectsOfType<SkierRuntime>();
            for (int i = 0; i < skiers.Length; i++)
            {
                var s = skiers[i]; if (s == null) continue;
                total++; satisfaction += s.Satisfaction;
                switch (s.State)
                {
                    case SkierState.Skiing: skiing++; break;
                    case SkierState.WaitingForLift: waiting++; break;
                    case SkierState.RidingLift: riding++; break;
                    case SkierState.Resting: resting++; break;
                    case SkierState.Arriving: arriving++; break;
                    case SkierState.Leaving: leaving++; break;
                }
            }

            int openSlopes = 0;
            var slopes = FindObjectsOfType<SlopeRuntime>();
            for (int i = 0; i < slopes.Length; i++) if (slopes[i] != null && slopes[i].Open) openSlopes++;

            int openLifts = 0;
            var lifts = FindObjectsOfType<LiftRuntime>();
            for (int i = 0; i < lifts.Length; i++) if (lifts[i] != null && lifts[i].Open && !lifts[i].Faulted) openLifts++;

            int vehicles = StationRoadVehicleRuntime.I != null ? StationRoadVehicleRuntime.I.ActiveVehicleCount : 0;
            Snapshot = new SkiDomainFlowSnapshot(total, skiing, waiting, riding, resting, arriving, leaving,
                openSlopes, openLifts, vehicles, total > 0 ? satisfaction / total : 0f);
        }
    }
}
