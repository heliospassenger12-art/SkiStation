using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// Lightweight, deterministic utility scoring shared by visitor decisions.
    /// This component makes no movement or teleportation decisions: routing remains
    /// the responsibility of the existing skier and pedestrian systems.
    /// </summary>
    public static class VisitorChoiceUtility249
    {
        /// <summary>Returns a bounded adjustment for an accessible service.
        /// Higher queue pressure, travel distance and price lower its appeal;
        /// quality and fit with the current need raise it.</summary>
        public static float ServiceAdjustment(float needFit, float quality01,
            float queuePressure01, float distanceMeters, float priceRatio,
            float budgetSensitivity01)
        {
            float fit = Mathf.Clamp(needFit, -1f, 1f);
            float quality = Mathf.Clamp01(quality01);
            float queue = Mathf.Clamp01(queuePressure01);
            float distance = Mathf.Clamp01(Mathf.Max(0f, distanceMeters) / 250f);
            float price = Mathf.Clamp(Mathf.Max(0f, priceRatio - 1f), 0f, 3f);
            float budget = Mathf.Clamp01(budgetSensitivity01);
            return Mathf.Clamp(fit * .16f + quality * .10f - queue * .18f
                - distance * .12f - price * (.08f + budget * .12f), -.65f, .26f);
        }
    }
}
