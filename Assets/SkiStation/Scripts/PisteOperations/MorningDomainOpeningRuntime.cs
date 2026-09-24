using System;
using UnityEngine;

namespace SkiStation.PisteOperations
{
    /// <summary>
    /// Morning opening pass: the result of the night preparation becomes the
    /// actual operating state of the domain. It never damages equipment.
    /// </summary>
    public sealed class MorningDomainOpeningRuntime : MonoBehaviour
    {
        public static MorningDomainOpeningRuntime I { get; private set; }
        [Range(0f, 1f)] public float minimumSlopeQuality = .34f;
        [Range(0f, 1f)] public float minimumGrooming = .28f;
        public int OpenSlopes { get; private set; }
        public int ClosedSlopes { get; private set; }
        public int OpenLifts { get; private set; }
        public float DomainReadiness { get; private set; }
        public string LastReport { get; private set; } = "En attente de l'ouverture";
        public event Action Changed;
        private int evaluatedDay = -1;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
            if (GameClock.I != null) GameClock.I.MorningStarted += EvaluateMorningOpening;
        }

        void OnDestroy()
        {
            if (GameClock.I != null) GameClock.I.MorningStarted -= EvaluateMorningOpening;
            if (I == this) I = null;
        }

        void Update()
        {
            if (GameClock.I == null) return;
            // Also supports starting a scene directly after the morning event.
            if (evaluatedDay != GameClock.I.CurrentDay && GameClock.I.CurrentHour >= 6f && GameClock.I.CurrentHour < 6.25f)
                EvaluateMorningOpening();
        }

        public void EvaluateMorningOpening()
        {
            if (GameClock.I == null) return;
            if (evaluatedDay == GameClock.I.CurrentDay) return;
            evaluatedDay = GameClock.I.CurrentDay;

            var slopes = FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None);
            OpenSlopes = ClosedSlopes = 0;
            float total = 0f; int counted = 0;
            foreach (var slope in slopes)
            {
                if (slope == null) continue;
                var condition = slope.GetComponent<SlopeConditionRuntime>();
                bool advanced = SurfaceConditionOptionsRuntime.I == null || SurfaceConditionOptionsRuntime.I.AdvancedSurfaceConditions;
                bool safe = condition == null ||
                    (condition.SnowQuality01 >= minimumSlopeQuality && condition.GroomingProgress01 >= minimumGrooming && (!advanced || (condition.IceRisk01 < .78f && condition.SoupRisk01 < .92f)));
                if (safe && (MountainWeatherRuntime.I == null || MountainWeatherRuntime.I.Visibility01 >= .22f))
                {
                    slope.SetOpen(true); OpenSlopes++;
                }
                else
                {
                    slope.SetOpen(false); ClosedSlopes++;
                }
                if (condition != null) { total += condition.VisitorQualityFactor; counted++; }
            }

            var lifts = FindObjectsByType<LiftRuntime>(FindObjectsSortMode.None);
            OpenLifts = 0;
            foreach (var lift in lifts)
            {
                if (lift == null || lift.Faulted) continue;
                if (MountainWeatherRuntime.I == null || MountainWeatherRuntime.I.Visibility01 >= .22f)
                {
                    lift.SetOpen(true); OpenLifts++;
                }
                else lift.SetOpen(false);
            }

            DomainReadiness = counted == 0 ? (OpenSlopes > 0 ? 100f : 0f) : Mathf.Clamp01(total / counted) * 100f;
            LastReport = string.Format("Ouverture J{0} : {1} piste(s), {2} fermée(s), {3} remontée(s) — préparation {4:0}%",
                GameClock.I.CurrentDay, OpenSlopes, ClosedSlopes, OpenLifts, DomainReadiness);
            Changed?.Invoke();
        }

        public void ForceRecheck()
        {
            evaluatedDay = -1;
            EvaluateMorningOpening();
        }
    }
}
