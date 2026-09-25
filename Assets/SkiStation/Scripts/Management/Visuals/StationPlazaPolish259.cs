using UnityEngine;

namespace SkiStation
{
    /// <summary>2.59 visual-only plaza polish. Keeps gameplay, roads, visitors and construction untouched.</summary>
    public sealed class StationPlazaPolish259 : MonoBehaviour
    {
        static StationPlazaPolish259 instance; GameObject root; float retry;
        Material paving, stone, wood, snow, evergreen, warm;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){ if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive||instance!=null)return; var g=new GameObject("SkiStation_259_Place_Centrale_VISUELLE"); instance=g.AddComponent<StationPlazaPolish259>(); }
        static Material Mat(Color c){var s=Shader.Find("Universal Render Pipeline/Lit");if(s==null)s=Shader.Find("Standard");if(s==null)s=Shader.Find("Sprites/Default");var m=new Material(s);m.color=c;return m;}
        static float H(float x,float z)=>SkiStationGuaranteedMountainVisual.Height(x,z);
        void Update(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive){Destroy(gameObject);return;} if(root!=null||Time.unscaledTime<retry)return; retry=Time.unscaledTime+1f; var p=StationGatewayRuntime.I!=null?StationGatewayRuntime.I.GatewayArrivalPoint:null; Vector3 a=p!=null?p.position:new Vector3(0,H(0,-2600),-2600); Build(a);}
        GameObject Box(string n,Vector3 p,Vector3 s,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(root.transform,false);g.transform.position=p;g.transform.localScale=s;g.GetComponent<Renderer>().sharedMaterial=m;var c=g.GetComponent<Collider>();if(c!=null)Destroy(c);return g;}
        void Build(Vector3 a){
            root=new GameObject("Place_centrale_259_VISUELLE");root.transform.SetParent(transform,false);
            paving=Mat(new Color(.55f,.56f,.56f));stone=Mat(new Color(.38f,.36f,.33f));wood=Mat(new Color(.27f,.16f,.09f));snow=Mat(new Color(.96f,.98f,1f));evergreen=Mat(new Color(.08f,.20f,.12f));warm=Mat(new Color(1f,.58f,.22f));
            // Irregular plaza terraces: deliberately not a perfectly flat slab.
            for(int r=0;r<5;r++){float z=a.z+38f+r*18f;float y=H(a.x,z)+.18f;float w=38f+r*5f;Box("Parvis_pierre",new Vector3(a.x,y,z),new Vector3(w,.18f,14f),paving);}
            // Low retaining edges help the built plaza blend into uneven snow.
            for(int side=-1;side<=1;side+=2)for(int r=0;r<4;r++){float x=a.x+side*(24f+r*2f),z=a.z+48f+r*22f,y=H(x,z);Box("Muret_pierre",new Vector3(x,y+.55f,z),new Vector3(8f,1.1f,1.2f),stone);Box("Chapeau_neige",new Vector3(x,y+1.14f,z),new Vector3(8.3f,.14f,1.45f),snow);}
            // Small planted islands and conifers break up the large white front-de-neige surface.
            for(int side=-1;side<=1;side+=2)for(int r=0;r<3;r++){float x=a.x+side*(43f+r*4f),z=a.z+56f+r*31f;Tree(x,z,4.5f+r*.6f);}
            // Warm bollards around the square, outside the central visitor corridor.
            for(int side=-1;side<=1;side+=2)for(int r=0;r<5;r++){float x=a.x+side*16f,z=a.z+42f+r*20f,y=H(x,z);Box("Borne",new Vector3(x,y+.55f,z),new Vector3(.45f,1.1f,.45f),wood);Box("Lumiere",new Vector3(x,y+1.15f,z),new Vector3(.55f,.25f,.55f),warm);}
            Debug.Log("SkiStation 2.59: place centrale, murets et vegetation visuelle installes; gameplay inchange.");
        }
        void Tree(float x,float z,float h){float y=H(x,z);Box("Tronc",new Vector3(x,y+h*.22f,z),new Vector3(.5f,h*.44f,.5f),wood);for(int i=0;i<3;i++){var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name="Sapin";g.transform.SetParent(root.transform,false);g.transform.position=new Vector3(x,y+h*(.35f+i*.17f),z);g.transform.localScale=new Vector3(h*(.20f-i*.035f),h*.18f,h*(.20f-i*.035f));g.GetComponent<Renderer>().sharedMaterial=evergreen;var c=g.GetComponent<Collider>();if(c!=null)Destroy(c);}Box("Neige_sapin",new Vector3(x,y+h*.82f,z),new Vector3(h*.16f,.12f,h*.16f),snow);}
        void OnDestroy(){if(instance==this)instance=null;if(root!=null)Destroy(root);foreach(var m in new[]{paving,stone,wood,snow,evergreen,warm})if(m!=null)Destroy(m);}
    }
}
