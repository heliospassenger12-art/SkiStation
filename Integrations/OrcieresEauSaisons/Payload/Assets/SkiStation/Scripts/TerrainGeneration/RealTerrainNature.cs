using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SkiStation.TerrainGeneration
{
    // Cartographic scenery is rebuilt from the domain resource. The native terrain
    // remains the owner of heights, collisions and checkpoint state.
    internal sealed class RealTerrainNature : MonoBehaviour
    {
        const float TileSize = 384f;

        [Serializable] sealed class Map
        {
            public int version;
            public string domain;
            public float west, south, size;
            public Surface[] waters, streams;
            public string rockMask;
            public int rockAreas, rockResolution;
            public Placement[] trees, rocks;
        }

        [Serializable] sealed class Surface
        {
            public string name;
            public int kind;
            public Vector2[] vertices;
            public int[] triangles;
        }

        [Serializable] sealed class Placement
        {
            public Vector2 at;
            public int kind;
            public float yaw, height, width;
        }

        sealed class Part
        {
            public Mesh mesh;
            public Material[] materials;
            public Matrix4x4 matrix;
        }

        sealed class Model
        {
            public Part[] near, far;
        }

        sealed class Batch
        {
            public Model model;
            public Bounds bounds;
            public readonly List<Matrix4x4> matrices = new List<Matrix4x4>();
        }

        readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        readonly Dictionary<Material, Material> materialCopies = new Dictionary<Material, Material>();
        readonly Dictionary<(int x, int z, int kind), Batch> batches =
            new Dictionary<(int x, int z, int kind), Batch>();
        readonly List<(Mesh mesh, Vector2[] points, int kind)> surfaceMeshes =
            new List<(Mesh mesh, Vector2[] points, int kind)>();
        readonly List<(Vector3 a, Vector3 b, float radius)> corridors =
            new List<(Vector3 a, Vector3 b, float radius)>();
        readonly Matrix4x4[] drawMatrices = new Matrix4x4[511];
        TerrainGenerationWorld world;
        Map data;
        Model[] models;
        Transform presentation;
        TerrainCollider groundCollider;
        byte[] rockCover;
        float nextCheck;
        int infrastructureHash;
        bool heightChanged;
        internal int SurfaceCount { get; private set; }
        internal int InstanceCount { get; private set; }
        internal int RockPaintResolution { get; private set; }
        internal float RockCoverage(int x, int z) => rockCover == null ? 0 : rockCover[z * RockPaintResolution + x] / 255f;

        internal static void TryInstall(TerrainGenerationWorld world, string resource)
        {
            var asset = Resources.Load<TextAsset>(resource);
            if (asset == null) return;
            var map = JsonUtility.FromJson<Map>(asset.text);
            if (!Matches(world, map))
            {
                Debug.LogWarning("Décor géographique ignoré : emprise incompatible avec le relief actif.", world);
                return;
            }
            var geologyAsset = Resources.Load<TextAsset>(resource + "Geology");
            var geology = geologyAsset != null ? JsonUtility.FromJson<Map>(geologyAsset.text) : null;
            if (Matches(world, geology))
            {
                map.rockMask = geology.rockMask;
                map.rockResolution = geology.rockResolution;
                map.rockAreas = geology.rockAreas;
                var rocks = new List<Placement>(map.rocks);
                rocks.AddRange(geology.rocks);
                map.rocks = rocks.ToArray();
            }

            var controller = world.gameObject.AddComponent<RealTerrainNature>();
            try { controller.Build(world, map); }
            catch (Exception e)
            {
                Debug.LogException(e, world);
                Destroy(controller);
            }
        }

        static bool Matches(TerrainGenerationWorld world, Map map)
        {
            var terrain = world.Recipe;
            return map != null && map.version == 1 && map.domain == world.domainId &&
                Mathf.Approximately(map.west, terrain.originX) && Mathf.Approximately(map.south, terrain.originZ) &&
                Mathf.Approximately(map.size, terrain.sizeX) && Mathf.Approximately(map.size, terrain.sizeZ);
        }

        internal bool TryGetWater(string name, out Vector3 center)
        {
            center = default;
            foreach (var surface in data.waters)
            {
                if (surface.name != name || surface.vertices.Length == 0) continue;
                Vector2 min = surface.vertices[0], max = min;
                foreach (var v in surface.vertices) { min = Vector2.Min(v, min); max = Vector2.Max(v, max); }
                center = Ground((min + max) * .5f);
                return true;
            }
            return false;
        }

        void Build(TerrainGenerationWorld owner, Map map)
        {
            world = owner;
            data = map;
            groundCollider = world.Terrain.GetComponent<TerrainCollider>();
            // Terrain descendants are already excluded by ManagementTerrainCheckpointBridge.
            presentation = new GameObject("Décor géographique — Orcières").transform;
            presentation.SetParent(world.Terrain.transform, false);
            var palette = Resources.Load<ValAlpin.ValAlpinNatureAssets>("VisualProfiles/ValAlpinNature");
            var lit = palette != null ? palette.litShader : Shader.Find("Universal Render Pipeline/Lit");
            var water = Material(lit, "Lacs et retenues", new Color(.075f, .31f, .43f), .63f);
            var stream = Material(lit, "Cours d’eau", new Color(.20f, .46f, .54f), .48f);
            var hydro = world.gameObject.AddComponent<RealTerrainWater>();
            foreach (var body in data.waters) hydro.AddSurface(body.vertices, body.triangles, true);
            foreach (var body in data.streams) hydro.AddSurface(body.vertices, body.triangles, false);
            BuildSurfaces(data.waters, water, 0);
            BuildSurfaces(data.streams, stream, 1);

            var pine = Tree(lit, false);
            var deciduous = Tree(lit, true);
            var fallbackRock = Rock(lit);
            models = new[]
            {
                ModelFor(palette != null ? palette.trees[6] : null, pine, lit),
                deciduous,
                ModelFor(palette != null ? palette.boulders[0] : null, fallbackRock, lit),
                ModelFor(palette != null ? palette.cliffs[2] : null, fallbackRock, lit)
            };
            infrastructureHash = CaptureCorridors();
            RefreshInstances();
            if (!string.IsNullOrEmpty(data.rockMask) && data.rockResolution == 512)
            {
                var mask = Convert.FromBase64String(data.rockMask);
                if (mask.Length == data.rockResolution * data.rockResolution)
                {
                    rockCover = mask;
                    RockPaintResolution = data.rockResolution;
                    SurfaceCount += data.rockAreas;
                    world.Terrain.GetComponent<TerrainGeneratedAppearance>().RefreshClimate();
                }
            }
            Vector3? center = TryGetWater("Grand Lac des Estaris",out var lake) ? lake : null;
            hydro.Bind(world,water,stream,presentation,center);
            world.gameObject.AddComponent<WaterCrossingTool>();
            TerrainCallbacks.heightmapChanged += OnHeightmapChanged;
        }

        Material Material(Shader shader, string name, Color color, float smoothness)
        {
            var material = new Material(shader) {name = name, enableInstancing = true};
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            owned.Add(material);
            return material;
        }

        Material Adapt(Material source, Shader lit)
        {
            if (materialCopies.TryGetValue(source, out var result)) return result;
            string shader = source.shader.name;
            bool leaves = shader.Contains("Leaves", StringComparison.Ordinal);
            Color tint = leaves ? new Color(.46f, .59f, .51f) : new Color(.72f, .74f, .76f);
            result = Material(lit, source.name + " · relief", tint, .09f);
            foreach (string property in new[] { "_BaseMap", "_MainTex", "_Albedo" })
            {
                if (!source.HasProperty(property) || source.GetTexture(property) == null) continue;
                result.SetTexture("_BaseMap", source.GetTexture(property));
                break;
            }
            if (leaves)
            {
                result.SetFloat("_AlphaClip", 1f);
                result.SetFloat("_Cutoff", .38f);
                result.SetFloat("_Cull", 0f);
                result.EnableKeyword("_ALPHATEST_ON");
                result.renderQueue = (int)RenderQueue.AlphaTest;
            }
            materialCopies.Add(source, result);
            return result;
        }

        Model ModelFor(GameObject prefab, Model fallback, Shader lit)
        {
            if (prefab == null) return fallback;
            var group = prefab.GetComponentInChildren<LODGroup>(true);
            var renderers = group != null && group.lodCount > 0
                ? group.GetLODs()[0].renderers : prefab.GetComponentsInChildren<MeshRenderer>(true);
            var parts = new List<Part>();
            Bounds bounds = default;
            foreach (var renderer in renderers)
            {
                if (renderer == null || !renderer.TryGetComponent<MeshFilter>(out var filter) || filter.sharedMesh == null) continue;
                var mesh = filter.sharedMesh;
                var matrix = prefab.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                var materials = renderer.sharedMaterials;
                if (materials.Length == 0 || Array.Exists(materials, m => m == null)) continue;
                var b = mesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var point = matrix.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                    if (parts.Count == 0 && i == 0) bounds = new Bounds(point, Vector3.zero);
                    else bounds.Encapsulate(point);
                }
                parts.Add(new Part {mesh = mesh, materials = Array.ConvertAll(materials, m => Adapt(m, lit)), matrix = matrix});
            }
            if (parts.Count == 0 || bounds.size.y <= 0) return fallback;
            Matrix4x4 normalize = Matrix4x4.Scale(new Vector3(1f / bounds.size.x, 1f / bounds.size.y, 1f / bounds.size.z)) *
                Matrix4x4.Translate(-new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
            foreach (var part in parts) part.matrix = normalize * part.matrix;
            return new Model {near = parts.ToArray(), far = fallback.far};
        }

        Model Tree(Shader shader, bool deciduous)
        {
            var canopy = Material(shader, deciduous ? "Couronne hivernale" : "Conifères", deciduous
                ? new Color(.35f, .34f, .28f) : new Color(.19f, .31f, .27f), .08f);
            var bark = Material(shader, "Troncs", new Color(.27f, .23f, .20f), .06f);
            var parts = new List<Part>
            {
                RingModel(bark, 5, new[] {new Vector2(.065f, 0), new Vector2(.019f, .85f)})
            };
            if (deciduous)
                parts.Add(RingModel(canopy, 7, new[] {new Vector2(.09f,.28f), new Vector2(.50f,.60f),
                    new Vector2(.30f,.85f), new Vector2(0,1)}));
            else
                for (int i = 0; i < 3; i++)
                    parts.Add(RingModel(canopy, 7, new[] {new Vector2(.10f,.18f+i*.21f),
                        new Vector2(.49f-i*.11f,.23f+i*.21f),new Vector2(0,.62f+i*.19f)}));
            var geometry = parts.ToArray();
            return new Model {near = geometry, far = geometry};
        }

        Model Rock(Shader shader)
        {
            var material = Material(shader, "Roche et falaises", new Color(.42f, .43f, .44f), .1f);
            var part = RingModel(material, 7, new[] {new Vector2(.42f,0), new Vector2(.50f,.3f),
                new Vector2(.32f,.86f), new Vector2(.06f,1)});
            var geometry = new[]{part};
            return new Model {near = geometry, far = geometry};
        }

        Part RingModel(Material material, int sides, Vector2[] rings)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int y = 0; y < rings.Length; y++)
            for (int x = 0; x < sides; x++)
            {
                float angle = x * Mathf.PI * 2 / sides + y * .14f;
                float radius = rings[y].x * (1 + .10f * Mathf.Sin(x * 17 + y));
                vertices.Add(new Vector3(radius * Mathf.Sin(angle), rings[y].y, radius * Mathf.Cos(angle)));
                if (y == 0) continue;
                int a = (y-1) * sides + x, b = (y-1) * sides + (x+1)%sides, c = a+sides, d = b+sides;
                triangles.AddRange(new[] {a,b,c,b,d,c});
            }
            var mesh = new Mesh {name = material.name};
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals();
            owned.Add(mesh);
            return new Part {mesh = mesh, materials = new[]{material}, matrix = Matrix4x4.identity};
        }

        void BuildSurfaces(Surface[] surfaces, Material material, int kind)
        {
            var pages = new Dictionary<Vector2Int, (List<Vector2> vertices, List<int> indices)>();
            foreach (var feature in surfaces)
            {
                if (feature.kind != kind || feature.triangles.Length == 0) continue;
                var first = feature.vertices[feature.triangles[0]];
                var tile = new Vector2Int(Mathf.FloorToInt(first.x/TileSize), Mathf.FloorToInt(first.y/TileSize));
                if (!pages.TryGetValue(tile, out var page))
                {
                    page = (new List<Vector2>(), new List<int>());
                    pages.Add(tile, page);
                }
                int start = page.vertices.Count;
                page.vertices.AddRange(feature.vertices);
                foreach (int index in feature.triangles) page.indices.Add(start + index);
                SurfaceCount++;
            }

            foreach (var page in pages.Values)
            {
                var surface = new GameObject(material.name);
                surface.transform.SetParent(presentation, false);
                var mesh = new Mesh {name = material.name, indexFormat = IndexFormat.UInt32};
                var points = page.vertices.ToArray();
                ProjectMesh(mesh, points, kind);
                mesh.SetTriangles(page.indices, 0);
                mesh.RecalculateBounds();
                surface.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = surface.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                surfaceMeshes.Add((mesh, points, kind));
                owned.Add(mesh);
            }
        }

        Vector3 Ground(Vector2 point) => new Vector3(point.x, world.ClampedHeight(point.x, point.y), point.y);

        void ProjectMesh(Mesh mesh, Vector2[] points, int kind)
        {
            float lift = kind == 0 ? .9f : .65f;
            var vertices = new Vector3[points.Length];
            var normals = new Vector3[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                var point = Ground(points[i]);
                if (groundCollider.Raycast(new Ray(point + Vector3.up * 160f, Vector3.down), out var hit, 320f))
                    point.y = Mathf.Max(point.y, hit.point.y);
                vertices[i] = presentation.InverseTransformPoint(point + Vector3.up * lift);
                normals[i] = Vector3.up;
            }
            mesh.vertices = vertices;
            mesh.uv = points;
            mesh.normals = normals;
        }

        void OnHeightmapChanged(Terrain terrain, RectInt region, bool synced)
        {
            if (world != null && terrain == world.Terrain) heightChanged = true;
        }

        int CaptureCorridors()
        {
            corridors.Clear();
            int hash = 17;
            var scene = gameObject.scene;
            foreach (var slope in FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.InstanceID))
            {
                if (slope.gameObject.scene != scene) continue;
                hash = HashCode.Combine(hash, slope.GetInstanceID(), slope.GeometryRevision261);
                for (int i = 1; i < slope.Points.Count; i++)
                    corridors.Add((slope.Points[i-1], slope.Points[i], Mathf.Max(slope.GetWidthAt(i-1),slope.GetWidthAt(i))*.5f+3));
            }
            foreach (var lift in FindObjectsByType<LiftRuntime>(FindObjectsSortMode.InstanceID))
            {
                if (lift.gameObject.scene != scene) continue;
                hash = HashCode.Combine(hash, lift.GetInstanceID(), lift.GeometryRevision261);
                corridors.Add((lift.Bottom, lift.Bottom, 23));
                corridors.Add((lift.Top, lift.Top, 23));
                for (int i = 1; i < lift.Route.Count; i++) corridors.Add((lift.Route[i-1], lift.Route[i], 7));
            }
            var road = StationRoadNetwork247.I;
            if (road != null && road.gameObject.scene == scene && road.IsReady)
            {
                AddRoad(road.GetPermanentRoadPath(), StationRoadNetwork247.MainRoadHalfWidth);
                foreach (var branch in road.Branches) AddRoad(branch, 10);
            }
            foreach (var structure in FindObjectsByType<StationStructureStatus249>(FindObjectsSortMode.InstanceID))
            {
                if (structure.gameObject.scene != scene) continue;
                var p = structure.transform.position;
                hash = HashCode.Combine(hash, p);
                corridors.Add((p,p,18));
            }
            var front = world.FrontDeNeige.position;
            corridors.Add((front,front,80));
            return hash;

            void AddRoad(IReadOnlyList<Vector3> path, float radius)
            {
                for (int i = 1; i < path.Count; i++)
                {
                    hash = HashCode.Combine(hash, path[i-1], path[i]);
                    corridors.Add((path[i-1],path[i],radius));
                }
            }
        }

        void RefreshInstances()
        {
            batches.Clear();
            InstanceCount = 0;
            Append(data.trees);
            Append(data.rocks);
        }

        void Append(Placement[] placements)
        {
            foreach (var item in placements)
            {
                Vector3 position = Ground(item.at);
                float width = item.width > 0 ? item.width : item.height * (item.kind == 0 ? .46f : .62f);
                float depth = item.kind == 3 ? Mathf.Min(14, width * .55f) : width;
                bool blocked = false;
                foreach (var (a,b,radius) in corridors)
                {
                    float limit = radius + width*.5f;
                    if (position.x < Mathf.Min(a.x,b.x)-limit || position.x > Mathf.Max(a.x,b.x)+limit ||
                        position.z < Mathf.Min(a.z,b.z)-limit || position.z > Mathf.Max(a.z,b.z)+limit) continue;
                    if (ConstructionRules349.SegmentDistanceXZ(position,a,b) <= limit) { blocked = true; break; }
                }
                if (blocked) continue;
                if (item.kind == 3)
                {
                    var downslope = Quaternion.Euler(0,item.yaw,0) * Vector3.back;
                    var toe = item.at + new Vector2(downslope.x,downslope.z) * (depth * .32f);
                    position.y = Mathf.Min(position.y, Ground(toe).y) - 1f;
                }
                var key = (Mathf.FloorToInt(item.at.x/TileSize), Mathf.FloorToInt(item.at.y/TileSize), item.kind);
                if (!batches.TryGetValue(key, out var batch))
                {
                    batch = new Batch {model = models[item.kind], bounds = new Bounds(position, Vector3.zero)};
                    batches.Add(key,batch);
                }
                float radiusBounds = Mathf.Max(width, depth);
                batch.bounds.Encapsulate(position + new Vector3(radiusBounds,item.height,radiusBounds));
                batch.bounds.Encapsulate(position - new Vector3(radiusBounds,1,radiusBounds));
                batch.matrices.Add(Matrix4x4.TRS(position, Quaternion.Euler(0,item.yaw,0), new Vector3(width,item.height,depth)));
                InstanceCount++;
            }
        }

        void LateUpdate()
        {
            if (world == null || models == null || !world.IsReady || !presentation.gameObject.activeInHierarchy ||
                world.CheckpointTerrainBusy) return;
            if (Time.unscaledTime >= nextCheck)
            {
                nextCheck = Time.unscaledTime + .55f;
                int hash = CaptureCorridors();
                if (hash != infrastructureHash || heightChanged) RefreshInstances();
                infrastructureHash = hash;
                if (heightChanged)
                {
                    foreach (var (mesh, points, kind) in surfaceMeshes)
                    {
                        ProjectMesh(mesh, points, kind);
                        mesh.RecalculateBounds();
                    }
                    heightChanged = false;
                }
            }

            DrawInstances(SkiStation.Arcade.ArcadeSkiSession.ActiveSession?.playerCamera ?? Camera.main);
        }

        void DrawInstances(Camera camera)
        {
            if (camera == null || !SystemInfo.supportsInstancing) return;
            var view = GeometryUtility.CalculateFrustumPlanes(camera);
            foreach (var batch in batches.Values)
            {
                if (!GeometryUtility.TestPlanesAABB(view, batch.bounds)) continue;
                bool near = batch.bounds.SqrDistance(camera.transform.position) < 850f * 850f;
                foreach (var part in near ? batch.model.near : batch.model.far)
                {
                    for (int offset = 0; offset < batch.matrices.Count; offset += drawMatrices.Length)
                    {
                        int count = Mathf.Min(drawMatrices.Length, batch.matrices.Count - offset);
                        for (int i = 0; i < count; i++) drawMatrices[i] = batch.matrices[i+offset] * part.matrix;
                        for (int slot = 0; slot < Mathf.Min(part.materials.Length,part.mesh.subMeshCount); slot++)
                        {
                            var render = new RenderParams(part.materials[slot]) {camera = camera, worldBounds = batch.bounds,
                                shadowCastingMode = near ? ShadowCastingMode.On : ShadowCastingMode.Off,
                                lightProbeUsage = LightProbeUsage.Off};
                            Graphics.RenderMeshInstanced(render, part.mesh, slot, drawMatrices, count);
                        }
                    }
                }
            }
        }

        void OnDestroy()
        {
            TerrainCallbacks.heightmapChanged -= OnHeightmapChanged;
            if (presentation != null) Destroy(presentation.gameObject);
            foreach (var item in owned) if (item != null) Destroy(item);
        }
    }
}
