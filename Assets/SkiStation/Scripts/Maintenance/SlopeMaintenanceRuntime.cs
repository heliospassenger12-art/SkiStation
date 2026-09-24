using UnityEngine;
namespace SkiStation {
 public class SlopeMaintenanceRuntime:MonoBehaviour {
  public float snowCondition=1f; public float grooming=1f; public bool groomingInProgress;
  public void Tick(float traffic, float hours){ snowCondition=Mathf.Clamp01(snowCondition-Mathf.Max(0f,traffic)*.002f*hours); grooming=Mathf.Clamp01(grooming-Mathf.Max(0f,traffic)*.001f*hours); }
  public void GroomInstant(){ snowCondition=1f; grooming=1f; groomingInProgress=false; }
  public void BeginGrooming(){ groomingInProgress=true; }
  public void CompleteGrooming(){ GroomInstant(); }
 }
}
