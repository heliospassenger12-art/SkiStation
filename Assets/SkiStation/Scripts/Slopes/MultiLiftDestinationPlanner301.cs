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
            return TryPlan(skier, from, target, out lift, out _);
        }

        public bool TryGetNextSlope(SkierRuntime skier, LiftRuntime justRidden, out SlopeRuntime slope)
        {
            slope = null;
            if (skier == null || justRidden == null) return false;
            SlopeRuntime target = GetTarget(skier, justRidden.Top);
            if (target == null) return false;
            if (Vector3.Distance(justRidden.Top, target.TopPoint) <= startRadius && IsAllowed(skier, target))
            {
                slope = target;
                return true;
            }
            return TryPlan(skier, justRidden.Top, target, out _, out slope);
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
                if (distance < 55f) continue; // local choices already work well; 3.01 targets the wider domain.
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

        private bool TryPlan(SkierRuntime skier, Vector3 from, SlopeRuntime target, out LiftRuntime firstLift, out SlopeRuntime firstSlope)
        {
            firstLift = null; firstSlope = null;
            var network = IntegratedSkiDomainNetworkRuntime.I;
            if (network == null || target == null || network.Links == null || network.Links.Count == 0) return false;

            var links = network.Links;
            int n = links.Count;
            float[] dist = new float[n];
            int[] prev = new int[n];
            bool[] used = new bool[n];
            for (int i = 0; i < n; i++) { dist[i] = float.PositiveInfinity; prev[i] = -1; }

            for (int i = 0; i < n; i++)
            {
                if (!links[i].usable || !AllowedLink(skier, links[i])) continue;
                Vector3 d = links[i].from - from;
                if (new Vector2(d.x, d.z).magnitude <= startRadius && d.y <= network.maxTransferClimb)
                    dist[i] = d.magnitude + DynamicLinkCost(links[i]);
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
                    if (used[v] || !links[v].usable || !AllowedLink(skier, links[v])) continue;
                    Vector3 delta = links[v].from - end;
                    if (delta.sqrMagnitude > nodeMergeRadius * nodeMergeRadius || delta.y > network.maxTransferClimb) continue;
                    float nd = dist[u] + delta.magnitude * network.transferCostMultiplier + DynamicLinkCost(links[v]);
                    if (nd < dist[v]) { dist[v] = nd; prev[v] = u; }
                }
            }
            if (goal < 0) return false;

            var chain = new List<int>();
            for (int p = goal; p >= 0; p = prev[p]) chain.Add(p);
            chain.Reverse();
            for (int i = 0; i < chain.Count; i++)
            {
                var link = links[chain[i]];
                if (firstLift == null && link.kind == SkiDomainLinkKind.Lift) firstLift = link.lift;
                if (firstSlope == null && link.kind == SkiDomainLinkKind.Slope) firstSlope = link.slope;
                if (firstLift != null && firstSlope != null) break;
            }
            return firstLift != null || firstSlope != null;
        }

        private float DynamicLinkCost(SkiDomainLink link)
        {
            float cost = link.cost;
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
            if (link.kind == SkiDomainLinkKind.Lift) return link.lift != null && link.lift.Open && !link.lift.Faulted;
            if (link.kind == SkiDomainLinkKind.Slope) return link.slope != null && IsAllowed(skier, link.slope);
            return true;
        }

        private static bool IsAllowed(SkierRuntime skier, SlopeRuntime slope)
        {
            if (!IsUsable(slope)) return false;
            if (skier == null) return true;
            CustomerProfile profile = skier.PopulationProfile != null ? skier.PopulationProfile.ToCustomerProfile() : CustomerProfile.Tourist;
            int ability = SlopeVisitorExperienceRuntime.MaxDifficulty(profile);
            return (int)slope.Difficulty <= ability + 1;
        }
    }
}
