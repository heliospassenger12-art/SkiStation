using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// 1.77 bridge between skier AI and the unified 1.76 ski-domain graph.
    /// It deliberately coexists with the legacy route agent: it supplies terrain/network-aware
    /// choices while existing lift queues, ride sessions and slope movement remain authoritative.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class IntegratedSkierDomainNavigator : MonoBehaviour
    {
        [SerializeField] private SkierRuntime skier;
        [SerializeField] private SkierRouteAgent routeAgent;
        [SerializeField, Range(0f, 100f)] private float congestionAversion = 55f;
        [SerializeField] private bool avoidExtremeUnlessAthlete = true;

        public SlopeRuntime LastChosenSlope { get; private set; }
        public LiftRuntime LastChosenLift { get; private set; }

        private void Awake()
        {
            if (skier == null) skier = GetComponent<SkierRuntime>();
            if (routeAgent == null) routeAgent = GetComponent<SkierRouteAgent>();
        }

        public SlopeRuntime ChooseSlopeFromLift(LiftRuntime lift)
        {
            if (lift == null) return null;
            IntegratedSkiDomainNetworkRuntime network = IntegratedSkiDomainNetworkRuntime.I;
            if (network == null) return null;

            SlopeRuntime best = null;
            float bestScore = float.MaxValue;
            SlopeRuntime[] slopes = Object.FindObjectsOfType<SlopeRuntime>();
            for (int i = 0; i < slopes.Length; i++)
            {
                SlopeRuntime slope = slopes[i];
                if (slope == null || !slope.Open || slope.Points == null || slope.Points.Count < 2) continue;
                Vector3 delta = slope.TopPoint - lift.Top;
                if (delta.sqrMagnitude > network.endpointRadius * network.endpointRadius || delta.y > network.maxTransferClimb) continue;
                if (!DifficultyAllowed(slope)) continue;

                float score = delta.magnitude;
                score += slope.TrafficDensity * congestionAversion * 20f;
                score += DifficultyPreferencePenalty(slope);
                IntelligentSkiDayRuntime dayMind = GetComponent<IntelligentSkiDayRuntime>();
                if (dayMind != null) score -= dayMind.ScoreSlope(slope);
                if (score < bestScore) { bestScore = score; best = slope; }
            }
            LastChosenSlope = best;
            return best;
        }

        public LiftRuntime ChooseLiftFromSlope(SlopeRuntime slope)
        {
            IntegratedSkiDomainNetworkRuntime network = IntegratedSkiDomainNetworkRuntime.I;
            if (network == null || slope == null) return null;
            LastChosenLift = network.FindReachableLiftFromSlope(slope);
            // The current network exposes the reachable lift first; the day-mind scoring hook is
            // intentionally passive until multi-lift candidate enumeration is exposed.
            return LastChosenLift;
        }

        public bool ContinueFromLift(LiftRuntime lift)
        {
            if (routeAgent == null) return false;
            SlopeRuntime slope = ChooseSlopeFromLift(lift);
            if (slope == null) return false;
            routeAgent.BeginFromLift(lift, slope);
            return true;
        }

        private bool DifficultyAllowed(SlopeRuntime slope)
        {
            if (!avoidExtremeUnlessAthlete || slope.Difficulty != SlopeDifficulty.Extreme) return true;
            SkierPopulationProfile profile = skier != null ? skier.PopulationProfile : null;
            return profile != null && profile.IsEliteAthlete;
        }

        private float DifficultyPreferencePenalty(SlopeRuntime slope)
        {
            SkierPopulationProfile profile = skier != null ? skier.PopulationProfile : null;
            if (profile == null) return 0f;
            if (profile.IsEliteAthlete)
                return slope.Difficulty == SlopeDifficulty.Black || slope.Difficulty == SlopeDifficulty.Extreme ? 0f : 18f;
            if (profile.IsChild)
                return slope.Difficulty == SlopeDifficulty.Green ? 0f : slope.Difficulty == SlopeDifficulty.Blue ? 10f : 60f;
            if (profile.Archetype == SkierPopulationArchetype.Family)
                return slope.Difficulty == SlopeDifficulty.Green || slope.Difficulty == SlopeDifficulty.Blue ? 0f : 30f;
            return slope.Difficulty == SlopeDifficulty.Blue || slope.Difficulty == SlopeDifficulty.Red ? 0f : 8f;
        }
    }
}
