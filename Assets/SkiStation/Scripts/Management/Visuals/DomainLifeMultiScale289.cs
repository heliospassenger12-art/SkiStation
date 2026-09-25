using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>2.89 visual-only multi-scale domain life: real piste gateways and lift-terminal dressing with distance LOD.</summary>
    public sealed class DomainLifeMultiScale289 : MonoBehaviour
    {
        static DomainLifeMultiScale289 instance;
        readonly HashSet<int> slopesDone = new HashSet<int>();
        readonly HashSet<int> liftsDone = new HashSet<int>();
        readonly List<DetailGroup> groups = new List<DetailGroup>();
        Material wood, metal, snow, blue, red, orange, dark;
        float nextScan, nextLod;
        sealed class DetailGroup { public GameObject medium, close; public Vector3 anchor; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){ if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive || instance!=null)return; var g=new GameObject("SkiStation_289_Vie_Domaine_MultiEchelle_VISUEL"); instance=g.AddComponent<DomainLifeMultiScale289>(); }

        void Start(){
            wood=Mat(new Color(.27f,.14f,.07f),.18f); metal=Mat(new Color(.13f,.15f,.16f),.32f); snow=Mat(new Color(.95f,.98f,1f),.28f);
            blue=Mat(new Color(.055f,.22f,.55f),.2f); red=Mat(new Color(.62f,.06f,.045f),.18f); orange=Mat(new Color(.95f,.31f,.035f),.16f); dark=Mat(new Color(.055f,.065f,.07f),.22f);
        }
        static Material Mat(Color c,float s){var sh=Shader.Find("Universal Render Pipeline/Lit");if(sh==null)sh=Shader.Find("Standard");if(sh==null)sh=Shader.Find("Sprites/Default");var m=new Material(sh){color=c};if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",s);return m;}
        void Update(){ if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive){Destroy(gameObject);return;} if(Time.unscaledTime>=nextScan){nextScan=Time.unscaledTime+3f;Scan();} if(Time.unscaledTime>=nextLod){nextLod=Time.unscaledTime+.45f;LOD();} }

        void Scan(){
            foreach(var s in FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None)) if(s!=null && s.Points!=null && s.Points.Count>1 && s.name.StartsWith("Piste_") && slopesDone.Add(s.GetInstanceID())) DecorateSlope(s);
            foreach(var l in FindObjectsByType<LiftRuntime>(FindObjectsSortMode.None)) if(l!=null && l.Definition!=null && l.Route!=null && l.Route.Count>1 && !l.IsUnderground && liftsDone.Add(l.GetInstanceID())) DecorateLift(l);
        }
        void DecorateSlope(SlopeRuntime s){
            Vector3 a=s.TopPoint,b=s.Points[Mathf.Min(1,s.Points.Count-1)]; Vector3 dir=Vector3.ProjectOnPlane(b-a,Vector3.up).normalized;if(dir.sqrMagnitude<.1f)dir=Vector3.forward;Vector3 side=Vector3.Cross(Vector3.up,dir).normalized;
            var med=new GameObject("289_EntreePiste_Moyen_"+s.name);med.transform.SetParent(transform,true);
            float half=Mathf.Clamp(s.AverageWidth*.42f,2.8f,6.5f); Vector3 p=a+Vector3.up*.1f;
            Box(med.transform,"PortiqueG",p-side*half+Vector3.up*1.65f,new Vector3(.16f,3.3f,.16f),wood,Quaternion.identity);
            Box(med.transform,"PortiqueD",p+side*half+Vector3.up*1.65f,new Vector3(.16f,3.3f,.16f),wood,Quaternion.identity);
            Box(med.transform,"Bandeau",p+Vector3.up*3.05f,new Vector3(half*2+.3f,.48f,.16f),DifficultyMat(s.Difficulty),Quaternion.LookRotation(dir,Vector3.up));
            var close=new GameObject("289_EntreePiste_Proche_"+s.name);close.transform.SetParent(transform,true);
            for(int k=-1;k<=1;k+=2){Vector3 q=p+side*(half+.65f)*k;Box(close.transform,"SocleNeige",q+Vector3.up*.18f,new Vector3(.85f,.32f,.85f),snow,Quaternion.identity);Box(close.transform,"Balise",q+Vector3.up*.95f,new Vector3(.10f,1.55f,.10f),metal,Quaternion.identity);Box(close.transform,"Fanion",q+Vector3.up*1.55f+dir*.04f,new Vector3(.72f,.34f,.08f),DifficultyMat(s.Difficulty),Quaternion.LookRotation(dir,Vector3.up));}
            groups.Add(new DetailGroup{medium=med,close=close,anchor=a});
        }
        void DecorateLift(LiftRuntime l){ DecorateTerminal(l,l.Bottom,"Bas"); DecorateTerminal(l,l.Top,"Haut"); }
        void DecorateTerminal(LiftRuntime l,Vector3 p,string label){
            Vector3 line=Vector3.ProjectOnPlane(l.Top-l.Bottom,Vector3.up).normalized;if(line.sqrMagnitude<.1f)line=Vector3.forward;Vector3 side=Vector3.Cross(Vector3.up,line).normalized;
            var med=new GameObject("289_Gare_"+label+"_Moyen_"+l.name);med.transform.SetParent(transform,true);
            for(int k=-1;k<=1;k+=2){Vector3 q=p+side*4.7f*k;Box(med.transform,"JalonGare",q+Vector3.up*1.45f,new Vector3(.14f,2.9f,.14f),metal,Quaternion.identity);Box(med.transform,"FanionGare",q+Vector3.up*2.25f+line*.04f,new Vector3(.85f,.42f,.08f),blue,Quaternion.LookRotation(line,Vector3.up));}
            var close=new GameObject("289_Gare_"+label+"_Proche_"+l.name);close.transform.SetParent(transform,true);
            // Queue-guide furniture only: no collider, no effect on the actual lift queue.
            for(int i=-2;i<=2;i++){Vector3 q=p-side*2.6f+line*(i*1.45f-1.2f);Box(close.transform,"PiquetFile",q+Vector3.up*.55f,new Vector3(.08f,1.1f,.08f),dark,Quaternion.identity);}
            Box(close.transform,"TapisNeige",p-line*2.2f+Vector3.up*.035f,new Vector3(5.8f,.05f,3.8f),snow,Quaternion.LookRotation(line,Vector3.up));
            Box(close.transform,"Protection",p+side*5.35f+Vector3.up*.8f,new Vector3(.52f,1.6f,.52f),orange,Quaternion.identity);
            groups.Add(new DetailGroup{medium=med,close=close,anchor=p});
        }
        Material DifficultyMat(SlopeDifficulty d){switch(d){case SlopeDifficulty.Green:return newMatGreen();case SlopeDifficulty.Red:return red;case SlopeDifficulty.Black:case SlopeDifficulty.Extreme:return dark;default:return blue;}}
        Material greenCache; Material newMatGreen(){if(greenCache==null)greenCache=Mat(new Color(.06f,.42f,.13f),.18f);return greenCache;}
        GameObject Box(Transform parent,string n,Vector3 pos,Vector3 scale,Material m,Quaternion rot){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(parent,true);g.transform.position=pos;g.transform.rotation=rot;g.transform.localScale=scale;var c=g.GetComponent<Collider>();if(c!=null)Destroy(c);var r=g.GetComponent<Renderer>();if(r!=null)r.sharedMaterial=m;return g;}
        void LOD(){var cam=Camera.main;if(cam==null)return;foreach(var g in groups){float d=Vector3.Distance(cam.transform.position,g.anchor);if(g.medium!=null&&g.medium.activeSelf!=(d<900f))g.medium.SetActive(d<900f);if(g.close!=null&&g.close.activeSelf!=(d<360f))g.close.SetActive(d<360f);}}
        void OnDestroy(){if(instance==this)instance=null;foreach(var m in new[]{wood,metal,snow,blue,red,orange,dark,greenCache})if(m!=null)Destroy(m);}
    }
}
