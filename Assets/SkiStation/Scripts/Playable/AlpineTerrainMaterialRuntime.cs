using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SkiStation
{
    /// <summary>
    /// 2.18 visual-only terrain readability pass. Splits the existing massif mesh into
    /// snow / wind-scoured snow / exposed rock without changing vertices, collider or gameplay height.
    /// </summary>
    public sealed class AlpineTerrainMaterialRuntime : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (FindFirstObjectByType<AlpineTerrainMaterialRuntime>() == null)
                new GameObject("SkiStation_2.18_SnowRockRelief").AddComponent<AlpineTerrainMaterialRuntime>();
        }

        private void Start() { Invoke(nameof(Apply), .35f); }

        private void Apply()
        {
            var massif = GameObject.Find("SkiStation_MASSIF_01");
            if (massif == null) return;
            var mf = massif.GetComponent<MeshFilter>();
            var mr = massif.GetComponent<MeshRenderer>();
            if (mf == null || mr == null || mf.sharedMesh == null) return;

            Mesh mesh = mf.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] source = mesh.triangles;
            var snow = new List<int>(source.Length);
            var wind = new List<int>(source.Length / 4);
            var rock = new List<int>(source.Length / 4);

            for (int i = 0; i + 2 < source.Length; i += 3)
            {
                int a = source[i], b = source[i + 1], c = source[i + 2];
                float y = (vertices[a].y + vertices[b].y + vertices[c].y) / 3f;
                float ny = (normals[a].y + normals[b].y + normals[c].y) / 3f;
                float z = (vertices[a].z + vertices[b].z + vertices[c].z) / 3f;
                // Steep upper faces expose rock. Moderate high faces get blue-grey wind-packed snow.
                List<int> target = (z > AlpineMassifGeneratorV2.RockStartZ && ny < .94f) ? rock : (y > 1050f ? wind : snow);
                target.Add(a); target.Add(b); target.Add(c);
            }

            mesh.subMeshCount = 3;
            mesh.SetTriangles(snow, 0, false);
            mesh.SetTriangles(wind, 1, false);
            mesh.SetTriangles(rock, 2, true);
            AlpineRockMaterial.ApplyUV(mesh);
            mr.sharedMaterials = new[]
            {
                Mat("2.18_FreshSnow", new Color(.91f,.95f,.98f,1f), .18f),
                Mat("2.18_WindSnow", new Color(.70f,.79f,.84f,1f), .10f),
                AlpineRockMaterial.Shared
            };
            mr.shadowCastingMode = ShadowCastingMode.On;
            mr.receiveShadows = true;
            Debug.Log("SkiStation 2.18: SNOW/ROCK RELIEF READY — terrain materials split by altitude and slope; gameplay geometry unchanged.");
        }

        private static Material Mat(string name, Color color, float smoothness)
        {
            Shader s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Unlit/Color");
            if (s == null) return null;
            var m = new Material(s) { name = name, color = color };
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            return m;
        }
    }
}
