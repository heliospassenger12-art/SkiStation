using System.Collections.Generic;
using UnityEngine;
namespace SkiStation
{
    public sealed class TerrainQuery : MonoBehaviour
    {
        public bool TrySample(Vector3 worldPosition, out Vector3 point, out float slopeAngle, out MountainZoneType zone)
        {
            point = worldPosition; slopeAngle = 0f; zone = MountainZoneType.Base;
            if (MountainRuntime.I == null) return false;
            var sample = MountainRuntime.I.Sample(worldPosition); slopeAngle = sample.slopeAngle; zone = sample.zone;
            if (MountainRuntime.I.TryGetSurface(worldPosition, out var surface)) point = surface; else point.y = sample.altitude;
            return true;
        }
    }
    public static class AccessRoadMeshBuilder
    {
        public static Mesh Build(IReadOnlyList<Vector3> points, float width, float sidewalkWidth, bool sidewalk)
        {
            var mesh = new Mesh { name = "AccessRoadMesh" };
            if (points == null || points.Count < 2) return mesh;
            var verts = new List<Vector3>(); var tris = new List<int>(); var uvs = new List<Vector2>();
            float half = Mathf.Max(.5f, width * .5f);
            for (int i=0;i<points.Count;i++) { Vector3 f=(i==points.Count-1?points[i]-points[i-1]:points[i+1]-points[i]).normalized; Vector3 side=Vector3.Cross(Vector3.up,f).normalized*half; verts.Add(points[i]-side); verts.Add(points[i]+side); uvs.Add(new Vector2(0,i)); uvs.Add(new Vector2(1,i)); if(i>0){int a=(i-1)*2,b=a+1,c=i*2,d=c+1;tris.Add(a);tris.Add(c);tris.Add(b);tris.Add(b);tris.Add(c);tris.Add(d);} }
            mesh.SetVertices(verts); mesh.SetTriangles(tris,0); mesh.SetUVs(0,uvs); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }
    public sealed class StationRoadVehicleMotionState : MonoBehaviour { public Vector3 lastPosition; }
}
