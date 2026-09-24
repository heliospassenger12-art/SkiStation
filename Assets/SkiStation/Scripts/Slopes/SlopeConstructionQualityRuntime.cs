using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum SlopeConstructionInvestment { Economy, Standard, Premium }
    public enum SlopeQualityTier { Basic, Good, Excellent, Elite }

    /// <summary>
    /// Stores the construction quality of a real piste. Quality is derived from the route,
    /// terrain conformity, profile and the investment chosen by the player.
    /// </summary>
    public sealed class SlopeConstructionQualityRuntime : MonoBehaviour
    {
        public float QualityScore { get; private set; }
        public SlopeQualityTier Tier { get; private set; } = SlopeQualityTier.Basic;
        public SlopeConstructionInvestment Investment { get; private set; } = SlopeConstructionInvestment.Standard;
        public bool Evaluated { get; private set; }
        public float InvestmentMultiplier => Investment == SlopeConstructionInvestment.Economy ? 0.82f
            : Investment == SlopeConstructionInvestment.Premium ? 1.32f : 1f;

        public void Evaluate(SlopeConstructionInvestment investment)
        {
            Investment = investment;
            var slope = GetComponent<SlopeRuntime>();
            if (slope == null || slope.Points.Count < 2)
            {
                QualityScore = 0f;
                Tier = SlopeQualityTier.Basic;
                Evaluated = false;
                return;
            }

            float score = 62f;
            var conformity = GetComponent<SlopeTerrainConformityRuntime>();
            if (conformity != null && conformity.LastConformSucceeded) score += 12f;

            var planner = GetComponent<SlopeTerrainWorkPlanner>();
            if (planner != null && planner.HasPlan)
            {
                score += planner.MaxRequiredWork <= 2f ? 10f : planner.MaxRequiredWork <= 6f ? 5f : 0f;
            }
            else score += 4f;

            var profile = GetComponent<SlopeProfileRuntime>();
            if (profile != null && profile.Applied) score += 7f;

            score += WidthConsistencyBonus(slope);
            if (investment == SlopeConstructionInvestment.Economy) score -= 10f;
            if (investment == SlopeConstructionInvestment.Premium) score += 13f;

            QualityScore = Mathf.Clamp(score, 0f, 100f);
            Tier = QualityScore >= 90f ? SlopeQualityTier.Elite
                : QualityScore >= 78f ? SlopeQualityTier.Excellent
                : QualityScore >= 65f ? SlopeQualityTier.Good
                : SlopeQualityTier.Basic;
            Evaluated = true;
        }

        static float WidthConsistencyBonus(SlopeRuntime slope)
        {
            var widths = slope.Widths;
            if (widths == null || widths.Count < 2) return 0f;
            float avg = 0f;
            for (int i = 0; i < widths.Count; i++) avg += widths[i];
            avg /= widths.Count;
            if (avg <= 0.01f) return 0f;
            float variance = 0f;
            for (int i = 0; i < widths.Count; i++)
            {
                float d = widths[i] - avg;
                variance += d * d;
            }
            float cv = Mathf.Sqrt(variance / widths.Count) / avg;
            return Mathf.Clamp(8f * (1f - cv * 2.5f), 0f, 8f);
        }

        public string GetTierLabel()
        {
            switch (Tier)
            {
                case SlopeQualityTier.Elite: return "Élite";
                case SlopeQualityTier.Excellent: return "Excellente";
                case SlopeQualityTier.Good: return "Bonne";
                default: return "Basique";
            }
        }
    }
}
