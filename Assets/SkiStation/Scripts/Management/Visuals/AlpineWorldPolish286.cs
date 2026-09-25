using UnityEngine;
using UnityEngine.Rendering;

namespace SkiStation
{
    /// <summary>2.86 high-impact visual-only alpine world polish: ridgelines, cornices, rock strata and foreground snow banks.</summary>
    public sealed class AlpineWorldPolish286 : MonoBehaviour
    {
        static AlpineWorldPolish286 instance;
        GameObject root; float retry;
        Material snow, snowShade, rock, rockDark, pine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive || instance != null) return;
            var g = new GameObject("SkiStation_286_Alpine_World_Polish_VISUEL");
            instance = g.AddComponent<AlpineWorldPolish286>();
        }

        static float H(float x, float z) => SkiStationGuaranteedMountainVisual.Height(x, z);
        static Material Mat(Color c, float smooth)
        {
            var s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Sprites/Default");
            var m = new Material(s); m.color = c;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            return m;
        }

        void Update()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) { Destroy(gameObject); return; }
            if (root != null || Time.unscaledTime < retry) return;
            retry = Time.unscaledTime + 1f;
            if (GameObject.Find("SkiStation_MASSIF_01") == null) return;
            var p = StationGatewayRuntime.I != null ? StationGatewayRuntime.I.GatewayArrivalPoint : null;
            Build(p != null ? p.position : new Vector3(0f, H(0f,-2600f), -2600f));
        }

        GameObject P(string n, PrimitiveType t, Vector3 p, Vector3 sc, Material m, Quaternion q)
        {
            var g = GameObject.CreatePrimitive(t); g.name=n; g.transform.SetParent(root.transform,false);
            g.transform.position=p; g.transform.localScale=sc; g.transform.rotation=q;
            var c=g.GetComponent<Collider>(); if(c!=null) Destroy(c);
            var r=g.GetComponent<Renderer>(); if(r!=null){ r.sharedMaterial=m; r.receiveShadows=true; r.shadowCastingMode=ShadowCastingMode.On; }
            return g;
        }

        void Build(Vector3 a)
        {
            root=new GameObject("Finition_monde_alpin_286_VISUEL"); root.transform.SetParent(transform,false);
            snow=Mat(new Color(.91f,.95f,.98f,1f),.32f); snowShade=Mat(new Color(.68f,.76f,.83f,1f),.16f);
            rock=Mat(new Color(.29f,.28f,.27f,1f),.08f); rockDark=Mat(new Color(.18f,.19f,.19f,1f),.05f);
            pine=Mat(new Color(.035f,.095f,.065f,1f),.08f);

            // Layered ridges: large shapes create readable mountain scale from the management camera.
            for(int side=-1; side<=1; side+=2)
            for(int i=0;i<6;i++)
            {
                float z=a.z+760f+i*150f, x=a.x+side*(300f+i*52f), y=H(x,z);
                float h=42f+(i%3)*13f;
                P("Crete_rocheuse",PrimitiveType.Cube,new Vector3(x,y+h*.38f,z),new Vector3(74f+i*7f,h,45f+i*4f),i%2==0?rock:rockDark,Quaternion.Euler(10f+i*2f,side*(24f+i*8f),side*(15f+i)));
                P("Corniche_neige",PrimitiveType.Sphere,new Vector3(x-side*5f,y+h*.79f,z-5f),new Vector3(62f+i*5f,2.5f,31f+i*3f),snow,Quaternion.Euler(0f,side*12f,side*5f));
                // Snow tongue breaks the cube silhouette and visually joins the ridge to the skiable mountain.
                P("Langue_neige",PrimitiveType.Sphere,new Vector3(x-side*14f,y+h*.34f,z-18f),new Vector3(25f,1.2f,44f),snowShade,Quaternion.Euler(0f,side*18f,0f));
            }

            // Sparse near/mid-ground snow banks and exposed rocks add parallax without blocking buildable center areas.
            for(int i=0;i<18;i++)
            {
                int side=i%2==0?-1:1; float z=a.z+210f+i*55f;
                float x=a.x+side*(145f+(i%5)*31f); float y=H(x,z);
                P("Congere_naturelle",PrimitiveType.Sphere,new Vector3(x,y+.35f,z),new Vector3(13f+(i%3)*4f,.75f,7f+(i%4)*2f),i%4==0?snowShade:snow,Quaternion.Euler(0f,i*29f,0f));
                if(i%3==0) P("Rocher_affleurant",PrimitiveType.Cube,new Vector3(x+side*5f,y+1.1f,z+4f),new Vector3(5f+(i%2)*2f,2.8f,4f),rockDark,Quaternion.Euler(17f,i*37f,11f));
            }

            // A few tall silhouettes frame the domain and strengthen depth; kept far from the central arrival axis.
            for(int side=-1;side<=1;side+=2)
            for(int i=0;i<5;i++)
            {
                float z=a.z+330f+i*115f, x=a.x+side*(235f+(i%2)*28f), y=H(x,z), h=17f+i%3*3f;
                P("Sapin_cadrage",PrimitiveType.Cylinder,new Vector3(x,y+h*.42f,z),new Vector3(7f,h*.42f,7f),pine,Quaternion.identity);
                P("Sapin_cime",PrimitiveType.Cylinder,new Vector3(x,y+h*.72f,z),new Vector3(4.5f,h*.28f,4.5f),pine,Quaternion.identity);
            }
            Debug.Log("SkiStation 2.86: finition monde alpin maximale installee (cretes, corniches, strates rocheuses, congeres et cadrage); gameplay inchange.");
        }

        void OnDestroy()
        {
            if(instance==this) instance=null; if(root!=null) Destroy(root);
            foreach(var m in new[]{snow,snowShade,rock,rockDark,pine}) if(m!=null) Destroy(m);
        }
    }
}
