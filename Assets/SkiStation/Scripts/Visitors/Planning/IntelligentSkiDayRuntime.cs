using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum SkiDayGoal
    {
        WarmUp,
        Explore,
        Vertical,
        Progress,
        FamilyCruise,
        Training,
        AvoidQueues,
        Lunch,
        Recover,
        ReturnToBase
    }

    [Serializable]
    public struct SkiDayMindSnapshot
    {
        public SkiDayGoal goal;
        public float energy01;
        public float hunger01;
        public float exploration01;
        public float queuePatience01;
        public float verticalAppetite01;
        public float goalConfidence01;
        public int runsToday;
        public int liftsToday;
        public int day;
    }

    /// <summary>
    /// 1.78 high-level decision layer for a skier's day. It does not move agents or create
    /// visitors: existing lift/slope/service runtimes remain authoritative. This component
    /// only supplies a coherent goal and scoring modifiers to the network-aware navigator.
    /// </summary>
    public sealed class IntelligentSkiDayRuntime : MonoBehaviour
    {
        [SerializeField] private SkierRuntime skier;
        [SerializeField] private float refreshSeconds = 2f;
        [SerializeField] private float lunchStartHour = 11.5f;
        [SerializeField] private float lunchEndHour = 14.25f;
        [SerializeField] private float returnToBaseHour = 16.35f;

        public SkiDayMindSnapshot Mind { get; private set; }
        public event Action Changed;
        private float timer;

        private void Awake()
        {
            if (skier == null) skier = GetComponent<SkierRuntime>();
            ResetForDay(CurrentDay());
        }

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < Mathf.Max(.5f, refreshSeconds)) return;
            timer = 0f;
            if (Mind.day != CurrentDay()) ResetForDay(CurrentDay());
            UpdateNeeds();
            ChooseGoal();
        }

        public void RegisterSlopeCompleted(SlopeRuntime slope)
        {
            SkiDayMindSnapshot m = Mind;
            m.runsToday++;
            m.energy01 = Mathf.Clamp01(m.energy01 - .055f - Difficulty01(slope) * .025f);
            m.hunger01 = Mathf.Clamp01(m.hunger01 + .035f);
            m.exploration01 = Mathf.Clamp01(m.exploration01 + .04f);
            Mind = m;
            ChooseGoal();
        }

        public void RegisterLiftCompleted(LiftRuntime lift)
        {
            SkiDayMindSnapshot m = Mind;
            m.liftsToday++;
            m.energy01 = Mathf.Clamp01(m.energy01 + .018f);
            Mind = m;
        }

        public void RegisterMealOrRest(bool meal)
        {
            SkiDayMindSnapshot m = Mind;
            if (meal) m.hunger01 = Mathf.Clamp01(m.hunger01 - .72f);
            m.energy01 = Mathf.Clamp01(m.energy01 + (meal ? .20f : .30f));
            Mind = m;
            ChooseGoal();
        }

        public float ScoreSlope(SlopeRuntime slope)
        {
            if (slope == null) return -1000f;
            float difficulty = Difficulty01(slope);
            float length = Mathf.Clamp01(slope.Length / 3000f);
            float congestion = Mathf.Clamp01(slope.TrafficDensity);
            float score = -congestion * (20f + 35f * (1f - Mind.queuePatience01));

            switch (Mind.goal)
            {
                case SkiDayGoal.WarmUp: score += (1f - difficulty) * 30f; break;
                case SkiDayGoal.Explore: score += length * 22f + Mind.exploration01 * 8f; break;
                case SkiDayGoal.Vertical: score += length * 30f + difficulty * 10f; break;
                case SkiDayGoal.Progress: score += 25f - Mathf.Abs(difficulty - PreferredDifficulty01()) * 45f; break;
                case SkiDayGoal.FamilyCruise: score += (1f - difficulty) * 38f - congestion * 18f; break;
                case SkiDayGoal.Training: score += difficulty * 32f + length * 20f; break;
                case SkiDayGoal.AvoidQueues: score -= congestion * 50f; break;
                case SkiDayGoal.Recover: score += (1f - difficulty) * 20f; break;
                case SkiDayGoal.ReturnToBase: score += (1f - difficulty) * 10f; break;
            }
            return score;
        }

        public float ScoreLift(LiftRuntime lift)
        {
            if (lift == null) return -1000f;
            float pressure = InfrastructureCongestionRuntime.I != null
                ? InfrastructureCongestionRuntime.I.GetLiftPressure01(lift) : 0f;
            float score = -pressure * (25f + 45f * (1f - Mind.queuePatience01));
            if (Mind.goal == SkiDayGoal.Vertical || Mind.goal == SkiDayGoal.Training)
                score += Mathf.Clamp01(lift.EffectiveSpeedKmh / 30f) * 25f;
            if (Mind.goal == SkiDayGoal.FamilyCruise && lift.Definition != null && lift.Definition.detachable)
                score += 8f;
            return score;
        }

        private void ResetForDay(int day)
        {
            SkierPopulationProfile p = skier != null ? skier.PopulationProfile : null;
            float patience = p != null && p.Archetype == SkierPopulationArchetype.Family ? .72f : .52f;
            float vertical = p != null && p.IsEliteAthlete ? .95f : .58f;
            Mind = new SkiDayMindSnapshot
            {
                goal = SkiDayGoal.WarmUp,
                energy01 = 1f,
                hunger01 = .08f,
                exploration01 = .15f,
                queuePatience01 = patience,
                verticalAppetite01 = vertical,
                goalConfidence01 = .8f,
                runsToday = 0,
                liftsToday = 0,
                day = day
            };
            Changed?.Invoke();
        }

        private void UpdateNeeds()
        {
            SkiDayMindSnapshot m = Mind;
            float dtHours = Time.unscaledDeltaTime / 3600f;
            m.hunger01 = Mathf.Clamp01(m.hunger01 + dtHours * .30f);
            if (skier != null && skier.State == SkierState.Resting)
                m.energy01 = Mathf.Clamp01(m.energy01 + dtHours * .45f);
            Mind = m;
        }

        private void ChooseGoal()
        {
            SkiDayMindSnapshot m = Mind;
            SkiDayGoal old = m.goal;
            float hour = GameClock.I != null ? GameClock.I.CurrentHour : 10f;
            SkierPopulationProfile p = skier != null ? skier.PopulationProfile : null;
            VisitorIntent intent = VisitorIntentRuntime.I != null && skier != null
                ? VisitorIntentRuntime.I.GetIntent(skier) : VisitorIntent.SkiDay;

            if (hour >= returnToBaseHour) m.goal = SkiDayGoal.ReturnToBase;
            else if (m.hunger01 >= .66f && hour >= lunchStartHour && hour <= lunchEndHour) m.goal = SkiDayGoal.Lunch;
            else if (m.energy01 <= .30f) m.goal = SkiDayGoal.Recover;
            else if (m.runsToday < 2) m.goal = SkiDayGoal.WarmUp;
            else if (p != null && p.Archetype == SkierPopulationArchetype.Family) m.goal = SkiDayGoal.FamilyCruise;
            else if (intent == VisitorIntent.SportPerformance || intent == VisitorIntent.Training) m.goal = SkiDayGoal.Training;
            else if (intent == VisitorIntent.MountainDiscovery) m.goal = SkiDayGoal.Explore;
            else if (m.verticalAppetite01 > .72f) m.goal = SkiDayGoal.Vertical;
            else if (m.queuePatience01 < .40f) m.goal = SkiDayGoal.AvoidQueues;
            else m.goal = SkiDayGoal.Progress;

            m.goalConfidence01 = old == m.goal ? Mathf.MoveTowards(m.goalConfidence01, .95f, .04f) : .72f;
            Mind = m;
            if (old != m.goal) Changed?.Invoke();
        }

        private float PreferredDifficulty01()
        {
            SkierPopulationProfile p = skier != null ? skier.PopulationProfile : null;
            if (p == null) return .45f;
            if (p.IsEliteAthlete) return .92f;
            if (p.IsChild) return .18f;
            if (p.Archetype == SkierPopulationArchetype.Family) return .28f;
            return .52f;
        }

        private static float Difficulty01(SlopeRuntime slope)
        {
            if (slope == null) return 0f;
            switch (slope.Difficulty)
            {
                case SlopeDifficulty.Green: return .15f;
                case SlopeDifficulty.Blue: return .35f;
                case SlopeDifficulty.Red: return .62f;
                case SlopeDifficulty.Black: return .84f;
                case SlopeDifficulty.Extreme: return 1f;
                default: return .5f;
            }
        }

        private static int CurrentDay() { return GameClock.I != null ? GameClock.I.CurrentDay : 1; }
    }
}
