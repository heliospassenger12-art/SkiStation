using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>2.85 visual-only cohesion pass: softly links nearby real piste endpoints and lift terminals.</summary>
    public sealed class DomainCohesion285 : MonoBehaviour
    {
        static DomainCohesion285 instance;
        readonly HashSet<string> made = new HashSet<string>();
        Material snow, stone, wood, blue;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive || instance != null) return;
            var go = new GameObject("SkiStation_285_Cohesion_Domaine_VISUEL");
            instance = go.AddComponent<DomainCohesion285>();
        }

        void Start()
        {
            snow = Mat("285_NeigeTransition", new Color(.91f,.96f,.985f), .12f);
            stone = Mat("285_Pierre", new Color(.30f,.31f,.31f), .16f);
            wood = Mat("285_Bois", new Color(.26f,.14f,.075f), .22f);
            blue = Mat("285_Signal", new Color(.07f,.25f,.56f), .24f);
            InvokeRepeating(nameof(Refresh), 2.2f, 3.5f);
        }

        void Refresh()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;
            var slopes = FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None);
            var lifts = FindObjectsByType<LiftRuntime>(FindObjectsSortMode.None);
            foreach (var s in slopes)
            {
                if (s == null || s.Points == null || s.Points.Count < 2 || !s.name.StartsWith("Piste_")) continue;
                TryEndpoint(s, s.TopPoint, "Sommet", lifts);
                TryEndpoint(s, s.BottomPoint, "Bas", lifts);
            }
        }

        void TryEndpoint(SlopeRuntime slope, Vector3 endpoint, string endLabel, LiftRuntime[] lifts)
        {
            LiftRuntime best = null; Vector3 terminal = default; float bestD = 34f;
            foreach (var l in lifts)
            {
                if (l == null || l.Definition == null || l.Route == null || l.Route.Count < 2 || l.IsUnderground) continue;
                float db = Horizontal(endpoint,l.Bottom), dt = Horizontal(endpoint,l.Top);
                if (db < bestD) { bestD=db; best=l; terminal=l.Bottom; }
                if (dt < bestD) { bestD=dt; best=l; terminal=l.Top; }
            }
            if (best == null || bestD < 2.5f) return;
            string key = slope.GetInstanceID()+"_"+endLabel+"_"+best.GetInstanceID();
            if (!made.Add(key)) return;
            BuildTransition(slope, endpoint, terminal, endLabel);
        }

        void BuildTransition(SlopeRuntime slope, Vector3 a, Vector3 b, string label)
        {
            var root = new GameObject("285_RaccordVisuel_"+label+"_"+slope.name);
            root.transform.SetParent(transform,true);
            Vector3 delta=b-a; float len=delta.magnitude; if(len<.1f)return;
            Vector3 dir=delta.normalized; float width=Mathf.Clamp(slope.AverageWidth*.48f,3.2f,8.5f);
            var strip=Box(root.transform,"NeigeLiaison",(a+b)*.5f+Vector3.up*.035f,new Vector3(width,.025f,len*.92f),snow,Quaternion.LookRotation(dir,Vector3.up));
            // Low snow/stone shoulders visually anchor the junction while remaining non-interactive.
            Vector3 across=Vector3.Cross(Vector3.up,dir).normalized;
            for(int side=-1;side<=1;side+=2)
            {
                Vector3 p=Vector3.Lerp(a,b,.62f)+across*(width*.58f*side)+Vector3.up*.13f;
                Box(root.transform,"BordNeige",p,new Vector3(.65f,.22f,Mathf.Min(5.5f,len*.35f)),snow,Quaternion.LookRotation(dir,Vector3.up));
            }
            // Compact wayfinding marker at the piste side of the junction.
            Vector3 signPos=Vector3.Lerp(a,b,.20f)+across*(width*.62f)+Vector3.up*1.25f;
            Box(root.transform,"PoteauDirection",signPos,new Vector3(.10f,2.5f,.10f),wood,Quaternion.identity);
            Box(root.transform,"PlaqueDirection",signPos+Vector3.up*.78f+dir*.02f,new Vector3(1.8f,.42f,.10f),blue,Quaternion.LookRotation(dir,Vector3.up));
            Box(root.transform,"SoclePierre",signPos-Vector3.up*1.12f,new Vector3(.55f,.18f,.55f),stone,Quaternion.identity);
            Debug.Log("SkiStation 2.85: raccord visuel piste/remontee ajoute pour "+slope.name+" (visuel uniquement).");
        }

        static float Horizontal(Vector3 a,Vector3 b){a.y=0;b.y=0;return Vector3.Distance(a,b);}
        GameObject Box(Transform p,string n,Vector3 pos,Vector3 scale,Material m,Quaternion q){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.rotation=q;g.transform.localScale=scale;var c=g.GetComponent<Collider>();if(c!=null)Destroy(c);var r=g.GetComponent<Renderer>();if(r!=null)r.sharedMaterial=m;return g;}
        Material Mat(string n,Color c,float sm){var s=Shader.Find("Universal Render Pipeline/Lit");if(s==null)s=Shader.Find("Standard");if(s==null)s=Shader.Find("Sprites/Default");var m=new Material(s){name=n,color=c};if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",sm);if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",sm);return m;}
        void OnDestroy(){if(instance==this)instance=null;foreach(var m in new[]{snow,stone,wood,blue})if(m!=null)Destroy(m);}
    }
}
