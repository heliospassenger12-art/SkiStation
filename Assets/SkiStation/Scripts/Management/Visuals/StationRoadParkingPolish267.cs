using UnityEngine;

namespace SkiStation
{
    /// <summary>2.67 visual-only road/parking polish. Does not alter traffic, parking capacity or visitor routing.</summary>
    public sealed class StationRoadParkingPolish267 : MonoBehaviour
    {
        static StationRoadParkingPolish267 instance;
        GameObject root; float retry;
        Material asphalt, marking, stone, snow, metal, wood, warm;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive || instance != null) return;
            var go = new GameObject("SkiStation_267_Routes_Parkings_VISUEL");
            instance = go.AddComponent<StationRoadParkingPolish267>();
        }
        static Material Mat(Color c){var s=Shader.Find("Universal Render Pipeline/Lit");if(s==null)s=Shader.Find("Standard");if(s==null)s=Shader.Find("Sprites/Default");var m=new Material(s);m.color=c;return m;}
        static float Ground(float x,float z)=>SkiStationGuaranteedMountainVisual.Height(x,z);
        GameObject Box(string n,Vector3 p,Vector3 size,Material m,Transform parent=null)
        {var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(parent!=null?parent:root.transform,false);g.transform.position=p;g.transform.localScale=size;var c=g.GetComponent<Collider>();if(c!=null)Destroy(c);g.GetComponent<Renderer>().sharedMaterial=m;return g;}
        void Update()
        {
            if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive){Destroy(gameObject);return;}
            if(root!=null||Time.unscaledTime<retry)return; retry=Time.unscaledTime+1f;
            Vector3 a; var gw=StationGatewayRuntime.I;
            if(gw!=null&&gw.GatewayArrivalPoint!=null)a=gw.GatewayArrivalPoint.position;
            else {const float x=0,z=-2600;a=new Vector3(x,Ground(x,z),z);}
            Build(a);
        }
        void Build(Vector3 a)
        {
            root=new GameObject("Habillage_routes_parkings_267");root.transform.SetParent(transform,false);
            asphalt=Mat(new Color(.12f,.135f,.15f));marking=Mat(new Color(.94f,.93f,.82f));stone=Mat(new Color(.48f,.49f,.48f));snow=Mat(new Color(.95f,.98f,1f));metal=Mat(new Color(.13f,.16f,.19f));wood=Mat(new Color(.34f,.21f,.12f));warm=Mat(new Color(1f,.68f,.32f));
            BuildArrivalParking(a, -58f, 60f, 6, "Parking_visiteurs");
            BuildArrivalParking(a, 58f, 60f, 5, "Parking_autocars_et_services");
            BuildRoundabout(a);
            BuildPedestrianPromenade(a);
            Debug.Log("SkiStation 2.67: habillage routes, parkings et trottoirs installe; trafic et capacites inchanges.");
        }
        void BuildArrivalParking(Vector3 a,float dx,float dz,int bays,string label)
        {
            float cx=a.x+dx, cz=a.z+dz, y=Ground(cx,cz);
            var area=new GameObject(label);area.transform.SetParent(root.transform,false);
            Box("Asphalte",new Vector3(cx,y+.18f,cz),new Vector3(42f,.18f,31f),asphalt,area.transform);
            // Snow banks make the paved area feel cut into the winter landscape.
            Box("Bourrelet_neige",new Vector3(cx-22f,y+.55f,cz),new Vector3(2.2f,.75f,33f),snow,area.transform);
            Box("Bourrelet_neige",new Vector3(cx+22f,y+.55f,cz),new Vector3(2.2f,.75f,33f),snow,area.transform);
            Box("Bourrelet_neige",new Vector3(cx,y+.55f,cz+16.5f),new Vector3(46f,.75f,2f),snow,area.transform);
            float start=cx-(bays-1)*3.25f;
            for(int i=0;i<bays;i++)
            {
                float x=start+i*6.5f;
                Box("Marquage_place",new Vector3(x,y+.30f,cz),new Vector3(.14f,.035f,11f),marking,area.transform);
            }
            Box("Ligne_fond",new Vector3(cx,y+.30f,cz+5.45f),new Vector3((bays-1)*6.5f+.2f,.035f,.14f),marking,area.transform);
            for(int side=-1;side<=1;side+=2) ParkingLamp(cx+side*18f,cz-12f,area.transform);
        }
        void BuildRoundabout(Vector3 a)
        {
            float x=a.x,z=a.z+22f,y=Ground(x,z);
            // Decorative island only: kept above ground and without collider, so existing traffic logic remains authoritative.
            Box("Ilot_central_pierre",new Vector3(x,y+.28f,z),new Vector3(11f,.45f,11f),stone);
            Box("Ilot_central_neige",new Vector3(x,y+.58f,z),new Vector3(10.4f,.22f,10.4f),snow);
            Box("Totem_bois",new Vector3(x,y+2.25f,z),new Vector3(1.4f,3.5f,1.4f),wood);
            Box("Sommet_neige",new Vector3(x,y+4.08f,z),new Vector3(1.8f,.22f,1.8f),snow);
        }
        void BuildPedestrianPromenade(Vector3 a)
        {
            for(int side=-1;side<=1;side+=2)
            {
                float x=a.x+side*20f;
                for(int i=0;i<5;i++)
                {
                    float z=a.z+90f+i*18f,y=Ground(x,z);
                    Box("Bordure_pierre",new Vector3(x,y+.28f,z),new Vector3(2.8f,.42f,13f),stone);
                    Box("Trottoir_neige_tassee",new Vector3(x,y+.52f,z),new Vector3(2.45f,.10f,12.5f),snow);
                    if(i%2==0) ParkingLamp(x+side*3.2f,z,null);
                }
            }
        }
        void ParkingLamp(float x,float z,Transform parent)
        {
            float y=Ground(x,z);var p=parent!=null?parent:root.transform;
            Box("Lampadaire_parking",new Vector3(x,y+2.5f,z),new Vector3(.18f,5f,.18f),metal,p);
            Box("Lumiere",new Vector3(x,y+5.05f,z),new Vector3(.7f,.5f,.7f),warm,p);
            Box("Neige_lampe",new Vector3(x,y+5.38f,z),new Vector3(.86f,.12f,.86f),snow,p);
        }
        void OnDestroy(){if(instance==this)instance=null;if(root!=null)Destroy(root);foreach(var m in new[]{asphalt,marking,stone,snow,metal,wood,warm})if(m!=null)Destroy(m);}
    }
}
