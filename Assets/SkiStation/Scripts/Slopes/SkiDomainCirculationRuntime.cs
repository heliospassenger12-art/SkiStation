using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    [Serializable]
    public struct SkiDomainCirculationSnapshot
    {
        public int openSlopes;
        public int connectedSlopes;
        public int terminalSlopes;
        public int activeSkiers;
        public float averageTrafficDensity;
    }

    /// <summary>0.99: observation layer for the real skiable domain.</summary>
    public sealed class SkiDomainCirculationRuntime : MonoBehaviour
    {
        public static SkiDomainCirculationRuntime I { get; private set; }
        public SkiDomainCirculationSnapshot Snapshot { get; private set; }
        public event Action<SkiDomainCirculationSnapshot> Changed;
        [Min(0.1f)] public float refreshInterval = 0.5f;
        private float timer;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = refreshInterval;
            Refresh();
        }

        public void Refresh()
        {
            var snapshot = new SkiDomainCirculationSnapshot();
            if (SlopeNetwork.I != null)
            {
                float densitySum = 0f;
                for (int i = 0; i < SlopeNetwork.I.Slopes.Count; i++)
                {
                    var slope = SlopeNetwork.I.Slopes[i];
                    if (slope == null || !slope.Open) continue;
                    snapshot.openSlopes++;
                    densitySum += slope.TrafficDensity;
                    var c = slope.GetComponent<SlopeConnectionRuntime>();
                    if (c != null && c.Bottom.IsValid && c.Bottom.slope != null) snapshot.connectedSlopes++;
                    else snapshot.terminalSlopes++;
                    snapshot.activeSkiers += slope.TrafficCount;
                }
                snapshot.averageTrafficDensity = snapshot.openSlopes > 0 ? densitySum / snapshot.openSlopes : 0f;
            }
            Snapshot = snapshot;
            Changed?.Invoke(snapshot);
        }
    }
}
