namespace SkiStation
{
    public enum VisitorDayAction329 { Ski, Eat, Rest, Home }

    // Pure decision policy; durations are all central-clock game minutes.
    public static class VisitorDecisionRules329
    {
        public static VisitorDayAction329 Choose(float remaining, float reserve, float skiMinutes,
            float hunger, float energy, bool mealFits)
        {
            if (remaining <= reserve) return VisitorDayAction329.Home;
            if (energy <= .30f && remaining >= reserve + 10f) return VisitorDayAction329.Rest;
            if (hunger >= .66f && mealFits) return VisitorDayAction329.Eat;
            if (!float.IsNaN(skiMinutes) && !float.IsInfinity(skiMinutes) && skiMinutes >= 0f)
                return remaining >= reserve + skiMinutes ? VisitorDayAction329.Ski : VisitorDayAction329.Home;
            return remaining >= reserve + 10f ? VisitorDayAction329.Rest : VisitorDayAction329.Home;
        }
    }
}
