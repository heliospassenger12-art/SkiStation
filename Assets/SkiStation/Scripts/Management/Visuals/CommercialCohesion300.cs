using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>3.00 visual-only milestone: coherent near/mid/far reading for the confirmed player village.</summary>
    public sealed class CommercialCohesion300 : MonoBehaviour
    {
        static CommercialCohesion300 instance;
        readonly HashSet<int> done = new HashSet<int>();
        readonly List<Entry> entries = new List<Entry>();
        GameObject root; Material wood, stone, snow, glass, warm, dark, accent; float nextScan, nextLod;
        sealed class Entry { public Transform anchor; public GameObject near, mid, far; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive||instance!=null)return;var g=new GameObject("SkiStation_300_Cohesion_Commerciale_VISUEL");instance=g.AddComponent<CommercialCohesion300>();}
        static bool Player(string n){return n.StartsWith("Chalet_joueur_")||n.StartsWith("Hotel_joueur_")||n.StartsWith("Commerce_joueur_")||n.StartsWith("Immeuble_appartements_joueur_")||n.StartsWith("Residence_tourisme_joueur_")||n.StartsWith("Magasin_sports_hiver_");}
        static Material Mat(Color c,float smooth=.12f){var s=Shader.Find("Universal Render Pipeline/Lit");if(s==null)s=Shader.Find("Standard");if(s==null)s=Shader.Find("Sprites/Default");var m=new Material(s){color=c};if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);return m;}
        GameObject Box(Transform p,string n,Vector3 lp,Vector3 sc,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=lp;g.transform.localScale=sc;var c=g.GetComponent<Collider>();if(c!=null)Destroy(c);g.GetComponent<Renderer>().sharedMaterial=m;return g;}
        void Start(){root=new GameObject("Finition_globale_300_VISUEL");root.transform.SetParent(transform,false);wood=Mat(new Color(.27f,.15f,.075f));stone=Mat(new Color(.28f,.29f,.29f));snow=Mat(new Color(.97f,.985f,1f),.24f);glass=Mat(new Color(.30f,.47f,.55f),.65f);warm=Mat(new Color(1f,.62f,.22f),.42f);dark=Mat(new Color(.08f,.10f,.11f),.3f);accent=Mat(new Color(.48f,.08f,.055f),.2f);}
        void Update(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive){Destroy(gameObject);return;}if(Time.unscaledTime>=nextScan){nextScan=Time.unscaledTime+2.5f;Scan();}if(Time.unscaledTime>=nextLod){nextLod=Time.unscaledTime+.35f;Lod();}}
        void Scan(){foreach(var s in FindObjectsByType<StationStructureStatus249>(FindObjectsSortMode.None)){if(s==null||!Player(s.gameObject.name)||!done.Add(s.gameObject.GetInstanceID()))continue;Decorate(s.transform);}}
        void BoundsOf(Transform b,out float w,out float h,out float d){var rs=b.GetComponentsInChildren<Renderer>();Bounds bd=new Bounds(b.position,new Vector3(12,7,10));bool got=false;foreach(var r in rs){if(r==null||r.transform.name.StartsWith("288_")||r.transform.name.StartsWith("294_")||r.transform.name.StartsWith("295_")||r.transform.name.StartsWith("296_")||r.transform.name.StartsWith("297_")||r.transform.name.StartsWith("298_")||r.transform.name.StartsWith("299_")||r.transform.name.StartsWith("300_"))continue;if(!got){bd=r.bounds;got=true;}else bd.Encapsulate(r.bounds);}w=Mathf.Clamp(bd.size.x,9,32);h=Mathf.Clamp(bd.size.y,5,24);d=Mathf.Clamp(bd.size.z,8,26);}
        void Decorate(Transform b){BoundsOf(b,out var w,out var h,out var d);var holder=new GameObject("300_Cohesion_"+b.name);holder.transform.SetParent(b,false);
            var far=new GameObject("300_FAR_silhouette");far.transform.SetParent(holder.transform,false);var mid=new GameObject("300_MID_architecture");mid.transform.SetParent(holder.transform,false);var near=new GameObject("300_NEAR_microdetails");near.transform.SetParent(holder.transform,false);
            float front=-d*.515f;float roof=h*.56f;
            // FAR: strong alpine silhouette readable from the management overview.
            Box(far.transform,"300_Socle_pierre",new Vector3(0,.34f,0),new Vector3(w*1.035f,.68f,d*1.035f),stone);
            Box(far.transform,"300_Corniche_neige",new Vector3(0,roof+.20f,0),new Vector3(w*1.055f,.18f,d*1.055f),snow);
            // MID: recognizable entrance rhythm and warm facade punctuation.
            Box(mid.transform,"300_Auvent",new Vector3(0,2.35f,front-.42f),new Vector3(Mathf.Clamp(w*.34f,3.5f,7.2f),.20f,1.25f),wood);
            Box(mid.transform,"300_Neige_auvent",new Vector3(0,2.49f,front-.42f),new Vector3(Mathf.Clamp(w*.36f,3.7f,7.5f),.09f,1.32f),snow);
            for(int i=-1;i<=1;i+=2)Box(mid.transform,"300_Fenetre_chaude",new Vector3(i*w*.27f,2.65f,front-.055f),new Vector3(Mathf.Clamp(w*.12f,1.15f,2.3f),1.15f,.09f),warm);
            Box(mid.transform,"300_Porte_verre",new Vector3(0,1.25f,front-.065f),new Vector3(1.45f,2.25f,.10f),glass);
            // NEAR: restrained premium details; no collider and no gameplay light.
            Box(near.transform,"300_Tapis_entree",new Vector3(0,.075f,front-1.05f),new Vector3(1.65f,.05f,1.35f),accent);
            Box(near.transform,"300_Panneau_entree",new Vector3(-Mathf.Min(w*.30f,4.2f),1.30f,front-.72f),new Vector3(1.15f,1.55f,.12f),dark);
            Box(near.transform,"300_Bandeau_panneau",new Vector3(-Mathf.Min(w*.30f,4.2f),1.55f,front-.79f),new Vector3(.78f,.12f,.04f),warm);
            if(b.name.StartsWith("Commerce_joueur_")||b.name.StartsWith("Magasin_sports_hiver_")){Box(near.transform,"300_Vitrine",new Vector3(Mathf.Min(w*.29f,4.3f),1.55f,front-.07f),new Vector3(Mathf.Clamp(w*.22f,2.2f,4.6f),2.15f,.10f),glass);}
            entries.Add(new Entry{anchor=b,near=near,mid=mid,far=far});Debug.Log("SkiStation 3.00: cohesion commerciale multi-echelle ajoutee a "+b.name+" (visuel uniquement).");}
        void Lod(){var cam=Camera.main;if(cam==null)return;foreach(var e in entries){if(e.anchor==null)continue;float d=Vector3.Distance(cam.transform.position,e.anchor.position);Set(e.far,d<1450f);Set(e.mid,d<760f);Set(e.near,d<330f);}}
        static void Set(GameObject g,bool on){if(g!=null&&g.activeSelf!=on)g.SetActive(on);}
        void OnDestroy(){if(instance==this)instance=null;if(root!=null)Destroy(root);foreach(var m in new[]{wood,stone,snow,glass,warm,dark,accent})if(m!=null)Destroy(m);}
    }
}
