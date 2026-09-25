using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>2.97 visual-only: readable entrances, warm shopfronts and restrained alpine facade details on confirmed player buildings.</summary>
    public sealed class BuildingFacadeLife297 : MonoBehaviour
    {
        static BuildingFacadeLife297 instance;
        readonly HashSet<int> done = new HashSet<int>();
        readonly List<Transform> nearGroups = new List<Transform>();
        GameObject root; Material wood, glass, warm, dark, snow, red; float nextScan, nextLod;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive||instance!=null)return;var g=new GameObject("SkiStation_297_Facades_Vivantes_VISUEL");instance=g.AddComponent<BuildingFacadeLife297>();}
        static bool Player(string n){return n.StartsWith("Chalet_joueur_")||n.StartsWith("Hotel_joueur_")||n.StartsWith("Commerce_joueur_")||n.StartsWith("Immeuble_appartements_joueur_")||n.StartsWith("Residence_tourisme_joueur_")||n.StartsWith("Magasin_sports_hiver_");}
        static Material Mat(Color c){var s=Shader.Find("Universal Render Pipeline/Lit");if(s==null)s=Shader.Find("Standard");if(s==null)s=Shader.Find("Sprites/Default");var m=new Material(s){color=c};if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.18f);return m;}
        GameObject Box(Transform p,string n,Vector3 lp,Vector3 sc,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=lp;g.transform.localScale=sc;var c=g.GetComponent<Collider>();if(c!=null)Destroy(c);g.GetComponent<Renderer>().sharedMaterial=m;return g;}
        void Start(){root=new GameObject("Facades_vivantes_297_VISUEL");root.transform.SetParent(transform,false);wood=Mat(new Color(.30f,.18f,.10f));glass=Mat(new Color(.18f,.29f,.34f));warm=Mat(new Color(1f,.67f,.30f));dark=Mat(new Color(.10f,.11f,.12f));snow=Mat(new Color(.97f,.985f,1f));red=Mat(new Color(.52f,.08f,.055f));}
        void Update(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive){Destroy(gameObject);return;}if(Time.unscaledTime>=nextScan){nextScan=Time.unscaledTime+2.5f;Scan();}if(Time.unscaledTime>=nextLod){nextLod=Time.unscaledTime+.5f;Lod();}}
        void Scan(){foreach(var s in FindObjectsByType<StationStructureStatus249>(FindObjectsSortMode.None)){if(s==null||!Player(s.gameObject.name)||!done.Add(s.gameObject.GetInstanceID()))continue;Decorate(s.transform);}}
        void BoundsOf(Transform b,out float w,out float d,out float h){var rs=b.GetComponentsInChildren<Renderer>();Bounds bd=new Bounds(b.position,new Vector3(12,7,10));bool got=false;foreach(var r in rs){if(r==null||r.transform.name.StartsWith("288_")||r.transform.name.StartsWith("294_")||r.transform.name.StartsWith("295_")||r.transform.name.StartsWith("296_")||r.transform.name.StartsWith("297_"))continue;if(!got){bd=r.bounds;got=true;}else bd.Encapsulate(r.bounds);}w=Mathf.Clamp(bd.size.x,9,32);d=Mathf.Clamp(bd.size.z,8,26);h=Mathf.Clamp(bd.size.y,5,18);}
        void Decorate(Transform b){BoundsOf(b,out var w,out var d,out var h);var g=new GameObject("297_Facade_"+b.name);g.transform.SetParent(b,false);nearGroups.Add(g.transform);float front=-d*.51f;
            // A clear entrance silhouette: canopy + warm door + small snow cap.
            Box(g.transform,"297_Porte_entree",new Vector3(0,1.45f,front-.05f),new Vector3(1.45f,2.65f,.16f),glass);
            Box(g.transform,"297_Lueur_entree",new Vector3(0,1.55f,front-.15f),new Vector3(1.05f,1.75f,.06f),warm);
            Box(g.transform,"297_Auvent_bois",new Vector3(0,3.05f,front-.55f),new Vector3(3.2f,.20f,1.35f),wood);
            Box(g.transform,"297_Neige_auvent",new Vector3(0,3.18f,front-.55f),new Vector3(3.35f,.10f,1.48f),snow);
            // Readable facade rhythm from management-camera distance.
            int windows=w>18f?4:2;for(int i=0;i<windows;i++){float x=(-windows*.5f+.5f+i)*(w*.68f/windows);if(Mathf.Abs(x)<1.2f)x+=1.8f;Box(g.transform,"297_Fenetre_chaude",new Vector3(x,Mathf.Min(4.2f,h*.48f),front-.04f),new Vector3(1.25f,1.25f,.12f),warm);Box(g.transform,"297_Cadre_fenetre",new Vector3(x,Mathf.Min(4.2f,h*.48f),front-.12f),new Vector3(1.48f,1.48f,.08f),dark);}
            bool shop=b.name.StartsWith("Commerce_joueur_")||b.name.StartsWith("Magasin_sports_hiver_");
            if(shop){Box(g.transform,"297_Enseigne_commerce",new Vector3(0,4.15f,front-.18f),new Vector3(Mathf.Min(w*.48f,7f),.72f,.18f),red);Box(g.transform,"297_Bandeau_neige",new Vector3(0,4.57f,front-.18f),new Vector3(Mathf.Min(w*.51f,7.4f),.10f,.24f),snow);for(int i=-1;i<=1;i+=2){Box(g.transform,"297_Presentoir_ski",new Vector3(i*2.15f,.85f,front-.75f),new Vector3(.18f,1.7f,.18f),dark);Box(g.transform,"297_Ski_expose",new Vector3(i*2.15f,.92f,front-.86f),new Vector3(.09f,1.9f,.08f),red);}}
            if(b.name.StartsWith("Hotel_joueur_")||b.name.StartsWith("Residence_tourisme_joueur_")){Box(g.transform,"297_Marquee_hotel",new Vector3(0,3.35f,front-.85f),new Vector3(4.6f,.18f,1.9f),dark);Box(g.transform,"297_Neige_marquee",new Vector3(0,3.48f,front-.85f),new Vector3(4.75f,.10f,2.02f),snow);}
            Debug.Log("SkiStation 2.97: facade vivante ajoutee a "+b.name+" (visuel uniquement).");
        }
        void Lod(){var cam=Camera.main;if(cam==null)return;foreach(var t in nearGroups){if(t==null)continue;bool on=Vector3.Distance(cam.transform.position,t.position)<600f;if(t.gameObject.activeSelf!=on)t.gameObject.SetActive(on);}}
        void OnDestroy(){if(instance==this)instance=null;if(root!=null)Destroy(root);foreach(var m in new[]{wood,glass,warm,dark,snow,red})if(m!=null)Destroy(m);}
    }
}
