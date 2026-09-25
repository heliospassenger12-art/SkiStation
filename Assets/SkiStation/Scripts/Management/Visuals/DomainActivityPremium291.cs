using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>2.91 visual-only domain activity: animated piste/lift flags and restrained snow movement around real infrastructure.</summary>
    public sealed class DomainActivityPremium291 : MonoBehaviour
    {
        static DomainActivityPremium291 instance;
        readonly HashSet<int> slopesDone = new HashSet<int>();
        readonly HashSet<int> liftsDone = new HashSet<int>();
        readonly List<Flag> flags = new List<Flag>();
        Material pole, blue, red, dark, green, orange;
        float nextScan;
        sealed class Flag { public Transform t; public Quaternion baseRot; public float phase, amp; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){ if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive || instance!=null)return; var g=new GameObject("SkiStation_291_Vie_Domaine_Animee_VISUEL"); instance=g.AddComponent<DomainActivityPremium291>(); }

        void Start(){ pole=Mat(new Color(.10f,.11f,.12f),.25f); blue=Mat(new Color(.04f,.20f,.55f),.18f); red=Mat(new Color(.65f,.045f,.035f),.16f); dark=Mat(new Color(.035f,.04f,.045f),.18f); green=Mat(new Color(.045f,.40f,.12f),.16f); orange=Mat(new Color(.94f,.28f,.025f),.16f); }
        static Material Mat(Color c,float s){var sh=Shader.Find("Universal Render Pipeline/Lit");if(sh==null)sh=Shader.Find("Standard");if(sh==null)sh=Shader.Find("Sprites/Default");var m=new Material(sh){color=c};if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",s);return m;}

        void Update(){
            if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive){Destroy(gameObject);return;}
            if(Time.unscaledTime>=nextScan){nextScan=Time.unscaledTime+3f;Scan();}
            float t=Time.unscaledTime;
            foreach(var f in flags) if(f.t!=null) f.t.localRotation=f.baseRot*Quaternion.Euler(0f,Mathf.Sin(t*1.35f+f.phase)*f.amp,Mathf.Sin(t*1.8f+f.phase)*1.4f);
        }

        void Scan(){
            foreach(var s in FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None)) if(s!=null&&s.Points!=null&&s.Points.Count>2&&s.name.StartsWith("Piste_")&&slopesDone.Add(s.GetInstanceID())) AddSlopeLife(s);
            foreach(var l in FindObjectsByType<LiftRuntime>(FindObjectsSortMode.None)) if(l!=null&&l.Definition!=null&&l.Route!=null&&l.Route.Count>1&&!l.IsUnderground&&liftsDone.Add(l.GetInstanceID())) { AddLiftLife(l,l.Bottom,"Bas"); AddLiftLife(l,l.Top,"Haut"); }
        }

        void AddSlopeLife(SlopeRuntime s){
            int[] ids={Mathf.Clamp(s.Points.Count/4,0,s.Points.Count-2),Mathf.Clamp(s.Points.Count*3/4,0,s.Points.Count-2)};
            for(int n=0;n<ids.Length;n++){
                int i=ids[n]; Vector3 a=s.Points[i], b=s.Points[i+1]; Vector3 dir=Vector3.ProjectOnPlane(b-a,Vector3.up).normalized;if(dir.sqrMagnitude<.1f)continue;Vector3 side=Vector3.Cross(Vector3.up,dir).normalized;
                float half=Mathf.Clamp(s.AverageWidth*.46f,3f,7f); Vector3 p=a+side*half*(n==0?-1f:1f);
                CreateFlag("291_FanionPiste_"+s.name+"_"+n,p,dir,Difficulty(s.Difficulty),2.25f,.72f,7f+n*1.7f);
            }
        }

        void AddLiftLife(LiftRuntime l,Vector3 p,string label){
            Vector3 dir=Vector3.ProjectOnPlane(l.Top-l.Bottom,Vector3.up).normalized;if(dir.sqrMagnitude<.1f)dir=Vector3.forward;Vector3 side=Vector3.Cross(Vector3.up,dir).normalized;
            CreateFlag("291_FanionGare_"+label+"_"+l.name,p+side*6.1f,dir,blue,2.7f,.9f,3.2f+(label=="Haut"?2.4f:0f));
            AddSnowDust("291_Poudrerie_"+label+"_"+l.name,p-dir*2.1f+Vector3.up*.18f,dir);
        }

        void CreateFlag(string name,Vector3 p,Vector3 dir,Material mat,float height,float width,float phase){
            var root=new GameObject(name);root.transform.SetParent(transform,true);root.transform.position=p;
            Box(root.transform,"Mat",Vector3.up*height*.5f,new Vector3(.09f,height,.09f),pole,Quaternion.identity,true);
            var cloth=Box(root.transform,"Toile",Vector3.up*(height-.38f)+dir*.05f,new Vector3(width,.42f,.055f),mat,Quaternion.LookRotation(dir,Vector3.up),false);
            flags.Add(new Flag{t=cloth.transform,baseRot=cloth.transform.localRotation,phase=phase,amp=3.6f});
        }

        void AddSnowDust(string name,Vector3 p,Vector3 dir){
            var go=new GameObject(name);go.transform.SetParent(transform,true);go.transform.position=p;
            var ps=go.AddComponent<ParticleSystem>();var main=ps.main;main.loop=true;main.startLifetime=new ParticleSystem.MinMaxCurve(.65f,1.35f);main.startSpeed=new ParticleSystem.MinMaxCurve(.45f,1.15f);main.startSize=new ParticleSystem.MinMaxCurve(.025f,.075f);main.maxParticles=38;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startColor=new Color(1f,1f,1f,.38f);main.gravityModifier=.03f;
            var emission=ps.emission;emission.rateOverTime=8f;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(3.8f,.15f,2.2f);
            var vel=ps.velocityOverLifetime;vel.enabled=true;vel.space=ParticleSystemSimulationSpace.World;vel.x=dir.x*.65f;vel.y=.12f;vel.z=dir.z*.65f;
            var rend=ps.GetComponent<ParticleSystemRenderer>();rend.renderMode=ParticleSystemRenderMode.Billboard;
        }

        Material Difficulty(SlopeDifficulty d){switch(d){case SlopeDifficulty.Green:return green;case SlopeDifficulty.Red:return red;case SlopeDifficulty.Black:case SlopeDifficulty.Extreme:return dark;default:return blue;}}
        GameObject Box(Transform parent,string n,Vector3 local,Vector3 scale,Material m,Quaternion worldRot,bool polePart){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(parent,false);g.transform.localPosition=local;g.transform.rotation=worldRot;g.transform.localScale=scale;var c=g.GetComponent<Collider>();if(c!=null)Destroy(c);var r=g.GetComponent<Renderer>();if(r!=null)r.sharedMaterial=m;return g;}
        void OnDestroy(){if(instance==this)instance=null;foreach(var m in new[]{pole,blue,red,dark,green,orange})if(m!=null)Destroy(m);}
    }
}
