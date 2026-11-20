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

        // Runtime provenance of this exact generated ground overlay. Never serialized
        // as an obstacle exemption and never inferred from object names.
        private Mesh generatedDescentMesh393;
        private Matrix4x4 generatedWorldMatrix393;
        private int generatedSlopeRevision393;
        private bool generatedAllOnTerrain393;

        public void Generate(SlopeRuntime slope)
        {
            if (slope == null || slope.Points.Count < 2) return;
            generatedDescentMesh393 = null;
            generatedAllOnTerrain393 = conformEdgesToMountain;
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
            generatedDescentMesh393 = mesh;
            generatedWorldMatrix393 = transform.localToWorldMatrix;
            generatedSlopeRevision393 = slope.GeometryRevision261;
            if (ValAlpin.ValAlpinScene.IsActive) VisitorSkiDescentPath393.Invalidate();
        }

        // The native conforming snow ribbon is a ground overlay. Its coarse triangle
        // interpolation may differ from ground between vertices; that does not turn
        // it into a building. Native descent still validates the actual terrain.
        // Never exempt another collider, a changed mesh or a displaced overlay.
        public bool SupportsDescent393(Collider candidate, Vector3 ground)
        {
            if (!conformEdgesToMountain || !(candidate is MeshCollider mesh) ||
                candidate.gameObject != gameObject || mesh.sharedMesh == null ||
                GetComponent<SlopeRuntime>() == null) return false;
            float expectedOffset = Mathf.Max(.25f, terrainOffset) + snowThickness;
            if (expectedOffset < 0 || expectedOffset > .5f) return false;
            if (ValAlpin.ValAlpinScene.IsActive && HasGeneratedSupport393(mesh)) return true;
            if (!candidate.Raycast(new Ray(ground + Vector3.up * 2f, Vector3.down), out var hit, 3f)) return false;
            float offset = hit.point.y - ground.y;
            return hit.normal.y > .1f && offset >= -.08f && offset <= expectedOffset + .08f;
        }

        private bool HasGeneratedSupport393(MeshCollider candidate)
        {
            var slope = GetComponent<SlopeRuntime>();
            var filter = GetComponent<MeshFilter>();
            return generatedAllOnTerrain393 && generatedDescentMesh393 != null &&
                candidate.sharedMesh == generatedDescentMesh393 && filter != null &&
                filter.sharedMesh == generatedDescentMesh393 && slope != null &&
                slope.GeometryRevision261 == generatedSlopeRevision393 &&
                transform.localToWorldMatrix == generatedWorldMatrix393;
        }

        public string DescentSupportDiagnostic393()
        {
            var collider = GetComponent<MeshCollider>();
            return "support natif vérifié " + (collider != null && HasGeneratedSupport393(collider)) +
                " · tous sommets sur terrain " + generatedAllOnTerrain393 +
                " · transformation inchangée " + (transform.localToWorldMatrix == generatedWorldMatrix393);
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
            if (ManagementSurface.TryTrail(point, out var surface)) return surface + Vector3.up * Mathf.Max(.25f, terrainOffset);
            generatedAllOnTerrain393 = false;
            point.y = fallbackY + terrainOffset;
            return point;
        }
    }
}
