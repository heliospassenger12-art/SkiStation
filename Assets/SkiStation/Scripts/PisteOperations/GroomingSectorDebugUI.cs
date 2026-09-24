using UnityEngine;

namespace SkiStation.PisteOperations
{
    public class GroomingSectorDebugUI : MonoBehaviour
    {
        public bool visible;
        void Update(){ if(Input.GetKeyDown(KeyCode.F7)) visible=!visible; }
        void OnGUI()
        {
            if(!visible)return;
            var rt=GroomingSectorDispatchRuntime.I; if(rt==null)return;
            GUILayout.BeginArea(new Rect(12,12,390,500),"RÉSEAU DE DAMAGE","Window");
            GUILayout.Label("F7 : masquer");
            GUILayout.Label("Secteurs : "+rt.Sectors.Count);
            foreach(var s in rt.Sectors)
            {
                var x=s.Snapshot;
                GUILayout.Label(x.id+" | pistes "+x.slopeCount+" | dégradées "+x.degradedSlopeCount+" | priorité "+x.priorityScore.ToString("0"));
            }
            GUILayout.EndArea();
        }
    }
}
