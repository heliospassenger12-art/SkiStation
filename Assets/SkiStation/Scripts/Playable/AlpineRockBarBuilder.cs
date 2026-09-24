using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public static class AlpineRockBarBuilder
    {
        public static void Build()
        {
            if(GameObject.Find("SkiStation_RockBars_2_47") != null) return;
            var root=new GameObject("SkiStation_RockBars_2_47");
            // Finite, deterministic buttresses; the base is sampled from the rendered terrain.
            for(int i=0;i<72;i++)
            {
                float x=-3750f+i*(7500f/71f);
                float z=1770f+35f*Mathf.Sin(i*1.7f);
                float low=float.MaxValue,high=float.MinValue;
                for(int sample=0;sample<16;sample++)
                {
                    float angle=sample*Mathf.PI*2f/16f;
                    float ground=SkiStationGuaranteedMountainVisual.Height(x+95f*Mathf.Cos(angle),z+65f*Mathf.Sin(angle));
                    low=Mathf.Min(low,ground);high=Mathf.Max(high,ground);
                }
                var go=new GameObject("RockButtress_"+i);
                go.transform.SetParent(root.transform,false);
                go.transform.position=new Vector3(x,low-12f,z);
                Mesh mesh=BuildMesh(i,62f+12f*Mathf.Sin(i*2.1f),38f+10f*Mathf.Cos(i),high-low+45f+22f*(.5f+.5f*Mathf.Sin(i*1.3f)));
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial=AlpineRockMaterial.Shared;
                go.AddComponent<MeshCollider>().sharedMesh=mesh;
            }
        }

        public static Mesh BuildMesh(int seed,float rx,float rz,float height)
        {
            const int sides=10,levels=5;
            var rings=new Vector3[levels*sides];
            for(int j=0;j<levels;j++) for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2f/sides;
                float radius=1f+.13f*Mathf.Sin(i*2.4f+seed)+.09f*Mathf.Cos(j*2.2f+i+seed);
                float y=j*height/(levels-1);
                if(j==levels-1)y+=5f*Mathf.Sin(i*1.8f+seed);
                rings[j*sides+i]=new Vector3(Mathf.Cos(a)*rx*radius,y,Mathf.Sin(a)*rz*radius);
            }
            var v=new List<Vector3>();var tris=new List<int>();var uv=new List<Vector2>();
            for(int j=0;j<levels-1;j++) for(int i=0;i<sides;i++)
            {
                int next=(i+1)%sides;
                AddFace(rings[j*sides+i],rings[(j+1)*sides+i],rings[(j+1)*sides+next],v,tris,uv);
                AddFace(rings[j*sides+i],rings[(j+1)*sides+next],rings[j*sides+next],v,tris,uv);
            }
            for(int i=0;i<sides;i++)
            {
                int next=(i+1)%sides;
                AddFace(new Vector3(0,height,0),rings[(levels-1)*sides+next],rings[(levels-1)*sides+i],v,tris,uv);
                AddFace(Vector3.zero,rings[i],rings[next],v,tris,uv);
            }
            var mesh=new Mesh{name="FracturedRock_"+seed};mesh.SetVertices(v);mesh.SetTriangles(tris,0);mesh.SetUVs(0,uv);
            mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private static void AddFace(Vector3 a,Vector3 b,Vector3 c,List<Vector3> v,List<int> t,List<Vector2> uv)
        {
            var n=Vector3.Cross(b-a,c-a).normalized;int start=v.Count;
            foreach(var p in new[]{a,b,c})
            {
                v.Add(p);
                uv.Add(Mathf.Abs(n.y)>.8f?new Vector2(p.x,p.z)/24f:
                    Mathf.Abs(n.x)>Mathf.Abs(n.z)?new Vector2(p.z,p.y)/24f:new Vector2(p.x,p.y)/24f);
            }
            t.Add(start);t.Add(start+1);t.Add(start+2);
        }
    }
}
