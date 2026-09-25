using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>2.99 visual-only: small alpine ground details around confirmed player buildings.</summary>
    public sealed class BuildingGroundLife299 : MonoBehaviour
    {
        static BuildingGroundLife299 instance;
        readonly HashSet<int> done = new HashSet<int>();
        readonly List<Transform> nearGroups = new List<Transform>();
        GameObject root; Material wood, stone, snow, metal, warm, green; float nextScan, nextLod;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive||instance!=null)return;var g=new GameObject("SkiStation_299_Abords_Alpins_VISUEL");instance=g.AddComponent<BuildingGroundLife299>();}
        static bool Player(string n){return n.StartsWith("Chalet_joueur_")||n.StartsWith("Hotel_joueur_")||n.StartsWith("Commerce_joueur_")||n.StartsWith("Immeuble_appartements_joueur_")||n.StartsWith("Residence_tourisme_joueur_")||n.StartsWith("Magasin_sports_hiver_");}
        static Material Mat(Color c,float smooth=.12f){var s=Shader.Find("Universal Render Pipeline/Lit");if(s==null)s=Shader.Find("Standard");if(s==null)s=Shader.Find("Sprites/Default");var m=new Material(s){color=c};if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);return m;}
        GameObject Box(Transform p,string n,Vector3 lp,Vector3 sc,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=lp;g.transform.localScale=sc;var c=g.GetComponent<Collider>();if(c!=null)Destroy(c);g.GetComponent<Renderer>().sharedMaterial=m;return g;}
        GameObject Sphere(Transform p,string n,Vector3 lp,Vector3 sc,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=lp;g.transform.localScale=sc;var c=g.GetComponent<Collider>();if(c!=null)Destroy(c);g.GetComponent<Renderer>().sharedMaterial=m;return g;}
        void Start(){root=new GameObject("Abords_alpins_coherents_299_VISUEL");root.transform.SetParent(transform,false);wood=Mat(new Color(.29f,.17f,.09f));stone=Mat(new Color(.30f,.31f,.30f));snow=Mat(new Color(.97f,.985f,1f),.23f);metal=Mat(new Color(.10f,.12f,.13f),.32f);warm=Mat(new Color(1f,.66f,.25f),.38f);green=Mat(new Color(.10f,.24f,.13f));}
        void Update(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive){Destroy(gameObject);return;}if(Time.unscaledTime>=nextScan){nextScan=Time.unscaledTime+2.5f;Scan();}if(Time.unscaledTime>=nextLod){nextLod=Time.unscaledTime+.5f;Lod();}}
        void Scan(){foreach(var s in FindObjectsByType<StationStructureStatus249>(FindObjectsSortMode.None)){if(s==null||!Player(s.gameObject.name)||!done.Add(s.gameObject.GetInstanceID()))continue;Decorate(s.transform);}}
        void BoundsOf(Transform b,out float w,out float d){var rs=b.GetComponentsInChildren<Renderer>();Bounds bd=new Bounds(b.position,new Vector3(12,7,10));bool got=false;foreach(var r in rs){if(r==null||r.transform.name.StartsWith("288_")||r.transform.name.StartsWith("294_")||r.transform.name.StartsWith("295_")||r.transform.name.StartsWith("296_")||r.transform.name.StartsWith("297_")||r.transform.name.StartsWith("298_")||r.transform.name.StartsWith("299_"))continue;if(!got){bd=r.bounds;got=true;}else bd.Encapsulate(r.bounds);}w=Mathf.Clamp(bd.size.x,9,32);d=Mathf.Clamp(bd.size.z,8,26);}
        void Lamp(Transform p,float x,float z){Box(p,"299_Lampadaire",new Vector3(x,1.35f,z),new Vector3(.11f,2.7f,.11f),metal);Box(p,"299_Lanterne",new Vector3(x,2.72f,z),new Vector3(.42f,.34f,.42f),warm);Box(p,"299_Chapeau_neige",new Vector3(x,2.93f,z),new Vector3(.50f,.08f,.50f),snow);}
        void Planter(Transform p,float x,float z){Box(p,"299_Bac_pierre",new Vector3(x,.24f,z),new Vector3(1.05f,.48f,.72f),stone);Sphere(p,"299_Arbuste",new Vector3(x,.72f,z),new Vector3(.70f,.72f,.70f),green);Sphere(p,"299_Neige_arbuste",new Vector3(x,.98f,z),new Vector3(.54f,.15f,.54f),snow);}
        void Decorate(Transform b){BoundsOf(b,out var w,out var d);var g=new GameObject("299_Abords_"+b.name);g.transform.SetParent(b,false);nearGroups.Add(g.transform);float front=-d*.58f;float side=w*.54f;
            // A readable, compact entrance apron; no gameplay collider/path changes.
            Box(g.transform,"299_Seuil_neige_tassee",new Vector3(0,.045f,front-1.65f),new Vector3(Mathf.Clamp(w*.28f,3.2f,6.8f),.08f,2.5f),snow);
            Lamp(g.transform,-Mathf.Min(side,5.2f),front-1.0f);Lamp(g.transform,Mathf.Min(side,5.2f),front-1.0f);
            Planter(g.transform,-Mathf.Min(side*.72f,4.2f),front-.25f);Planter(g.transform,Mathf.Min(side*.72f,4.2f),front-.25f);
            // Side snow/stone accents visually anchor the footprint without touching terrain.
            Box(g.transform,"299_Muret_gauche",new Vector3(-side-.30f,.30f,0),new Vector3(.48f,.58f,Mathf.Clamp(d*.45f,3.5f,8f)),stone);
            Box(g.transform,"299_Neige_muret_gauche",new Vector3(-side-.30f,.63f,0),new Vector3(.54f,.09f,Mathf.Clamp(d*.44f,3.4f,7.8f)),snow);
            Box(g.transform,"299_Muret_droit",new Vector3(side+.30f,.30f,0),new Vector3(.48f,.58f,Mathf.Clamp(d*.45f,3.5f,8f)),stone);
            Box(g.transform,"299_Neige_muret_droit",new Vector3(side+.30f,.63f,0),new Vector3(.54f,.09f,Mathf.Clamp(d*.44f,3.4f,7.8f)),snow);
            if(b.name.StartsWith("Hotel_joueur_")||b.name.StartsWith("Residence_tourisme_joueur_")){Box(g.transform,"299_Banc",new Vector3(side*.58f,.46f,front-1.8f),new Vector3(1.75f,.16f,.48f),wood);Box(g.transform,"299_Dossier_banc",new Vector3(side*.58f,.85f,front-1.57f),new Vector3(1.75f,.58f,.12f),wood);}
            Debug.Log("SkiStation 2.99: abords alpins coherents ajoutes a "+b.name+" (visuel uniquement).");}
        void Lod(){var cam=Camera.main;if(cam==null)return;foreach(var t in nearGroups){if(t==null)continue;bool on=Vector3.Distance(cam.transform.position,t.position)<610f;if(t.gameObject.activeSelf!=on)t.gameObject.SetActive(on);}}
        void OnDestroy(){if(instance==this)instance=null;if(root!=null)Destroy(root);foreach(var m in new[]{wood,stone,snow,metal,warm,green})if(m!=null)Destroy(m);}
    }
}
