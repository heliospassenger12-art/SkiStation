using System;
using UnityEngine;

namespace SkiStation
{
    public enum MountainZoneType { Base, Meadow, Forest, Alpine, Rock, Summit, BuildablePlateau }

    public readonly struct MountainSample
    {
        public readonly float altitude;
        public readonly float slopeAngle;
        public readonly MountainZoneType zone;
        public readonly bool buildable;
        public readonly bool skiable;
        public MountainSample(float altitude, float slopeAngle, MountainZoneType zone, bool buildable, bool skiable)
        { this.altitude = altitude; this.slopeAngle = slopeAngle; this.zone = zone; this.buildable = buildable; this.skiable = skiable; }
    }

    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public sealed class MountainRuntime : MonoBehaviour
    {
        public static MountainRuntime I { get; private set; }
        [SerializeField] private MountainDefinition definition;
        [SerializeField] private bool generateOnAwake = true;
        [SerializeField] private Material mountainMaterial;

        public MountainDefinition Definition => definition;
        public Bounds WorldBounds { get; private set; }
        public float MinAltitude { get; private set; }
        public float MaxAltitude { get; private set; }
        public int GeneratedResolution { get; private set; }
        public bool IsGenerated { get; private set; }

        private float[,] heights;
        private Mesh mesh;
        private float[,] workingHeights;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            if (generateOnAwake) Generate();
        }

        public void Configure(MountainDefinition config) { definition = config; }

        public void Generate()
        {
            if (definition == null) definition = CreateRuntimeDefault();
            int r = Mathf.Clamp(definition.resolution, 33, 257);
            if ((r & (r - 1)) != 0) r = 129;
            GeneratedResolution = r;
            heights = new float[r, r];
            workingHeights = new float[r, r];
            var vertices = new Vector3[r * r];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[(r - 1) * (r - 1) * 6];
            float half = definition.size * 0.5f;
            MinAltitude = float.MaxValue; MaxAltitude = float.MinValue;

            for (int z = 0; z < r; z++)
            for (int x = 0; x < r; x++)
            {
                float u = x / (float)(r - 1), v = z / (float)(r - 1);
                float wx = (u - 0.5f) * definition.size;
                float wz = (v - 0.5f) * definition.size;
                float h = EvaluateHeight(wx, wz);
                heights[x, z] = h;
                workingHeights[x, z] = h;
                vertices[z * r + x] = new Vector3(wx, h, wz);
                uv[z * r + x] = new Vector2(u, v);
                MinAltitude = Mathf.Min(MinAltitude, h); MaxAltitude = Mathf.Max(MaxAltitude, h);
            }
            int ti = 0;
            for (int z = 0; z < r - 1; z++) for (int x = 0; x < r - 1; x++)
            {
                int a = z * r + x, b = a + 1, c = a + r, d = c + 1;
                triangles[ti++] = a; triangles[ti++] = c; triangles[ti++] = d;
                triangles[ti++] = a; triangles[ti++] = d; triangles[ti++] = b;
            }
            if (mesh != null) Destroy(mesh);
            mesh = new Mesh { name = "SkiStation_MountainMesh" };
            mesh.indexFormat = vertices.Length > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            GetComponent<MeshCollider>().sharedMesh = null; GetComponent<MeshCollider>().sharedMesh = mesh;
            if (mountainMaterial == null)
            {
                mountainMaterial = RuntimeMaterialFactory.Create(new Color(0.90f, 0.93f, 0.96f, 1f));
                if (mountainMaterial != null) mountainMaterial.name = "SkiStation_RuntimeMountainMaterial";
            }
            if (mountainMaterial != null) GetComponent<MeshRenderer>().sharedMaterial = mountainMaterial;
            WorldBounds = mesh.bounds;
            IsGenerated = true;
        }

        public MountainSample Sample(Vector3 worldPosition)
        {
            if (!IsGenerated || definition == null) return new MountainSample(0f, 0f, MountainZoneType.Base, true, false);
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            float half = definition.size * 0.5f;
            float u = Mathf.Clamp01((local.x + half) / definition.size);
            float v = Mathf.Clamp01((local.z + half) / definition.size);
            float h = SampleHeight(u, v);
            float eps = definition.size / Mathf.Max(32, GeneratedResolution - 1);
            float hx1 = SampleHeight(Mathf.Clamp01(u + eps / definition.size), v);
            float hx0 = SampleHeight(Mathf.Clamp01(u - eps / definition.size), v);
            float hz1 = SampleHeight(u, Mathf.Clamp01(v + eps / definition.size));
            float hz0 = SampleHeight(u, Mathf.Clamp01(v - eps / definition.size));
            float gx = (hx1 - hx0) / (2f * eps), gz = (hz1 - hz0) / (2f * eps);
            float slope = Mathf.Atan(Mathf.Sqrt(gx * gx + gz * gz)) * Mathf.Rad2Deg;
            MountainZoneType zone = Classify(h);
            bool buildable = slope <= definition.buildableMaxSlope && zone != MountainZoneType.Rock && zone != MountainZoneType.Summit;
            bool skiable = slope <= 42f && zone != MountainZoneType.Summit;
            return new MountainSample(h, slope, zone, buildable, skiable);
        }

