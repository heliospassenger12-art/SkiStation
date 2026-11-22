using System;
using System.Collections.Generic;
using SkiStation.Arcade;
using UnityEngine;
using UnityEngine.Rendering;

namespace SkiStation.TerrainGeneration
{
    // Cartographic scenery is rebuilt from the domain resource. The native terrain
    // remains the owner of heights, collisions and checkpoint state.
    internal sealed class RealTerrainNature : MonoBehaviour
    {
        const float TileSize = 384f;
        const int BushKind = 8;
        const int HerbKind = 9;

        [Serializable] sealed class Map
        {
            public int version;
            public string domain;
            public float west, south, size, sizeX, sizeZ;
            public string focusWater;
            public bool pureNaturePalette;
            public Surface[] waters, streams;
            public string rockMask;
            public int rockAreas, rockResolution, forestResolution;
            public string forestMask;
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
            [NonSerialized] public bool estaris;
            [NonSerialized] public int groundVersion = -1;
            [NonSerialized] public float groundHeight;
        }

        sealed class Part
        {
            public Mesh mesh;
            public Material[] materials;
            public Matrix4x4 matrix;
        }

        sealed class Model
        {
            public Part[] near, middle, far;
            public bool groundCover, seasonal;
        }

        sealed class Batch
        {
            public Model model;
            public Bounds bounds;
            public Vector3 tileCenter;
            public readonly List<Matrix4x4> matrices = new List<Matrix4x4>();
        }

        readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        readonly Dictionary<Material, Material> materialCopies = new Dictionary<Material, Material>();
        readonly Dictionary<(int x, int z, int kind, int variant), Batch> batches =
            new Dictionary<(int x, int z, int kind, int variant), Batch>();
        readonly Dictionary<(int x, int z, int kind, int variant), Batch> overviewBatches =
            new Dictionary<(int x, int z, int kind, int variant), Batch>();
        readonly List<(Mesh mesh, Vector2[] points, int kind)> surfaceMeshes =
            new List<(Mesh mesh, Vector2[] points, int kind)>();
        readonly List<(Vector3 a, Vector3 b, float radius)> corridors =
            new List<(Vector3 a, Vector3 b, float radius)>();
        readonly Matrix4x4[] drawMatrices = new Matrix4x4[511];
        TerrainGenerationWorld world;
        Map data;
        Model[] models;
        Model[][] variants;
        Visuals.VisualProfile terrainProfile;
        Transform presentation;
        Transform estarisRocks;
        TerrainCollider groundCollider;
        byte[] rockCover, forestCover;
        bool useDistanceCulling;
        float nextCheck;
        int infrastructureHash;
        int terrainHeightVersion = 1;
        bool heightChanged;
        internal int SurfaceCount { get; private set; }
        internal int InstanceCount { get; private set; }
        internal int RockPaintResolution { get; private set; }
        internal int ForestPaintResolution { get; private set; }
        internal float ForestCoverage(int x, int z) => forestCover == null ? 0 : forestCover[z * ForestPaintResolution + x] / 255f;
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
            var vegetationAsset = Resources.Load<TextAsset>(resource + "Vegetation");
            var vegetation = vegetationAsset != null ? JsonUtility.FromJson<Map>(vegetationAsset.text) : null;
            if (Matches(world, vegetation))
            {
                map.pureNaturePalette = vegetation.pureNaturePalette;
                map.forestResolution = vegetation.forestResolution;
                map.forestMask = vegetation.forestMask;
                map.trees = MergePlacements(map.trees, vegetation.trees);
                map.rocks = MergePlacements(map.rocks, vegetation.rocks);
            }
            var geologyAsset = Resources.Load<TextAsset>(resource + "Geology");
            var geology = geologyAsset != null ? JsonUtility.FromJson<Map>(geologyAsset.text) : null;
            if (Matches(world, geology))
            {
                map.rockMask = geology.rockMask;
                map.rockResolution = geology.rockResolution;
                map.rockAreas = geology.rockAreas;
                map.rocks = MergePlacements(map.rocks, geology.rocks);
            }

            var controller = world.gameObject.AddComponent<RealTerrainNature>();
            try { controller.Build(world, map); }
            catch (Exception e)
            {
                Debug.LogException(e, world);
                Destroy(controller);
            }
        }

        static Placement[] MergePlacements(Placement[] current, Placement[] extra)
        {
            if (current == null || current.Length == 0) return extra ?? Array.Empty<Placement>();
            if (extra == null || extra.Length == 0) return current;
            var all = new Placement[current.Length + extra.Length];
            current.CopyTo(all, 0);
            extra.CopyTo(all, current.Length);
            return all;
        }

        static bool Matches(TerrainGenerationWorld world, Map map)
        {
            var terrain = world.Recipe;
            return map != null && map.version == 1 && map.domain == world.domainId &&
                Mathf.Approximately(map.west, terrain.originX) && Mathf.Approximately(map.south, terrain.originZ) &&
                Mathf.Approximately(map.sizeX > 0 ? map.sizeX : map.size, terrain.sizeX) &&
                Mathf.Approximately(map.sizeZ > 0 ? map.sizeZ : map.size, terrain.sizeZ);
        }

