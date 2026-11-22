#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using SkiStation.TerrainGeneration;
using SkiStation.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SkiStation.EditorTools
{
    internal sealed class PureNatureMountainsReliefWindow : EditorWindow
    {
        const string LabScene = "Assets/Scenes/SkiStation_TerrainStampLab.unity";
        const string ProfileId = "BK_PURE_NATURE_2_MOUNTAINS";
        const string Source = "Assets/BK/PureNature_Mountains/Models/TerrainMountain.asset";
        const int StampResolution = 129;
        [SerializeField] int seed = 6312;
        [SerializeField] bool previewRequested;
        [SerializeField] string status;
        TerrainHeightfield pending;
        double waitUntil;

        [MenuItem("SkiStation/Terrain/Pure Nature 2/Relief aléatoire")]
        static void Open() => GetWindow<PureNatureMountainsReliefWindow>("Relief Pure Nature 2");

        void OnGUI()
        {
            EditorGUILayout.LabelField("Pure Nature 2 · Mountains", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Deux portions du relief BK sont assemblées à chaque génération.", EditorStyles.wordWrappedLabel);
            seed = EditorGUILayout.IntField("Graine", seed);
            bool otherScene = EditorApplication.isPlaying && SceneManagerPath() != LabScene;
            using (new EditorGUI.DisabledScope(previewRequested || otherScene))
            {
                if (GUILayout.Button("Générer cette carte")) QueuePreview();
                if (GUILayout.Button("Générer une autre carte"))
                {
                    seed = Guid.NewGuid().GetHashCode();
                    QueuePreview();
                }
            }
            if (otherScene)
                EditorGUILayout.HelpBox("L’aperçu s’ouvre dans le laboratoire depuis le mode Édition.", MessageType.Info);
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.None);
        }

        static string SceneManagerPath() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;

        void QueuePreview()
        {
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying) return;
                if (!EditorApplication.isPlaying && SceneManagerPath() != LabScene)
                {
                    if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                    EditorSceneManager.OpenScene(LabScene);
                }
                status = "Génération du relief…";
                previewRequested = true;
                waitUntil = 0;
                pending = null;
                if (!EditorApplication.isPlaying) EditorApplication.EnterPlaymode();
            }
            catch (Exception error) { Finish(error.Message); }
        }

        void Update()
        {
            if (!previewRequested || !EditorApplication.isPlaying) return;
            try
            {
                if (waitUntil == 0) waitUntil = EditorApplication.timeSinceStartup + 30;
                var lab = FindFirstObjectByType<SkiStationTerrainStampLab>();
                if (lab != null && lab.gameObject.scene.path == LabScene)
                {
                    pending ??= Generate(seed);
                    if (lab.PreviewLocalHeightfield(pending))
                    {
                        Finish($"Relief généré · graine {seed}.");
                        return;
                    }
                }
                if (EditorApplication.timeSinceStartup > waitUntil) Finish("Le laboratoire est occupé. Relancez la génération une fois disponible.");
            }
            catch (Exception error) { Finish(error.Message); }
        }

        void Finish(string message)
        {
            status = message;
            previewRequested = false;
            pending = null;
            Repaint();
        }

        internal static TerrainHeightfield Generate(int seed)
        {
            var source = AssetDatabase.LoadAssetAtPath<TerrainData>(Source);
            if (source == null) throw new InvalidOperationException("Le terrain de démonstration Pure Nature 2 Mountains est absent.");
            var stamps = new List<TerrainStampDefinition>();
            var library = CreateInstance<TerrainStampLibrary>();
            library.hideFlags = HideFlags.HideAndDontSave;
            library.libraryId = ProfileId;
            try
            {
                for (int z = 0; z < 3; z++)
                    for (int x = 0; x < 3; x++)
                    {
                        var crop = ReadCrop(source, x, z);
                        if (crop != null) stamps.Add(crop);
                    }
                if (stamps.Count < 2)
                    throw new InvalidOperationException("Deux portions de relief exploitables sont nécessaires pour cette composition.");
                library.stamps = stamps.ToArray();
                var random = new System.Random(seed);
                int first = random.Next(stamps.Count);
                int second = random.Next(stamps.Count - 1);
                if (second >= first) second++;
                var recipe = new TerrainGenerationRecipe
                {
                    generationVersion = TerrainGenerationEngine.VerticalVersion,
                    profileId = ProfileId,
                    stampLibraryId = library.libraryId,
                    visualProfileId = VisualProfileRegistry.PureNatureId,
                    mode = GenerationMode.HYBRID,
                    seed = seed,
                    resolution = 513,
                    sizeX = 7600f,
                    sizeZ = 7600f,
                    originX = -3800f,
                    originZ = -3800f,
                    heightScale = 3000f,
                    baseHeight = 220f,
                    noise = new TerrainNoiseRules { macroAmplitude = 62f, secondaryAmplitude = 20f, microAmplitude = 3f },
                    composition = new TerrainCompositionRules { edgeFeather = 260f },
                    compositionPlan = new TerrainCompositionPlan { validatePrimaryPeaks = false },
                    altitudeGeneration = new TerrainAltitudeGenerationSettings { requireFamily = false },
                    placements = new[] { Place(stamps[first], -1400f, random), Place(stamps[second], 1400f, random) }
                };
                var field = TerrainGenerationEngine.Generate(recipe, library);
                field.recipe.scenery = TerrainSceneryPlanner.Prepare(field, field.creationReport);
                field.notice = "Pure Nature 2 · " + stamps[first].stampId + " + " + stamps[second].stampId;
                return field;
            }
            finally
            {
                foreach (var stamp in stamps) Object.DestroyImmediate(stamp);
                Object.DestroyImmediate(library);
            }
        }

        static TerrainStampDefinition ReadCrop(TerrainData source, int column, int row)
        {
            const float extent = .46f;
            var h = new float[StampResolution, StampResolution];
            float minimum = float.PositiveInfinity, maximum = float.NegativeInfinity;
            for (int z = 0; z < StampResolution; z++)
                for (int x = 0; x < StampResolution; x++)
                {
                    float u = column * .27f + extent * x / (StampResolution - 1);
                    float v = row * .27f + extent * z / (StampResolution - 1);
                    float height = source.GetInterpolatedHeight(u, v);
                    h[z, x] = height;
                    minimum = Mathf.Min(minimum, height);
                    maximum = Mathf.Max(maximum, height);
                }
            float relief = maximum - minimum;
            if (relief < 15f) return null;
            for (int z = 0; z < StampResolution; z++)
                for (int x = 0; x < StampResolution; x++)
                    h[z, x] = (h[z, x] - minimum) / relief;

            var stamp = CreateInstance<TerrainStampDefinition>();
            stamp.hideFlags = HideFlags.HideAndDontSave;
            stamp.stampId = $"BK_MOUNTAINS_{column + 1}_{row + 1}";
            stamp.category = "MOUNTAIN";
            stamp.tags = new[] { "MOUNTAIN", ProfileId };
            stamp.logicalSize = new Vector2(3300f, 3300f);
            stamp.recommendedHeight = Mathf.Clamp(relief * 1.3f, 350f, 1050f);
            stamp.feather = .24f;
            stamp.SetTransientHeightData(h);
            return stamp;
        }

        static TerrainStampPlacement Place(TerrainStampDefinition stamp, float x, System.Random random) =>
            new TerrainStampPlacement
            {
                stampId = stamp.stampId,
                x = x + random.Next(-300, 301),
                z = random.Next(-650, 1201),
                rotation = random.Next(0, 360),
                mirrorX = random.Next(0, 2) == 1,
                scaleX = random.Next(90, 116) / 100f,
                scaleZ = random.Next(90, 116) / 100f,
                amplitude = stamp.recommendedHeight * random.Next(90, 116) / 100f,
                operation = StampOperation.MAX
            };
    }
}
#endif

