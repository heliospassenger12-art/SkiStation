using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// Records real visitor experiences on slopes and feeds the satisfaction loop.
    /// Lightweight by design: the skier movement remains authoritative in SkierRouteAgent.
    /// </summary>
    public sealed class SlopeVisitorExperienceRuntime : MonoBehaviour
    {
        public static SlopeVisitorExperienceRuntime I { get; private set; }

        [Serializable]
        public struct SlopeProfileStats
        {
            public int runs;
            public float satisfactionSum;
            public int satisfiedRuns;
        }

        private readonly Dictionary<SlopeRuntime, Dictionary<CustomerProfile, SlopeProfileStats>> stats = new();
        public event Action Changed;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        public void RecordCompletion(SkierRuntime skier, SlopeRuntime slope)
        {
            if (skier == null || slope == null) return;
            CustomerProfile profile = skier.PopulationProfile != null ? skier.PopulationProfile.ToCustomerProfile() : CustomerProfile.Tourist;
            float fit = DifficultyFit(profile, slope.Difficulty);
            float lengthScore = Mathf.Clamp01(slope.Length / 2500f);
            float widthScore = Mathf.Clamp01((slope.AverageWidth - 4f) / 16f);
            float trafficScore = 1f - Mathf.Clamp01(slope.TrafficDensity * 900f);
            var condition = SlopeConditionRuntime.Get(slope);
            float snowScore = condition != null ? Mathf.Clamp01(condition.VisitorQualityFactor / 1.08f) : 1f;
            float experience = Mathf.Clamp01(fit * .50f + lengthScore * .14f + widthScore * .10f + trafficScore * .16f + snowScore * .10f);
            float satisfactionDelta = Mathf.Lerp(-9f, 6f, experience);
            skier.ChangeSatisfaction(satisfactionDelta);
            slope.RegisterCompletedRun(experience);

            if (!stats.TryGetValue(slope, out var byProfile))
            {
                byProfile = new Dictionary<CustomerProfile, SlopeProfileStats>();
                stats[slope] = byProfile;
            }
            if (!byProfile.TryGetValue(profile, out var value)) value = new SlopeProfileStats();
            value.runs++;
            value.satisfactionSum += skier.Satisfaction;
            if (experience >= .60f) value.satisfiedRuns++;
            byProfile[profile] = value;
            Changed?.Invoke();
        }

        public int GetRuns(SlopeRuntime slope, CustomerProfile profile)
        {
            return stats.TryGetValue(slope, out var byProfile) && byProfile.TryGetValue(profile, out var v) ? v.runs : 0;
        }

        public float GetAverageSatisfaction(SlopeRuntime slope, CustomerProfile profile)
        {
            if (!stats.TryGetValue(slope, out var byProfile) || !byProfile.TryGetValue(profile, out var v) || v.runs == 0) return 0f;
            return v.satisfactionSum / v.runs;
        }

        public static float DifficultyFit(CustomerProfile profile, SlopeDifficulty difficulty)
        {
            int ability = MaxDifficulty(profile);
            int required = (int)difficulty;
            if (required > ability) return Mathf.Clamp01(1f - (required - ability) * .75f);
            float margin = ability - required;
            return Mathf.Clamp01(1f - margin * .12f);
        }

        public static int MaxDifficulty(CustomerProfile profile)
        {
            switch (profile)
            {
                case CustomerProfile.Beginner: return (int)SlopeDifficulty.Green;
                case CustomerProfile.Family: return (int)SlopeDifficulty.Blue;
                case CustomerProfile.Youth: return (int)SlopeDifficulty.Blue;
                case CustomerProfile.Intermediate: return (int)SlopeDifficulty.Red;
                case CustomerProfile.Tourist: return (int)SlopeDifficulty.Blue;
                case CustomerProfile.Premium: return (int)SlopeDifficulty.Blue;
                case CustomerProfile.Advanced: return (int)SlopeDifficulty.Black;
                case CustomerProfile.Expert: return (int)SlopeDifficulty.Extreme;
                case CustomerProfile.Athlete: return (int)SlopeDifficulty.Extreme;
                default: return (int)SlopeDifficulty.Blue;
            }
        }
    }
}