        internal string FocusWater => data.focusWater;
        internal bool HasRockSource => data.rockAreas > 0;

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
            useDistanceCulling = data.pureNaturePalette && Mathf.Max(world.Recipe.sizeX, world.Recipe.sizeZ) > 12000f;
            bool isOrcieres = world.domainId == ValAlpin.ValAlpinScene.OrcieresDomainId;
            if (isOrcieres && string.IsNullOrEmpty(data.focusWater)) data.focusWater = "Grand Lac des Estaris";
            if (isOrcieres) data.rocks = MergePlacements(data.rocks, BuildHighOutcrops());
            if (isOrcieres) data.rocks = MergePlacements(data.rocks, BuildEstarisOutcrops());
            groundCollider = world.Terrain.GetComponent<TerrainCollider>();
            // Terrain descendants are already excluded by ManagementTerrainCheckpointBridge.
            presentation = new GameObject("Décor géographique — " + (isOrcieres ? "Orcières" : ValAlpin.ValAlpinScene.MapName)).transform;
            presentation.SetParent(world.Terrain.transform, false);
            var palette = data.trees.Length == 0 && data.rocks.Length == 0 ? null
                : Resources.Load<ValAlpin.ValAlpinNatureAssets>("VisualProfiles/ValAlpinNature");
            var lit = palette != null && palette.litShader != null ? palette.litShader : Shader.Find("Universal Render Pipeline/Lit");
            var water = Material(lit, "Lacs et retenues", new Color(.075f, .31f, .43f), .63f);
            var stream = Material(lit, "Cours d’eau", new Color(.20f, .46f, .54f), .48f);
            var hydro = world.gameObject.AddComponent<RealTerrainWater>();
            foreach (var body in data.waters) hydro.AddSurface(body.vertices, body.triangles, true);
            foreach (var body in data.streams) hydro.AddSurface(body.vertices, body.triangles, false);
            BuildSurfaces(data.waters, water, 0);
            BuildSurfaces(data.streams, stream, 1);

