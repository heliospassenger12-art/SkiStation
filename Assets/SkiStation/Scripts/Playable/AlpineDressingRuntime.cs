using UnityEngine;

namespace SkiStation
{
    /// <summary>2.16 visual dressing only: converts prototype forest/rock primitives into readable alpine silhouettes.</summary>
    public sealed class AlpineDressingRuntime : MonoBehaviour
    {
        private Material pine, trunk, rock;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (FindFirstObjectByType<AlpineDressingRuntime>() == null)
                new GameObject("SkiStation_2.16_AlpineDressing").AddComponent<AlpineDressingRuntime>();
        }

        private void Start() { Invoke(nameof(Build), .15f); }

        private void Build()
        {
            pine = Mat("2.16_Pine", new Color(.055f,.16f,.10f,1f));
            trunk = Mat("2.16_Trunk", new Color(.20f,.12f,.07f,1f));
            rock = AlpineRockMaterial.Shared;
            UpgradePrototypeForest();
            UpgradeRockBars();
            AddAlpineOutcrops();
            AlpineRockBarBuilder.Build();
            Debug.Log("SkiStation 2.16: ALPINE DRESSING READY — conifers + irregular rock outcrops (visual only).");
        }

        private void UpgradePrototypeForest()
        {
            var crowns = GameObject.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (var r in crowns)
            {
                if (r == null || r.gameObject.name != "PineCrown") continue;
                Transform old = r.transform;
                float s = Mathf.Max(.25f, old.localScale.x / 13f);
                r.enabled = false;
                CreateCone(old.parent, old.position + Vector3.up * 2f*s, 15f*s, 42f*s, pine, "FirCrown_Lower");
                CreateCone(old.parent, old.position + Vector3.up * 15f*s, 11f*s, 34f*s, pine, "FirCrown_Upper");
            }
        }

        private void UpgradeRockBars()
        {
            var renderers = GameObject.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (var r in renderers)
            {
                if (r == null || r.gameObject.name != "RockBar") continue;
                Transform t = r.transform;
                r.enabled = false;
                Vector3 sc = t.lossyScale;
                for (int i=0;i<5;i++)
                {
                    float f = i / 4f;
                    Vector3 p = t.position + t.right * Mathf.Lerp(-sc.x*.38f, sc.x*.38f, f) + Vector3.up * Mathf.Sin(f*Mathf.PI)*sc.y*.30f;
                    CreateRock(t.parent, p, new Vector3(sc.x*.28f, sc.y*(.75f + .2f*Mathf.Sin(i*2.1f)), sc.z*(.8f+.12f*i)), Quaternion.Euler(7f*i, 19f*i, -4f*i));
                }
            }
        }

        private void AddAlpineOutcrops()
        {
            var root = new GameObject("SkiStation_2.16_NATURAL_OUTCROPS").transform;
            Random.State old = Random.state; Random.InitState(21616);
            int made=0;
            for (int i=0;i<520 && made<115;i++)
            {
                float x=Random.Range(-1750f,1750f), z=Random.Range(1700f,2000f);
                float y=SkiStationGuaranteedMountainVisual.Height(x,z);
                if (y<480f || y>1550f) continue;
                float s=Random.Range(10f,32f);
                CreateRock(root,new Vector3(x,y+2f,z),new Vector3(s*Random.Range(1.3f,2.7f),s*Random.Range(.55f,1.25f),s*Random.Range(.8f,1.8f)),Quaternion.Euler(Random.Range(-12f,12f),Random.Range(0f,180f),Random.Range(-10f,10f)));
                made++;
            }
            Random.state=old;
        }

        private void CreateRock(Transform parent, Vector3 p, Vector3 scale, Quaternion rot)
        {
            var go=new GameObject("NaturalRock"); go.transform.SetParent(parent,false); go.transform.position=p; go.transform.rotation=rot; go.transform.localScale=scale;
            var mf=go.AddComponent<MeshFilter>();
            mf.sharedMesh=AlpineRockBarBuilder.BuildMesh(Mathf.RoundToInt(p.x),scale.x*.6f,scale.z*.6f,scale.y);
            go.transform.localScale=Vector3.one;
            var mr=go.AddComponent<MeshRenderer>(); mr.sharedMaterial=rock; mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
        }

        private static void CreateCone(Transform parent, Vector3 p, float radius, float height, Material mat, string name)
        {
            const int n=9; var v=new Vector3[n+2]; var tri=new int[n*6];
            v[0]=new Vector3(0,height*.5f,0); v[n+1]=new Vector3(0,-height*.5f,0);
            for(int i=0;i<n;i++){ float a=i*Mathf.PI*2f/n; v[i+1]=new Vector3(Mathf.Cos(a)*radius,-height*.5f,Mathf.Sin(a)*radius); }
            int k=0; for(int i=0;i<n;i++){int a=i+1,b=((i+1)%n)+1; tri[k++]=0;tri[k++]=a;tri[k++]=b; tri[k++]=n+1;tri[k++]=b;tri[k++]=a;}
            var mesh=new Mesh{name=name+"Mesh"}; mesh.vertices=v;mesh.triangles=tri;mesh.RecalculateNormals();mesh.RecalculateBounds();
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=p;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;
        }

        private static Mesh RockMesh()
        {
            var m=new Mesh{name="2.16_IrregularRock"};
            m.vertices=new[]{new Vector3(-.8f,-.45f,-.65f),new Vector3(.75f,-.5f,-.7f),new Vector3(.9f,-.35f,.55f),new Vector3(-.7f,-.5f,.75f),new Vector3(-.55f,.55f,-.4f),new Vector3(.45f,.75f,-.35f),new Vector3(.65f,.5f,.5f),new Vector3(-.4f,.65f,.45f)};
            m.triangles=new[]{0,1,5,0,5,4,1,2,6,1,6,5,2,3,7,2,7,6,3,0,4,3,4,7,4,5,6,4,6,7,3,2,1,3,1,0};
            m.RecalculateNormals();m.RecalculateBounds();return m;
        }

        private static Material Mat(string name, Color c)
        {
            Shader s=Shader.Find("Standard"); if(s==null)s=Shader.Find("Universal Render Pipeline/Lit"); if(s==null)s=Shader.Find("Unlit/Color");
            if(s==null)return null; var m=new Material(s){name=name,color=c}; return m;
        }
    }
}
