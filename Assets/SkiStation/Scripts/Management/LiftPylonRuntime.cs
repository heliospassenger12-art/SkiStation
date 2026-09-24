using System; using UnityEngine;
public class LiftPylonRuntime : MonoBehaviour {
 [Serializable] public struct Snapshot { public string id,displayName; public float altitude,height,spanToPrevious,spanToNext,estimatedLoad,condition01; public bool selected; }
 [SerializeField] string pylonId,displayName; [SerializeField] float towerHeight=8f,estimatedLoad; [SerializeField,Range(0f,1f)] float condition01=1f;
 public string Id=>string.IsNullOrEmpty(pylonId)?name:pylonId; public float TowerHeight=>towerHeight; public float Condition01=>condition01;
 public Snapshot BuildSnapshot(float previousSpan,float nextSpan,bool selected)=>new Snapshot{id=Id,displayName=string.IsNullOrEmpty(displayName)?name:displayName,altitude=transform.position.y,height=towerHeight,spanToPrevious=Mathf.Max(0,previousSpan),spanToNext=Mathf.Max(0,nextSpan),estimatedLoad=estimatedLoad,condition01=condition01,selected=selected};
 public void SetTechnicalData(float height,float load,float condition){towerHeight=Mathf.Max(0,height);estimatedLoad=Mathf.Max(0,load);condition01=Mathf.Clamp01(condition);}
}