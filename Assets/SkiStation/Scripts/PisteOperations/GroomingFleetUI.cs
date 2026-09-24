using UnityEngine;
namespace SkiStation.PisteOperations {
 public class GroomingFleetUI:MonoBehaviour {
  bool show; void Update(){if(Input.GetKeyDown(KeyCode.F6))show=!show;}
  void OnGUI(){if(!show)return;var f=GroomingFleetRuntime.I;var s=GroomingScheduleRuntime.I;if(f==null)return;GUI.Box(new Rect(12,460,430,210),"DAMEUSES — F6");GUI.Label(new Rect(25,490,390,22),$"Flotte : {f.Vehicles.Count}   File : {(s!=null?s.Queue.Count:0)}");int y=520;int i=1;foreach(var v in f.Vehicles){string state=v==null?"inconnue":(v.working?"TRAVAIL":(v.returning?"RETOUR GARAGE":"DISPONIBLE"));GUI.Label(new Rect(25,y,390,20),$"Dameuse {i:00} : {state} {(v!=null&&v.target!=null?v.target.name:"")}");y+=22;i++;if(y>640)break;}}
 }
}
