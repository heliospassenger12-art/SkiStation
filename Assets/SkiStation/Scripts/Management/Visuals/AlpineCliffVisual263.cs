using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SkiStation
{
    /// <summary>
    /// 2.63 visual-only pass for the existing rock buttresses. Keeps their gameplay
    /// colliders untouched, but replaces the visible mesh with a more fractured alpine
    /// silhouette and adds small snow shelves on upward-facing ledges.
    /// </summary>
    public sealed class AlpineCliffVisual263 : MonoBehaviour
    {
        static AlpineCliffVisual263 instance;
        readonly List<Mesh> generated = new List<Mesh>();
        Material rock, darkRock, snow;
        float retry;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive || instance != null) return;
            var g = new GameObject("SkiStation_263_Falaises_Alpines_VISUEL");
            instance = g.AddComponent<AlpineCliffVisual263>();
        }

        void Update()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) { Destroy(gameObject); return; }
            if (generated.Count > 0 || Time.unscaledTime < retry) return;
            retry = Time.unscaledTime + 1f;
            Apply();
        }

        void Apply()
        {
            var root = GameObject.Find("SkiStation_RockBars_2_47");
            if (root == null) return;
            rock = Mat("263_RocheAlpine", new Color(.31f,.30f,.285f), .08f);
            darkRock = Mat("263_Fissures", new Color(.16f,.17f,.17f), .035f);
            snow = Mat("263_NeigeCorniches", new Color(.91f,.95f,.975f), .24f);
            int count = 0;
            foreach (Transform t in root.transform)
            {
                if (t == null || !t.name.StartsWith("RockButtress_")) continue;
                var srcFilter = t.GetComponent<MeshFilter>();
                var srcRenderer = t.GetComponent<MeshRenderer>();
                if (srcFilter == null || srcFilter.sharedMesh == null || srcRenderer == null) continue;

                int seed = Mathf.Abs(t.name.GetHashCode());
                Mesh fractured = BuildFractured(srcFilter.sharedMesh, seed);
                generated.Add(fractured);
                srcRenderer.enabled = false; // collider remains on the original object

                var visual = new GameObject("Falais_Visuel_263");
                visual.transform.SetParent(t, false);
                visual.AddComponent<MeshFilter>().sharedMesh = fractured;
                var mr = visual.AddComponent<MeshRenderer>();
                mr.sharedMaterials = new[] { rock, darkRock, snow };
                mr.shadowCastingMode = ShadowCastingMode.On;
                mr.receiveShadows = true;
                count++;
            }
            if (count > 0) Debug.Log("SkiStation 2.63: " + count + " falaises visuelles fracturees + corniches neigeuses; colliders et gameplay inchanges.");
        }

        static Mesh BuildFractured(Mesh src, int seed)
        {
            var sv = src.vertices;
            var st = src.triangles;
            var verts = new List<Vector3>(st.Length * 2);
            var rockTris = new List<int>();
            var darkTris = new List<int>();
            var snowTris = new List<int>();
            var uv = new List<Vector2>();

            for (int i=0; i+2<st.Length; i+=3)
            {
                Vector3 a = Warp(sv[st[i]], seed), b = Warp(sv[st[i+1]], seed), c = Warp(sv[st[i+2]], seed);
                Vector3 n = Vector3.Cross(b-a,c-a).normalized;
                bool top = n.y > .58f;
                bool dark = !top && Hash01(seed + i*17) > .67f;
                AddTri(a,b,c, top ? snowTris : (dark ? darkTris : rockTris), verts, uv, n);

                // A thin offset snow cap makes ledges readable without touching collision.
                if (top && (a.y+b.y+c.y)/3f > 8f)
                {
                    Vector3 lift = n * .42f + Vector3.up * .18f;
                    AddTri(a+lift,b+lift,c+lift,snowTris,verts,uv,n);
                }
            }
            var m = new Mesh { name = "SkiStation_263_FalaiseFracturee" };
            if (verts.Count > 65000) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(verts); m.SetUVs(0,uv); m.subMeshCount=3;
            m.SetTriangles(rockTris,0,false); m.SetTriangles(darkTris,1,false); m.SetTriangles(snowTris,2,true);
            m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        static Vector3 Warp(Vector3 p, int seed)
        {
            float h1 = Hash01(seed + Mathf.RoundToInt(p.y*3.1f) + Mathf.RoundToInt(p.x*.7f));
            float h2 = Hash01(seed*3 + Mathf.RoundToInt(p.y*1.7f) + Mathf.RoundToInt(p.z*.9f));
            float vertical = Mathf.Clamp01(p.y / 180f);
            p.x *= .92f + h1*.18f; p.z *= .90f + h2*.20f;
            p.x += (h2-.5f) * (3.5f + 5f*vertical);
            p.z += (h1-.5f) * (3f + 4f*vertical);
            return p;
        }

        static float Hash01(int x)
        {
            unchecked { x ^= x << 13; x ^= x >> 17; x ^= x << 5; }
            return (x & 0x7fffffff) / 2147483647f;
        }

        static void AddTri(Vector3 a, Vector3 b, Vector3 c, List<int> tri, List<Vector3> v, List<Vector2> uv, Vector3 n)
        {
            int s=v.Count; v.Add(a);v.Add(b);v.Add(c);
            Vector3[] ps={a,b,c};
            foreach(var p in ps) uv.Add(Mathf.Abs(n.y)>.7f ? new Vector2(p.x,p.z)/28f : (Mathf.Abs(n.x)>Mathf.Abs(n.z)?new Vector2(p.z,p.y)/28f:new Vector2(p.x,p.y)/28f));
            tri.Add(s);tri.Add(s+1);tri.Add(s+2);
        }

        static Material Mat(string name, Color color, float smooth)
        {
            var s=Shader.Find("Universal Render Pipeline/Lit"); if(s==null)s=Shader.Find("Standard"); if(s==null)s=Shader.Find("Unlit/Color");
            if(s==null)return null; var m=new Material(s){name=name,color=color};
            if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth); if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",smooth); return m;
        }

        void OnDestroy()
        {
            if(instance==this)instance=null;
            foreach(var m in generated) if(m!=null)Destroy(m);
            if(rock!=null)Destroy(rock); if(darkRock!=null)Destroy(darkRock); if(snow!=null)Destroy(snow);
        }
    }
}
