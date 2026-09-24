using System.Collections.Generic;
using UnityEngine;
using SkiStation;
namespace SkiStation.PisteOperations {
 public class GroomingVehicleFleetRuntime:MonoBehaviour {
  public static GroomingVehicleFleetRuntime I {get;private set;}
  public int fleetSize=2; public bool automatic=true; public bool instantOperations=false; public float dispatchInterval=1f; public float operationCost=45f;
  readonly List<GroomingVehicleRuntime> vehicles=new List<GroomingVehicleRuntime>(); float timer;
  public IReadOnlyList<GroomingVehicleRuntime> Vehicles=>vehicles;
  void Awake(){if(I!=null&&I!=this){Destroy(gameObject);return;}I=this;EnsureGarage();RebuildFleet();}
  void Update(){timer+=Time.deltaTime;if(timer>=dispatchInterval){timer=0;Dispatch();}SyncStrategy();}
  void EnsureGarage(){if(GroomingGarageRuntime.I!=null)return;var go=new GameObject("GroomingGarage");go.transform.position=Vector3.zero;go.AddComponent<GroomingGarageRuntime>();}
  public void RebuildFleet(){fleetSize=Mathf.Clamp(fleetSize,1,16);foreach(var v in vehicles)if(v!=null)Destroy(v.gameObject);vehicles.Clear();var garage=GroomingGarageRuntime.I;garage.SetCapacity(Mathf.Max(garage.Capacity,fleetSize));for(int i=0;i<fleetSize;i++){var go=new GameObject("Dameuse_"+(i+1).ToString("00"));go.transform.SetParent(transform);var v=go.AddComponent<GroomingVehicleRuntime>();v.SetGaragePoint(garage.GetParkingPoint(i));v.Finished+=OnVehicleFinished;vehicles.Add(v);}}
  void Dispatch(){if(!automatic)return;
   var team=GroomingTeamRuntime.I;
   float hour=GameClock.I!=null?GameClock.I.GameHour:12f;
   if(team!=null && !team.CanOperate(hour))return;var sch=GroomingScheduleRuntime.I;if(sch==null)return;var strategy=GroomingFleetStrategyRuntime.I;int guard=sch.Queue.Count;while(guard-->0){GroomingVehicleRuntime v=FindAvailable();if(v==null)break;SortQueueBySector(sch.Queue,strategy);if(sch.Queue.Count==0)break;var slope=FindAuthorizedSlope(sch.Queue);if(slope==null)break;sch.Queue.Remove(slope);if(slope==null)continue;var m=slope.GetComponent<SlopeMaintenanceRuntime>()??slope.gameObject.AddComponent<SlopeMaintenanceRuntime>();m.BeginGrooming();if(FinanceRuntime.I!=null&&operationCost>0f)FinanceRuntime.I.RecordExpense(operationCost,FinanceCategory.Maintenance,"grooming_fleet","Damage de piste");v.speed *= team!=null?team.EfficiencyMultiplier:1f; v.StartWork(slope,instantOperations);}}
  GroomingVehicleRuntime FindAvailable(){foreach(var v in vehicles)if(v!=null&&v.IsAvailable&&!v.returning)return v;return null;}

  SlopeRuntime FindAuthorizedSlope(List<SlopeRuntime> q){
   var mg=GroomingSectorManagementRuntime.I;
   if(mg==null||!mg.HasAnyAssignment()) return q.Count>0?q[0]:null;
   foreach(var candidate in q){ if(candidate==null)continue; var sec=GroomingSectorDispatchRuntime.I!=null?GroomingSectorDispatchRuntime.I.FindFor(candidate):null; if(mg.IsSectorAuthorized(sec)) return candidate; }
   return null;
  }
  void SortQueue(List<SlopeRuntime> q,GroomingFleetStrategyRuntime strategy){q.Sort((a,b)=>{if(strategy!=null&&strategy.mode==GroomingPriorityMode.ManualPriority){bool ap=a!=null&&a.name==strategy.manualPrioritySlopeId;bool bp=b!=null&&b.name==strategy.manualPrioritySlopeId;if(ap!=bp)return ap?-1:1;}return Score(b).CompareTo(Score(a));});}

  void SortQueueBySector(List<SlopeRuntime> q,GroomingFleetStrategyRuntime strategy){
   var sector=GroomingSectorDispatchRuntime.I;
   if(sector==null){SortQueue(q,strategy);return;}
   q.Sort((a,b)=>{
    if(strategy!=null&&strategy.mode==GroomingPriorityMode.ManualPriority){
     bool ap=a!=null&&a.name==strategy.manualPrioritySlopeId, bp=b!=null&&b.name==strategy.manualPrioritySlopeId;
     if(ap!=bp)return ap?-1:1;
    }
    float sa=SectorScore(a), sb=SectorScore(b); return sb.CompareTo(sa);
   });
  }
  float SectorScore(SlopeRuntime s){
   if(s==null)return -999f;
   var sec=GroomingSectorDispatchRuntime.I.FindFor(s);
   float baseScore=Score(s);
   return baseScore+(sec!=null?sec.Snapshot.priorityScore*0.35f:0f)+(GroomingSectorManagementRuntime.I!=null?GroomingSectorManagementRuntime.I.PriorityBonus(sec):0f);
  }

  float Score(SlopeRuntime s){if(s==null)return -999f;var m=s.GetComponent<SlopeMaintenanceRuntime>();float q=m?m.snowCondition:1f;float g=m?m.grooming:1f;return (1f-q)*70f+(1f-g)*40f+(s.TrafficDensity*5f);}
  void OnVehicleFinished(GroomingVehicleRuntime v,SlopeRuntime s){ }
  void SyncStrategy(){var st=GroomingFleetStrategyRuntime.I;if(st==null)return;st.SetRuntimeState(CountAvailable(),CountActive(),GroomingScheduleRuntime.I!=null?GroomingScheduleRuntime.I.Queue.Count:0);}
  int CountAvailable(){int n=0;foreach(var v in vehicles)if(v!=null&&v.IsAvailable)n++;return n;}
  int CountActive(){int n=0;foreach(var v in vehicles)if(v!=null&&v.working)n++;return n;}
 }
}
