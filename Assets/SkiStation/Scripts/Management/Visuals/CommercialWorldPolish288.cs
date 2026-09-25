using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>2.88 visual-only commercial polish: decorates confirmed player buildings and adapts micro-detail visibility to camera distance.</summary>
    public sealed class CommercialWorldPolish288 : MonoBehaviour
    {
        static CommercialWorldPolish288 instance;
        readonly HashSet<int> decorated = new HashSet<int>();
        readonly List<Transform> microGroups = new List<Transform>();
        GameObject root; Material wood,dark,stone,snow,glass,metal,warm,red,green;
        float nextScan, nextLod;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive||instance!=null)return;var g=new GameObject("SkiStation_288_Finition_Commerciale_VISUEL");instance=g.AddComponent<CommercialWorldPolish288>();}
        static Material Mat(Color c,float sm=.15f){var s=Shader.Find("Universal Render Pipeline/Lit");if(s==null)s=Shader.Find("Standard");if(s==null)s=Shader.Find("Sprites/Default");var m=new Material(s){color=c};if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",sm);return m;}
        GameObject Box(Transform p,string n,Vector3 lp,Vector3 sc,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=lp;g.transform.localScale=sc;var c=g.GetComponent<Collider>();if(c!=null)Destroy(c);g.GetComponent<Renderer>().sharedMaterial=m;return g;}
        void Start(){root=new GameObject("Monde_vivant_premium_288_VISUEL");root.transform.SetParent(transform,false);wood=Mat(new Color(.39f,.22f,.10f));dark=Mat(new Color(.13f,.09f,.065f));stone=Mat(new Color(.38f,.40f,.40f));snow=Mat(new Color(.96f,.985f,1f),.30f);glass=Mat(new Color(.28f,.52f,.61f),.5f);metal=Mat(new Color(.12f,.14f,.15f),.3f);warm=Mat(new Color(1f,.61f,.22f),.45f);red=Mat(new Color(.62f,.08f,.06f));green=Mat(new Color(.055f,.18f,.10f));}
        void Update(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive){Destroy(gameObject);return;}if(Time.unscaledTime>=nextScan){nextScan=Time.unscaledTime+2.5f;ScanBuildings();}if(Time.unscaledTime>=nextLod){nextLod=Time.unscaledTime+.5f;UpdateDetailVisibility();}}
        void ScanBuildings()
        {
            var all=FindObjectsByType<StationStructureStatus249>(FindObjectsSortMode.None);
            foreach(var status in all){if(status==null)continue;var b=status.gameObject;if(!IsPlayerBuilding(b.name)||!decorated.Add(b.GetInstanceID()))continue;Decorate(b.transform);}
        }
        static bool IsPlayerBuilding(string n){return n.StartsWith("Chalet_joueur_")||n.StartsWith("Hotel_joueur_")||n.StartsWith("Commerce_joueur_")||n.StartsWith("Immeuble_appartements_joueur_")||n.StartsWith("Residence_tourisme_joueur_")||n.StartsWith("Magasin_sports_hiver_");}
        void Decorate(Transform b)
        {
            var g=new GameObject("288_Details_premium_"+b.name);g.transform.SetParent(b,false);microGroups.Add(g.transform);
            var rends=b.GetComponentsInChildren<Renderer>();Bounds bd=new Bounds(Vector3.zero,new Vector3(14,8,12));bool got=false;foreach(var r in rends){if(r==null)continue;if(!got){bd=r.bounds;got=true;}else bd.Encapsulate(r.bounds);}float w=Mathf.Clamp(bd.size.x,10f,32f),d=Mathf.Clamp(bd.size.z,9f,26f),h=Mathf.Clamp(bd.size.y,7f,32f);
            // local-space facade dressing, deliberately decorative and collider-free.
            Box(g.transform,"Soubassement_pierre",new Vector3(0,.9f,-d*.48f),new Vector3(w*.88f,1.5f,.35f),stone);
            Box(g.transform,"Auvent_entree",new Vector3(0,2.8f,-d*.55f),new Vector3(4.8f,.28f,2.2f),wood);
            Box(g.transform,"Neige_auvent",new Vector3(0,3.02f,-d*.55f),new Vector3(5.05f,.13f,2.35f),snow);
            Box(g.transform,"Porte_vitree",new Vector3(0,1.75f,-d*.515f),new Vector3(1.8f,3.2f,.16f),glass);
            for(int side=-1;side<=1;side+=2){Box(g.transform,"Fenetre_chaude",new Vector3(side*w*.26f,3.0f,-d*.515f),new Vector3(Mathf.Min(3.6f,w*.18f),2.4f,.14f),warm);Box(g.transform,"Volet_bois",new Vector3(side*w*.38f,3.0f,-d*.525f),new Vector3(.55f,2.65f,.18f),dark);}
            if(h>11f){for(float y=6.2f;y<h-2f;y+=4.2f){Box(g.transform,"Balcon",new Vector3(0,y,-d*.55f),new Vector3(w*.72f,.18f,1.35f),wood);for(int i=-2;i<=2;i++)Box(g.transform,"Barreau_balcon",new Vector3(i*w*.075f,y+.65f,-d*.61f),new Vector3(.12f,1.25f,.12f),dark);}}
            // Entrance micro-life: readable only at closer management zooms.
            Box(g.transform,"Panneau_entree",new Vector3(w*.32f,2.1f,-d*.62f),new Vector3(2.1f,1.25f,.16f),red);
            Box(g.transform,"Rack",new Vector3(-w*.34f,.75f,-d*.62f),new Vector3(3.0f,.18f,.4f),metal);
            for(int i=-1;i<=1;i++)Box(g.transform,"Ski",new Vector3(-w*.34f+i*.55f,1.55f,-d*.62f),new Vector3(.09f,2.7f,.12f),i==0?red:glass);
            for(int side=-1;side<=1;side+=2){Box(g.transform,"Bac_neige",new Vector3(side*w*.42f,.55f,-d*.58f),new Vector3(1.7f,.75f,1.5f),stone);Box(g.transform,"Arbuste_alpin",new Vector3(side*w*.42f,1.55f,-d*.58f),new Vector3(.7f,1.7f,.7f),green);}
            Debug.Log("SkiStation 2.88: finition premium ajoutee a "+b.name+" (visuel uniquement).");
        }
        void UpdateDetailVisibility(){var cam=Camera.main;if(cam==null)return;foreach(var t in microGroups){if(t==null)continue;float dist=Vector3.Distance(cam.transform.position,t.position);bool on=dist<520f;if(t.gameObject.activeSelf!=on)t.gameObject.SetActive(on);}}
        void OnDestroy(){if(instance==this)instance=null;if(root!=null)Destroy(root);foreach(var m in new[]{wood,dark,stone,snow,glass,metal,warm,red,green})if(m!=null)Destroy(m);}
    }
}
