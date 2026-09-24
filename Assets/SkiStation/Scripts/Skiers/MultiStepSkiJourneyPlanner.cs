using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum SkiJourneyTarget { FreeSki, Explore, Vertical, EasySector, ReturnToBase }

    [Serializable]
    public struct SkiJourneyStep
    {
        public SkiDomainLinkKind kind;
        public SlopeRuntime slope;
        public LiftRuntime lift;
        public Vector3 from;
        public Vector3 to;
        public float cost;
    }

    /// <summary>
    /// 1.79 multi-step ski-domain journey planner. Builds a lightweight graph from the
    /// unified domain links and runs Dijkstra to plan several pistes/lifts/transfers ahead.
    /// It is advisory: legacy movement, queues and ride sessions remain authoritative.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MultiStepSkiJourneyPlanner : MonoBehaviour
    {
        [SerializeField] private SkierRuntime skier;
        [SerializeField] private IntelligentSkiDayRuntime dayMind;
        [SerializeField] private SkiDestinationRuntime destinationMind;
        [SerializeField, Min(2f)] private float nodeMergeRadius = 24f;
        [SerializeField, Min(1)] private int maxSteps = 18;
        [SerializeField, Min(.5f)] private float replanSeconds = 8f;

        private readonly List<SkiJourneyStep> plan = new List<SkiJourneyStep>();
        private float timer;
        public IReadOnlyList<SkiJourneyStep> Plan => plan;
        public bool HasPlan => plan.Count > 0;
        public event Action PlanChanged;

        private void Awake()
        {
            if (skier == null) skier = GetComponent<SkierRuntime>();
            if (dayMind == null) dayMind = GetComponent<IntelligentSkiDayRuntime>();
            if (destinationMind == null) destinationMind = GetComponent<SkiDestinationRuntime>();
        }

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < replanSeconds) return;
            timer = 0f;
            if (plan.Count == 0 || !PlanStillUsable()) ReplanFrom(transform.position);
        }

        public bool ReplanFrom(Vector3 worldPosition)
        {
            IntegratedSkiDomainNetworkRuntime network = IntegratedSkiDomainNetworkRuntime.I;
            if (network == null) return false;
            network.Rebuild();
            IReadOnlyList<SkiDomainLink> links = network.Links;
            if (links == null || links.Count == 0) return false;

            List<Vector3> nodes = BuildNodes(links);
            int start = NearestNode(nodes, worldPosition);
            int target = destinationMind != null && destinationMind.HasDestination
                ? NearestNode(nodes, destinationMind.Current.position)
                : ChooseTargetNode(nodes, start);
            if (start < 0 || target < 0 || start == target) return false;

            List<int> edgePath = FindPath(nodes, links, start, target);
            plan.Clear();
            for (int i = 0; i < edgePath.Count && i < maxSteps; i++)
            {
                SkiDomainLink link = links[edgePath[i]];
                plan.Add(new SkiJourneyStep { kind = link.kind, slope = link.slope, lift = link.lift, from = link.from, to = link.to, cost = EffectiveCost(link) });
            }
            PlanChanged?.Invoke();
            return plan.Count > 0;
        }

        public bool TryPeek(out SkiJourneyStep step)
        {
            if (plan.Count > 0) { step = plan[0]; return true; }
            step = default(SkiJourneyStep); return false;
        }

        public void CompleteCurrentStep()
        {
            if (plan.Count == 0) return;
            plan.RemoveAt(0);
            PlanChanged?.Invoke();
        }

        public void Invalidate()
        {
            if (plan.Count == 0) return;
            plan.Clear(); PlanChanged?.Invoke();
        }

        private List<Vector3> BuildNodes(IReadOnlyList<SkiDomainLink> links)
        {
            List<Vector3> nodes = new List<Vector3>();
            for (int i = 0; i < links.Count; i++)
            {
                FindOrAdd(nodes, links[i].from);
                FindOrAdd(nodes, links[i].to);
            }
            return nodes;
        }

        private int FindOrAdd(List<Vector3> nodes, Vector3 p)
        {
            float r2 = nodeMergeRadius * nodeMergeRadius;
            for (int i = 0; i < nodes.Count; i++) if ((nodes[i] - p).sqrMagnitude <= r2) return i;
            nodes.Add(p); return nodes.Count - 1;
        }

        private int FindNode(List<Vector3> nodes, Vector3 p)
        {
            int n = NearestNode(nodes, p);
            if (n < 0) return -1;
            return (nodes[n] - p).sqrMagnitude <= nodeMergeRadius * nodeMergeRadius * 2.25f ? n : -1;
        }

        private static int NearestNode(List<Vector3> nodes, Vector3 p)
        {
            int best = -1; float d = float.MaxValue;
            for (int i = 0; i < nodes.Count; i++) { float x = (nodes[i] - p).sqrMagnitude; if (x < d) { d = x; best = i; } }
            return best;
        }

        private int ChooseTargetNode(List<Vector3> nodes, int start)
        {
            if (nodes.Count == 0) return -1;
            SkiDayGoal goal = dayMind != null ? dayMind.Mind.goal : SkiDayGoal.Explore;
            int best = -1; float bestScore = float.MinValue;
            float minY = float.MaxValue;
            for (int i = 0; i < nodes.Count; i++) minY = Mathf.Min(minY, nodes[i].y);
            for (int i = 0; i < nodes.Count; i++)
            {
                if (i == start) continue;
                float horizontal = Vector2.Distance(new Vector2(nodes[start].x, nodes[start].z), new Vector2(nodes[i].x, nodes[i].z));
                float score;
                if (goal == SkiDayGoal.ReturnToBase || goal == SkiDayGoal.Lunch || goal == SkiDayGoal.Recover)
                    score = -(nodes[i].y - minY) - horizontal * .02f;
                else if (goal == SkiDayGoal.Vertical || goal == SkiDayGoal.Training)
                    score = nodes[i].y + horizontal * .04f;
                else if (goal == SkiDayGoal.Explore)
                    score = horizontal + Mathf.Abs(nodes[i].y - nodes[start].y) * .25f;
                else
                    score = horizontal * .35f + nodes[i].y * .10f;
                if (score > bestScore) { bestScore = score; best = i; }
            }
            return best;
        }

        private List<int> FindPath(List<Vector3> nodes, IReadOnlyList<SkiDomainLink> links, int start, int target)
        {
            int n = nodes.Count;
            float[] dist = new float[n]; int[] prevNode = new int[n]; int[] prevEdge = new int[n]; bool[] used = new bool[n];
            for (int i = 0; i < n; i++) { dist[i] = float.MaxValue; prevNode[i] = -1; prevEdge[i] = -1; }
            dist[start] = 0f;
            for (int pass = 0; pass < n; pass++)
            {
                int u = -1; float best = float.MaxValue;
                for (int i = 0; i < n; i++) if (!used[i] && dist[i] < best) { best = dist[i]; u = i; }
                if (u < 0 || u == target) break;
                used[u] = true;
                for (int e = 0; e < links.Count; e++)
                {
                    SkiDomainLink link = links[e]; if (!link.usable || !LinkAllowed(link)) continue;
                    int a = FindNode(nodes, link.from); if (a != u) continue;
                    int b = FindNode(nodes, link.to); if (b < 0) continue;
                    float nd = dist[u] + EffectiveCost(link);
                    if (nd < dist[b]) { dist[b] = nd; prevNode[b] = u; prevEdge[b] = e; }
                }
            }
            List<int> result = new List<int>();
            if (target < 0 || prevEdge[target] < 0) return result;
            int cur = target;
            while (cur != start && cur >= 0 && result.Count < maxSteps)
            {
                int e = prevEdge[cur]; if (e < 0) { result.Clear(); return result; }
                result.Add(e); cur = prevNode[cur];
            }
            result.Reverse(); return result;
        }

        private bool LinkAllowed(SkiDomainLink link)
        {
            if (link.kind != SkiDomainLinkKind.Slope || link.slope == null) return true;
            SkierPopulationProfile p = skier != null ? skier.PopulationProfile : null;
            if (link.slope.Difficulty == SlopeDifficulty.Extreme && (p == null || !p.IsEliteAthlete)) return false;
            if (p != null && p.IsChild && (link.slope.Difficulty == SlopeDifficulty.Black || link.slope.Difficulty == SlopeDifficulty.Extreme)) return false;
            return true;
        }

        private float EffectiveCost(SkiDomainLink link)
        {
            float c = Mathf.Max(.1f, link.cost);
            if (dayMind != null)
            {
                if (link.kind == SkiDomainLinkKind.Slope && link.slope != null) c -= Mathf.Clamp(dayMind.ScoreSlope(link.slope), -80f, 80f) * .35f;
                if (link.kind == SkiDomainLinkKind.Lift && link.lift != null) c -= Mathf.Clamp(dayMind.ScoreLift(link.lift), -80f, 80f) * .35f;
            }
            return Mathf.Max(.1f, c);
        }

        private bool PlanStillUsable()
        {
            for (int i = 0; i < plan.Count; i++)
            {
                SkiJourneyStep s = plan[i];
                if (s.kind == SkiDomainLinkKind.Slope && (s.slope == null || !s.slope.Open)) return false;
                if (s.kind == SkiDomainLinkKind.Lift && (s.lift == null || !s.lift.Open || s.lift.Faulted)) return false;
            }
            return true;
        }
    }
}
