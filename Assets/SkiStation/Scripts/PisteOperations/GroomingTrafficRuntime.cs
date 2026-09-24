using System.Collections.Generic;
using UnityEngine;

namespace SkiStation.PisteOperations
{
    /// <summary>Lightweight traffic manager for real grooming vehicles. No artificial vehicles are created.</summary>
    public class GroomingTrafficRuntime : MonoBehaviour
    {
        public static GroomingTrafficRuntime I { get; private set; }
        public float safeDistance = 12f;
        public float stopDistance = 5f;
        public float maxSlowDistance = 28f;
        public float blockedRerouteSeconds = 8f;
        public bool allowSectorYield = true;
        readonly Dictionary<GroomingVehicleRuntime, float> blockedSince = new Dictionary<GroomingVehicleRuntime, float>();

        void Awake(){ if(I!=null&&I!=this){Destroy(gameObject);return;} I=this; }
        void Update()
        {
            var fleet=GroomingVehicleFleetRuntime.I;
            if(fleet==null) return;
            var vehicles=fleet.Vehicles;
            for(int i=0;i<vehicles.Count;i++)
            {
                var a=vehicles[i]; if(a==null) continue;
                a.TrafficSpeedMultiplier=1f; a.TrafficHold=false;
                if(!a.working) continue;
                GroomingVehicleRuntime leader=null; float best=float.MaxValue;
                for(int j=0;j<vehicles.Count;j++)
                {
                    if(i==j) continue; var b=vehicles[j]; if(b==null||!b.working) continue;
                    float d=Vector3.Distance(a.transform.position,b.transform.position);
                    if(d<best && SameOperationalArea(a,b)){best=d;leader=b;}
                }
                if(leader==null) { blockedSince.Remove(a); continue; }
                if(best<=stopDistance) { a.TrafficHold=true; MarkBlocked(a); }
                else if(best<maxSlowDistance) { a.TrafficSpeedMultiplier=Mathf.Clamp01((best-stopDistance)/(maxSlowDistance-stopDistance)); blockedSince.Remove(a); }
                else blockedSince.Remove(a);
            }
            TryResolveBlocked(fleet);
        }
        bool SameOperationalArea(GroomingVehicleRuntime a,GroomingVehicleRuntime b)
        {
            if(a.target!=null && b.target!=null && a.target==b.target) return true;
            if(!allowSectorYield) return false;
            var s=GroomingSectorDispatchRuntime.I;
            if(s==null||a.target==null||b.target==null) return false;
            var sa=s.FindFor(a.target); var sb=s.FindFor(b.target);
            return sa!=null && sa==sb;
        }
        void MarkBlocked(GroomingVehicleRuntime v){if(!blockedSince.ContainsKey(v))blockedSince[v]=Time.time;}
        void TryResolveBlocked(GroomingVehicleFleetRuntime fleet)
        {
            var sector=GroomingSectorDispatchRuntime.I; if(sector==null) return;
            var keys=new List<GroomingVehicleRuntime>(blockedSince.Keys);
            foreach(var v in keys)
            {
                if(v==null||!v.working||Time.time-blockedSince[v]<blockedRerouteSeconds) continue;
                var current=v.target; var next=sector.ChooseNext(current);
                if(next!=null && next!=current && next.isActiveAndEnabled)
                {
                    var m=next.GetComponent<SlopeMaintenanceRuntime>();
                    if(m==null || m.grooming<0.98f)
                    {
                        v.RerouteTo(next);
                        blockedSince.Remove(v);
                    }
                }
            }
        }
    }
}
