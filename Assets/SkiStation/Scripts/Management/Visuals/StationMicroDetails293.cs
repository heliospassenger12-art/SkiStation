using UnityEngine;

namespace SkiStation
{
    /// <summary>2.93 visual-only alpine micro-details around the established station core.</summary>
    public sealed class StationMicroDetails293 : MonoBehaviour
    {
        static StationMicroDetails293 instance; GameObject root; float retry;
        Material wood, dark, snow, fabric, warm;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive||instance!=null)return;var g=new GameObject("SkiStation_293_Micro_Details_VISUEL");instance=g.AddComponent<StationMicroDetails293>();}
        static Material Mat(Color c){var s=Shader.Find("Universal Render Pipeline/Lit");if(s==null)s=Shader.Find("Standard");if(s==null)s=Shader.Find("Sprites/Default");var m=new Material(s);m.color=c;return m;}
        static float H(float x,float z)=>SkiStationGuaranteedMountainVisual.Height(x,z);
        GameObject Box(string n,Vector3 p,Vector3 sc,Material m,Transform parent=null){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(parent!=null?parent:root.transform,false);g.transform.position=p;g.transform.localScale=sc;var c=g.GetComponent<Collider>();if(c!=null)Destroy(c);g.GetComponent<Renderer>().sharedMaterial=m;return g;}
        void Update(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive){Destroy(gameObject);return;}if(root!=null||Time.unscaledTime<retry)return;retry=Time.unscaledTime+1f;var hub=ArrivalHub250.I;if(hub==null||!hub.Ready)return;Build(hub);}
        void Build(ArrivalHub250 hub){
            root=new GameObject("Micro_details_station_293_VISUEL");root.transform.SetParent(transform,false);
            wood=Mat(new Color(.30f,.16f,.07f));dark=Mat(new Color(.11f,.12f,.13f));snow=Mat(new Color(.96f,.98f,1f));fabric=Mat(new Color(.48f,.08f,.06f));warm=Mat(new Color(1f,.58f,.22f));
            Firewood(hub.Point(-39f,77f)); Firewood(hub.Point(37f,84f));
            Parasol(hub.Point(-24f,72f)); Parasol(hub.Point(24f,80f));
            for(int i=0;i<9;i++) Footprint(hub.Point(-9f+i*2.2f,29f+i*2.7f),i);
            Crates(hub.Point(-46f,65f)); Crates(hub.Point(45f,69f));
            SnowBank(hub.Point(-49f,41f),5.2f); SnowBank(hub.Point(50f,44f),4.7f);
            Debug.Log("SkiStation 2.93: micro-details alpins, traces pietonnes et accessoires de vie; visuel uniquement.");
        }
        void Firewood(Vector3 p){float y=H(p.x,p.z);var t=new GameObject("Bois_chauffage");t.transform.SetParent(root.transform,false);for(int r=0;r<3;r++)for(int i=0;i<5;i++){var b=Box("Buche",new Vector3(p.x+(i-2)*.55f,y+.22f+r*.34f,p.z),new Vector3(.48f,.28f,1.05f),wood,t.transform);b.transform.rotation=Quaternion.Euler(0,(i%2==0?3f:-3f),0);}Box("Support_bois",new Vector3(p.x,y+.55f,p.z+.58f),new Vector3(3.4f,1.5f,.12f),dark,t.transform);}
        void Parasol(Vector3 p){float y=H(p.x,p.z);var t=new GameObject("Parasol_alpin");t.transform.SetParent(root.transform,false);Box("Mat",new Vector3(p.x,y+1.65f,p.z),new Vector3(.12f,3.2f,.12f),dark,t.transform);var top=Box("Toile",new Vector3(p.x,y+3.15f,p.z),new Vector3(3.6f,.16f,3.6f),fabric,t.transform);top.transform.rotation=Quaternion.Euler(0,45f,0);Box("Neige_parasol",new Vector3(p.x,y+3.28f,p.z),new Vector3(2.8f,.10f,2.8f),snow,t.transform);}
        void Footprint(Vector3 p,int i){float y=H(p.x,p.z);var f=Box("Trace_pas",new Vector3(p.x+(i%2==0?-.28f:.28f),y+.035f,p.z),new Vector3(.32f,.035f,.72f),dark);f.transform.rotation=Quaternion.Euler(0,(i%3-1)*7f,0);}
        void Crates(Vector3 p){float y=H(p.x,p.z);for(int i=0;i<3;i++){var b=Box("Caisse_service",new Vector3(p.x+i*.85f,y+.42f+(i==2?.45f:0),p.z),new Vector3(.72f,.72f,.72f),wood);b.transform.rotation=Quaternion.Euler(0,i*9f,0);}Box("Balise_chaude",new Vector3(p.x-1.0f,y+.7f,p.z),new Vector3(.22f,1.35f,.22f),warm);}
        void SnowBank(Vector3 p,float s){float y=H(p.x,p.z);var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name="Bourrelet_neige_station";g.transform.SetParent(root.transform,false);g.transform.position=new Vector3(p.x,y+.38f,p.z);g.transform.localScale=new Vector3(s,.75f,s*.48f);var c=g.GetComponent<Collider>();if(c!=null)Destroy(c);g.GetComponent<Renderer>().sharedMaterial=snow;}
        void OnDestroy(){if(instance==this)instance=null;if(root!=null)Destroy(root);foreach(var m in new[]{wood,dark,snow,fabric,warm})if(m!=null)Destroy(m);}
    }
}
