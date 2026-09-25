using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>2.98 visual-only: inhabited alpine balconies and facade depth on confirmed player buildings.</summary>
    public sealed class BuildingBalconyLife298 : MonoBehaviour
    {
        static BuildingBalconyLife298 instance;
        readonly HashSet<int> done = new HashSet<int>();
        readonly List<Transform> detailGroups = new List<Transform>();
        GameObject root; Material wood, darkWood, snow, glass, fabric, warm; float nextScan, nextLod;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive||instance!=null)return;var g=new GameObject("SkiStation_298_Balcons_Alpins_VISUEL");instance=g.AddComponent<BuildingBalconyLife298>();}
        static bool Player(string n){return n.StartsWith("Chalet_joueur_")||n.StartsWith("Hotel_joueur_")||n.StartsWith("Immeuble_appartements_joueur_")||n.StartsWith("Residence_tourisme_joueur_");}
        static Material Mat(Color c,float smooth=.12f){var s=Shader.Find("Universal Render Pipeline/Lit");if(s==null)s=Shader.Find("Standard");if(s==null)s=Shader.Find("Sprites/Default");var m=new Material(s){color=c};if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);return m;}
        GameObject Box(Transform p,string n,Vector3 lp,Vector3 sc,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=lp;g.transform.localScale=sc;var c=g.GetComponent<Collider>();if(c!=null)Destroy(c);g.GetComponent<Renderer>().sharedMaterial=m;return g;}
        void Start(){root=new GameObject("Balcons_alpins_298_VISUEL");root.transform.SetParent(transform,false);wood=Mat(new Color(.34f,.20f,.105f));darkWood=Mat(new Color(.17f,.105f,.065f));snow=Mat(new Color(.975f,.99f,1f),.25f);glass=Mat(new Color(.18f,.30f,.34f),.42f);fabric=Mat(new Color(.48f,.075f,.055f));warm=Mat(new Color(1f,.69f,.32f),.30f);}
        void Update(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive){Destroy(gameObject);return;}if(Time.unscaledTime>=nextScan){nextScan=Time.unscaledTime+2.5f;Scan();}if(Time.unscaledTime>=nextLod){nextLod=Time.unscaledTime+.5f;Lod();}}
        void Scan(){foreach(var s in FindObjectsByType<StationStructureStatus249>(FindObjectsSortMode.None)){if(s==null||!Player(s.gameObject.name)||!done.Add(s.gameObject.GetInstanceID()))continue;Decorate(s.transform);}}
        void BoundsOf(Transform b,out float w,out float d,out float h){var rs=b.GetComponentsInChildren<Renderer>();Bounds bd=new Bounds(b.position,new Vector3(12,7,10));bool got=false;foreach(var r in rs){if(r==null||r.transform.name.StartsWith("288_")||r.transform.name.StartsWith("294_")||r.transform.name.StartsWith("295_")||r.transform.name.StartsWith("296_")||r.transform.name.StartsWith("297_")||r.transform.name.StartsWith("298_"))continue;if(!got){bd=r.bounds;got=true;}else bd.Encapsulate(r.bounds);}w=Mathf.Clamp(bd.size.x,9,32);d=Mathf.Clamp(bd.size.z,8,26);h=Mathf.Clamp(bd.size.y,5,18);}
        void Decorate(Transform b){BoundsOf(b,out var w,out var d,out var h);var g=new GameObject("298_Balcons_"+b.name);g.transform.SetParent(b,false);detailGroups.Add(g.transform);float front=-d*.52f;int floors=h>11f?2:1;int bays=w>19f?3:2;
            for(int f=0;f<floors;f++){float y=Mathf.Min(h*.38f+f*3.25f,h-2.0f);for(int i=0;i<bays;i++){float x=(-bays*.5f+.5f+i)*(w*.72f/bays);float bw=Mathf.Clamp(w*.20f,2.5f,4.6f);Box(g.transform,"298_Plancher_balcon",new Vector3(x,y-.55f,front-.72f),new Vector3(bw,.16f,1.42f),wood);Box(g.transform,"298_Neige_balcon",new Vector3(x,y-.44f,front-.72f),new Vector3(bw*.96f,.07f,1.32f),snow);Box(g.transform,"298_Garde_corps",new Vector3(x,y+.02f,front-1.38f),new Vector3(bw,.72f,.10f),darkWood);for(int r=-1;r<=1;r++)Box(g.transform,"298_Montant",new Vector3(x+r*bw*.38f,y+.02f,front-1.38f),new Vector3(.09f,.78f,.12f),darkWood);Box(g.transform,"298_Baie",new Vector3(x,y+.45f,front-.04f),new Vector3(Mathf.Min(1.65f,bw*.55f),1.65f,.10f),glass);Box(g.transform,"298_Lueur_interieure",new Vector3(x,y+.45f,front-.12f),new Vector3(Mathf.Min(1.25f,bw*.42f),1.30f,.05f),warm);
                    if((i+f)%2==0){Box(g.transform,"298_Chaise",new Vector3(x-bw*.20f,y-.08f,front-.88f),new Vector3(.42f,.48f,.42f),darkWood);Box(g.transform,"298_Plaid",new Vector3(x-bw*.20f,y+.20f,front-.89f),new Vector3(.48f,.10f,.48f),fabric);Box(g.transform,"298_Petite_table",new Vector3(x+bw*.18f,y-.15f,front-.90f),new Vector3(.52f,.10f,.52f),wood);}}
            }
            if(b.name.StartsWith("Chalet_joueur_")){Box(g.transform,"298_Poutre_faitiere",new Vector3(0,Mathf.Min(h*.82f,h-1f),front-.10f),new Vector3(Mathf.Min(w*.68f,8f),.22f,.22f),darkWood);}
            Debug.Log("SkiStation 2.98: balcons alpins habites ajoutes a "+b.name+" (visuel uniquement).");
        }
        void Lod(){var cam=Camera.main;if(cam==null)return;foreach(var t in detailGroups){if(t==null)continue;bool on=Vector3.Distance(cam.transform.position,t.position)<540f;if(t.gameObject.activeSelf!=on)t.gameObject.SetActive(on);}}
        void OnDestroy(){if(instance==this)instance=null;if(root!=null)Destroy(root);foreach(var m in new[]{wood,darkWood,snow,glass,fabric,warm})if(m!=null)Destroy(m);}
    }
}
