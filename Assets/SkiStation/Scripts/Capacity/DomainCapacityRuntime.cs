using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    [Serializable]
    public struct DomainCapacitySnapshot
    {
        public int activeSkiers;
        public float liftCapacityPerHour;
        public float slopeCapacityPerHour;
        public float effectiveCapacityPerHour;
        public float estimatedLoad01;
        public bool bottleneck;
        public string bottleneckName;
    }

    /// <summary>
    /// Lightweight domain-capacity layer. It identifies the natural bottleneck
    /// between uphill transport and skiable terrain without requiring manual
    /// capacity management from the player.
    /// </summary>
    public sealed class DomainCapacityRuntime : MonoBehaviour
    {
        public static DomainCapacityRuntime I { get; private set; }

        [SerializeField] private float refreshSeconds = 3f;
        [SerializeField] private float skierTurnsPerHour = 3.5f;
        [SerializeField] private float bottleneckMargin = .12f;

        public DomainCapacitySnapshot Snapshot { get; private set; }
        public event Action Changed;
        public float DomainPressure01 => Snapshot.estimatedLoad01;
        public bool HasBottleneck => Snapshot.bottleneck;
        public string BottleneckName => Snapshot.bottleneckName;

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
            if (timer < Mathf.Max(.5f, refreshSeconds)) return;
            timer = 0f;
            Recalculate();
        }

        public void Recalculate()
        {
            float liftCapacity = 0f;
            float slopeCapacity = 0f;
            string bottleneckName = string.Empty;
            float lowestCapacity = float.MaxValue;

            var network = ResortRouteNetwork.I;
            if (network != null)
            {
                foreach (var lift in network.Lifts)
                {
                    if (lift == null || !lift.Open || lift.Faulted) continue;
                    liftCapacity += Mathf.Max(0f, lift.CapacityPerHour);
                }

                foreach (var slope in network.Slopes)
                {
                    if (slope == null || !slope.Open) continue;
                    var traffic = slope.GetComponent<SlopeTraffic>();
                    float capacity = traffic != null ? traffic.CapacityPerHour : 180f;
                    slopeCapacity += Mathf.Max(0f, capacity);
                }

                // A local bottleneck is a lift/slope pair whose throughput is
                // materially lower than the other side of the network.
                foreach (var slope in network.Slopes)
                {
                    if (slope == null || !slope.Open) continue;
                    float localSlope = GetSlopeCapacity(slope);
                    float localLift = 0f;
                    foreach (var lift in network.Lifts)
                    {
                        if (lift == null || !lift.Open || lift.Faulted) continue;
                        if (Vector3.Distance(lift.Top, slope.TopPoint) <= 35f)
                            localLift += Mathf.Max(0f, lift.CapacityPerHour);
                    }
                    if (localLift <= 0f) continue;
                    float local = Mathf.Min(localLift, localSlope);
                    float imbalance = Mathf.Abs(localLift - localSlope) / Mathf.Max(1f, Mathf.Max(localLift, localSlope));
                    if (imbalance >= bottleneckMargin && local < lowestCapacity)
                    {
                        lowestCapacity = local;
                        bottleneckName = localLift < localSlope ? "Remontée → " + slope.name : "Piste → " + slope.name;
                    }
                }
            }

            int active = 0;
            foreach (var skier in FindObjectsOfType<SkierRuntime>())
            {
                if (skier != null && skier.State != SkierState.Leaving) active++;
            }

            float effective = Mathf.Min(liftCapacity, slopeCapacity);
            float estimatedHourlyLoad = active * Mathf.Max(.5f, skierTurnsPerHour);
            float pressure = effective <= 0f ? (active > 0 ? 1f : 0f) : Mathf.Clamp01(estimatedHourlyLoad / effective);

            Snapshot = new DomainCapacitySnapshot
            {
                activeSkiers = active,
                liftCapacityPerHour = liftCapacity,
                slopeCapacityPerHour = slopeCapacity,
                effectiveCapacityPerHour = effective,
                estimatedLoad01 = pressure,
                bottleneck = !string.IsNullOrEmpty(bottleneckName),
                bottleneckName = bottleneckName
            };
            Changed?.Invoke();
        }

        private static float GetSlopeCapacity(SlopeRuntime slope)
        {
            var traffic = slope != null ? slope.GetComponent<SlopeTraffic>() : null;
            return traffic != null ? Mathf.Max(1f, traffic.CapacityPerHour) : 180f;
        }
    }
}
