using UnityEngine;

namespace SkiStation
{
    // 1.67: first real skiable massif built on the rendering path validated in 1.66.
    // Kept on Main Camera so the mountain is guaranteed to exist before gameplay starts.
    public sealed class SkiStationGuaranteedMountainVisual : MonoBehaviour
    {
        private Camera cam;
        private GameObject mountain;

        private const int Grid = 401;
        private const float MinZ = AlpineMassifGeneratorV2.MinZ;
        private const float Size = AlpineMassifGeneratorV2.HalfSize - MinZ;
        private const float Width = AlpineMassifGeneratorV2.HalfWidth * 2f;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            BuildMassif();
            EnsureTerrainFoundation();
            ForceCamera();
        }


        private void BuildMassif()
        {
            mountain = GameObject.Find("SkiStation_MASSIF_01");
            if (mountain != null) return;

            var verts = new Vector3[Grid * Grid];
            var tris = new int[(Grid - 1) * (Grid - 1) * 6];

            for (int z = 0; z < Grid; z++)
            for (int x = 0; x < Grid; x++)
            {
                float nx = (float)x / (Grid - 1);
                float nz = (float)z / (Grid - 1);
                float px = (nx - 0.5f) * Width;
                float pz = MinZ + nz * Size;
                float h = AlpineMassifGeneratorV2.Height(px, pz);
                verts[z * Grid + x] = new Vector3(px, h, pz);
            }

            int t = 0;
            for (int z = 0; z < Grid - 1; z++)
            for (int x = 0; x < Grid - 1; x++)
            {
                int a = z * Grid + x;
                int b = a + 1;
                int c = a + Grid;
                int d = c + 1;
                tris[t++] = a; tris[t++] = c; tris[t++] = d;
                tris[t++] = a; tris[t++] = d; tris[t++] = b;
            }

            mountain = new GameObject("SkiStation_MASSIF_01");
            var mf = mountain.AddComponent<MeshFilter>();
            var mr = mountain.AddComponent<MeshRenderer>();
            var mc = mountain.AddComponent<MeshCollider>();
            var mesh = new Mesh { name = "SkiStation_Massif01_Mesh" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;
            mc.sharedMesh = mesh;
            mr.sharedMaterial = MakeMaterial("SkiStation_Snow", new Color(0.82f, 0.88f, 0.91f, 1f));

            BuildForest();
            Debug.Log("SkiStation 2.46: WIDE RESORT BASE — four rolling sectors and clustered conifers.");
        }

        public static float Height(float x, float z)
        {
            float modifiedHeight;
            if (ConstructionTerrainAdaptation251.TrySampleHeight(x, z, out modifiedHeight)) return modifiedHeight;
            // Interpolate the same diagonal triangles as the visible/collision mesh.
            float step = Size / (Grid - 1);
            float stepX = Width / (Grid - 1);
            float gx = Mathf.Clamp((x + Width*.5f)/stepX,0f,Grid-1f);
            float gz = Mathf.Clamp((z - MinZ)/step,0f,Grid-1f);
            int ix = Mathf.Min(Mathf.FloorToInt(gx),Grid-2);
            int iz = Mathf.Min(Mathf.FloorToInt(gz),Grid-2);
            float tx=gx-ix, tz=gz-iz;
            float ax=ix*stepX-Width*.5f, az=iz*step+MinZ;
            float a=AlpineMassifGeneratorV2.Height(ax,az);
            float b=AlpineMassifGeneratorV2.Height(ax+stepX,az);
            float c=AlpineMassifGeneratorV2.Height(ax,az+step);
            float d=AlpineMassifGeneratorV2.Height(ax+stepX,az+step);
            return tz >= tx ? a+(d-c)*tx+(c-a)*tz : a+(b-a)*tx+(d-b)*tz;
        }

        private static float Gaussian(float x, float z, float cx, float cz, float sx, float sz)
        {
            float dx = (x - cx) / sx;
            float dz = (z - cz) / sz;
            return Mathf.Exp(-(dx * dx + dz * dz) * 0.5f);
        }

        private static void BuildRockBars()
        {
            var root = new GameObject("SkiStation_ROCK_BARS");
            Material rock = MakeMaterial("SkiStation_Rock", new Color(0.31f, 0.32f, 0.33f, 1f));
            CreateRock(root.transform, new Vector3(-620f, 720f, 360f), new Vector3(360f, 48f, 68f), new Vector3(5f, -22f, 8f), rock);
            CreateRock(root.transform, new Vector3(500f, 820f, 410f), new Vector3(330f, 55f, 74f), new Vector3(-5f, 18f, -7f), rock);
            CreateRock(root.transform, new Vector3(120f, 1130f, 680f), new Vector3(280f, 52f, 70f), new Vector3(7f, 5f, 3f), rock);
            CreateRock(root.transform, new Vector3(820f, 650f, 560f), new Vector3(250f, 45f, 65f), new Vector3(-5f, -18f, 6f), rock);
        }

        private static void CreateRock(Transform parent, Vector3 position, Vector3 scale, Vector3 euler, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "RockBar";
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.transform.rotation = Quaternion.Euler(euler);
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = material;
        }

        private static void BuildForest()
        {
            var root = new GameObject("SkiStation_FOREST");
            Material trunk = MakeMaterial("SkiStation_Trunk", new Color(0.24f, 0.16f, 0.10f, 1f));
            Material needles = MakeMaterial("SkiStation_Pines", new Color(0.08f, 0.22f, 0.14f, 1f));
            Random.State old = Random.state;
            Random.InitState(16701);

            int made = 0;
            for (int i = 0; i < 6000 && made < 600; i++)
            {
                float x = Random.Range(-3800f, 3800f);
                float z = Random.Range(-3050f, 1250f);
                float y = Height(x, z);
                if (y > 1050f) continue;
                // Preserve a 6km-wide station core; small woods only at its outer edges.
                if (z < -1400f && Mathf.Abs(x) < 3000f) continue;
                // Four gently bending clearings for future pistes, not auto-built runs.
                float drift=75f*Mathf.Sin(z*.002f);
                if (Mathf.Abs(x+3300f-drift)<170f || Mathf.Abs(x+1067f-drift)<170f ||
                    Mathf.Abs(x-1067f-drift)<170f || Mathf.Abs(x-3300f-drift)<170f) continue;
                float cluster = Mathf.PerlinNoise((x+5000f)*.0018f,(z+5000f)*.0018f);
                if (cluster < .44f || Random.value > cluster) continue;
                float gradientX = (Height(x+10f,z)-Height(x-10f,z))/20f;
                float gradientZ = (Height(x,z+10f)-Height(x,z-10f))/20f;
                if (gradientX*gradientX+gradientZ*gradientZ > .45f) continue;
                CreatePine(root.transform, new Vector3(x, y, z), Random.Range(.30f, .58f), trunk, needles);
                made++;
            }
            Random.state = old;
            Debug.Log("SkiStation 2.46: " + made + " conifers placed outside the station core and piste clearings.");
        }

        private static void CreatePine(Transform parent, Vector3 p, float scale, Material trunkMat, Material needlesMat)
        {
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "PineTrunk";
            trunk.GetComponent<Collider>().enabled = false;
            trunk.transform.SetParent(parent, false);
            trunk.transform.position = p + Vector3.up * (8f * scale);
            trunk.transform.localScale = new Vector3(2.4f * scale, 8f * scale, 2.4f * scale);
            var tr = trunk.GetComponent<Renderer>(); if (tr != null) tr.sharedMaterial = trunkMat;

            var crown = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            crown.name = "PineCrown";
            crown.GetComponent<Collider>().enabled = false;
            crown.transform.SetParent(parent, false);
            crown.transform.position = p + Vector3.up * (25f * scale);
            crown.transform.localScale = new Vector3(13f * scale, 20f * scale, 13f * scale);
            var cr = crown.GetComponent<Renderer>(); if (cr != null) cr.sharedMaterial = needlesMat;
        }

        private static void BuildBaseStationApron()
        {
            var apron = GameObject.CreatePrimitive(PrimitiveType.Cube);
            apron.name = "SkiStation_FRONT_DE_NEIGE";
            apron.transform.position = new Vector3(0f, 76f, -1550f);
            apron.transform.localScale = new Vector3(1450f, 10f, 300f);
            var r = apron.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = MakeMaterial("SkiStation_BaseSnow", new Color(0.96f, 0.98f, 1f, 1f));
        }


        private void EnsureTerrainFoundation()
        {
            var foundation = FindObjectOfType<AlpineTerrainFoundationRuntime>();
            if (foundation == null)
            {
                var go = new GameObject("SkiStation_ALPINE_TERRAIN_FOUNDATION");
                foundation = go.AddComponent<AlpineTerrainFoundationRuntime>();
            }
            foundation.RebuildSummary();

            var intelligence = FindObjectOfType<AlpineTerrainIntelligenceRuntime>();
            if (intelligence == null)
            {
                var intelligenceGo = new GameObject("SkiStation_ALPINE_TERRAIN_INTELLIGENCE");
                intelligence = intelligenceGo.AddComponent<AlpineTerrainIntelligenceRuntime>();
            }
            intelligence.Initialize(foundation);

            var bridge = FindObjectOfType<AlpineDomainTerrainBridgeRuntime>();
            if (bridge == null)
            {
                var bridgeGo = new GameObject("SkiStation_ALPINE_DOMAIN_TERRAIN_BRIDGE");
                bridge = bridgeGo.AddComponent<AlpineDomainTerrainBridgeRuntime>();
            }
            bridge.Initialize(foundation, intelligence);
        }

        private static Material MakeMaterial(string name, Color color)
        {
            Shader sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            if (sh == null) return null;
            var mat = new Material(sh) { name = name };
            mat.color = color;
            return mat;
        }

        private void ForceCamera()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null) return;
            foreach (var other in FindObjectsOfType<Camera>())
                if (other != null && other != cam) other.enabled = false;
            cam.enabled = true;
            cam.targetTexture = null;
            cam.targetDisplay = 0;
            cam.rect = new Rect(0f, 0f, 1f, 1f);
            cam.cullingMask = ~0;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.43f, 0.70f, 0.91f, 1f);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 12000f;
            cam.fieldOfView = 60f;
            cam.transform.position = new Vector3(-3650f, 1900f, -4100f);
            cam.transform.LookAt(new Vector3(0f, 760f, 300f));
        }
    }
}
