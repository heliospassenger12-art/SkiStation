using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>2.95 visual-only: blends confirmed player buildings into the snow without changing terrain or navigation.</summary>
    public sealed class BuildingSnowIntegration295 : MonoBehaviour
    {
        static BuildingSnowIntegration295 instance;
        readonly HashSet<int> done = new HashSet<int>();
        readonly List<Transform> near = new List<Transform>();
        GameObject root; Material snow, packed, stone;
        float nextScan, nextLod;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){ if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive || instance!=null) return; var g=new GameObject("SkiStation_295_Integration_Neige_Batiments_VISUEL"); instance=g.AddComponent<BuildingSnowIntegration295>(); }
        static Material Mat(Color c,float smooth=.18f){var s=Shader.Find("Universal Render Pipeline/Lit");if(s==null)s=Shader.Find("Standard");if(s==null)s=Shader.Find("Sprites/Default");var m=new Material(s){color=c};if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);return m;}
        GameObject Prim(PrimitiveType type,Transform p,string n,Vector3 pos,Vector3 scale,Material m){var g=GameObject.CreatePrimitive(type);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=scale;var c=g.GetComponent<Collider>();if(c!=null)Destroy(c);g.GetComponent<Renderer>().sharedMaterial=m;return g;}
        void Start(){root=new GameObject("Integration_neige_batiments_295_VISUEL");root.transform.SetParent(transform,false);snow=Mat(new Color(.96f,.985f,1f),.28f);packed=Mat(new Color(.82f,.89f,.93f),.24f);stone=Mat(new Color(.34f,.36f,.37f));}
        void Update(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive){Destroy(gameObject);return;}if(Time.unscaledTime>=nextScan){nextScan=Time.unscaledTime+2.5f;Scan();}if(Time.unscaledTime>=nextLod){nextLod=Time.unscaledTime+.5f;Lod();}}
        static bool IsPlayerBuilding(string n){return n.StartsWith("Chalet_joueur_")||n.StartsWith("Hotel_joueur_")||n.StartsWith("Commerce_joueur_")||n.StartsWith("Immeuble_appartements_joueur_")||n.StartsWith("Residence_tourisme_joueur_")||n.StartsWith("Magasin_sports_hiver_");}
        void Scan(){foreach(var s in FindObjectsByType<StationStructureStatus249>(FindObjectsSortMode.None)){if(s==null||!IsPlayerBuilding(s.gameObject.name)||!done.Add(s.gameObject.GetInstanceID()))continue;Decorate(s.transform);}}
        void Size(Transform b,out float w,out float d){var rs=b.GetComponentsInChildren<Renderer>();Bounds bd=new Bounds(b.position,new Vector3(14,8,12));bool got=false;foreach(var r in rs){if(r==null||r.transform.name.StartsWith("288_")||r.transform.name.StartsWith("294_")||r.transform.name.StartsWith("295_"))continue;if(!got){bd=r.bounds;got=true;}else bd.Encapsulate(r.bounds);}w=Mathf.Clamp(bd.size.x,10,32);d=Mathf.Clamp(bd.size.z,9,26);}
        void Decorate(Transform b){Size(b,out var w,out var d);var g=new GameObject("295_Neige_sol_"+b.name);g.transform.SetParent(b,false);near.Add(g.transform);
            // Low, irregular snow banks around corners; entrance axis stays open.
            AddBank(g.transform,new Vector3(-w*.46f,.12f,-d*.42f),new Vector3(w*.28f,.34f,d*.18f),-8f);
            AddBank(g.transform,new Vector3( w*.46f,.10f,-d*.35f),new Vector3(w*.24f,.28f,d*.16f),11f);
            AddBank(g.transform,new Vector3(-w*.43f,.11f, d*.43f),new Vector3(w*.26f,.31f,d*.18f),7f);
            AddBank(g.transform,new Vector3( w*.42f,.09f, d*.44f),new Vector3(w*.22f,.26f,d*.16f),-12f);
            // Packed snow approach: purely visual, deliberately narrow and shallow.
            Prim(PrimitiveType.Cube,g.transform,"295_Neige_tassee_entree",new Vector3(0,.035f,-d*.60f),new Vector3(Mathf.Min(4.4f,w*.30f),.055f,Mathf.Min(4.8f,d*.32f)),packed);
            // Small stone/snow edge accents, away from the central doorway.
            for(int side=-1;side<=1;side+=2){Prim(PrimitiveType.Cube,g.transform,"295_Bord_pierre",new Vector3(side*w*.34f,.18f,-d*.55f),new Vector3(1.2f,.30f,.55f),stone);Prim(PrimitiveType.Cube,g.transform,"295_Neige_bord",new Vector3(side*w*.34f,.36f,-d*.55f),new Vector3(1.35f,.10f,.68f),snow);}
            // A few close-range footprints leading toward the entrance.
            for(int i=0;i<5;i++){float z=-d*.76f+i*.62f;float x=(i%2==0?-.20f:.20f);var p=Prim(PrimitiveType.Cube,g.transform,"295_Trace_pas",new Vector3(x,.065f,z),new Vector3(.24f,.035f,.42f),packed);p.transform.localRotation=Quaternion.Euler(0,(i%2==0?-8f:8f),0);}
        }
        void AddBank(Transform p,Vector3 pos,Vector3 scale,float yaw){var g=Prim(PrimitiveType.Sphere,p,"295_Bourrelet_neige",pos,scale,snow);g.transform.localRotation=Quaternion.Euler(0,yaw,0);}
        void Lod(){var cam=Camera.main;if(cam==null)return;foreach(var t in near){if(t==null)continue;bool on=Vector3.Distance(cam.transform.position,t.position)<360f;if(t.gameObject.activeSelf!=on)t.gameObject.SetActive(on);}}
        void OnDestroy(){if(instance==this)instance=null;if(root!=null)Destroy(root);foreach(var m in new[]{snow,packed,stone})if(m!=null)Destroy(m);}
    }
}
