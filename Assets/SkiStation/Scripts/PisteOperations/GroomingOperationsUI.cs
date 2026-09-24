using UnityEngine;
namespace SkiStation { public class GroomingOperationsUI:MonoBehaviour {
 bool show; void Update(){if(Input.GetKeyDown(KeyCode.F5))show=!show;}
 void OnGUI(){if(!show)return;var s=GroomingScheduleRuntime.I;if(s==null)return;GUI.Box(new Rect(12,270,360,180),"DAMAGE — F5");GUI.Label(new Rect(25,300,330,22),$"File : {s.Queue.Count}   Actif : {(s.Active!=null?s.Active.name:"aucun")}");GUI.Label(new Rect(25,325,330,22),$"Automatique : {(s.automatic?"OUI":"NON")}");int y=350;foreach(var p in s.Queue){GUI.Label(new Rect(25,y,330,20),"• "+p.name);y+=20;if(y>425)break;}}
 }}
