using UnityEngine;

namespace SkiStation
{
    /// <summary>2.74 visual-only snow accumulation around station objects. No gameplay colliders or rules.</summary>
    public sealed class SnowAccumulationPolish274 : MonoBehaviour
    {
        static SnowAccumulationPolish274 instance;
        GameObject root; Material snow, packed, shade; float retry;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){ if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive||instance!=null)return; var g=new GameObject("SkiStation_274_Accumulation_Neige_VISUEL"); instance=g.AddComponent<SnowAccumulationPolish274>(); }
        static Material Mat(Color c,float sm){var s=Shader.Find("Universal Render Pipeline/Lit");if(s==null)s=Shader.Find("Standard");if(s==null)s=Shader.Find("Sprites/Default");var m=new Material(s);m.color=c;if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",sm);return m;}
        static float H(float x,float z)=>SkiStationGuaranteedMountainVisual.Height(x,z);
        void Update(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive){Destroy(gameObject);return;}if(root!=null||Time.unscaledTime<retry)return;retry=Time.unscaledTime+1f;var gw=StationGatewayRuntime.I;Vector3 a=gw!=null&&gw.GatewayArrivalPoint!=null?gw.GatewayArrivalPoint.position:new Vector3(0,H(0,-2600),-2600);Build(a);}
        GameObject P(string n,PrimitiveType t,Vector3 p,Vector3 sc,Material m,Quaternion? q=null){var g=GameObject.CreatePrimitive(t);g.name=n;g.transform.SetParent(root.transform,false);g.transform.position=p;g.transform.localScale=sc;if(q.HasValue)g.transform.rotation=q.Value;var c=g.GetComponent<Collider>();if(c!=null)Destroy(c);var r=g.GetComponent<Renderer>();if(r!=null)r.sharedMaterial=m;return g;}
        void Bank(float x,float z,float sx,float sz,float rot=0){float y=H(x,z);P("Congere_naturelle",PrimitiveType.Sphere,new Vector3(x,y+.32f,z),new Vector3(sx,.62f,sz),snow,Quaternion.Euler(0,rot,0));P("Ombre_neige",PrimitiveType.Sphere,new Vector3(x,y+.10f,z),new Vector3(sx*1.02f,.18f,sz*1.02f),shade,Quaternion.Euler(0,rot,0));}
        void PoleFoot(float x,float z){float y=H(x,z);P("Neige_pied_structure",PrimitiveType.Sphere,new Vector3(x,y+.24f,z),new Vector3(2.4f,.48f,2.4f),snow);}
        void Build(Vector3 a){root=new GameObject("Accumulation_neige_274_VISUELLE");root.transform.SetParent(transform,false);snow=Mat(new Color(.95f,.975f,1f),.38f);packed=Mat(new Color(.82f,.89f,.94f),.26f);shade=Mat(new Color(.68f,.77f,.84f),.18f);
            // Snowbanks along the arrival/plaza edges: irregular and low so they never block circulation.
            for(int side=-1;side<=1;side+=2)for(int i=0;i<9;i++){float z=a.z+18+i*24;float x=a.x+side*(45f+4f*Mathf.Sin(i*1.6f));Bank(x,z,9f+(i%3)*2f,3.8f+(i%2),side*(8+i*5));}
            // Ploughed/packed snow shoulders next to the central approach.
            for(int side=-1;side<=1;side+=2)for(int i=0;i<7;i++){float z=a.z-8+i*27;float x=a.x+side*18.5f;float y=H(x,z);P("Bord_neige_tassee",PrimitiveType.Sphere,new Vector3(x,y+.12f,z),new Vector3(4.2f,.24f,10f),packed);}
            // Small accumulation islands around likely lamp/sign/pole positions, purely decorative.
            for(int side=-1;side<=1;side+=2)for(int i=0;i<6;i++)PoleFoot(a.x+side*27f,a.z+32+i*24);
            // Wind-shaped drifts where village transitions to the mountain.
            for(int side=-1;side<=1;side+=2)for(int i=0;i<6;i++){float z=a.z+230+i*38;float x=a.x+side*(70+i*7);Bank(x,z,13f+i%2*4f,5f+i%3,side*(18+i*7));}
            Debug.Log("SkiStation 2.74: accumulation visuelle de neige, congeres et raccords doux installes; gameplay inchange.");}
        void OnDestroy(){if(instance==this)instance=null;if(root!=null)Destroy(root);foreach(var m in new[]{snow,packed,shade})if(m!=null)Destroy(m);}
    }
}
