using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>2.94 visual-only: gives each confirmed player building family a clearer alpine identity.</summary>
    public sealed class BuildingIdentityPolish294 : MonoBehaviour
    {
        static BuildingIdentityPolish294 instance;
        readonly HashSet<int> done = new HashSet<int>();
        readonly List<Transform> nearGroups = new List<Transform>();
        GameObject root; Material wood,dark,stone,snow,glass,warm,red,blue,green;
        float nextScan,nextLod;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive||instance!=null)return;var g=new GameObject("SkiStation_294_Identite_Batiments_VISUEL");instance=g.AddComponent<BuildingIdentityPolish294>();}
        static Material Mat(Color c,float smooth=.18f){var s=Shader.Find("Universal Render Pipeline/Lit");if(s==null)s=Shader.Find("Standard");if(s==null)s=Shader.Find("Sprites/Default");var m=new Material(s){color=c};if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);return m;}
        GameObject Box(Transform p,string n,Vector3 lp,Vector3 sc,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=lp;g.transform.localScale=sc;var c=g.GetComponent<Collider>();if(c!=null)Destroy(c);g.GetComponent<Renderer>().sharedMaterial=m;return g;}
        void Start(){root=new GameObject("Identite_architecturale_294_VISUEL");root.transform.SetParent(transform,false);wood=Mat(new Color(.34f,.18f,.075f));dark=Mat(new Color(.10f,.075f,.055f));stone=Mat(new Color(.35f,.37f,.38f));snow=Mat(new Color(.97f,.99f,1f),.32f);glass=Mat(new Color(.25f,.48f,.58f),.52f);warm=Mat(new Color(1f,.58f,.20f),.4f);red=Mat(new Color(.58f,.07f,.055f));blue=Mat(new Color(.06f,.20f,.38f));green=Mat(new Color(.06f,.20f,.11f));}
        void Update(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive){Destroy(gameObject);return;}if(Time.unscaledTime>=nextScan){nextScan=Time.unscaledTime+2.5f;Scan();}if(Time.unscaledTime>=nextLod){nextLod=Time.unscaledTime+.5f;Lod();}}
        void Scan(){foreach(var s in FindObjectsByType<StationStructureStatus249>(FindObjectsSortMode.None)){if(s==null)continue;var b=s.gameObject;if(!PlayerBuilding(b.name)||!done.Add(b.GetInstanceID()))continue;Decorate(b.transform);}}
        static bool PlayerBuilding(string n){return n.StartsWith("Chalet_joueur_")||n.StartsWith("Hotel_joueur_")||n.StartsWith("Commerce_joueur_")||n.StartsWith("Immeuble_appartements_joueur_")||n.StartsWith("Residence_tourisme_joueur_")||n.StartsWith("Magasin_sports_hiver_");}
        void Size(Transform b,out float w,out float d,out float h){var rs=b.GetComponentsInChildren<Renderer>();Bounds bd=new Bounds(b.position,new Vector3(14,8,12));bool got=false;foreach(var r in rs){if(r==null||r.transform.name.StartsWith("288_")||r.transform.name.StartsWith("294_"))continue;if(!got){bd=r.bounds;got=true;}else bd.Encapsulate(r.bounds);}w=Mathf.Clamp(bd.size.x,10,32);d=Mathf.Clamp(bd.size.z,9,26);h=Mathf.Clamp(bd.size.y,7,32);}
        void Decorate(Transform b){Size(b,out var w,out var d,out var h);var far=new GameObject("294_Identite_"+b.name);far.transform.SetParent(b,false);var near=new GameObject("294_Micro_"+b.name);near.transform.SetParent(far.transform,false);nearGroups.Add(near.transform);
            if(b.name.StartsWith("Chalet_")) Chalet(far.transform,near.transform,w,d,h);
            else if(b.name.StartsWith("Hotel_")) Hotel(far.transform,near.transform,w,d,h);
            else if(b.name.StartsWith("Commerce_")) Commerce(far.transform,near.transform,w,d);
            else if(b.name.StartsWith("Magasin_sports_hiver_")) SkiShop(far.transform,near.transform,w,d);
            else Apartments(far.transform,near.transform,w,d,h,b.name.StartsWith("Residence_"));
        }
        void Chalet(Transform f,Transform n,float w,float d,float h){Box(f,"Poutre_faitiere",new Vector3(0,h*.72f,-d*.49f),new Vector3(w*.72f,.28f,.24f),dark);for(int s=-1;s<=1;s+=2)Box(f,"Volet_chalet",new Vector3(s*w*.30f,3.2f,-d*.53f),new Vector3(1.0f,2.5f,.16f),wood);Box(f,"Cheminee_pierre",new Vector3(w*.25f,h*.72f,-d*.08f),new Vector3(1.15f,3.4f,1.15f),stone);Box(f,"Chapeau_neige",new Vector3(w*.25f,h*.72f+1.75f,-d*.08f),new Vector3(1.45f,.18f,1.45f),snow);Box(n,"Banc_chalet",new Vector3(-w*.30f,.55f,-d*.62f),new Vector3(2.8f,.18f,.65f),wood);}
        void Hotel(Transform f,Transform n,float w,float d,float h){Box(f,"Marquise_hotel",new Vector3(0,3.2f,-d*.62f),new Vector3(Mathf.Min(8f,w*.5f),.35f,2.8f),dark);Box(f,"Enseigne_hotel",new Vector3(0,5.1f,-d*.56f),new Vector3(Mathf.Min(7f,w*.42f),1.2f,.18f),red);for(float y=7f;y<h-1f;y+=4f)for(int s=-1;s<=1;s+=2)Box(f,"Balcon_hotel",new Vector3(s*w*.25f,y,-d*.57f),new Vector3(w*.34f,.18f,1.25f),wood);for(int s=-1;s<=1;s+=2)Box(n,"Lanterne_hotel",new Vector3(s*2.7f,2.1f,-d*.69f),new Vector3(.35f,1.0f,.35f),warm);}
        void Commerce(Transform f,Transform n,float w,float d){Box(f,"Vitrine_commerce",new Vector3(0,2.2f,-d*.54f),new Vector3(w*.62f,3.2f,.15f),glass);Box(f,"Bandeau_commerce",new Vector3(0,4.15f,-d*.57f),new Vector3(w*.68f,.75f,.20f),green);Box(n,"Presentoir_commerce",new Vector3(w*.28f,.8f,-d*.68f),new Vector3(2.1f,1.35f,.8f),wood);}
        void SkiShop(Transform f,Transform n,float w,float d){Box(f,"Vitrine_magasin_ski",new Vector3(0,2.25f,-d*.54f),new Vector3(w*.68f,3.35f,.15f),glass);Box(f,"Enseigne_sports_hiver",new Vector3(0,4.35f,-d*.58f),new Vector3(w*.70f,.82f,.20f),blue);for(int i=-3;i<=3;i++)Box(n,"Ski_exposition",new Vector3(i*.48f,1.45f,-d*.70f),new Vector3(.10f,2.7f,.12f),i%2==0?red:blue);}
        void Apartments(Transform f,Transform n,float w,float d,float h,bool residence){for(float y=4.8f;y<h-1f;y+=3.6f){Box(f,"Balcon_logement",new Vector3(0,y,-d*.57f),new Vector3(w*.78f,.16f,1.05f),wood);for(int i=-2;i<=2;i++)Box(f,"Separation_balcon",new Vector3(i*w*.14f,y+.52f,-d*.62f),new Vector3(.10f,1.0f,.10f),dark);}Box(f,residence?"Enseigne_residence":"Entree_immeuble",new Vector3(0,2.8f,-d*.59f),new Vector3(Mathf.Min(6f,w*.42f),.85f,.18f),residence?green:stone);Box(n,"Boites_aux_lettres",new Vector3(w*.30f,1.15f,-d*.66f),new Vector3(1.5f,1.5f,.25f),dark);}
        void Lod(){var cam=Camera.main;if(cam==null)return;foreach(var t in nearGroups){if(t==null)continue;bool on=Vector3.Distance(cam.transform.position,t.position)<300f;if(t.gameObject.activeSelf!=on)t.gameObject.SetActive(on);}}
        void OnDestroy(){if(instance==this)instance=null;if(root!=null)Destroy(root);foreach(var m in new[]{wood,dark,stone,snow,glass,warm,red,blue,green})if(m!=null)Destroy(m);}
    }
}
