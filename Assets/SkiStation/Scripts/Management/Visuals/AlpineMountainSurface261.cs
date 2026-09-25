using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SkiStation
{
    /// <summary>
    /// 2.61 visual-only mountain surface pass. Adds readable fresh snow, compact snow,
    /// wind-scoured ridges and two rock tones according to altitude/slope/exposure.
    /// It never changes vertices, collider, pistes, lifts or gameplay heights.
    /// </summary>
    public sealed class AlpineMountainSurface261 : MonoBehaviour
    {
        static AlpineMountainSurface261 instance;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive || instance != null) return;
            var g = new GameObject("SkiStation_261_Surface_Montagne_VISUEL");
            instance = g.AddComponent<AlpineMountainSurface261>();
        }

        void Start() { Invoke(nameof(Apply), .85f); }

        void Apply()
        {
            var massif = GameObject.Find("SkiStation_MASSIF_01");
            if (massif == null) { Invoke(nameof(Apply), 1f); return; }
            var mf = massif.GetComponent<MeshFilter>(); var mr = massif.GetComponent<MeshRenderer>();
            if (mf == null || mr == null || mf.sharedMesh == null) return;
            var mesh = mf.sharedMesh; var v = mesh.vertices; var n = mesh.normals;
            // Recover all triangles even if an earlier visual pass already split the mesh.
            var all = new List<int>();
            for (int s=0;s<mesh.subMeshCount;s++) all.AddRange(mesh.GetTriangles(s));
            var fresh=new List<int>(all.Count); var packed=new List<int>(); var wind=new List<int>(); var rock=new List<int>(); var darkRock=new List<int>();
            for(int i=0;i+2<all.Count;i+=3)
            {
                int a=all[i],b=all[i+1],c=all[i+2];
                float y=(v[a].y+v[b].y+v[c].y)/3f;
                float z=(v[a].z+v[b].z+v[c].z)/3f;
                float ny=Mathf.Clamp01((n[a].y+n[b].y+n[c].y)/3f);
                float slope=1f-ny;
                float exposure=Mathf.PerlinNoise((v[a].x+4300f)*.0014f,(z+3400f)*.0017f);
                List<int> dst;
                if (slope>.22f && y>720f) dst = (slope>.36f || exposure>.63f) ? darkRock : rock;
                else if (y>1120f && (slope>.075f || exposure>.60f)) dst=wind;
                else if (slope>.095f || (y>760f && exposure>.70f)) dst=packed;
                else dst=fresh;
                dst.Add(a);dst.Add(b);dst.Add(c);
            }
            mesh.subMeshCount=5;
            mesh.SetTriangles(fresh,0,false); mesh.SetTriangles(packed,1,false); mesh.SetTriangles(wind,2,false); mesh.SetTriangles(rock,3,false); mesh.SetTriangles(darkRock,4,true);
            AlpineRockMaterial.ApplyUV(mesh);
            mr.sharedMaterials=new[]{
                Mat("261_NeigeFraiche",new Color(.94f,.97f,.985f),.34f),
                Mat("261_NeigeCompacte",new Color(.80f,.87f,.91f),.22f),
                Mat("261_NeigeVent",new Color(.66f,.75f,.81f),.12f),
                Mat("261_Roche",new Color(.38f,.36f,.33f),.09f),
                Mat("261_RocheOmbre",new Color(.22f,.23f,.23f),.06f)};
            mr.shadowCastingMode=ShadowCastingMode.On; mr.receiveShadows=true;
            Debug.Log("SkiStation 2.61: surface montagne enrichie neige/roche selon pente, altitude et exposition; geometrie et gameplay inchanges.");
        }

        static Material Mat(string name,Color c,float smooth)
        {
            var s=Shader.Find("Universal Render Pipeline/Lit"); if(s==null)s=Shader.Find("Standard"); if(s==null)s=Shader.Find("Unlit/Color");
            if(s==null)return null; var m=new Material(s){name=name,color=c};
            if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth); if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",smooth);
            return m;
        }
        void OnDestroy(){if(instance==this)instance=null;}
    }
}
