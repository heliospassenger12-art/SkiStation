// SkiStation — couche Unity de la gestion : relief (maillage par blocs + collisions), surface neige/roche reprenant
// la palette et la texture de roche de la version ChatGPT (AlpineMountainSurface261 / AlpineRockMaterial), forêts
// dessinées par instanciation avec le sapin étagé de AlpineForestVisual262.
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using SkiStation.Gestion;

namespace SkiStation.GestionUnity
{
    /// <summary>Passage coordonnées du noyau (x, altitude, z ; z croissant vers le village) ↔ Unity (main gauche : z inversé).</summary>
    public static class W
    {
        public static Vector3 U(double x, double y, double z) { return new Vector3((float)x, (float)y, (float)-z); }
        public static Vector3 U(P3 p) { return U(p.x, p.y, p.z); }
        public static Vector3 Ground(double x, double z, double lift = 0) { return U(x, Game.T.GetH(x, z) + lift, z); }
        public static P2 Core(Vector3 v) { return new P2(v.x, -v.z); }
        public static float Yaw(double dx, double dz) { return Mathf.Atan2((float)dx, (float)-dz) * Mathf.Rad2Deg; }

        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        public static Shader Lit { get { var s = Shader.Find("Standard"); if (s == null) s = Shader.Find("Universal Render Pipeline/Lit"); if (s == null) s = Shader.Find("Unlit/Color"); return s; } }
        public static Material Mat(string name, Color c, float smooth = 0.2f, bool instancing = false)
        {
            Material m; if (mats.TryGetValue(name, out m) && m != null) return m;
            m = new Material(Lit) { name = name, color = c, enableInstancing = instancing };
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            mats[name] = m; return m;
        }
        public static Material Unlit(string name, Color c)
        {
            Material m; if (mats.TryGetValue(name, out m) && m != null) return m;
            var s = Shader.Find("Unlit/Color"); if (s == null) s = Lit;
            m = new Material(s) { name = name, color = c }; mats[name] = m; return m;
        }
        public static Color Hex(string h) { Color c; return ColorUtility.TryParseHtmlString(h, out c) ? c : Color.magenta; }
        public static Color Hex(int rgb) { return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f); }
    }

    public sealed class GestionWorld : MonoBehaviour
    {
        public const int CHUNK = 64;                       // cellules par bloc
        Relief T { get { return Game.T; } }
        MeshFilter[,] chunks; Material[] surface; Transform terrainRoot;
        readonly HashSet<long> dirty = new HashSet<long>();
        public bool summerLook;

        // forêts
        Mesh crown, trunk; Material needles, snowLoad, bark; Matrix4x4[][] treeBatches; bool treesDirty = true;

        public void Build()
        {
            terrainRoot = new GameObject("SkiStation_Gestion_Relief").transform; terrainRoot.SetParent(transform, false);
            surface = SurfaceMaterials();
            int n = (Relief.N - 1) / CHUNK; chunks = new MeshFilter[n, n];
            for (int cz = 0; cz < n; cz++) for (int cx = 0; cx < n; cx++)
                {
                    var go = new GameObject("Relief_" + cx + "_" + cz); go.transform.SetParent(terrainRoot, false);
                    var mf = go.AddComponent<MeshFilter>(); var mr = go.AddComponent<MeshRenderer>();
                    mr.sharedMaterials = surface; mr.shadowCastingMode = ShadowCastingMode.On; mr.receiveShadows = true;
                    go.AddComponent<MeshCollider>();
                    chunks[cx, cz] = mf; BuildChunk(cx, cz);
                }
            T.RegionChanged += (x0, z0, x1, z1) => { for (int cz = z0 / CHUNK; cz <= Mathf.Min(n - 1, z1 / CHUNK); cz++) for (int cx = x0 / CHUNK; cx <= Mathf.Min(n - 1, x1 / CHUNK); cx++) dirty.Add(cx * 1000L + cz); treesDirty = true; };
            T.TreeRemoved += i => treesDirty = true;
            BuildTreeAssets();
        }
        public void RebuildAll() { int n = chunks.GetLength(0); for (int cz = 0; cz < n; cz++) for (int cx = 0; cx < n; cx++) BuildChunk(cx, cz); treesDirty = true; }

        /// <summary>Palette de la surface 2.61 de la version ChatGPT ; seuils d'altitude adaptés au massif 1 640 → 4 200 m.</summary>
        Material[] SurfaceMaterials()
        {
            var rock = AlpineRockMaterial.Shared ?? W.Mat("Roche", new Color(.38f, .36f, .33f), .09f);
            return new[] {
                W.Mat("261_NeigeFraiche", new Color(.94f, .97f, .985f), .34f),
                W.Mat("261_NeigeCompacte", new Color(.80f, .87f, .91f), .22f),
                W.Mat("261_NeigeVent", new Color(.66f, .75f, .81f), .12f),
                rock,
                W.Mat("261_RocheOmbre", new Color(.22f, .23f, .23f), .06f),
                W.Mat("Ete_Alpage", new Color(.36f, .52f, .26f), .08f) };
        }
        void BuildChunk(int cx, int cz)
        {
            int V = CHUNK + 1, N = Relief.N; var H = T.H;
            var verts = new Vector3[V * V]; var norms = new Vector3[V * V]; var uv = new Vector2[V * V];
            for (int j = 0; j < V; j++) for (int i = 0; i < V; i++)
                {
                    int gx = cx * CHUNK + i, gz = cz * CHUNK + j, k = j * V + i;
                    double x = -Relief.HALF + gx * Relief.CELL, z = -Relief.HALF + gz * Relief.CELL;
                    verts[k] = W.U(x, H[gz * N + gx], z);
                    float hx = H[gz * N + Mathf.Min(N - 1, gx + 1)] - H[gz * N + Mathf.Max(0, gx - 1)], hz = H[Mathf.Min(N - 1, gz + 1) * N + gx] - H[Mathf.Max(0, gz - 1) * N + gx];
                    norms[k] = new Vector3(-hx / (2 * (float)Relief.CELL), 1, hz / (2 * (float)Relief.CELL)).normalized;
                    uv[k] = new Vector2(verts[k].x / 24f, (verts[k].y + verts[k].z * .15f) / 24f);      // UV de AlpineRockMaterial.ApplyUV
                }
            var subs = new List<int>[surface.Length]; for (int s = 0; s < subs.Length; s++) subs[s] = new List<int>();
            for (int j = 0; j < CHUNK; j++) for (int i = 0; i < CHUNK; i++)
                {
                    int a = j * V + i, d = a + 1, b = a + V, c = b + 1;
                    Tri(subs, verts, norms, a, d, b); Tri(subs, verts, norms, d, c, b);
                }
            var mf = chunks[cx, cz]; var m = mf.sharedMesh; if (m == null) { m = new Mesh { name = "Relief" }; m.MarkDynamic(); }
            m.Clear(); m.vertices = verts; m.normals = norms; m.uv = uv; m.subMeshCount = subs.Length;
            for (int s = 0; s < subs.Length; s++) m.SetTriangles(subs[s], s, false);
            m.RecalculateBounds(); mf.sharedMesh = m;
            var mc = mf.GetComponent<MeshCollider>(); mc.sharedMesh = null; mc.sharedMesh = m;
        }
        // z inversé : l'ordre (a, d, b) du noyau devient (a, b, d) pour garder des faces tournées vers le ciel
        void Tri(List<int>[] subs, Vector3[] v, Vector3[] n, int a, int b, int c)
        {
            float y = (v[a].y + v[b].y + v[c].y) / 3f, ny = Mathf.Clamp01((n[a].y + n[b].y + n[c].y) / 3f), slope = 1 - ny;
            float x = (v[a].x + v[b].x + v[c].x) / 3f, z = (v[a].z + v[b].z + v[c].z) / 3f;
            float exposure = Mathf.PerlinNoise((x + 4300f) * .0014f, (z + 3400f) * .0017f);
            double snowMask = T.SnowMask != null ? SnowAt(x, -z) : 0;
            int s;
            if (summerLook) s = slope > .30f || y > 3000f ? (slope > .42f ? 4 : 3) : 5;
            else if (snowMask > 0.5) s = 1;
            else if (slope > .30f && y > 1800f) s = (slope > .42f || exposure > .63f) ? 4 : 3;
            else if (y > 2900f && (slope > .075f || exposure > .60f)) s = 2;
            else if (slope > .095f || (y > 2300f && exposure > .70f)) s = 1;
            else s = 0;
            subs[s].Add(a); subs[s].Add(c); subs[s].Add(b);
        }
        double SnowAt(double x, double z)
        {
            int gx = Mathf.Clamp((int)System.Math.Floor((x + Relief.HALF) / Relief.CELL + .5), 0, Relief.N - 1), gz = Mathf.Clamp((int)System.Math.Floor((z + Relief.HALF) / Relief.CELL + .5), 0, Relief.N - 1);
            return T.SnowMask[gz * Relief.N + gx];
        }
        public void SetSummer(bool on) { if (summerLook == on) return; summerLook = on; RebuildAll(); }

        // ---------- forêts : sapin étagé 2.62 de la version ChatGPT, dessiné par instanciation ----------
        void BuildTreeAssets()
        {
            var mi = typeof(AlpineForestVisual262).GetMethod("BuildCrown", BindingFlags.NonPublic | BindingFlags.Static);
            crown = mi != null ? mi.Invoke(null, null) as Mesh : null;
            if (crown == null) { var tmp = GameObject.CreatePrimitive(PrimitiveType.Capsule); crown = tmp.GetComponent<MeshFilter>().sharedMesh; Destroy(tmp); }
            var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder); trunk = cyl.GetComponent<MeshFilter>().sharedMesh; Destroy(cyl);
            needles = W.Mat("262_Aiguilles", new Color(.035f, .13f, .075f), .12f, true);
            snowLoad = W.Mat("262_NeigeBranches", new Color(.93f, .965f, .985f), .28f, true);
            bark = W.Mat("Tronc", new Color(.24f, .16f, .10f), .05f, true);
        }
        void RebuildTrees()
        {
            treesDirty = false; var list = new List<Matrix4x4>();
            foreach (var t in T.Trees)
            {
                if (!t.alive) continue;
                float h = (float)(9 + 9 * t.s), r = h * .32f;
                var p = W.U(t.x, T.GetH(t.x, t.z), t.z);
                list.Add(Matrix4x4.TRS(p, Quaternion.Euler(0, (float)((t.x * 37 + t.z * 11) % 360), 0), new Vector3(r, h, r)));
            }
            int nb = (list.Count + 1022) / 1023; treeBatches = new Matrix4x4[nb][];
            for (int b = 0; b < nb; b++) treeBatches[b] = list.GetRange(b * 1023, Mathf.Min(1023, list.Count - b * 1023)).ToArray();
        }
        static readonly Matrix4x4 crownOffset = Matrix4x4.TRS(new Vector3(0, .38f, 0), Quaternion.identity, Vector3.one);
        static readonly Matrix4x4 trunkOffset = Matrix4x4.TRS(new Vector3(0, .1f, 0), Quaternion.identity, new Vector3(.12f, .1f, .12f));
        Matrix4x4[] tmpC = new Matrix4x4[1023], tmpT = new Matrix4x4[1023];
        public bool showTrees = true; public float treeDistance = 3500;

        void Update()
        {
            if (chunks == null) return;
            if (dirty.Count > 0) { foreach (var k in dirty) BuildChunk((int)(k / 1000), (int)(k % 1000)); dirty.Clear(); }
            if (treesDirty) RebuildTrees();
            if (!showTrees || treeBatches == null) return;
            var cam = Camera.main; float far = treeDistance; Vector3 cp = cam != null ? cam.transform.position : Vector3.zero;
            if (cam != null && cp.y - Game.T.GetH(cp.x, -cp.z) > 2600) return;            // vue très haute : forêts masquées
            foreach (var batch in treeBatches)
            {
                int n = 0;
                for (int i = 0; i < batch.Length; i++)
                {
                    var m = batch[i]; var pos = new Vector3(m.m03, m.m13, m.m23);
                    if (cam != null && (pos - cp).sqrMagnitude > far * far) continue;
                    tmpC[n] = m * crownOffset; tmpT[n] = m * trunkOffset; n++;
                }
                if (n == 0) continue;
                for (int s = 0; s < Mathf.Min(2, crown.subMeshCount); s++) Graphics.DrawMeshInstanced(crown, s, s == 0 ? needles : (summerLook ? needles : snowLoad), tmpC, n, null, ShadowCastingMode.On, true);
                Graphics.DrawMeshInstanced(trunk, 0, bark, tmpT, n);
            }
        }

        /// <summary>Point du relief sous le pointeur (coordonnées du noyau).</summary>
        public static bool Pick(Ray ray, out P2 p, out Vector3 hitPoint)
        {
            RaycastHit hit; p = new P2(0, 0); hitPoint = Vector3.zero;
            if (!Physics.Raycast(ray, out hit, 40000f)) return false;
            hitPoint = hit.point; p = W.Core(hit.point); return true;
        }
    }
}
