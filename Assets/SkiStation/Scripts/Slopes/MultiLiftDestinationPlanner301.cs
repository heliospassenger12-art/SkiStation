using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// 3.01: lightweight multi-lift route planner for the real ski-domain graph.
    /// It never moves visitors itself. It computes a shortest usable chain of real
    /// pistes/lifts/transfers and exposes the next lift/slope to existing movement code.
    /// </summary>
    public sealed class MultiLiftDestinationPlanner301 : MonoBehaviour
    {
        public static MultiLiftDestinationPlanner301 I { get; private set; }

        [SerializeField, Min(2f)] private float nodeMergeRadius = 10f;
        [SerializeField, Min(10f)] private float startRadius = 42f;
        [SerializeField, Min(10f)] private float targetRefreshSeconds = 75f;
        // 3.04: once a visitor has chosen a destination piste, keep that objective
        // until it is completed. Re-route around live closures/congestion instead of
        // randomly abandoning the destination. A target is dropped only if no valid
        // route remains for this skier.
        [SerializeField] private bool commitToReachableDestination = true;
        [SerializeField, Range(0f, 3f)] private float repeatPenalty = 1.1f;
        // 3.03: dynamic queue-aware routing. A busy lift remains usable, but its
        // estimated wait is added to the route cost so visitors can prefer a
        // slightly longer but faster chain through the live domain.
        [SerializeField, Range(0f, 2f)] private float queueWaitWeight = 1f;
        [SerializeField, Min(15f)] private float maxQueuePenaltySeconds = 240f;

        private readonly Dictionary<SkierRuntime, SlopeRuntime> targets = new Dictionary<SkierRuntime, SlopeRuntime>();
        private readonly Dictionary<SkierRuntime, float> targetTimes = new Dictionary<SkierRuntime, float>();
        private readonly Dictionary<SkierRuntime, SlopeRuntime> lastTargets = new Dictionary<SkierRuntime, SlopeRuntime>();

        public IReadOnlyDictionary<SkierRuntime, SlopeRuntime> Targets => targets;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;
            if (I == null) new GameObject("SkiStation_301_Navigation_Multi_Remontees").AddComponent<MultiLiftDestinationPlanner301>();
        }

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        public SlopeRuntime GetTarget(SkierRuntime skier, Vector3 from)
        {
            if (skier == null) return null;
            if (targets.TryGetValue(skier, out var current) && IsUsable(current))
            {
                bool reachableNow = HasPath(skier, from, current);
                if (reachableNow && commitToReachableDestination)
                    return current;

                if (reachableNow && targetTimes.TryGetValue(skier, out float t) &&
                    Time.unscaledTime - t < targetRefreshSeconds)
                    return current;

                // The objective has become impossible (closure/fault/network change):
                // forget it cleanly and select another reachable destination.
                if (!reachableNow)
                {
                    lastTargets[skier] = current;
                    targets.Remove(skier);
                    targetTimes.Remove(skier);
                }
            }

            // Drop stale closed/deleted objectives even when no replacement exists.
            targets.Remove(skier); targetTimes.Remove(skier);
            SlopeRuntime selected = SelectDestination(skier, from);
            if (selected != null)
            {
                if (targets.TryGetValue(skier, out var old) && old != null) lastTargets[skier] = old;
                targets[skier] = selected;
                targetTimes[skier] = Time.unscaledTime;
            }
            return selected;
        }

        public bool TryGetNextLift(SkierRuntime skier, Vector3 from, out LiftRuntime lift)
        {
            lift = null;
            SlopeRuntime target = GetTarget(skier, from);
            if (target == null) return false;
            return TryPlan(skier, from, target, out lift, out _) && lift != null;
        }

        public bool TryGetNextSlope(SkierRuntime skier, LiftRuntime justRidden, out SlopeRuntime slope)
        {
            slope = null;
            if (skier == null || justRidden == null) return false;
            SlopeRuntime target = GetTarget(skier, justRidden.Top);
            if (target == null) return false;
            return TryPlan(skier, justRidden.Top, target, out _, out slope) && slope != null;
        }

        public void RegisterCompletedSlope(SkierRuntime skier, SlopeRuntime slope)
        {
            if (skier == null || slope == null) return;
            if (targets.TryGetValue(skier, out var target) && target == slope)
            {
                lastTargets[skier] = slope;
                targets.Remove(skier);
                targetTimes.Remove(skier);
            }
        }

        private SlopeRuntime SelectDestination(SkierRuntime skier, Vector3 from)
        {
            SlopeRuntime[] slopes = FindObjectsOfType<SlopeRuntime>();
            SlopeRuntime best = null;
            float bestScore = float.MinValue;
            for (int i = 0; i < slopes.Length; i++)
            {
                var s = slopes[i];
                if (!IsUsable(s) || !IsAllowed(skier, s)) continue;
                float distance = Vector3.Distance(from, s.TopPoint);
                // Local pistes remain valid goals for a small or newly opened domain.
                float score = Mathf.Clamp(distance / 450f, 0f, 2.2f);
                score += Mathf.Clamp01(s.Length / 1800f) * .55f;
                score -= s.TrafficDensity * 18f;
                if (lastTargets.TryGetValue(skier, out var last) && last == s) score -= repeatPenalty;
                if (VisitorIntentRuntime.I != null) score += VisitorIntentRuntime.I.ScoreSlopeIntent(skier, s) * .35f;
                if (score > bestScore && HasPath(skier, from, s)) { bestScore = score; best = s; }
            }
            return best;
        }

        private bool HasPath(SkierRuntime skier, Vector3 from, SlopeRuntime target)
        {
            // 3.04: validate the complete chain with the visitor's real ability.
            // This prevents selecting a destination whose only access would require
            // an intermediate piste that is too difficult for that visitor.
            return TryPlan(skier, from, target, out _, out _);
        }

        public bool TryGetNextStep(SkierRuntime skier, Vector3 from, out SkiDomainLink next, out string itinerary)
        {
            next = default; itinerary = "Aucun itineraire accessible";
            var target = GetTarget(skier, from);
            if (target == null || !TryBuildPlan(skier, from, target, out var chain) || chain.Count == 0) return false;
            next = chain[0];
            var labels = new List<string>();
            foreach (var link in chain)
                labels.Add(link.kind == SkiDomainLinkKind.Lift ? "Remontee : " + link.lift.name :
                    link.kind == SkiDomainLinkKind.Slope ? "Piste : " + link.slope.name : "Liaison a pied");
            itinerary = string.Join("\n", labels);
            return true;
        }
        public void Forget(SkierRuntime skier)
        { if (skier == null) return; targets.Remove(skier); targetTimes.Remove(skier); lastTargets.Remove(skier); }
        private bool TryPlan(SkierRuntime skier, Vector3 from, SlopeRuntime target, out LiftRuntime firstLift, out SlopeRuntime firstSlope)
        {
            firstLift = null; firstSlope = null;
            if (!TryBuildPlan(skier, from, target, out var chain)) return false;
            // Legacy callers may only use the immediate action, never a later lift or piste.
            if (chain.Count > 0 && chain[0].kind == SkiDomainLinkKind.Lift) firstLift = chain[0].lift;
            if (chain.Count > 0 && chain[0].kind == SkiDomainLinkKind.Slope) firstSlope = chain[0].slope;
            return chain.Count > 0;
        }
        private bool TryBuildPlan(SkierRuntime skier, Vector3 from, SlopeRuntime target, out List<SkiDomainLink> ordered)
        {
            ordered = new List<SkiDomainLink>();
            var network = IntegratedSkiDomainNetworkRuntime.I;
            if (network == null || target == null || network.Links == null || network.Links.Count == 0) return false;

            var links = network.Links;
            int n = links.Count;
            float[] dist = new float[n];
            int[] prev = new int[n];
            bool[] used = new bool[n];
            bool[] allowed = new bool[n];
            float[] costs = new float[n];
            // Evaluate live restrictions and queues once per search, not for every relaxation.
            for (int i = 0; i < n; i++)
            {
                dist[i] = float.PositiveInfinity; prev[i] = -1;
                allowed[i] = links[i].usable && AllowedLink(skier, links[i]);
                if (allowed[i]) costs[i] = DynamicLinkCost(links[i]);
            }

            var agent = skier != null ? skier.GetComponent<SkierRouteAgent>() : null;
            bool firstRide = skier != null && skier.GetComponent<StationVisitorRoadArrival248>() != null &&
                (agent == null || !agent.HasCompletedLiftRide);
            for (int i = 0; i < n; i++)
            {
                if (!allowed[i]) continue;
                if (firstRide && links[i].kind != SkiDomainLinkKind.Lift) continue;
                if (links[i].kind == SkiDomainLinkKind.Transfer && Vector3.Distance(from, links[i].to) <= 1.5f) continue;
                float radius = links[i].kind == SkiDomainLinkKind.Lift ? ManagementJourneyAccess.LiftApproachRadius : startRadius;
                if (ManagementJourneyAccess.CanWalk(from, links[i].from, radius))
                    dist[i] = Vector3.Distance(from, links[i].from) / 6f + costs[i];
            }

            int goal = -1;
            for (int step = 0; step < n; step++)
            {
                int u = -1; float best = float.PositiveInfinity;
                for (int i = 0; i < n; i++) if (!used[i] && dist[i] < best) { best = dist[i]; u = i; }
                if (u < 0) break;
                used[u] = true;
                if (links[u].kind == SkiDomainLinkKind.Slope && links[u].slope == target) { goal = u; break; }

                Vector3 end = links[u].to;
                for (int v = 0; v < n; v++)
                {
                    if (used[v] || !allowed[v]) continue;
                    Vector3 delta = links[v].from - end;
                    if (delta.sqrMagnitude > nodeMergeRadius * nodeMergeRadius || delta.y > network.maxTransferClimb) continue;
                    if (!ManagementJourneyAccess.CanWalk(end, links[v].from, nodeMergeRadius)) continue;
                    float nd = dist[u] + delta.magnitude / 6f + costs[v];
                    if (nd < dist[v]) { dist[v] = nd; prev[v] = u; }
                }
            }
            if (goal < 0) return false;

            var chain = new List<int>();
            for (int p = goal; p >= 0; p = prev[p]) chain.Add(p);
            chain.Reverse();
            for (int i = 0; i < chain.Count; i++) ordered.Add(links[chain[i]]);
            return ordered.Count > 0;
        }

        private float DynamicLinkCost(SkiDomainLink link)
        {
            // One unit throughout the graph: estimated elapsed seconds.
            float cost = link.kind == SkiDomainLinkKind.Slope && link.slope != null ?
                Mathf.Max(1f, link.slope.Length / 12f) * (1f + Mathf.Max(0, link.slope.TrafficDensity)) :
                link.kind == SkiDomainLinkKind.Lift && link.lift != null ?
                Mathf.Max(1f, link.lift.Length / Mathf.Max(.1f, link.lift.EffectiveSpeedKmh / 3.6f)) :
                Mathf.Max(.01f, Vector3.Distance(link.from, link.to) / 6f);
            if (link.kind != SkiDomainLinkKind.Lift || link.lift == null) return cost;

            LiftRuntime lift = link.lift;
            LiftStationLogic station = lift.StationLogic;
            if (station == null || station.QueueSize <= 0) return cost;

            // CapacityPerHour is the stable gameplay throughput already exposed by LiftRuntime.
            // Convert the people ahead in the queue into an approximate waiting time.
            float capacityPerSecond = lift.CapacityPerHour / 3600f;
            if (capacityPerSecond <= 0.0001f) return cost + maxQueuePenaltySeconds;
            float estimatedWait = station.QueueSize / capacityPerSecond;
            return cost + Mathf.Min(maxQueuePenaltySeconds, estimatedWait) * queueWaitWeight;
        }

        public float EstimateLiftWaitSeconds(LiftRuntime lift)
        {
            if (lift == null || lift.StationLogic == null || lift.StationLogic.QueueSize <= 0) return 0f;
            float capacityPerSecond = lift.CapacityPerHour / 3600f;
            if (capacityPerSecond <= 0.0001f) return maxQueuePenaltySeconds;
            return Mathf.Min(maxQueuePenaltySeconds, lift.StationLogic.QueueSize / capacityPerSecond);
        }

        private static bool IsUsable(SlopeRuntime s) => s != null && s.Open && s.Points != null && s.Points.Count >= 2;

        private static bool AllowedLink(SkierRuntime skier, SkiDomainLink link)
        {
            if (link.kind == SkiDomainLinkKind.Lift) return link.lift != null && link.lift.Definition != null &&
                link.lift.Open && !link.lift.Faulted && link.lift.EffectiveSpeedKmh > .01f &&
                link.lift.LiftId != "starter_panorama_6seat" && !link.lift.name.Contains("Demonstration") && !link.lift.name.Contains("Démonstration");
            if (link.kind == SkiDomainLinkKind.Slope) return link.slope != null && IsAllowed(skier, link.slope);
            return ManagementJourneyAccess.CanWalk(link.from, link.to, 90f);
        }

        private static bool IsAllowed(SkierRuntime skier, SlopeRuntime slope)
        {
            if (!IsUsable(slope) || !slope.name.StartsWith("Piste_")) return false;
            if (skier == null) return true;
            CustomerProfile profile = skier.PopulationProfile != null ? skier.PopulationProfile.ToCustomerProfile() : CustomerProfile.Tourist;
            int ability = SlopeVisitorExperienceRuntime.MaxDifficulty(profile);
            return (int)slope.Difficulty <= ability;
        }
    }
}
