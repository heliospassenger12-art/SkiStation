using System;
using UnityEngine;
namespace SkiStation {
 public class GroomingVehicleRuntime:MonoBehaviour {
  public SlopeRuntime target; public float speed=12f; public float returnSpeed=10f; public float progress; public bool working;
  public bool returning {get;private set;} public bool IsAvailable=>!working&&!returning;
  public float TrafficSpeedMultiplier {get;set;} = 1f; public bool TrafficHold {get;set;}
  public void RerouteTo(SlopeRuntime slope){ if(slope==null)return; target=slope; progress=0f; index=0; route=slope.Points!=null?new System.Collections.Generic.List<Vector3>(slope.Points).ToArray():null; if(route!=null&&route.Length>0) transform.position=route[0]; working=true; returning=false; }
  public event Action<GroomingVehicleRuntime,SlopeRuntime> Finished;
  Vector3[] route; Vector3 garagePoint; int index;
  public void SetGaragePoint(Vector3 point){garagePoint=point; if(!working&&!returning)transform.position=point;}
  public void StartWork(SlopeRuntime slope,bool instant){ if(slope==null)return; target=slope; working=true; returning=false; progress=0; index=0; route=slope.Points!=null?new System.Collections.Generic.List<Vector3>(slope.Points).ToArray():null; transform.position=route!=null&&route.Length>0?route[0]:transform.position; if(instant)Finish(); }
  void Update(){
   if(returning){ transform.position=Vector3.MoveTowards(transform.position,garagePoint,Mathf.Max(.1f,returnSpeed)*Time.deltaTime); if(Vector3.Distance(transform.position,garagePoint)<.05f){transform.position=garagePoint;returning=false;} return; }
   if(!working||target==null||route==null||route.Length<2)return;
   if(TrafficHold) return;
   progress+=Time.deltaTime*Mathf.Max(.1f,speed)*Mathf.Clamp01(TrafficSpeedMultiplier)/Mathf.Max(1f,target.Length);
   if(progress>=1f)Finish(); else transform.position=Sample(progress);
  }
  Vector3 Sample(float t){ float f=t*(route.Length-1); int i=Mathf.Clamp(Mathf.FloorToInt(f),0,route.Length-2); return Vector3.Lerp(route[i],route[i+1],f-i); }
  void Finish(){ if(!working)return; var completed=target; progress=1f; working=false; var m=completed.GetComponent<SlopeMaintenanceRuntime>()??completed.gameObject.AddComponent<SlopeMaintenanceRuntime>(); m.CompleteGrooming(); GroomingScheduleRuntime.I?.NotifyFinished(completed); Finished?.Invoke(this,completed); target=null; route=null; returning=true; }
 }
}
