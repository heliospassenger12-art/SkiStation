using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum SeasonDayKind361 { Normal, Weekend, Holiday, PeakHoliday, Event }

    [Serializable]
    public struct SeasonCalendarDay361
    {
        public int day;
        public SeasonDayKind361 kind;
        public string label;
        public string detail;
        public float demandMultiplier;
        public bool competitionWindow;
        public CompetitionLevel suggestedCompetition;
    }

    /// <summary>
    /// 3.61 - Predictable season rhythm used for planning. The calendar never spawns visitors itself:
    /// it only provides a bounded multiplier consumed by the existing real arrival runtime.
    /// </summary>
    public sealed class SeasonCalendarRuntime361 : MonoBehaviour
    {
        public static SeasonCalendarRuntime361 I { get; private set; }
        [SerializeField, Min(14)] private int seasonLengthDays = 42;
        [SerializeField, Range(1f, 1.5f)] private float weekendDemand = 1.12f;
        [SerializeField, Range(1f, 1.6f)] private float holidayDemand = 1.22f;
        [SerializeField, Range(1f, 1.7f)] private float peakHolidayDemand = 1.32f;
        [SerializeField, Range(1f, 1.5f)] private float eventDemand = 1.16f;

        public int CurrentDay => GameClock.I != null ? GameClock.I.CurrentDay : (StationRuntime.I != null ? StationRuntime.I.State.day : 1);
        internal int SeasonLengthDays => Mathf.Max(14, seasonLengthDays);
        public int SeasonDay => ((Mathf.Max(1, CurrentDay) - 1) % Mathf.Max(14, seasonLengthDays)) + 1;
        public int SeasonNumber => ((Mathf.Max(1, CurrentDay) - 1) / Mathf.Max(14, seasonLengthDays)) + 1;
        public SeasonCalendarDay361 Today => GetDay(CurrentDay);
        public float TodayDemandMultiplier => Today.demandMultiplier;
        public string TodayLabel => Today.label;
        public string PlanningHint => BuildPlanningHint();

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy() { if (I == this) I = null; }

        public SeasonCalendarDay361 GetDay(int absoluteDay)
        {
            int safe = Mathf.Max(1, absoluteDay);
            int sd = ((safe - 1) % Mathf.Max(14, seasonLengthDays)) + 1;
            int weekday = ((safe - 1) % 7) + 1; // 1=lundi, 6=samedi, 7=dimanche
            bool weekend = weekday >= 6;

            SeasonDayKind361 kind = weekend ? SeasonDayKind361.Weekend : SeasonDayKind361.Normal;
            string label = weekend ? "Week-end" : "Journée normale";
            string detail = weekend ? "Fréquentation renforcée : surveillez les files." : "Demande standard : bon jour pour construire et observer.";
            float demand = weekend ? weekendDemand : 1f;

            // Two readable holiday waves in each 42-day season.
            bool holiday = (sd >= 8 && sd <= 14) || (sd >= 29 && sd <= 35);
            bool peak = sd == 11 || sd == 12 || sd == 32 || sd == 33;
            if (holiday)
            {
                kind = peak ? SeasonDayKind361.PeakHoliday : SeasonDayKind361.Holiday;
                label = peak ? "Pic vacances" : "Vacances scolaires";
                detail = peak ? "Très forte demande prévue : capacité et services doivent être prêts." : "Demande élevée : anticipez remontées, restauration et location.";
                demand = peak ? peakHolidayDemand : holidayDemand;
            }

            CompetitionLevel suggested;
            if (TryCompetitionWindow(sd, out suggested))
            {
                kind = SeasonDayKind361.Event;
                string comp = CompetitionLabel(suggested);
                label = "Fenêtre événement — " + comp;
                detail = IsCompetitionUnlocked(suggested)
                    ? "Créneau favorable : vous pouvez organiser cet événement si l'infrastructure est prête."
                    : "Créneau annoncé : augmentez le prestige pour débloquer cet événement.";
                demand = Mathf.Max(demand, eventDemand);
                return new SeasonCalendarDay361 { day = safe, kind = kind, label = label, detail = detail, demandMultiplier = Mathf.Clamp(demand, .90f, 1.35f), competitionWindow = true, suggestedCompetition = suggested };
            }

            return new SeasonCalendarDay361 { day = safe, kind = kind, label = label, detail = detail, demandMultiplier = Mathf.Clamp(demand, .90f, 1.35f), competitionWindow = false, suggestedCompetition = CompetitionLevel.None };
        }

        public SeasonCalendarDay361[] GetUpcoming(int count)
        {
            count = Mathf.Clamp(count, 1, 14);
            var result = new SeasonCalendarDay361[count];
            for (int i = 0; i < count; i++) result[i] = GetDay(CurrentDay + i);
            return result;
        }

        public int DaysUntilNextPeak()
        {
            for (int offset = 0; offset <= seasonLengthDays; offset++)
            {
                var d = GetDay(CurrentDay + offset);
                if (d.demandMultiplier >= 1.20f) return offset;
            }
            return -1;
        }

        private string BuildPlanningHint()
        {
            int days = DaysUntilNextPeak();
            if (days == 0) return "Pic de fréquentation aujourd'hui : surveillez files et capacité.";
            if (days == 1) return "Pic de fréquentation demain : dernier jour pour renforcer la station.";
            if (days > 1 && days <= 4) return "Pic de fréquentation dans " + days + " jours : préparez vos investissements.";
            return "Pas de pic majeur immédiat : bon moment pour développer la station.";
        }

        private static bool TryCompetitionWindow(int seasonDay, out CompetitionLevel level)
        {
            if (seasonDay == 18) { level = CompetitionLevel.Regional; return true; }
            if (seasonDay == 27) { level = CompetitionLevel.National; return true; }
            if (seasonDay == 40) { level = CompetitionLevel.International; return true; }
            level = CompetitionLevel.None;
            return false;
        }

        private static bool IsCompetitionUnlocked(CompetitionLevel level)
        {
            var p = CareerPrestigeRuntime357.I;
            if (p == null || CareerProgressionRuntime.I == null || CareerProgressionRuntime.I.IsSandbox) return true;
            if (level == CompetitionLevel.Regional) return p.RegionalCompetitionUnlocked;
            if (level == CompetitionLevel.National) return p.NationalCompetitionUnlocked;
            if (level == CompetitionLevel.International) return p.OlympicObjectiveUnlocked;
            return true;
        }

        private static string CompetitionLabel(CompetitionLevel level)
        {
            switch (level)
            {
                case CompetitionLevel.Regional: return "compétition régionale";
                case CompetitionLevel.National: return "compétition nationale";
                case CompetitionLevel.International: return "événement international";
                default: return "événement";
            }
        }
    }

    public static class SeasonCalendarBootstrap361
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;
            if (SeasonCalendarRuntime361.I != null) return;
            new GameObject("Season Calendar 3.61").AddComponent<SeasonCalendarRuntime361>();
        }
    }
}
