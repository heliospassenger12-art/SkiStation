using UnityEngine;
namespace SkiStation.PisteOperations {
 public class GroomingTrafficUI:MonoBehaviour {
  bool show; void Update(){if(Input.GetKeyDown(KeyCode.F8))show=!show;}
  void OnGUI(){if(!show)return;var f=GroomingVehicleFleetRuntime.I;if(f==null)return;GUI.Box(new Rect(12,680,470,180),"CIRCULATION DES DAMEUSES — F8");int y=710,i=1;foreach(var v in f.Vehicles){if(v==null)continue;string state=v.TrafficHold?"ATTENTE":(v.TrafficSpeedMultiplier<.99f?"RALENTIE":(v.working?"CIRCULE":"LIBRE"));GUI.Label(new Rect(25,y,430,20),$"Dameuse {i:00} : {state}  x{v.TrafficSpeedMultiplier:0.00}");y+=22;i++;}}
 }
}
