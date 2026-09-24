using UnityEngine;

namespace SkiStation
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public sealed class SlopeMeshGenerator : MonoBehaviour
    {
        [SerializeField] private float snowThickness = 0.04f;
        [SerializeField] private float terrainOffset = 0.09f;
        [SerializeField] private bool conformEdgesToMountain = true;
        [SerializeField] private float edgeRaycastHeight = 2500f;
        [SerializeField] private Material slopeMaterial;

        public void Generate(SlopeRuntime slope)
        {
            if (slope == null || slope.Points.Count < 2) return;
            var mesh = new Mesh { name = slope.name + "_SlopeMesh" };
            int n = slope.Points.Count;
            const int columns = 9;
            var vertices = new Vector3[n * columns];
            var uv = new Vector2[n * columns];
            var tris = new int[(n - 1) * (columns - 1) * 6];
            mesh.indexFormat = vertices.Length > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;

            for (int i = 0; i < n; i++)
            {
                Vector3 tangent = i == 0 ? slope.Points[1] - slope.Points[0] :
                    i == n - 1 ? slope.Points[n - 1] - slope.Points[n - 2] :
                    slope.Points[i + 1] - slope.Points[i - 1];
                tangent.y = 0f;
                if (tangent.sqrMagnitude < 0.001f) tangent = Vector3.forward;
                tangent.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
                float halfWidth = slope.GetWidthAt(i) * 0.5f;

                for (int col = 0; col < columns; col++)
                {
                    float u = col / (float)(columns - 1);
                    Vector3 point = slope.Points[i] + side * Mathf.Lerp(halfWidth, -halfWidth, u);
                    int v = i * columns + col;
                    vertices[v] = transform.InverseTransformPoint(ConformEdge(point, slope.Points[i].y) + Vector3.up * snowThickness);
                    uv[v] = new Vector2(u, i / (float)(n - 1));
                    if (i < n - 1 && col < columns - 1)
                    {
                        int t = (i * (columns - 1) + col) * 6;
                        // Up-facing winding. The previous ribbon faced underground.
                        tris[t] = v; tris[t + 1] = v + 1; tris[t + 2] = v + columns;
                        tris[t + 3] = v + 1; tris[t + 4] = v + columns + 1; tris[t + 5] = v + columns;
                    }
                }
            }

            mesh.vertices = vertices;
            mesh.triangles = tris;
            mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = GetComponent<MeshRenderer>();
            if (slopeMaterial != null) renderer.sharedMaterial = slopeMaterial;
            else
            {
                var mat = RuntimeMaterialFactory.Create(DifficultyColor(slope.Difficulty));
                if (mat != null) renderer.sharedMaterial = mat;
            }
            var collider = GetComponent<MeshCollider>();
            if (collider == null) collider = gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = null;
            collider.sharedMesh = mesh;
        }

        private static Color DifficultyColor(SlopeDifficulty d)
        {
            switch (d)
            {
                case SlopeDifficulty.Green: return new Color(.42f, .82f, .57f);
                case SlopeDifficulty.Blue: return new Color(.40f, .67f, .94f);
                case SlopeDifficulty.Red: return new Color(.92f, .48f, .44f);
                case SlopeDifficulty.Black: return new Color(.55f, .55f, .58f);
                default: return new Color(.82f, .70f, .88f);
            }
        }

        private Vector3 ConformEdge(Vector3 point, float fallbackY)
        {
            if (!conformEdgesToMountain)
            {
                point.y = fallbackY;
                return point;
            }
            if (ManagementSurface.TryGet(point, out var surface)) return surface + Vector3.up * Mathf.Max(.25f, terrainOffset);
            point.y = fallbackY + terrainOffset;
            return point;
        }
    }
}
