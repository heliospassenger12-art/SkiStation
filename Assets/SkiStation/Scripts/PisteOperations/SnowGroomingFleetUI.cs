using UnityEngine;
namespace SkiStation
{
    public sealed class SnowGroomingFleetUI : MonoBehaviour
    {
        bool show; Vector2 scroll;
        void Update(){if(Input.GetKeyDown(KeyCode.F6))show=!show;}
        void OnGUI(){if(!show)return; var f=SnowGroomingFleetRuntime.I; if(f==null)return;
            GUILayout.BeginArea(new Rect(15, Screen.height-430, 470, 415), GUI.skin.window);
            GUILayout.Label("EXPLOITATION — DAMEUSES & ÉQUIPES");
            GUILayout.Label($"Équipe : {f.CrewCount}  |  Jobs actifs : {f.ActiveJobs}  |  Préparation : {f.CalculateDomainPreparation():0}%");
            GUILayout.Label($"Dernière opération : {f.LastJob}");
            if(GUILayout.Button("Préparer le domaine maintenant"))f.RunNightPreparation();
            scroll=GUILayout.BeginScrollView(scroll);
            foreach(var v in f.Fleet) GUILayout.Label($"{v.id} — {v.type} — {(v.available?"Disponible":"En intervention")}");
            GUILayout.Space(8);
            foreach(var s in Object.FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None)){
                var c=s?.GetComponent<SlopeConditionRuntime>(); if(c==null)continue;
                GUILayout.BeginHorizontal(); GUILayout.Label($"{s.name} : {c.ConditionLabel}",GUILayout.Width(250));
                if(GUILayout.Button("Damer",GUILayout.Width(80)))f.QueueGrooming(s,1f);
                if(GUILayout.Button("Gros",GUILayout.Width(80)))f.QueueGrooming(s,1.6f); GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
    }
}
