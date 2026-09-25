using UnityEngine;

namespace SkiStation
{
    /// <summary>2.82 visual-only premium front-de-neige dressing. No colliders/gameplay changes.</summary>
    public sealed class FrontDeNeigePremium282 : MonoBehaviour
    {
        static FrontDeNeigePremium282 instance; GameObject root; float retry;
        Material wood, dark, stone, snow, metal, glass, blue, red, warm;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive||instance!=null)return;var g=new GameObject("SkiStation_282_FrontDeNeige_PREMIUM");instance=g.AddComponent<FrontDeNeigePremium282>();}
        static Material Mat(Color c){var s=Shader.Find("Universal Render Pipeline/Lit");if(s==null)s=Shader.Find("Standard");if(s==null)s=Shader.Find("Sprites/Default");var m=new Material(s);m.color=c;return m;}
        static float H(float x,float z)=>SkiStationGuaranteedMountainVisual.Height(x,z);
        GameObject Box(string n,Vector3 p,Vector3 sc,Material m,Transform parent=null){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(parent!=null?parent:root.transform,false);g.transform.position=p;g.transform.localScale=sc;var c=g.GetComponent<Collider>();if(c!=null)Destroy(c);g.GetComponent<Renderer>().sharedMaterial=m;return g;}
        void Update(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive){Destroy(gameObject);return;}if(root!=null||Time.unscaledTime<retry)return;retry=Time.unscaledTime+1f;var hub=ArrivalHub250.I;if(hub==null||!hub.Ready)return;Build(hub);}
        void Build(ArrivalHub250 hub){
            root=new GameObject("Habillage_front_de_neige_282");root.transform.SetParent(transform,false);
            wood=Mat(new Color(.36f,.20f,.10f));dark=Mat(new Color(.10f,.13f,.16f));stone=Mat(new Color(.46f,.47f,.46f));snow=Mat(new Color(.97f,.99f,1f));metal=Mat(new Color(.22f,.25f,.27f));glass=Mat(new Color(.34f,.62f,.72f));blue=Mat(new Color(.08f,.32f,.62f));red=Mat(new Color(.72f,.10f,.08f));warm=Mat(new Color(1f,.62f,.22f));
            WelcomeGate(hub.Point(0f,92f)); InfoKiosk(hub.Point(-25f,86f)); PisteMap(hub.Point(24f,88f));
            QueueArea(hub.Point(0f,112f));
            for(int s=-1;s<=1;s+=2){SkiRack(hub.Point(s*31f,99f)); Bench(hub.Point(s*19f,101f));}
            for(int s=-1;s<=1;s+=2)for(int i=0;i<3;i++) Bollard(hub.Point(s*(11f+i*7f),116f));
            Debug.Log("SkiStation 2.82: front de neige premium, accueil, plan, files et mobilier visuel installes; gameplay inchange.");
        }
        void WelcomeGate(Vector3 p){float y=H(p.x,p.z);Box("Pilier_accueil_G",new Vector3(p.x-7,y+3,p.z),new Vector3(1.2f,6f,1.2f),stone);Box("Pilier_accueil_D",new Vector3(p.x+7,y+3,p.z),new Vector3(1.2f,6f,1.2f),stone);Box("Poutre_accueil",new Vector3(p.x,y+6.1f,p.z),new Vector3(15f,.7f,.9f),wood);Box("Enseigne_SkiStation",new Vector3(p.x,y+6.15f,p.z-.5f),new Vector3(8f,.38f,.12f),dark);Box("Neige_portique",new Vector3(p.x,y+6.58f,p.z),new Vector3(15.5f,.18f,1.2f),snow);}
        void InfoKiosk(Vector3 p){float y=H(p.x,p.z);Box("Kiosque_info",new Vector3(p.x,y+1.7f,p.z),new Vector3(5.4f,3.4f,3.8f),wood);Box("Vitre_info",new Vector3(p.x,y+2.0f,p.z-1.94f),new Vector3(3.8f,1.5f,.12f),glass);Box("Toit_kiosque",new Vector3(p.x,y+3.65f,p.z),new Vector3(6.2f,.35f,4.6f),dark);Box("Neige_kiosque",new Vector3(p.x,y+3.92f,p.z),new Vector3(6.45f,.20f,4.85f),snow);Box("Lumiere_info",new Vector3(p.x,y+2.9f,p.z-2.03f),new Vector3(1.6f,.18f,.12f),warm);}
        void PisteMap(Vector3 p){float y=H(p.x,p.z);Box("Pied_plan_G",new Vector3(p.x-3,y+1.8f,p.z),new Vector3(.3f,3.6f,.3f),metal);Box("Pied_plan_D",new Vector3(p.x+3,y+1.8f,p.z),new Vector3(.3f,3.6f,.3f),metal);Box("Plan_domaines",new Vector3(p.x,y+3.3f,p.z),new Vector3(7.2f,4.1f,.28f),blue);Box("Bandeau_plan",new Vector3(p.x,y+5.0f,p.z-.17f),new Vector3(6.4f,.45f,.08f),red);Box("Neige_plan",new Vector3(p.x,y+5.5f,p.z),new Vector3(7.6f,.18f,.65f),snow);}
        void QueueArea(Vector3 p){float y=H(p.x,p.z);for(int row=-1;row<=1;row++)for(int i=-3;i<=3;i++){float x=p.x+i*3.0f,z=p.z+row*4.5f;Box("Potelet_file",new Vector3(x,y+.65f,z),new Vector3(.16f,1.3f,.16f),metal);if(i<3)Box("Sangle_file",new Vector3(x+1.5f,y+1.05f,z),new Vector3(3f,.10f,.10f),row==0?red:blue);}}
        void SkiRack(Vector3 p){float y=H(p.x,p.z);Box("Rack_premium",new Vector3(p.x,y+.55f,p.z),new Vector3(5f,.18f,.5f),metal);for(int i=-3;i<=3;i++){var ski=Box("Ski_visuel",new Vector3(p.x+i*.62f,y+1.35f,p.z),new Vector3(.09f,2.7f,.13f),i%2==0?blue:red);ski.transform.rotation=Quaternion.Euler(0,0,i*2.5f);}}
        void Bench(Vector3 p){float y=H(p.x,p.z);Box("Banc_front",new Vector3(p.x,y+.72f,p.z),new Vector3(4f,.28f,.8f),wood);Box("Dossier_front",new Vector3(p.x,y+1.35f,p.z+.35f),new Vector3(4f,1f,.18f),wood);Box("Neige_banc_front",new Vector3(p.x,y+.92f,p.z),new Vector3(3.85f,.10f,.72f),snow);}
        void Bollard(Vector3 p){float y=H(p.x,p.z);Box("Borne_front",new Vector3(p.x,y+.55f,p.z),new Vector3(.45f,1.1f,.45f),stone);Box("Chapeau_borne",new Vector3(p.x,y+1.13f,p.z),new Vector3(.55f,.12f,.55f),snow);}
        void OnDestroy(){if(instance==this)instance=null;if(root!=null)Destroy(root);foreach(var m in new[]{wood,dark,stone,snow,metal,glass,blue,red,warm})if(m!=null)Destroy(m);}
    }
}
