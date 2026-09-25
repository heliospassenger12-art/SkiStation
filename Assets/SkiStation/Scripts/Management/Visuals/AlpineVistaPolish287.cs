using UnityEngine;
using UnityEngine.Rendering;

namespace SkiStation
{
    /// <summary>2.87 visual-only cinematic alpine vista: distant peaks, snow caps, valley haze and foreground framing.</summary>
    public sealed class AlpineVistaPolish287 : MonoBehaviour
    {
        static AlpineVistaPolish287 instance;
        GameObject root; float retry;
        Material distantRock, distantSnow, haze, pineDark;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive || instance != null) return;
            var g = new GameObject("SkiStation_287_Alpine_Vista_Premium_VISUEL");
            instance = g.AddComponent<AlpineVistaPolish287>();
        }

        static float H(float x,float z)=>SkiStationGuaranteedMountainVisual.Height(x,z);
        static Material Mat(Color c,float smooth)
        {
            var s=Shader.Find("Universal Render Pipeline/Lit"); if(s==null)s=Shader.Find("Standard"); if(s==null)s=Shader.Find("Sprites/Default");
            var m=new Material(s); m.color=c;
            if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);
            if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",0f);
            return m;
        }

        void Update()
        {
            if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive){Destroy(gameObject);return;}
            if(root!=null || Time.unscaledTime<retry)return; retry=Time.unscaledTime+1f;
            if(GameObject.Find("SkiStation_MASSIF_01")==null)return;
            var p=StationGatewayRuntime.I!=null?StationGatewayRuntime.I.GatewayArrivalPoint:null;
            Build(p!=null?p.position:new Vector3(0f,H(0f,-2600f),-2600f));
        }

        GameObject P(string n,PrimitiveType t,Vector3 p,Vector3 sc,Material m,Quaternion q,bool shadows=true)
        {
            var g=GameObject.CreatePrimitive(t); g.name=n; g.transform.SetParent(root.transform,false); g.transform.position=p; g.transform.localScale=sc; g.transform.rotation=q;
            var c=g.GetComponent<Collider>(); if(c!=null)Destroy(c);
            var r=g.GetComponent<Renderer>(); if(r!=null){r.sharedMaterial=m;r.receiveShadows=true;r.shadowCastingMode=shadows?ShadowCastingMode.On:ShadowCastingMode.Off;}
            return g;
        }

        void Build(Vector3 a)
        {
            root=new GameObject("Panorama_alpin_cinematique_287_VISUEL"); root.transform.SetParent(transform,false);
            distantRock=Mat(new Color(.34f,.37f,.39f,1f),.06f);
            distantSnow=Mat(new Color(.88f,.93f,.97f,1f),.26f);
            haze=Mat(new Color(.72f,.80f,.86f,1f),.12f);
            pineDark=Mat(new Color(.025f,.070f,.050f,1f),.06f);

            // Distant asymmetric summit silhouettes: broad enough to read from the management overview,
            // deliberately kept beyond the central construction/play area.
            for(int side=-1;side<=1;side+=2)
            for(int i=0;i<4;i++)
            {
                float z=a.z+1450f+i*205f;
                float x=a.x+side*(500f+i*115f);
                float y=H(x,z);
                float w=150f+i*28f, h=105f+i*24f;
                P("Sommet_lointain",PrimitiveType.Cube,new Vector3(x,y+h*.34f,z),new Vector3(w,h,w*.52f),distantRock,Quaternion.Euler(13f,side*(34f+i*9f),side*(18f+i*3f)));
                P("Calotte_neigeuse",PrimitiveType.Sphere,new Vector3(x-side*10f,y+h*.78f,z-12f),new Vector3(w*.72f,8f,w*.34f),distantSnow,Quaternion.Euler(0f,side*17f,side*7f),false);
            }

            // Atmospheric valley layers increase scale without global post-processing or gameplay effects.
            for(int i=0;i<5;i++)
            {
                float z=a.z+850f+i*230f, x=a.x+(i%2==0?-1f:1f)*(330f+i*48f), y=H(x,z)+5f;
                P("Voile_vallee",PrimitiveType.Sphere,new Vector3(x,y,z),new Vector3(150f+i*18f,3.5f,62f+i*12f),haze,Quaternion.Euler(0f,i*23f,0f),false);
            }

            // Dark foreground tree groups frame wide screenshots and reinforce depth/parallax.
            for(int side=-1;side<=1;side+=2)
            for(int i=0;i<6;i++)
            {
                float z=a.z+95f+i*52f, x=a.x+side*(185f+(i%3)*22f), y=H(x,z), h=13f+(i%3)*3f;
                P("Sapin_premier_plan",PrimitiveType.Cylinder,new Vector3(x,y+h*.45f,z),new Vector3(5.8f,h*.45f,5.8f),pineDark,Quaternion.identity);
                P("Cime_premier_plan",PrimitiveType.Cylinder,new Vector3(x,y+h*.76f,z),new Vector3(3.4f,h*.28f,3.4f),pineDark,Quaternion.identity);
            }

            Debug.Log("SkiStation 2.87: panorama alpin cinematique installe (sommets lointains, calottes, profondeur de vallee et cadrage); gameplay inchange.");
        }

        void OnDestroy()
        {
            if(instance==this)instance=null; if(root!=null)Destroy(root);
            foreach(var m in new[]{distantRock,distantSnow,haze,pineDark})if(m!=null)Destroy(m);
        }
    }
}
