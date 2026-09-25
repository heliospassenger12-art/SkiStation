using UnityEngine;

namespace SkiStation
{
    /// <summary>2.92 visual-only lived-in station dressing around the established arrival hub.</summary>
    public sealed class StationLivedInPolish292 : MonoBehaviour
    {
        static StationLivedInPolish292 instance; GameObject root; float retry;
        Material wood, metal, snow, warm, dark, red, blue, green;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive||instance!=null)return;var g=new GameObject("SkiStation_292_Station_Habitee_VISUEL");instance=g.AddComponent<StationLivedInPolish292>();}
        static Material Mat(Color c){var s=Shader.Find("Universal Render Pipeline/Lit");if(s==null)s=Shader.Find("Standard");if(s==null)s=Shader.Find("Sprites/Default");var m=new Material(s);m.color=c;return m;}
        static float H(float x,float z)=>SkiStationGuaranteedMountainVisual.Height(x,z);
        GameObject Box(string n,Vector3 p,Vector3 sc,Material m,Transform parent=null){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(parent!=null?parent:root.transform,false);g.transform.position=p;g.transform.localScale=sc;var c=g.GetComponent<Collider>();if(c!=null)Destroy(c);g.GetComponent<Renderer>().sharedMaterial=m;return g;}
        void Update(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive){Destroy(gameObject);return;}if(root!=null||Time.unscaledTime<retry)return;retry=Time.unscaledTime+1f;var hub=ArrivalHub250.I;if(hub==null||!hub.Ready)return;Build(hub);}
        void Build(ArrivalHub250 hub){
            root=new GameObject("Finition_station_habitee_292_VISUEL");root.transform.SetParent(transform,false);
            wood=Mat(new Color(.34f,.19f,.09f));metal=Mat(new Color(.13f,.15f,.17f));snow=Mat(new Color(.97f,.985f,1f));warm=Mat(new Color(1f,.62f,.24f));dark=Mat(new Color(.08f,.10f,.12f));red=Mat(new Color(.72f,.10f,.08f));blue=Mat(new Color(.10f,.30f,.58f));green=Mat(new Color(.12f,.38f,.19f));
            // Sparse, readable clusters: forecourt, terraces and pedestrian spine.
            CafeCluster(hub.Point(-29f,69f)); CafeCluster(hub.Point(29f,76f));
            for(int i=0;i<4;i++) Bollard(hub.Point(-12f+i*8f,34f));
            NoticeBoard(hub.Point(-18f,43f)); NoticeBoard(hub.Point(19f,45f));
            for(int i=0;i<3;i++) SnowPile(hub.Point(-38f+i*38f,56f+i*7f),2.4f+i*.35f);
            EquipmentCluster(hub.Point(-33f,51f)); EquipmentCluster(hub.Point(34f,53f));
            Debug.Log("SkiStation 2.92: station habitee - terrasses, panneaux, equipement, bornes et neige d'usage; visuel uniquement.");
        }
        void CafeCluster(Vector3 p){float y=H(p.x,p.z);var t=new GameObject("Coin_terrasse_vivant");t.transform.SetParent(root.transform,false);for(int i=-1;i<=1;i++){float x=p.x+i*3.1f;Box("Table_terrasse",new Vector3(x,y+.92f,p.z),new Vector3(1.5f,.14f,1.5f),wood,t.transform);Box("Pied_table",new Vector3(x,y+.48f,p.z),new Vector3(.14f,.85f,.14f),metal,t.transform);for(int s=-1;s<=1;s+=2)Box("Chaise",new Vector3(x+s*1.15f,y+.55f,p.z),new Vector3(.65f,.8f,.65f),dark,t.transform);}Box("Menu_exterieur",new Vector3(p.x+5.2f,y+1.25f,p.z),new Vector3(1.5f,2.1f,.18f),dark,t.transform);Box("Entete_menu",new Vector3(p.x+5.2f,y+2.15f,p.z-.12f),new Vector3(1.25f,.25f,.08f),warm,t.transform);}
        void Bollard(Vector3 p){float y=H(p.x,p.z);Box("Borne_pietonne",new Vector3(p.x,y+.65f,p.z),new Vector3(.38f,1.25f,.38f),metal);Box("Neige_borne",new Vector3(p.x,y+1.32f,p.z),new Vector3(.52f,.12f,.52f),snow);}
        void NoticeBoard(Vector3 p){float y=H(p.x,p.z);Box("Pied_panneau",new Vector3(p.x,y+1.35f,p.z),new Vector3(.18f,2.7f,.18f),wood);Box("Panneau_infos",new Vector3(p.x,y+2.65f,p.z),new Vector3(3.2f,2.0f,.22f),dark);Box("Bandeau_infos",new Vector3(p.x,y+3.35f,p.z-.14f),new Vector3(2.8f,.35f,.08f),red);Box("Neige_panneau",new Vector3(p.x,y+3.75f,p.z),new Vector3(3.45f,.14f,.5f),snow);}
        void EquipmentCluster(Vector3 p){float y=H(p.x,p.z);for(int i=0;i<6;i++){float x=p.x+(i-2.5f)*.62f;var m=i%3==0?red:(i%3==1?blue:green);var s=Box("Skis_stationnes",new Vector3(x,y+1.35f,p.z),new Vector3(.09f,2.7f,.14f),m);s.transform.rotation=Quaternion.Euler(0,0,(i-2)*2.2f);}Box("Rack_station",new Vector3(p.x,y+.55f,p.z),new Vector3(4.5f,.16f,.45f),metal);}
        void SnowPile(Vector3 p,float size){float y=H(p.x,p.z);var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name="Tas_neige_usage";g.transform.SetParent(root.transform,false);g.transform.position=new Vector3(p.x,y+.35f,p.z);g.transform.localScale=new Vector3(size,.65f,size*.7f);var c=g.GetComponent<Collider>();if(c!=null)Destroy(c);g.GetComponent<Renderer>().sharedMaterial=snow;}
        void OnDestroy(){if(instance==this)instance=null;if(root!=null)Destroy(root);foreach(var m in new[]{wood,metal,snow,warm,dark,red,blue,green})if(m!=null)Destroy(m);}
    }
}
