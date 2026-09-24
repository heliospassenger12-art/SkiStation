using UnityEngine;
namespace SkiStation.PisteOperations
{
 public class GroomingSectorManagementUI:MonoBehaviour
 {
  bool show;
  void Update(){if(Input.GetKeyDown(KeyCode.F7))show=!show;}
  void OnGUI(){if(!show)return;var rt=GroomingSectorDispatchRuntime.I;var mg=GroomingSectorManagementRuntime.I;var team=GroomingTeamRuntime.I;if(rt==null||mg==null)return;
   GUI.Box(new Rect(12,12,520,Mathf.Min(700,180+rt.Sectors.Count*52)),"SECTEURS D’EXPLOITATION — F7");
   GUI.Label(new Rect(28,42,480,24),"Affecte les équipes aux secteurs et fixe leur priorité.");
   GUI.Label(new Rect(28,64,480,22),"Une affectation active restreint automatiquement le dispatch aux secteurs assignés.");
   int y=94; int max=team!=null?Mathf.Max(0,team.teamCount):0;
   foreach(var s in rt.Sectors){if(s==null)continue;var snap=s.Snapshot;int assigned=mg.GetAssignedTeams(s);string p=mg.GetPriority(s).ToString();
    GUI.Label(new Rect(28,y,300,22),$"{snap.id}  • {snap.slopeCount} pistes • priorité {p}");
    GUI.Label(new Rect(28,y+20,270,20),$"Dégradées: {snap.degradedSlopeCount} • équipes: {assigned}/{max}");
    if(GUI.Button(new Rect(315,y,60,24),"+"))mg.AddTeam(s,max);
    if(GUI.Button(new Rect(380,y,60,24),"−"))mg.RemoveTeam(s);
    if(GUI.Button(new Rect(445,y,75,24),"Priorité"))mg.CyclePriority(s);
    y+=52;
   }
   GUI.Label(new Rect(28,y+4,480,22),"F7 pour fermer");
  }
 }
}
