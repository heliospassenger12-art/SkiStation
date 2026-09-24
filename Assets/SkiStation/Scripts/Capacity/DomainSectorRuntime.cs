using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    [Serializable]
    public sealed class DomainSectorSnapshot
    {
        public string name;
        public int slopes;
        public int lifts;
        public float liftCapacityPerHour;
        public float slopeCapacityPerHour;
        public float effectiveCapacityPerHour;
        public float load01;
        public string status;
        public string bottleneck;
    }

    /// <summary>
    /// Automatically groups nearby pistes and lifts into readable operational sectors.
    /// The player does not have to paint zones manually: geography and the route network
    /// create the sectors, while the UI exposes their capacity and pressure.
    /// </summary>
    public sealed class DomainSectorRuntime : MonoBehaviour
    {
        public static DomainSectorRuntime I { get; private set; }
        public IReadOnlyList<DomainSectorSnapshot> Sectors => sectors;
        public event Action Changed;

        [SerializeField] private float refreshSeconds = 4f;
        [SerializeField] private float connectionDistance = 85f;
        [SerializeField] private float sectorMergeDistance = 180f;
        private readonly List<DomainSectorSnapshot> sectors = new List<DomainSectorSnapshot>();
        private float timer;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start() => Recalculate();

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < Mathf.Max(.75f, refreshSeconds)) return;
            timer = 0f;
            Recalculate();
        }

        public void Recalculate()
        {
            sectors.Clear();
            var network = ResortRouteNetwork.I;
            if (network == null) { Changed?.Invoke(); return; }

            var slopes = new List<SlopeRuntime>();
            foreach (var s in network.Slopes) if (s != null && s.Points.Count >= 2) slopes.Add(s);
            var lifts = new List<LiftRuntime>();
            foreach (var l in network.Lifts) if (l != null && l.Definition != null) lifts.Add(l);

            var used = new HashSet<SlopeRuntime>();
            int sectorIndex = 1;
            foreach (var seed in slopes)
            {
                if (used.Contains(seed)) continue;
                var group = new List<SlopeRuntime>();
                var queue = new Queue<SlopeRuntime>();
                queue.Enqueue(seed); used.Add(seed);
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    group.Add(current);
                    foreach (var candidate in slopes)
                    {
                        if (used.Contains(candidate)) continue;
                        if (ClosestDistance(current, candidate) <= sectorMergeDistance)
                        { used.Add(candidate); queue.Enqueue(candidate); }
                    }
                }
                BuildSector("Secteur " + sectorIndex++, group, lifts);
            }

            if (slopes.Count == 0 && lifts.Count > 0)
            {
                var snapshot = MakeSnapshot("Secteur remontées", new List<SlopeRuntime>(), lifts, lifts);
                sectors.Add(snapshot);
            }
            Changed?.Invoke();
        }

        private void BuildSector(string name, List<SlopeRuntime> group, List<LiftRuntime> allLifts)
        {
            var localLifts = new List<LiftRuntime>();
            foreach (var lift in allLifts)
            {
                foreach (var slope in group)
                {
                    if (Vector3.Distance(lift.Top, slope.TopPoint) <= connectionDistance ||
                        Vector3.Distance(lift.Bottom, slope.BottomPoint) <= connectionDistance)
                    { localLifts.Add(lift); break; }
                }
            }
            sectors.Add(MakeSnapshot(name, group, localLifts, allLifts));
        }

        private DomainSectorSnapshot MakeSnapshot(string name, List<SlopeRuntime> group, List<LiftRuntime> localLifts, List<LiftRuntime> allLifts)
        {
            float liftCap = 0f, slopeCap = 0f;
            string bottleneck = "Aucun";
            foreach (var lift in localLifts) if (lift != null && lift.Open && !lift.Faulted) liftCap += Mathf.Max(0f, lift.CapacityPerHour);
            foreach (var slope in group)
            {
                if (slope == null || !slope.Open) continue;
                var traffic = slope.GetComponent<SlopeTraffic>();
                slopeCap += traffic != null ? Mathf.Max(0f, traffic.CapacityPerHour) : 180f;
            }
            float effective = group.Count == 0 ? liftCap : Mathf.Min(liftCap > 0f ? liftCap : slopeCap, slopeCap > 0f ? slopeCap : liftCap);
            if (effective <= 0f) effective = Mathf.Max(liftCap, slopeCap);

            int active = 0;
            foreach (var skier in FindObjectsOfType<SkierRuntime>())
            {
                if (skier == null || skier.State == SkierState.Leaving) continue;
                Vector3 p = skier.transform.position;
                bool inside = false;
                foreach (var slope in group)
                {
                    if (slope != null && Vector3.Distance(p, slope.transform.position) <= 120f) { inside = true; break; }
                }
                if (inside) active++;
            }
            float load = effective <= 0f ? (active > 0 ? 1f : 0f) : Mathf.Clamp01(active * 3.5f / effective);
            if (liftCap > 0f && slopeCap > 0f && Mathf.Abs(liftCap - slopeCap) / Mathf.Max(liftCap, slopeCap) >= .12f)
                bottleneck = liftCap < slopeCap ? "Remontées" : "Pistes";

            string status = load >= .85f ? "Saturé" : load >= .65f ? "Très chargé" : load >= .40f ? "Chargé" : "Fluide";
            return new DomainSectorSnapshot { name = name, slopes = group.Count, lifts = localLifts.Count,
                liftCapacityPerHour = liftCap, slopeCapacityPerHour = slopeCap, effectiveCapacityPerHour = effective,
                load01 = load, status = status, bottleneck = bottleneck };
        }

        private static float ClosestDistance(SlopeRuntime a, SlopeRuntime b)
        {
            float best = float.MaxValue;
            best = Mathf.Min(best, Vector3.Distance(a.TopPoint, b.TopPoint));
            best = Mathf.Min(best, Vector3.Distance(a.BottomPoint, b.BottomPoint));
            best = Mathf.Min(best, Vector3.Distance(a.TopPoint, b.BottomPoint));
            best = Mathf.Min(best, Vector3.Distance(a.BottomPoint, b.TopPoint));
            return best;
        }
    }
}
