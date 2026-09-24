using System.Collections.Generic;
using UnityEngine;

namespace SkiStation.PisteOperations
{
    public class GroomingSectorDispatchRuntime : MonoBehaviour
    {
        public static GroomingSectorDispatchRuntime I { get; private set; }
        public float sectorRadius = 140f;
        public int maxSectors = 16;
        public bool keepVehiclesInSameSector = true;
        readonly List<GroomingSectorRuntime> sectors = new List<GroomingSectorRuntime>();

        public IReadOnlyList<GroomingSectorRuntime> Sectors => sectors;

        void Awake(){ if(I!=null&&I!=this){Destroy(gameObject);return;} I=this; }
        public void Rebuild()
        {
            sectors.Clear();
            var slopes = new List<SlopeRuntime>(Object.FindObjectsOfType<SlopeRuntime>());
            foreach(var s in slopes)
            {
                if(s==null||s.Points==null||s.Points.Count==0) continue;
                Vector3 p=s.Points[0]; GroomingSectorRuntime best=null; float bd=float.MaxValue;
                foreach(var sec in sectors){float d=Vector3.Distance(sec.Center,p); if(d<bd&&d<=sectorRadius){bd=d;best=sec;}}
                if(best==null && sectors.Count<maxSectors)
                {
                    var go=new GameObject("GroomingSector_"+sectors.Count.ToString("00"));
                    go.transform.SetParent(transform); go.transform.position=p;
                    best=go.AddComponent<GroomingSectorRuntime>(); best.sectorId=go.name; best.radius=sectorRadius; sectors.Add(best);
                }
                if(best!=null) best.Rebuild(slopes);
            }
        }
        public GroomingSectorRuntime FindFor(SlopeRuntime slope)
        {
            if(slope==null) return null;
            foreach(var sec in sectors) if(sec.Slopes.Contains(slope)) return sec;
            return null;
        }
        public SlopeRuntime ChooseNext(SlopeRuntime from)
        {
            RebuildIfNeeded();
            var source=FindFor(from);
            if(source!=null)
            {
                var candidate=Best(source, from);
                if(candidate!=null) return candidate;
            }
            SlopeRuntime best=null; float score=float.MinValue;
            foreach(var sec in sectors)
            {
                var c=Best(sec,null); if(c==null) continue;
                float x=sec.Snapshot.priorityScore - (from?Vector3.Distance(from.Points[0],c.Points[0])*0.05f:0f);
                if(x>score){score=x;best=c;}
            }
            return best;
        }
        SlopeRuntime Best(GroomingSectorRuntime sec,SlopeRuntime exclude)
        {
            SlopeRuntime best=null; float score=float.MinValue;
            foreach(var s in sec.Slopes)
            {
                if(s==null||s==exclude) continue;
                var m=s.GetComponent<SlopeMaintenanceRuntime>(); float q=m?m.snowCondition:1f,g=m?m.grooming:1f;
                float x=(1f-q)*70f+(1f-g)*40f+s.TrafficDensity*5f;
                if(x>score){score=x;best=s;}
            }
            return score>10f?best:null;
        }
        void RebuildIfNeeded(){ if(sectors.Count==0) Rebuild(); }
        public void NotifyCompleted(SlopeRuntime slope) { /* next dispatch naturally reevaluates sector */ }
    }
}
