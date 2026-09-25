using UnityEngine;

namespace SkiStation
{
    /// <summary>2.68 visual-only village life polish. No colliders and no routing/gameplay changes.</summary>
    public sealed class StationLifePolish268 : MonoBehaviour
    {
        static StationLifePolish268 instance; GameObject root; float retry;
        Material wood, darkWood, stone, snow, metal, glass, red, warm, pine;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive||instance!=null)return;var g=new GameObject("SkiStation_268_Vie_Station_VISUEL");instance=g.AddComponent<StationLifePolish268>();}
        static Material Mat(Color c){var s=Shader.Find("Universal Render Pipeline/Lit");if(s==null)s=Shader.Find("Standard");if(s==null)s=Shader.Find("Sprites/Default");var m=new Material(s);m.color=c;return m;}
        static float H(float x,float z)=>SkiStationGuaranteedMountainVisual.Height(x,z);
        GameObject Box(string n,Vector3 p,Vector3 sc,Material m,Transform parent=null){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(parent!=null?parent:root.transform,false);g.transform.position=p;g.transform.localScale=sc;var c=g.GetComponent<Collider>();if(c!=null)Destroy(c);g.GetComponent<Renderer>().sharedMaterial=m;return g;}
        void Update(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive){Destroy(gameObject);return;}if(root!=null||Time.unscaledTime<retry)return;retry=Time.unscaledTime+1f;var hub=ArrivalHub250.I;if(hub==null||!hub.Ready)return;Build(hub);}
        void Build(ArrivalHub250 hub)
        {
            root=new GameObject("Habillage_vie_station_268");root.transform.SetParent(transform,false);
            wood=Mat(new Color(.38f,.22f,.11f));darkWood=Mat(new Color(.18f,.11f,.07f));stone=Mat(new Color(.43f,.45f,.45f));snow=Mat(new Color(.96f,.985f,1f));metal=Mat(new Color(.12f,.15f,.17f));glass=Mat(new Color(.38f,.63f,.72f));red=Mat(new Color(.72f,.12f,.09f));warm=Mat(new Color(1f,.66f,.27f));pine=Mat(new Color(.08f,.23f,.14f));
            // Use the established arrival coordinate system so decoration follows the actual station entrance.
            for(int i=0;i<5;i++){Bench(hub.Point(-14f+i*7f,48f),hub.transform.forward);}
            for(int i=0;i<4;i++){Planter(hub.Point(-18f+i*12f,58f));}
            Terrace(hub.Point(-25f,72f),"Terrasse_cafe"); Terrace(hub.Point(26f,78f),"Terrasse_apres_ski");
            Signpost(hub.Point(0f,38f)); SkiRack(hub.Point(-34f,46f)); SkiRack(hub.Point(35f,48f));
            for(int side=-1;side<=1;side+=2)for(int i=0;i<3;i++)Lamp(hub.Point(side*23f,44f+i*17f));
            Debug.Log("SkiStation 2.68: mobilier, terrasses, signaletique et zone pietonne visuelle installes; gameplay inchange.");
        }
        void Bench(Vector3 p,Vector3 f){float y=H(p.x,p.z);Box("Banc_assise",new Vector3(p.x,y+.75f,p.z),new Vector3(3.4f,.28f,.75f),wood);Box("Banc_dossier",new Vector3(p.x,y+1.35f,p.z+.32f),new Vector3(3.4f,1f,.18f),darkWood);for(int s=-1;s<=1;s+=2)Box("Pied_banc",new Vector3(p.x+s*1.2f,y+.35f,p.z),new Vector3(.18f,.7f,.5f),metal);Box("Neige_banc",new Vector3(p.x,y+.94f,p.z),new Vector3(3.25f,.10f,.68f),snow);}
        void Planter(Vector3 p){float y=H(p.x,p.z);Box("Jardiniere_pierre",new Vector3(p.x,y+.48f,p.z),new Vector3(2.2f,.8f,2.2f),stone);Box("Neige_jardiniere",new Vector3(p.x,y+.93f,p.z),new Vector3(2.35f,.16f,2.35f),snow);Box("Petit_sapin",new Vector3(p.x,y+2.0f,p.z),new Vector3(.75f,2.2f,.75f),pine);}
        void Terrace(Vector3 p,string n){float y=H(p.x,p.z);var t=new GameObject(n);t.transform.SetParent(root.transform,false);Box("Platelage",new Vector3(p.x,y+.24f,p.z),new Vector3(15f,.25f,10f),wood,t.transform);for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z+=2){float px=p.x+x*4.2f,pz=p.z+z*2.7f;Box("Table",new Vector3(px,y+1.25f,pz),new Vector3(2.1f,.18f,2.1f),darkWood,t.transform);Box("Pied_table",new Vector3(px,y+.72f,pz),new Vector3(.22f,1.0f,.22f),metal,t.transform);Box("Parasol",new Vector3(px,y+3.45f,pz),new Vector3(4.1f,.18f,4.1f),red,t.transform);Box("Mat_parasol",new Vector3(px,y+2.3f,pz),new Vector3(.12f,2.5f,.12f),metal,t.transform);}}
        void Signpost(Vector3 p){float y=H(p.x,p.z);Box("Poteau_signaletique",new Vector3(p.x,y+2.2f,p.z),new Vector3(.25f,4.2f,.25f),darkWood);Box("Panneau_domaines",new Vector3(p.x+1.5f,y+3.4f,p.z),new Vector3(3.1f,.65f,.18f),red);Box("Panneau_services",new Vector3(p.x-1.25f,y+2.55f,p.z),new Vector3(2.6f,.58f,.18f),wood);Box("Chapeau_neige",new Vector3(p.x,y+4.4f,p.z),new Vector3(.65f,.14f,.65f),snow);}
        void SkiRack(Vector3 p){float y=H(p.x,p.z);Box("Rack_skis",new Vector3(p.x,y+.65f,p.z),new Vector3(4.5f,.18f,.45f),metal);for(int i=-2;i<=2;i++){float x=p.x+i*.75f;var ski=Box("Ski_range",new Vector3(x,y+1.35f,p.z),new Vector3(.10f,2.7f,.16f),i%2==0?red:glass);ski.transform.rotation=Quaternion.Euler(0,0,i*3f);}}
        void Lamp(Vector3 p){float y=H(p.x,p.z);Box("Lampadaire",new Vector3(p.x,y+2.7f,p.z),new Vector3(.16f,5.3f,.16f),metal);Box("Lanterne_chaude",new Vector3(p.x,y+5.45f,p.z),new Vector3(.62f,.62f,.62f),warm);Box("Neige_lanterne",new Vector3(p.x,y+5.82f,p.z),new Vector3(.82f,.12f,.82f),snow);}
        void OnDestroy(){if(instance==this)instance=null;if(root!=null)Destroy(root);foreach(var m in new[]{wood,darkWood,stone,snow,metal,glass,red,warm,pine})if(m!=null)Destroy(m);}
    }
}