            models = Array.Empty<Model>();
            if (data.trees.Length != 0 || data.rocks.Length != 0)
            {
                var pine = Tree(lit, false);
                var deciduous = Tree(lit, true);
                var fallbackRock = Rock(lit);
                if (data.pureNaturePalette || isOrcieres)
                {
                    bool mappedSpecies = Array.Exists(data.trees, tree => tree.kind == 4 || tree.kind == 5);
                    variants = new[]
                    {
                        ConiferModels(palette?.trees, pine, lit, mappedSpecies ? "Spruce" : null),
                        new[] { deciduous },
                        RockModels(palette?.boulders, fallbackRock, lit),
                        RockModels(palette?.cliffs, fallbackRock, lit),
                        ConiferModels(palette?.trees, pine, lit, "Pine"),
                        ConiferModels(palette?.trees, pine, lit, "Fir"),
                        RockModels(palette?.stones, fallbackRock, lit),
                        RockModels(palette?.pebbles, fallbackRock, lit),
                        UnderstoryModels(palette?.profile?.vegetation, lit, true),
                        UnderstoryModels(palette?.profile?.vegetation, lit, false)
                    };
                    models = Array.ConvertAll(variants, group => group.Length != 0 ? group[0] : null);
                    if (palette?.trees?.Length > 0)
                        data.trees = MergePlacements(data.trees, FillForestGaps(hydro));
                    data.trees = MergePlacements(data.trees, AddUnderstory(hydro));
                }
                else
                {
                    models = new[]
                    {
                        ModelFor(PrefabAt(palette?.trees, 6), pine, lit),
                        deciduous,
                        ModelFor(PrefabAt(palette?.boulders, 0), fallbackRock, lit),
                        ModelFor(PrefabAt(palette?.cliffs, 2), fallbackRock, lit)
                    };
                }
                infrastructureHash = CaptureCorridors();
                RefreshInstances();
            }
            if (!string.IsNullOrEmpty(data.rockMask) && data.rockResolution == 512)
            {
                var mask = Convert.FromBase64String(data.rockMask);
                if (mask.Length == data.rockResolution * data.rockResolution)
                {
                    rockCover = mask;
                    RockPaintResolution = data.rockResolution;
                    SurfaceCount += data.rockAreas;
                }
            }
            if (!string.IsNullOrEmpty(data.forestMask) && data.forestResolution == 512)
            {
                byte[] mask = Convert.FromBase64String(data.forestMask);
                if (mask.Length == data.forestResolution * data.forestResolution)
                {
                    forestCover = mask;
                    ForestPaintResolution = data.forestResolution;
                }
            }
            var appearance = world.Terrain.GetComponent<TerrainGeneratedAppearance>();
            bool naturalSurfaces = palette?.profile?.terrain != null && palette.profile.terrain.useNaturalSnowLayers;
            if (forestCover != null || naturalSurfaces) appearance.EnableSeasonalSnow();
            if ((data.pureNaturePalette || isOrcieres || naturalSurfaces) && palette != null && palette.rockAlbedo != null &&
                Visuals.VisualSafety.PipelineMatches(Visuals.VisualPipeline.Universal))
            {
                RegisterTerrainProfile(palette, isOrcieres);
                string profile = Visuals.DomainVisualPreferences.Get(world.domainId, terrainProfile.profileId);
                appearance.Select(profile);
                if (data.pureNaturePalette && profile == terrainProfile.profileId && RenderSettings.sun != null)
                {
                    RenderSettings.sun.color = new Color(1f, .96f, .91f);
                    RenderSettings.sun.intensity = .85f;
                    RenderSettings.sun.transform.rotation = Quaternion.Euler(36f, -53f, 0f);
                }
            }
            else if (rockCover != null || forestCover != null) appearance.RefreshClimate();
            Vector3? center = !string.IsNullOrEmpty(FocusWater) && TryGetWater(FocusWater,out var lake) ? lake : null;
            hydro.Bind(world,water,stream,presentation,center,isOrcieres,useFrostMaterial: forestCover != null);
            world.gameObject.AddComponent<WaterCrossingTool>();
            TerrainCallbacks.heightmapChanged += OnHeightmapChanged;
        }

        Placement[] BuildHighOutcrops()
        {
            var result = new List<Placement>();
            var terrain = world.Terrain.terrainData;
            var origin = world.Terrain.transform.position;
            var size = terrain.size;
            var reference = Altitude.WorldAltitudeService.Reference(world);
            const float cell = 57f;
            for (float z = cell; z < size.z - cell; z += cell)
            for (float x = cell; x < size.x - cell; x += cell)
            {
                float u = (x + (Mathf.PerlinNoise(x*.016f,z*.014f)-.5f)*39f) / size.x;
                float v = (z + (Mathf.PerlinNoise(z*.021f,x*.011f)-.5f)*39f) / size.z;
                float slope = terrain.GetSteepness(u,v);
                if (slope < 32f || slope > 67f) continue;
                float altitude = reference.ToMeters(origin.y + terrain.GetInterpolatedHeight(u,v));
                if (altitude < 2350f) continue;
                var normal = terrain.GetInterpolatedNormal(u,v);
                float cluster = Mathf.PerlinNoise(x*.0052f + 12f,z*.0052f + 31f);
                float exposed = Mathf.InverseLerp(32f,52f,slope)*.22f +
                    Mathf.InverseLerp(2350f,2750f,altitude)*.13f;
                if (cluster + exposed < .78f) continue;
                bool ledge = slope >= 43f && cluster >= .52f;
                float fractures = Mathf.PerlinNoise(x * .013f + 6f, z * .011f + 46f);
                bool scree = !ledge && fractures < .48f;
                float width = ledge ? Mathf.Lerp(9f, 27f, fractures) :
                    scree ? Mathf.Lerp(1.5f, 4f, cluster) : Mathf.Lerp(3f, 8f, cluster);
                result.Add(new Placement
                {
                    at = new Vector2(origin.x + size.x*u, origin.z + size.z*v),
                    kind = ledge ? 3 : scree ? 6 : 2,
                    yaw = Mathf.Atan2(-normal.x,-normal.z)*Mathf.Rad2Deg + (cluster-.5f)*65f,
                    height = width * (ledge ? Mathf.Lerp(.32f, .65f, fractures) : scree ? .43f : .64f),
                    width = width
                });
            }
            return result.ToArray();
        }

        internal static float EstarisInfluence(float x, float z)
        {
            float fromBasin = new Vector2((x - 1680f) / 780f, (z - 1480f) / 820f).magnitude;
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.48f, 1f, fromBasin));
        }

        Placement[] BuildEstarisOutcrops()
        {
            var placements = new List<Placement>();
            var terrain = world.Terrain.terrainData;
            var origin = world.Terrain.transform.position;
            var size = terrain.size;
            for (float z = 710f; z < 2290f; z += 43f)
            for (float x = 915f; x < 2450f; x += 43f)
            {
                float px = x + (Mathf.PerlinNoise(x*.025f,z*.031f)-.5f)*34f;
                float pz = z + (Mathf.PerlinNoise(z*.017f,x*.024f)-.5f)*34f;
                float influence = EstarisInfluence(px,pz);
                float u = (px-origin.x)/size.x, v = (pz-origin.z)/size.z;
                if (influence < .16f || u <= 0f || u >= 1f || v <= 0f || v >= 1f) continue;
                float slope = terrain.GetSteepness(u,v);
                float geology = Mathf.PerlinNoise(px*.009f+11f,pz*.013f+19f);
                if (slope < 30f || slope > 59f || geology + influence*.24f < .78f) continue;
                var normal = terrain.GetInterpolatedNormal(u,v);
                float joints = Mathf.PerlinNoise(px * .022f + 26f, pz * .019f + 12f);
                float yaw = Mathf.Atan2(-normal.x,-normal.z)*Mathf.Rad2Deg + (joints - .5f) * 28f;
                float width = Mathf.Lerp(12f,30f,joints);
                var ledge = new Vector2(px,pz);
                placements.Add(new Placement { at = ledge, kind = 3, width = width,
                    height = width*Mathf.Lerp(.37f,.62f,geology), yaw = yaw, estaris = true });
                var down = new Vector2(normal.x,normal.z).normalized;
                var side = new Vector2(-down.y,down.x);
                for (int i=0; i<3; i++)
                {
                    float scale = 2.2f + geology*3f - i*.6f;
                    var point = ledge + down*(13f+i*5f) + side*((i-1)*6f);
                    placements.Add(new Placement { at = point, kind = i == 0 ? 2 : 6,
                        width = scale, height = scale*(i == 0 ? .62f : .45f), yaw = yaw+i*43f, estaris=true });
                    float pebble = .7f + joints * .75f;
                    placements.Add(new Placement { at = point + side * (2f + joints), kind = 7,
                        width = pebble, height = pebble*.37f, yaw = yaw+i*67f, estaris=true });
                }
            }
            return placements.ToArray();
        }

        void RegisterTerrainProfile(ValAlpin.ValAlpinNatureAssets palette, bool isOrcieres)
        {
            var rock = new TerrainLayer
            {
                diffuseTexture = palette.rockAlbedo,
                normalMapTexture = palette.rockNormal,
                tileSize = new Vector2(26f, 26f),
                normalScale = isOrcieres ? .65f : .85f,
                diffuseRemapMax = isOrcieres ? new Color(.35f, .37f, .39f, 1f) : new Color(.43f, .41f, .40f, 1f),
                smoothness = .16f
            };
            var snow = ValAlpin.ValAlpinNatureSurface.CreateSoftSnow(fineDetail: forestCover != null || isOrcieres,
                brightSnow: isOrcieres);
            var snowNormal = isOrcieres ? ValAlpin.ValAlpinNatureSurface.CreateWindSnowNormal() : null;
            var set = ScriptableObject.CreateInstance<Visuals.TerrainVisualSet>();
            set.pipeline = Visuals.VisualPipeline.Universal;
            var mountainSurfaces = palette.profile?.terrain;
            set.useNaturalSnowLayers = mountainSurfaces != null && mountainSurfaces.useNaturalSnowLayers;
            var layers = new List<Visuals.TerrainVisualLayer>
            {
                new Visuals.TerrainVisualLayer { category = "SNOW", albedo = snow, normal = snowNormal,
                    tiling = Vector2.one * (isOrcieres || forestCover != null ? 16f : 55f), smoothness = .07f },
                new Visuals.TerrainVisualLayer { category = "ROCK", terrainLayer = rock }
            };
            if (set.useNaturalSnowLayers)
            {
                foreach (var entry in mountainSurfaces.layers)
                    if (entry != null && !layers.Exists(layer => layer.category == entry.category))
                        layers.Add(entry);
            }
            if (forestCover != null)
            {
                // Readable woodland from overview without drawing individual forest meshes at that distance.
                var grass = layers.Find(layer => layer.category == "ALPINE_GROUND")?.terrainLayer;
                var woodland = new TerrainLayer
                {
                    diffuseTexture = grass != null ? grass.diffuseTexture : snow, tileSize = new Vector2(64f,64f),
                    diffuseRemapMax = new Color(.43f,.54f,.49f,1f), smoothness = .06f
                };
                layers.Add(new Visuals.TerrainVisualLayer { category = "WOODLAND", terrainLayer = woodland });
                owned.Add(woodland);
            }
            set.layers = layers.ToArray();
            terrainProfile = ScriptableObject.CreateInstance<Visuals.VisualProfile>();
            terrainProfile.profileId = isOrcieres ? "ORCIERES_NEIGE" : world.domainId == ValAlpin.ValAlpinScene.AuronDomainId
                ? "AURON_PURE_NATURE" : world.domainId + ".pure-nature";
            terrainProfile.inheritNativeRendering = false;
            terrainProfile.terrain = set;
            Visuals.VisualProfileRegistry.Register(terrainProfile);
            owned.Add(rock);
            owned.Add(snow);
            if (snowNormal != null) owned.Add(snowNormal);
            owned.Add(set);
            owned.Add(terrainProfile);
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
            if (shader == "BK/Grass")
            {
                result = new Material(source) {enableInstancing = true};
                if (!Visuals.VisualSafety.Usable(source, Visuals.VisualPipeline.Universal))
                    ValAlpin.ValAlpinNatureMaterials.Configure(result, source, null, null, null);
                owned.Add(result);
                materialCopies.Add(source, result);
                return result;
            }
            bool leaves = shader.Contains("Leaves", StringComparison.Ordinal);
            Color tint = data.pureNaturePalette
                ? (leaves ? new Color(.69f, .78f, .71f) : new Color(.76f, .75f, .72f))
                : (leaves ? new Color(.46f, .59f, .51f) : new Color(.72f, .74f, .76f));
            result = Material(lit, source.name + " · relief", tint, .09f);
            string[] albedo = data.pureNaturePalette || leaves ? new[] { "_Diffuse", "_BaseMap", "_MainTex", "_Albedo" }
                : new[] { "_BaseMap", "_MainTex", "_Albedo" };
            foreach (string property in albedo)
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
            if (data.pureNaturePalette || leaves)
            {
                foreach (string property in new[] { "_Normal", "_BumpMap" })
                {
                    if (!source.HasProperty(property) || source.GetTexture(property) == null) continue;
                    result.SetTexture("_BumpMap", source.GetTexture(property));
                    result.SetFloat("_BumpScale", leaves ? .6f : .9f);
                    result.EnableKeyword("_NORMALMAP");
                    break;
                }
            }
            materialCopies.Add(source, result);
            return result;
        }

        static GameObject PrefabAt(GameObject[] entries, int index) =>
            entries != null && index >= 0 && index < entries.Length ? entries[index] : null;

        Model[] ConiferModels(GameObject[] prefabs, Model fallback, Shader lit, string family)
        {
            if (prefabs == null) return new[] { fallback };
            var models = new List<Model>();
            foreach (var prefab in prefabs)
            {
                if (prefab == null || (family != null && !prefab.name.StartsWith(family, StringComparison.OrdinalIgnoreCase)))
                    continue;
                models.Add(ModelFor(prefab, fallback, lit, includeTreeLod: true));
            }
            return models.Count == 0 ? new[] { fallback } : models.ToArray();
        }

        Model[] RockModels(GameObject[] prefabs, Model fallback, Shader lit)
        {
            if (prefabs == null || prefabs.Length == 0) return new[] { fallback };
            var results = new Model[prefabs.Length];
            for (int i = 0; i < prefabs.Length; i++)
            {
                var prefab = prefabs[i];
                var model = results[i] = ModelFor(prefab, fallback, lit);
                if (model == fallback || !prefab.name.StartsWith("Cliff", StringComparison.Ordinal)) continue;
                // Full vendor meshes are reserved for near terrain. Keep the broad silhouette
                // varied in distant management views with a small geometry budget per cliff.
                float shoulder = .28f + (i % 4) * .055f;
                float top = .16f + (i % 3) * .08f;
                model.far = new[] { RingModel(fallback.far[0].materials[0], 5 + i % 4, new[]
                {
                    new Vector2(.41f, 0f), new Vector2(.49f, .26f), new Vector2(shoulder, .65f),
                    new Vector2(top, .94f), new Vector2(.06f, 1f)
                }) };
            }
            return results;
        }

        Model ModelFor(GameObject prefab, Model fallback, Shader lit, bool includeTreeLod = false)
        {
            if (prefab == null) return fallback;
            var group = prefab.GetComponentInChildren<LODGroup>(true);
            var lods = group != null ? group.GetLODs() : Array.Empty<LOD>();
            var renderers = lods.Length > 0 ? lods[0].renderers : prefab.GetComponentsInChildren<MeshRenderer>(true);
            var parts = ReadParts(prefab, renderers, lit, out var bounds);
            if (parts.Length == 0 || bounds.size.x <= 0 || bounds.size.y <= 0 || bounds.size.z <= 0) return fallback;
            Matrix4x4 normalize = Matrix4x4.Scale(new Vector3(1f / bounds.size.x, 1f / bounds.size.y, 1f / bounds.size.z)) *
                Matrix4x4.Translate(-new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
            foreach (var part in parts) part.matrix = normalize * part.matrix;
            var model = new Model {near = parts, far = fallback?.far ?? parts};
            if (includeTreeLod && lods.Length > 1)
            {
                var middle = ReadParts(prefab, lods[1].renderers, lit, out _);
                foreach (var part in middle) part.matrix = normalize * part.matrix;
                if (middle.Length != 0) model.middle = middle;
            }
            return model;
        }

        Part[] ReadParts(GameObject prefab, Renderer[] renderers, Shader lit, out Bounds bounds)
        {
            var parts = new List<Part>();
            bounds = default;
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
            return parts.ToArray();
        }

        Model[] UnderstoryModels(Visuals.VegetationVisualSet vegetation, Shader lit, bool bushes)
        {
            var models = new List<Model>();
            foreach (var variant in vegetation?.variants ?? Array.Empty<Visuals.VisualVariant>())
            {
                var prefab = variant?.sourcePrefab;
                if (prefab == null || prefab.name.StartsWith("Bush", StringComparison.OrdinalIgnoreCase) != bushes) continue;
                // Reeds are reserved for shorelines rather than sprinkled into forests.
                if (prefab.name.Equals("Reeds", StringComparison.OrdinalIgnoreCase)) continue;
                var model = ModelFor(prefab, null, lit);
                if (model == null) continue;
                model.groundCover = true;
                model.seasonal = !bushes;
                models.Add(model);
            }
            return models.ToArray();
        }

        Placement[] FillForestGaps(RealTerrainWater water)
        {
            // Strengthen existing woods while keeping new trees close to the mapped canopy.
            int target = Mathf.Min(58000, Mathf.RoundToInt(data.trees.Length * 3.4f));
            var result = new List<Placement>(target);
            if (target == 0) return result.ToArray();
            var occupied = new HashSet<Vector2Int>();
            var woods = new Dictionary<Vector2Int, int>();
            var random = new System.Random(world.Recipe.seed ^ 438919);
            Vector2Int Cell(Vector2 p) => new Vector2Int(Mathf.FloorToInt(p.x / 3.2f), Mathf.FloorToInt(p.y / 3.2f));
            Vector2Int Wood(Vector2 p) => new Vector2Int(Mathf.FloorToInt(p.x / 60f), Mathf.FloorToInt(p.y / 60f));
            foreach (var tree in data.trees)
            {
                occupied.Add(Cell(tree.at));
                var wood = Wood(tree.at);
                woods.TryGetValue(wood, out int count);
                woods[wood] = count + 1;
            }
            for (int attempt = 0; attempt < target * 7 && result.Count < target; attempt++)
            {
                int source = random.Next(data.trees.Length + result.Count);
                var tree = source < data.trees.Length ? data.trees[source] : result[source - data.trees.Length];
                if (tree.kind != 0 && tree.kind != 4 && tree.kind != 5) continue;
                if (woods[Wood(tree.at)] < 3) continue;
                float angle = (float)random.NextDouble() * (2f * Mathf.PI);
                float distance = Mathf.Lerp(3.4f, 9f, (float)random.NextDouble());
                var at = tree.at + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                if (occupied.Contains(Cell(at)) || !world.Contains(at.x, at.y)) continue;
                if (!woods.TryGetValue(Wood(at), out int canopy) || canopy < 2) continue;
                var ground = Ground(at);
                if (Mathf.Abs(ground.y - Ground(tree.at).y) > distance * .55f) continue;
                var terrain = Visuals.TerrainVisualBridge.Sample(world, ground);
                if (terrain.forestDensity < .1f || terrain.glacier || terrain.permanentSnow || water.Intersects(ground, ground, 2f)) continue;
                float height = Mathf.Clamp(tree.height * Mathf.Lerp(.84f, 1.1f, (float)random.NextDouble()), 7f, 19f);
                result.Add(new Placement {at = at, kind = tree.kind, yaw = angle * Mathf.Rad2Deg,
                    height = height, width = height * .46f});
                occupied.Add(Cell(at));
            }
            return result.ToArray();
        }

        Placement[] AddUnderstory(RealTerrainWater water)
        {
            var placements = new List<Placement>();
            if (data.trees.Length == 0 || (models[BushKind] == null && models[HerbKind] == null)) return placements.ToArray();
            var occupied = new HashSet<Vector2Int>();
            int target = Mathf.Min(2700, Mathf.RoundToInt(data.trees.Length * .13f));
            var random = new System.Random(world.Recipe.seed ^ 748821);
            for (int i = 0; i < target * 3 && placements.Count < target; i++)
            {
                var tree = data.trees[random.Next(data.trees.Length)];
                var at = tree.at + new Vector2((float)random.NextDouble() * 22f - 11f, (float)random.NextDouble() * 22f - 11f);
                if (!world.Contains(at.x, at.y)) continue;
                var cell = new Vector2Int(Mathf.FloorToInt(at.x / 13f), Mathf.FloorToInt(at.y / 13f));
                if (!occupied.Add(cell)) continue;
                Vector3 ground = Ground(at);
                var sample = Visuals.TerrainVisualBridge.Sample(world, ground);
                if (sample.forestDensity < .1f || sample.slope > 30f || sample.glacier || sample.permanentSnow ||
                    water.Intersects(ground, ground, 1f)) continue;
                int kind = (random.NextDouble() < .42 && models[BushKind] != null) || models[HerbKind] == null ? BushKind : HerbKind;
                float height = kind == BushKind ? Mathf.Lerp(.65f, 1.45f, (float)random.NextDouble()) : Mathf.Lerp(.2f, .58f, (float)random.NextDouble());
                placements.Add(new Placement {at=at, kind=kind, height=height, width=height*1.65f,
                    yaw=(float)random.NextDouble()*360f});
            }
            return placements.ToArray();
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
            if (world == null || terrain != world.Terrain) return;
            if (!heightChanged) terrainHeightVersion++;
            heightChanged = true;
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
            foreach (var bridge in FindObjectsByType<WaterCrossing>(FindObjectsSortMode.InstanceID))
            {
                if (bridge.gameObject.scene != scene) continue;
                hash = HashCode.Combine(hash, bridge.GetInstanceID(), bridge.StartBank, bridge.EndBank);
                corridors.Add((bridge.StartBank, bridge.EndBank, bridge.HalfWidth + 3f));
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
            if (estarisRocks != null)
            {
                estarisRocks.gameObject.SetActive(false);
                Destroy(estarisRocks.gameObject);
                estarisRocks = null;
            }
            batches.Clear();
            overviewBatches.Clear();
            InstanceCount = 0;
            Append(data.trees);
            Append(data.rocks);
        }

        void Append(Placement[] placements)
        {
            foreach (var item in placements)
            {
                if (item.kind < 0 || item.kind >= models.Length || models[item.kind] == null) continue;
                Vector3 position = Ground(item.at);
                float width = item.width > 0 ? item.width : item.height * (item.kind == 0 ? .46f : .62f);
                float depth = item.kind == 3 ? (data.pureNaturePalette ? width * .69f : Mathf.Min(14, width * .55f)) : width;
                bool blocked = false;
                foreach (var (a,b,radius) in corridors)
                {
                    float limit = radius + width*.5f;
                    if (position.x < Mathf.Min(a.x,b.x)-limit || position.x > Mathf.Max(a.x,b.x)+limit ||
                        position.z < Mathf.Min(a.z,b.z)-limit || position.z > Mathf.Max(a.z,b.z)+limit) continue;
                    if (ConstructionRules349.SegmentDistanceXZ(position,a,b) <= limit) { blocked = true; break; }
                }
                if (blocked) continue;
                bool tree = item.kind == 0 || item.kind == 1 || item.kind == 4 || item.kind == 5;
                if ((useDistanceCulling || tree) && item.kind != 3)
                {
                    if (item.groundVersion != terrainHeightVersion)
                    {
                        item.groundHeight = groundCollider.Raycast(new Ray(position + Vector3.up * 200f, Vector3.down),
                            out var hit, 400f) ? hit.point.y : position.y;
                        if (tree)
                        {
                            // Embed the entire root flare, using the downhill side of the trunk on steep grades.
                            float radius = Mathf.Clamp(width * .22f, .4f, 2f);
                            for (int i = 0; i < 8; i++)
                            {
                                float angle = i * (Mathf.PI / 4f);
                                var root = item.at + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                                item.groundHeight = Mathf.Min(item.groundHeight, Ground(root).y);
                            }
                        }
                        item.groundVersion = terrainHeightVersion;
                    }
                    position.y = item.groundHeight - (tree ? Mathf.Clamp(item.height * .095f, .65f, 1.8f) : item.kind == 7 ? item.height * .12f :
                        Mathf.Clamp(item.height * .055f, item.kind >= BushKind ? .02f : .28f, .85f));
                }
                if (item.kind == 3)
                {
                    var downslope = Quaternion.Euler(0,item.yaw,0) * Vector3.back;
                    var toe = item.at + new Vector2(downslope.x,downslope.z) * (depth * .32f);
                    position.y = Mathf.Min(position.y, Ground(toe).y) - (data.pureNaturePalette ? item.height * .21f : 1f);
                }
                var choices = variants?[item.kind];
                int variant = choices == null ? 0 : PlacementVariant(item, choices.Length);
                if (item.estaris && item.kind != 7)
                {
                    var model = choices == null ? models[item.kind] : choices[variant];
                    PlaceEstarisRock(model, position, item.yaw, new Vector3(width,item.height,depth));
                    InstanceCount++;
                    continue;
                }
                int x = Mathf.FloorToInt(item.at.x / TileSize), z = Mathf.FloorToInt(item.at.y / TileSize);
                var key = (x,z,item.kind,variant);
                if (!batches.TryGetValue(key, out var batch))
                {
                    batch = new Batch {model = choices == null ? models[item.kind] : choices[variant],
                        bounds = new Bounds(position, Vector3.zero),
                        tileCenter = useDistanceCulling ? Ground(new Vector2((x+.5f)*TileSize,(z+.5f)*TileSize)) : default};
                    batches.Add(key,batch);
                }
                float radiusBounds = Mathf.Max(width, depth);
                var matrix = Matrix4x4.TRS(position, Quaternion.Euler(0,item.yaw,0), new Vector3(width,item.height,depth));
                AddInstance(batch, matrix, position, item.height, radiusBounds);
                if (useDistanceCulling && item.kind < BushKind)
                {
                    var overviewKey = (x,z,item.kind,item.kind == 3 ? variant : 0);
                    if (!overviewBatches.TryGetValue(overviewKey, out var overview))
                    {
                        overview = new Batch {model = item.kind == 3 ? batch.model : models[item.kind],
                            bounds = new Bounds(position,Vector3.zero),
                            tileCenter = batch.tileCenter};
                        overviewBatches.Add(overviewKey,overview);
                    }
                    AddInstance(overview, matrix, position, item.height, radiusBounds);
                }
                InstanceCount++;
            }
        }

        static int PlacementVariant(Placement item, int count)
        {
            if (count <= 1) return 0;
            // Rock bars share an aspect. Mix position as well as yaw so parallel faces can
            // use different meshes even where the terrain normal is nearly constant.
            if (item.kind == 2 || item.kind == 3 || item.kind == 6 || item.kind == 7)
            {
                unchecked
                {
                    uint x = (uint)Mathf.RoundToInt(item.at.x * 4f);
                    uint z = (uint)Mathf.RoundToInt(item.at.y * 4f);
                    uint value = x * 0x9e3779b9u ^ z * 0x85ebca6bu ^ (uint)item.kind * 0xc2b2ae35u;
                    value ^= value >> 16;
                    return (int)(value % (uint)count);
                }
            }
            return Mathf.FloorToInt(Mathf.Abs(item.yaw * 11f)) % count;
        }

        void PlaceEstarisRock(Model model, Vector3 position, float yaw, Vector3 scale)
        {
            if (estarisRocks == null)
            {
                estarisRocks = new GameObject("Estaris — barres et éboulis").transform;
                estarisRocks.SetParent(presentation, false);
            }
            var anchor = new GameObject("Affleurement").transform;
            anchor.SetParent(estarisRocks, false);
            anchor.SetPositionAndRotation(position, Quaternion.Euler(0f,yaw,0f));
            anchor.localScale = scale;
            foreach (var part in model.near)
            {
                var rock = new GameObject("Roche");
                rock.transform.SetParent(anchor,false);
                rock.transform.SetLocalPositionAndRotation(part.matrix.GetColumn(3),part.matrix.rotation);
                rock.transform.localScale = part.matrix.lossyScale;
                rock.AddComponent<MeshFilter>().sharedMesh = part.mesh;
                rock.AddComponent<MeshRenderer>().sharedMaterials = part.materials;
                rock.AddComponent<ArcadeSolidRock>();
                rock.AddComponent<MeshCollider>().sharedMesh = part.mesh;
            }
        }

        static void AddInstance(Batch batch, Matrix4x4 matrix, Vector3 position, float height, float radius)
        {
            batch.bounds.Encapsulate(position + new Vector3(radius,height,radius));
            batch.bounds.Encapsulate(position - new Vector3(radius,1f,radius));
            batch.matrices.Add(matrix);
        }

        void LateUpdate()
        {
            if (world == null || models == null || !world.IsReady || !presentation.gameObject.activeInHierarchy ||
                world.CheckpointTerrainBusy) return;
            if (Time.unscaledTime >= nextCheck)
            {
                nextCheck = Time.unscaledTime + .55f;
                if (models.Length != 0)
                {
                    int hash = CaptureCorridors();
                    if (hash != infrastructureHash || heightChanged) RefreshInstances();
                    infrastructureHash = hash;
                }
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
            var eye = camera.transform.position;
            if (useDistanceCulling)
            {
                const float detail = 620f, distance = 3800f;
                foreach (var batch in batches.Values)
                {
                    float range = (batch.tileCenter - eye).sqrMagnitude;
                    if (range < detail*detail && GroundCoverVisible(batch, eye) && GeometryUtility.TestPlanesAABB(view,batch.bounds))
                    {
                        bool close = batch.bounds.SqrDistance(eye) < 340f*340f;
                        DrawBatch(camera, batch, close ? batch.model.near : batch.model.middle ?? batch.model.near, close);
                    }
                }
                foreach (var batch in overviewBatches.Values)
                {
                    float range = (batch.tileCenter - eye).sqrMagnitude;
                    if (range >= detail*detail && range < distance*distance && GeometryUtility.TestPlanesAABB(view,batch.bounds))
                        DrawBatch(camera, batch, batch.model.far, false);
                }
                return;
            }
            foreach (var batch in batches.Values)
            {
                if (!GroundCoverVisible(batch, eye) || !GeometryUtility.TestPlanesAABB(view, batch.bounds)) continue;
                float range = batch.bounds.SqrDistance(eye);
                bool near = range < 850f*850f;
                var parts = !near ? batch.model.far : range < 340f*340f ? batch.model.near : batch.model.middle ?? batch.model.near;
                DrawBatch(camera, batch, parts, near && (batch.model.middle == null || range < 340f*340f));
            }
        }

        static bool GroundCoverVisible(Batch batch, Vector3 eye) => !batch.model.groundCover ||
            ((!batch.model.seasonal || RealTerrainWater.Season != AlpineSeason.Winter) && batch.bounds.SqrDistance(eye) < 340f*340f);

        void DrawBatch(Camera camera, Batch batch, Part[] parts, bool shadows)
        {
            foreach (var part in parts)
            for (int offset = 0; offset < batch.matrices.Count; offset += drawMatrices.Length)
            {
                int count = Mathf.Min(drawMatrices.Length, batch.matrices.Count - offset);
                for (int i = 0; i < count; i++) drawMatrices[i] = batch.matrices[i+offset] * part.matrix;
                for (int slot = 0; slot < Mathf.Min(part.materials.Length,part.mesh.subMeshCount); slot++)
                {
                    var render = new RenderParams(part.materials[slot]) {camera = camera, worldBounds = batch.bounds,
                        shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                        lightProbeUsage = LightProbeUsage.Off};
                    Graphics.RenderMeshInstanced(render, part.mesh, slot, drawMatrices, count);
                }
            }
        }

        void OnDestroy()
        {
            TerrainCallbacks.heightmapChanged -= OnHeightmapChanged;
            if (terrainProfile != null) Visuals.VisualProfileRegistry.Unregister(terrainProfile);
            if (presentation != null) Destroy(presentation.gameObject);
            foreach (var item in owned) if (item != null) Destroy(item);
        }
    }
}

