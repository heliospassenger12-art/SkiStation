using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation.PisteOperations
{
    [Serializable]
    public struct GroomingSectorSnapshot
    {
        public string id;
        public Vector3 center;
        public int slopeCount;
        public int degradedSlopeCount;
        public float priorityScore;
    }

    public class GroomingSectorRuntime : MonoBehaviour
    {
        public string sectorId = "SECTOR";
        public float radius = 140f;
        public Vector3 Center { get; private set; }
        public readonly List<SlopeRuntime> Slopes = new List<SlopeRuntime>();

        public void Rebuild(IEnumerable<SlopeRuntime> source)
        {
            Slopes.Clear();
            var sum = Vector3.zero; int n = 0;
            foreach (var s in source)
            {
                if (s == null || s.Points == null || s.Points.Count == 0) continue;
                Vector3 p = s.Points[0];
                if (Vector3.Distance(transform.position, p) <= radius)
                { Slopes.Add(s); sum += p; n++; }
            }
            Center = n > 0 ? sum / n : transform.position;
        }

        public GroomingSectorSnapshot Snapshot
        {
            get {
                int bad=0; float score=0;
                foreach(var s in Slopes) {
                    var m=s? s.GetComponent<SlopeMaintenanceRuntime>() : null;
                    float q=m?m.snowCondition:1f, g=m?m.grooming:1f;
                    float x=(1f-q)*70f+(1f-g)*40f+(s? s.TrafficDensity*5f:0f);
                    if(x>25f) bad++; score=Mathf.Max(score,x);
                }
                return new GroomingSectorSnapshot { id=sectorId, center=Center, slopeCount=Slopes.Count, degradedSlopeCount=bad, priorityScore=score };
            }
        }
    }
}
