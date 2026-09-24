using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// Small non-piste connector linking a point on an existing slope directly to a lift queue.
    /// It is deliberately not registered as a full slope: it is a service access branch.
    /// </summary>
    public sealed class SlopeLiftConnectorRuntime : MonoBehaviour
    {
        public SlopeRuntime SourceSlope { get; private set; }
        public LiftRuntime TargetLift { get; private set; }
        public Vector3 EntryPoint { get; private set; }
        public IReadOnlyList<Vector3> Route => route;
        public float Width { get; private set; } = 3.5f;
        public bool Open { get; private set; } = true;
        public float Length { get; private set; }

        private readonly List<Vector3> route = new List<Vector3>();
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        private void Awake()
        {
            if (SlopeLiftConnectorNetwork.I == null) new GameObject("SlopeLiftConnectorNetwork").AddComponent<SlopeLiftConnectorNetwork>();
            SlopeLiftConnectorNetwork.I.Register(this);
        }

        private void OnDestroy()
        {
            if (SlopeLiftConnectorNetwork.I != null) SlopeLiftConnectorNetwork.I.Unregister(this);
        }

        public void Build(SlopeRuntime source, LiftRuntime target, Vector3 entry, IReadOnlyList<Vector3> connectorRoute, float width = 3.5f)
        {
            SourceSlope = source;
            TargetLift = target;
            EntryPoint = entry;
            Width = Mathf.Clamp(width, 2f, 8f);
            route.Clear();
            if (connectorRoute != null) route.AddRange(connectorRoute);
            if (route.Count < 2) route.Add(entry);
            Length = 0f;
            for (int i = 1; i < route.Count; i++) Length += Vector3.Distance(route[i - 1], route[i]);
            BuildVisual();
        }

        public bool CanUse(Vector3 position, float radius = 4f)
        {
            if (!Open || TargetLift == null || !TargetLift.Open || TargetLift.Faulted || route.Count < 2) return false;
            return (EntryPoint - position).sqrMagnitude <= radius * radius;
        }

        public void SetOpen(bool value) => Open = value;

        private void BuildVisual()
        {
            meshFilter = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();
            var mesh = new Mesh { name = "SlopeLiftConnectorMesh" };
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uv = new List<Vector2>();
            for (int i = 0; i < route.Count; i++)
            {
                Vector3 tangent;
                if (i == 0) tangent = route[1] - route[0];
                else if (i == route.Count - 1) tangent = route[i] - route[i - 1];
                else tangent = route[i + 1] - route[i - 1];
                tangent.y = 0f;
                if (tangent.sqrMagnitude < 0.001f) tangent = Vector3.forward;
                Vector3 side = Vector3.Cross(Vector3.up, tangent.normalized) * (Width * .5f);
                int v = verts.Count;
                verts.Add(route[i] + side + Vector3.up * .035f);
                verts.Add(route[i] - side + Vector3.up * .035f);
                uv.Add(new Vector2(0f, i)); uv.Add(new Vector2(1f, i));
                if (i > 0)
                {
                    tris.Add(v - 2); tris.Add(v); tris.Add(v - 1);
                    tris.Add(v - 1); tris.Add(v); tris.Add(v + 1);
                }
            }
            mesh.SetVertices(verts); mesh.SetTriangles(tris, 0); mesh.SetUVs(0, uv); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            meshFilter.sharedMesh = mesh;
            if (meshRenderer.sharedMaterial == null)
            {
                var mat = RuntimeMaterialFactory.Create(Color.white);
                if (mat != null)
                {
                    mat.name = "SlopeLiftConnectorSnow";
                    meshRenderer.sharedMaterial = mat;
                }
            }
        }
    }
}
