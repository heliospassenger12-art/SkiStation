using UnityEngine;

namespace SkiStation
{
    // Applies local construction pads to the actual management mountain mesh.
    // No Unity Terrain package dependency. Edits happen only after confirmation.
    public static class ConstructionTerrainAdaptation251
    {
        private static MeshFilter filter;
        private static MeshCollider collider;
        private static Mesh writable;
        private static Vector3[] vertices;
        private static Vector3 origin;

        private static bool Ensure()
        {
            GameObject mountain = GameObject.Find("SkiStation_MASSIF_01");
            if (mountain == null) return false;
            MeshFilter current = mountain.GetComponent<MeshFilter>();
            if (current == null || current.sharedMesh == null) return false;
            if (filter == current && writable == current.sharedMesh && vertices != null) return true;
            filter = current;
            collider = mountain.GetComponent<MeshCollider>();
            writable = Object.Instantiate(current.sharedMesh);
            writable.name = current.sharedMesh.name + "_ConstructionPads";
            filter.sharedMesh = writable;
            vertices = writable.vertices;
            origin = mountain.transform.position;
            if (collider != null) { collider.sharedMesh = null; collider.sharedMesh = writable; }
            return true;
        }

        public static bool TrySampleHeight(float x, float z, out float height)
        {
            height = 0f;
            if (!Ensure()) return false;
            // The management mountain is a regular 401x401 grid, triangulated a-c-d and a-d-b.
            const int grid = 401;
            if (vertices.Length != grid * grid) return false;
            Vector3 min = vertices[0], max = vertices[vertices.Length - 1];
            float gx = (x - origin.x - min.x) / (max.x - min.x) * (grid - 1);
            float gz = (z - origin.z - min.z) / (max.z - min.z) * (grid - 1);
            if (gx < 0f || gz < 0f || gx > grid - 1 || gz > grid - 1) return false;
            int ix = Mathf.Min(Mathf.FloorToInt(gx), grid - 2);
            int iz = Mathf.Min(Mathf.FloorToInt(gz), grid - 2);
            float tx = gx - ix, tz = gz - iz;
            float a = vertices[iz * grid + ix].y;
            float b = vertices[iz * grid + ix + 1].y;
            float c = vertices[(iz + 1) * grid + ix].y;
            float d = vertices[(iz + 1) * grid + ix + 1].y;
            height = origin.y + (tz >= tx ? a + (d - c) * tx + (c - a) * tz : a + (b - a) * tx + (d - b) * tz);
            return true;
        }

        public static bool Flatten(Vector3 center, float width, float depth, float shoulder)
        {
            if (!Ensure()) return false;
            float halfX = width * .5f, halfZ = depth * .5f;
            float target = center.y - origin.y;
            bool changed = false;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 p = vertices[i];
                float dx = Mathf.Max(0f, Mathf.Abs(p.x + origin.x - center.x) - halfX);
                float dz = Mathf.Max(0f, Mathf.Abs(p.z + origin.z - center.z) - halfZ);
                float outside = Mathf.Sqrt(dx * dx + dz * dz);
                if (outside >= shoulder) continue;
                float t = Mathf.Clamp01(outside / Mathf.Max(.01f, shoulder));
                float influence = 1f - t * t * (3f - 2f * t);
                p.y = Mathf.Lerp(p.y, target, influence);
                vertices[i] = p;
                changed = true;
            }
            if (!changed) return false;
            writable.vertices = vertices;
            writable.RecalculateNormals();
            writable.RecalculateBounds();
            if (collider != null) { collider.sharedMesh = null; collider.sharedMesh = writable; }
            return true;
        }
    }
}