        public bool TryGetSurface(Vector3 worldPosition, out Vector3 surface)
        {
            surface = worldPosition;
            if (!IsGenerated) return false;
            Ray ray = new Ray(worldPosition + Vector3.up * 5000f, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 10000f, ~0, QueryTriggerInteraction.Ignore) && hit.collider == GetComponent<Collider>()) { surface = hit.point; return true; }
            return false;
        }

        private float EvaluateHeight(float x, float z)
        {
            // 2.17 Massif V3: broad connected alpine terrain instead of one rounded procedural mound.
            float sx = x / definition.size;
            float sz = z / definition.size;
            float nx = (sx + 0.5f) * definition.mainFrequency + definition.seed * 0.0137f;
            float nz = (sz + 0.5f) * definition.mainFrequency - definition.seed * 0.0091f;
            float broad = Mathf.PerlinNoise(nx, nz);
            float fine = Mathf.PerlinNoise(nx * 3.6f + 17.3f, nz * 3.6f + 41.7f);
            float ridgeNoise = 1f - Mathf.Abs(2f * Mathf.PerlinNoise(nx * 1.55f + 71f, nz * 1.55f + 29f) - 1f);

            float Summit(float cx, float cz, float rx, float rz, float power)
            {
                float dx = (sx - cx) / rx, dz = (sz - cz) / rz;
                return Mathf.Pow(Mathf.Clamp01(1f - (dx * dx + dz * dz)), power);
            }

            float peaks = Mathf.Max(Summit(-.22f,.16f,.34f,.30f,.75f), Summit(.18f,.22f,.31f,.34f,.82f));
            peaks = Mathf.Max(peaks, Summit(.28f,-.15f,.28f,.32f,.88f));
            float diagonalRidge = Mathf.Clamp01(1f - Mathf.Abs(sz - (.30f * sx + .05f)) / .18f);
            diagonalRidge *= Mathf.Clamp01(1f - Mathf.Abs(sx) / .62f);

            // Wide glacial valley/combe crossing the domain; softened floor leaves buildable terraces.
            float valleyAxis = Mathf.Abs(sx + .11f + .10f * Mathf.Sin(sz * 7f));
            float glacialValley = 1f - Mathf.SmoothStep(.045f, .22f, valleyAxis);
            float lowerBasin = Summit(-.05f,-.30f,.30f,.22f,1.4f);

            float shape = .18f + .38f * broad + .13f * fine + .18f * ridgeNoise;
            shape += .34f * peaks + .17f * diagonalRidge;
            shape -= .22f * glacialValley;
            shape -= .10f * lowerBasin;

            // Blend the outer boundary down naturally so the massif does not read as a square plate.
            float edge = Mathf.Max(Mathf.Abs(sx), Mathf.Abs(sz));
            float edgeFade = 1f - Mathf.SmoothStep(.38f, .52f, edge);
            shape = Mathf.Lerp(.03f + .08f * broad, shape, edgeFade);
            shape = Mathf.Clamp01(shape);

            float altitude = definition.baseAltitude + shape * definition.maximumAltitude;
            float baseBlend = 1f - Mathf.SmoothStep(0f, 1f, new Vector2(x, z).magnitude / Mathf.Max(1f, definition.baseFlatRadius));
            altitude = Mathf.Lerp(altitude, definition.baseAltitude + 25f, baseBlend * definition.baseFlatBlend * .72f);
            return Mathf.Max(definition.baseAltitude, altitude);
        }

        public float GetBaseHeightAtGrid(int x, int z)
        {
            if (heights == null) return 0f;
            return heights[Mathf.Clamp(x, 0, heights.GetLength(0) - 1), Mathf.Clamp(z, 0, heights.GetLength(1) - 1)];
        }

        public void RebuildFromTerrainWorks(MountainTerrainWorkRuntime works, bool rebuildCollider)
        {
            if (!IsGenerated || works == null || heights == null) return;
            int r = GeneratedResolution;
            if (workingHeights == null || workingHeights.GetLength(0) != r) workingHeights = new float[r, r];
            var vertices = mesh.vertices;
            MinAltitude = float.MaxValue; MaxAltitude = float.MinValue;
            for (int z = 0; z < r; z++) for (int x = 0; x < r; x++)
            {
                float h = heights[x, z] + works.GetDelta(x, z);
                workingHeights[x, z] = h;
                vertices[z * r + x].y = h;
                MinAltitude = Mathf.Min(MinAltitude, h); MaxAltitude = Mathf.Max(MaxAltitude, h);
            }
            mesh.vertices = vertices;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            if (rebuildCollider) { var collider = GetComponent<MeshCollider>(); collider.sharedMesh = null; collider.sharedMesh = mesh; }
            WorldBounds = mesh.bounds;
        }

        private float SampleHeight(float u, float v)
        {
            float fx = u * (GeneratedResolution - 1), fz = v * (GeneratedResolution - 1);
            int x0 = Mathf.Clamp(Mathf.FloorToInt(fx), 0, GeneratedResolution - 1), z0 = Mathf.Clamp(Mathf.FloorToInt(fz), 0, GeneratedResolution - 1);
            int x1 = Mathf.Min(x0 + 1, GeneratedResolution - 1), z1 = Mathf.Min(z0 + 1, GeneratedResolution - 1);
            float tx = fx - x0, tz = fz - z0;
            var source = workingHeights ?? heights;
            return Mathf.Lerp(Mathf.Lerp(source[x0,z0], source[x1,z0], tx), Mathf.Lerp(source[x0,z1], source[x1,z1], tx), tz);
        }

        private MountainZoneType Classify(float altitude)
        {
            if (altitude <= definition.baseAltitude + 100f) return MountainZoneType.Base;
            if (altitude >= definition.summitAltitude) return MountainZoneType.Summit;
            if (altitude >= definition.rockStartAltitude) return MountainZoneType.Rock;
            if (altitude >= definition.forestLimitAltitude) return MountainZoneType.Alpine;
            if (altitude >= definition.forestStartAltitude) return MountainZoneType.Forest;
            return MountainZoneType.Meadow;
        }

        private MountainDefinition CreateRuntimeDefault()
        {
            var config = ScriptableObject.CreateInstance<MountainDefinition>();
            config.name = "Runtime Mountain Definition";
            return config;
        }
    }
}
