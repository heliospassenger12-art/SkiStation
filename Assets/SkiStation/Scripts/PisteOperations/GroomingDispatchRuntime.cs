using UnityEngine;
namespace SkiStation { public class GroomingDispatchRuntime:MonoBehaviour {
  public float requestThreshold=.72f; public float criticalThreshold=.45f; public float scanInterval=5f; float t;
  void Update(){t+=Time.deltaTime;if(t<scanInterval)return;t=0;var sch=GroomingScheduleRuntime.I;if(sch==null)return;foreach(var s in FindObjectsOfType<SlopeRuntime>()){var m=s.GetComponent<SlopeMaintenanceRuntime>();if(m==null)continue;if(m.groomingInProgress)continue;if(m.snowCondition<criticalThreshold||m.grooming<criticalThreshold||m.snowCondition<requestThreshold||m.grooming<requestThreshold)sch.Request(s);}}
 }}
