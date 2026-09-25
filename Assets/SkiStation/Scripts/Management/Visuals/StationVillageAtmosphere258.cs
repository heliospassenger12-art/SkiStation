using UnityEngine;

namespace SkiStation
{
    /// <summary>2.58 visual-only polish for the front de neige. No AI, economy or construction rules.</summary>
    public sealed class StationVillageAtmosphere258 : MonoBehaviour
    {
        static StationVillageAtmosphere258 instance;
        GameObject decor; float nextBuild;
        Material wood, stone, metal, warm, blue, snow;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive || instance != null) return;
            var go = new GameObject("SkiStation_258_Ambiance_FrontDeNeige");
            instance = go.AddComponent<StationVillageAtmosphere258>();
        }
        static Material Mat(Color c) { var s=Shader.Find("Universal Render Pipeline/Lit"); if(s==null)s=Shader.Find("Standard"); if(s==null)s=Shader.Find("Sprites/Default"); var m=new Material(s); m.color=c; return m; }
        static float Ground(float x,float z)=>SkiStationGuaranteedMountainVisual.Height(x,z);
        GameObject Box(string n,Vector3 p,Vector3 s,Material m)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=n; g.transform.SetParent(decor.transform,false); g.transform.position=p; g.transform.localScale=s;
            var c=g.GetComponent<Collider>(); if(c!=null)Destroy(c); g.GetComponent<Renderer>().sharedMaterial=m; return g;
        }
        void Update()
        {
            if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive){Destroy(gameObject);return;}
            if(decor!=null || Time.unscaledTime<nextBuild)return; nextBuild=Time.unscaledTime+1f;
            Vector3 a; var gw=StationGatewayRuntime.I;
            if(gw!=null && gw.GatewayArrivalPoint!=null)a=gw.GatewayArrivalPoint.position;
            else { const float x=0,z=-2600; a=new Vector3(x,Ground(x,z),z); }
            Build(a);
        }
        void Build(Vector3 a)
        {
            decor=new GameObject("Ambiance_station_258_VISUELLE"); decor.transform.SetParent(transform,false);
            wood=Mat(new Color(.30f,.18f,.10f)); stone=Mat(new Color(.46f,.44f,.41f)); metal=Mat(new Color(.12f,.15f,.18f));
            warm=Mat(new Color(1f,.66f,.28f)); blue=Mat(new Color(.06f,.20f,.32f)); snow=Mat(new Color(.94f,.97f,1f));
            // Lamp posts frame the pedestrian approach without blocking the central corridor.
            for(int side=-1;side<=1;side+=2) for(int row=0;row<6;row++) Lamp(a.x+side*27f,a.z+32f+row*24f);
            // Stone/wood welcome walls at the entrance, outside the road envelope.
            WelcomeWall(a.x-24f,a.z+7f,-1); WelcomeWall(a.x+24f,a.z+7f,1);
            // Benches, planters and resort banners around the front-de-neige plaza.
            for(int side=-1;side<=1;side+=2) for(int row=0;row<4;row++)
            {
                float x=a.x+side*34f, z=a.z+48f+row*29f, y=Ground(x,z);
                Box("Banc",new Vector3(x,y+.55f,z),new Vector3(5f,.32f,1.2f),wood);
                Box("Dossier",new Vector3(x,y+1.2f,z+.48f),new Vector3(5f,1.1f,.18f),wood);
                Box("Bac_pierre",new Vector3(x+side*4f,y+.45f,z),new Vector3(2.1f,.9f,2.1f),stone);
                Box("Neige_bac",new Vector3(x+side*4f,y+.95f,z),new Vector3(2.2f,.12f,2.2f),snow);
            }
            for(int side=-1;side<=1;side+=2) for(int row=0;row<5;row++) Banner(a.x+side*19f,a.z+25f+row*27f,side);
            Debug.Log("SkiStation 2.58: ambiance visuelle du front de neige installee; gameplay inchange.");
        }
        void Lamp(float x,float z)
        {
            float y=Ground(x,z); Box("Lampadaire",new Vector3(x,y+2.4f,z),new Vector3(.22f,4.8f,.22f),metal);
            Box("Lanterne",new Vector3(x,y+4.9f,z),new Vector3(.75f,.75f,.75f),warm);
            Box("Chapeau_lanterne",new Vector3(x,y+5.35f,z),new Vector3(.95f,.16f,.95f),snow);
        }
        void WelcomeWall(float x,float z,int side)
        {
            float y=Ground(x,z); Box("Mur_bienvenue",new Vector3(x,y+1.55f,z),new Vector3(12f,3.1f,1.4f),stone);
            Box("Panneau_SKISTATION",new Vector3(x,y+2.05f,z-side*.76f),new Vector3(8f,1.45f,.12f),wood);
            Box("Logo_montagne",new Vector3(x,y+2.12f,z-side*.84f),new Vector3(3.2f,.22f,.10f),snow);
        }
        void Banner(float x,float z,int side)
        {
            float y=Ground(x,z); Box("Mat_banniere",new Vector3(x,y+2.7f,z),new Vector3(.15f,5.4f,.15f),metal);
            Box("Banniere_SKISTATION",new Vector3(x+side*.62f,y+3.25f,z),new Vector3(1.15f,2.65f,.08f),blue);
        }
        void OnDestroy(){if(instance==this)instance=null; if(decor!=null)Destroy(decor); foreach(var m in new[]{wood,stone,metal,warm,blue,snow})if(m!=null)Destroy(m);}
    }
}
