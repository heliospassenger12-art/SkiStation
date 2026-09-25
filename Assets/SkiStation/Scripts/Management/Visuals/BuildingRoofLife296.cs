using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>2.96 visual-only: roof silhouettes, chimneys and restrained smoke on confirmed player buildings.</summary>
    public sealed class BuildingRoofLife296 : MonoBehaviour
    {
        static BuildingRoofLife296 instance;
        readonly HashSet<int> done = new HashSet<int>();
        readonly List<Transform> closeGroups = new List<Transform>();
        GameObject root; Material dark, stone, snow, metal; float nextScan, nextLod;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive||instance!=null)return;var g=new GameObject("SkiStation_296_Toitures_Vivantes_VISUEL");instance=g.AddComponent<BuildingRoofLife296>();}
        static bool Player(string n){return n.StartsWith("Chalet_joueur_")||n.StartsWith("Hotel_joueur_")||n.StartsWith("Commerce_joueur_")||n.StartsWith("Immeuble_appartements_joueur_")||n.StartsWith("Residence_tourisme_joueur_")||n.StartsWith("Magasin_sports_hiver_");}
        static Material Mat(Color c){var s=Shader.Find("Universal Render Pipeline/Lit");if(s==null)s=Shader.Find("Standard");if(s==null)s=Shader.Find("Sprites/Default");return new Material(s){color=c};}
        GameObject Box(Transform p,string n,Vector3 lp,Vector3 sc,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=lp;g.transform.localScale=sc;var c=g.GetComponent<Collider>();if(c!=null)Destroy(c);g.GetComponent<Renderer>().sharedMaterial=m;return g;}
        void Start(){root=new GameObject("Toitures_vivantes_296_VISUEL");root.transform.SetParent(transform,false);dark=Mat(new Color(.16f,.12f,.10f));stone=Mat(new Color(.31f,.31f,.30f));snow=Mat(new Color(.97f,.985f,1f));metal=Mat(new Color(.12f,.14f,.16f));}
        void Update(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive){Destroy(gameObject);return;}if(Time.unscaledTime>=nextScan){nextScan=Time.unscaledTime+2.5f;Scan();}if(Time.unscaledTime>=nextLod){nextLod=Time.unscaledTime+.5f;Lod();}}
        void Scan(){foreach(var s in FindObjectsByType<StationStructureStatus249>(FindObjectsSortMode.None)){if(s==null||!Player(s.gameObject.name)||!done.Add(s.gameObject.GetInstanceID()))continue;Decorate(s.transform);}}
        void BoundsOf(Transform b,out float w,out float d,out float top){var rs=b.GetComponentsInChildren<Renderer>();Bounds bd=new Bounds(b.position,new Vector3(12,7,10));bool got=false;foreach(var r in rs){if(r==null||r.transform.name.StartsWith("288_")||r.transform.name.StartsWith("294_")||r.transform.name.StartsWith("295_")||r.transform.name.StartsWith("296_"))continue;if(!got){bd=r.bounds;got=true;}else bd.Encapsulate(r.bounds);}w=Mathf.Clamp(bd.size.x,9,32);d=Mathf.Clamp(bd.size.z,8,26);top=b.InverseTransformPoint(new Vector3(b.position.x,bd.max.y,b.position.z)).y;}
        void Decorate(Transform b){BoundsOf(b,out var w,out var d,out var top);var g=new GameObject("296_Toiture_"+b.name);g.transform.SetParent(b,false);closeGroups.Add(g.transform);
            // Snow lip makes the roof line read cleanly at management-camera distance.
            Box(g.transform,"296_Corniche_neige",new Vector3(0,top+.08f,0),new Vector3(w*.92f,.14f,d*.86f),snow);
            bool chimney=!b.name.StartsWith("Commerce_joueur_")&&!b.name.StartsWith("Magasin_sports_hiver_");
            if(chimney){float x=-w*.27f;float z=d*.12f;Box(g.transform,"296_Cheminee_pierre",new Vector3(x,top+.75f,z),new Vector3(.75f,1.45f,.75f),stone);Box(g.transform,"296_Chapeau_cheminee",new Vector3(x,top+1.52f,z),new Vector3(.98f,.18f,.98f),dark);Smoke(g.transform,new Vector3(x,top+1.72f,z));}
            // Small roof vents keep hotels/residences from looking like blank blocks.
            if(b.name.StartsWith("Hotel_joueur_")||b.name.StartsWith("Immeuble_appartements_joueur_")||b.name.StartsWith("Residence_tourisme_joueur_"))for(int i=-1;i<=1;i+=2){float x=i*w*.25f;Box(g.transform,"296_Aeration_toit",new Vector3(x,top+.42f,-d*.12f),new Vector3(.48f,.65f,.48f),metal);Box(g.transform,"296_Neige_aeration",new Vector3(x,top+.78f,-d*.12f),new Vector3(.60f,.10f,.60f),snow);}
            Debug.Log("SkiStation 2.96: toiture vivante ajoutee a "+b.name+" (visuel uniquement).");
        }
        void Smoke(Transform p,Vector3 lp){var go=new GameObject("296_Fumee_cheminee");go.transform.SetParent(p,false);go.transform.localPosition=lp;var ps=go.AddComponent<ParticleSystem>();var main=ps.main;main.loop=true;main.startLifetime=new ParticleSystem.MinMaxCurve(2.8f,4.8f);main.startSpeed=new ParticleSystem.MinMaxCurve(.35f,.75f);main.startSize=new ParticleSystem.MinMaxCurve(.45f,.9f);main.maxParticles=18;main.simulationSpace=ParticleSystemSimulationSpace.World;var em=ps.emission;em.rateOverTime=2.1f;var sh=ps.shape;sh.shapeType=ParticleSystemShapeType.Cone;sh.angle=10f;sh.radius=.12f;var col=ps.colorOverLifetime;col.enabled=true;Gradient gr=new Gradient();gr.SetKeys(new[]{new GradientColorKey(new Color(.86f,.88f,.90f),0),new GradientColorKey(new Color(.72f,.76f,.80f),1)},new[]{new GradientAlphaKey(.32f,0),new GradientAlphaKey(0f,1)});col.color=gr;var r=go.GetComponent<ParticleSystemRenderer>();var s=Shader.Find("Universal Render Pipeline/Particles/Unlit");if(s==null)s=Shader.Find("Particles/Standard Unlit");if(s!=null)r.material=new Material(s);}
        void Lod(){var cam=Camera.main;if(cam==null)return;foreach(var t in closeGroups){if(t==null)continue;bool on=Vector3.Distance(cam.transform.position,t.position)<650f;if(t.gameObject.activeSelf!=on)t.gameObject.SetActive(on);}}
        void OnDestroy(){if(instance==this)instance=null;if(root!=null)Destroy(root);foreach(var m in new[]{dark,stone,snow,metal})if(m!=null)Destroy(m);}
    }
}
