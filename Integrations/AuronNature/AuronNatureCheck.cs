using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using SkiStation;
using SkiStation.Arcade;
using SkiStation.TerrainGeneration;
using SkiStation.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class AuronNatureCheck
{
    private const string Key = "AuronNatureCheck.";
    private const string Domain = "lab.gestion.fr.auron";
    private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly Vector2 FirstBank = new Vector2(69.15f, -2254.48f);
    private static readonly Vector2 SecondBank = new Vector2(69.15f, -2208.48f);
    static AuronNatureCheck() => EditorApplication.update += Tick;

    public static void Run()
    {
        PlayerSettings.productName = "SkiStation_AuronNature_Smoke";
        SessionState.SetBool(Key + "Active", true);
        SessionState.SetBool(Key + "Legacy", false);
        Begin(0);
    }

    public static void RunLegacy()
    {
        PlayerSettings.productName = "SkiStation_HydroFrance_Smoke";
        SessionState.SetBool(Key + "Active", true);
        SessionState.SetBool(Key + "Legacy", true);
        Begin(8);
    }

    private static void Begin(int stage)
    {
        Stage(stage);
        SessionState.SetFloat(Key + "Timeout", (float)EditorApplication.timeSinceStartup + 200f);
        EditorSceneManager.OpenScene("Assets/Scenes/SkiStation_Bootstrap.unity");
        EditorApplication.EnterPlaymode();
    }

    private static void Stage(int value, float delay = 0f)
    {
        SessionState.SetInt(Key + "Stage", value);
        SessionState.SetFloat(Key + "Next", (float)EditorApplication.timeSinceStartup + delay);
    }

    private static object Call(object obj, string method, params object[] args) =>
        obj.GetType().GetMethod(method, Any).Invoke(obj, args);
    private static object Field(object obj, string field) => obj.GetType().GetField(field, Any).GetValue(obj);
    private static T Property<T>(object obj, string name) => (T)obj.GetType().GetProperty(name, Any).GetValue(obj);
    private static void Need(bool condition, string reason) { if (!condition) throw new InvalidOperationException(reason); }
    private static Vector3 Ground(TerrainGenerationWorld world, float x, float z) => new Vector3(x, world.ClampedHeight(x, z), z);

    private static void Tick()
    {
        if (!SessionState.GetBool(Key + "Active", false)) return;
        int stage = SessionState.GetInt(Key + "Stage", -1);
        try
        {
            var now = EditorApplication.timeSinceStartup;
            Need(now < SessionState.GetFloat(Key + "Timeout", 0f), "Délai dépassé à l’étape " + stage);
            if (now < SessionState.GetFloat(Key + "Next", 0)) return;
            if (!EditorApplication.isPlaying)
            {
                if (stage == 7 && !EditorApplication.isPlayingOrWillChangePlaymode) Begin(8);
                return;
            }
            var menu = SkiStationStartMenu249.Owner263;
            if (menu == null) return;
            if (stage == 0 || stage == 8)
            {
                if (!SkiStationStartMenu249.HomeOpen263 || SceneManager.GetActiveScene().name != "SkiStation_Bootstrap") return;
                if (stage == 0)
                {
                    Action<SkiStationStartMenu249> create = loaded => Call(Field(loaded, "alpinePreview"), "CreateSession");
                    Call(menu, "LoadValAlpin263", create, Domain);
                    Stage(1);
                }
                else
                {
                    var resume = DemoFlow263.ValidReprises(out var notice).Find(r => r.domainId == Domain);
                    Need(resume != null, "Reprise Auron absente " + notice);
                    Call(menu, "ContinueChoiceNow263", resume);
                    Stage(9, 1f);
                }
                return;
            }
            var world = TerrainGenerationWorld.Current;
            if (world == null || world.domainId != Domain || ArrivalHub250.I == null || !ArrivalHub250.I.Ready) return;
            if (stage == 1)
            {
                if (Field(Field(menu, "alpinePreview"), "session") == null) return;
                Call(menu, "LaunchAlpineStation263");
                Stage(2, .8f);
                return;
            }
            if (SkiStationStartMenu249.HomeOpen263) return;
            var components = world.GetComponents<Component>();
            var scenery = components.FirstOrDefault(c => c.GetType().Name == "RealTerrainNature");
            var water = components.FirstOrDefault(c => c.GetType().Name == "RealTerrainWater");
            if (scenery == null || water == null || Camera.main == null) return;
            if (stage == 2 || stage == 9)
            {
                ValidateScenery(world, scenery);
                if (stage == 9)
                {
                    int expected = SessionState.GetBool(Key + "Legacy", false) ? 2 : 1;
                    Need(UnityEngine.Object.FindObjectsByType<WaterCrossing>(FindObjectsSortMode.None).Length == expected,
                        "Franchissements non rechargés.");
                    Need(GameClock.I.CurrentDay == 95, "Saison non rechargée.");
                    Call(water, "Refresh");
                    Need(Property<object>(water, "Lakes").ToString() == "Open", "Eau d’été non restaurée.");
                    Debug.Log($"AURON_PASS reprise à froid du décor, des saisons et de {expected} franchissement(s).");
                    Finish(0);
                    return;
                }
                Need(Property<object>(water, "Lakes").ToString() != "Open", "Eau hivernale non gelée.");
                SetView(Ground(world, 580f, -2250f), new Vector3(-120f, 315f, -760f));
                Stage(3, .5f);
                return;
            }
            if (stage == 3)
            {
                Capture(scenery, "auron-forets.png");
                SetView(Ground(world, -1148f, -1230f), new Vector3(430f, 235f, -415f));
                Stage(4, .5f);
                return;
            }
            if (stage == 4)
            {
                Capture(scenery, "auron-falaises.png");
                object[] focus = { "Retenue d’altitude", null };
                Need((bool)Call(scenery, "TryGetWater", focus), "Retenue Auron introuvable.");
                SetView(Ground(world, -605f, -3186f), new Vector3(-360f, 330f, -510f));
                Stage(5, .5f);
                return;
            }
            if (stage == 5)
            {
                Capture(scenery, "auron-retenue.png");
                GameClock.I.RestoreCheckpoint261(95, 11);
                Call(water, "Refresh");
                Need(Property<object>(water, "Lakes").ToString() == "Open", "Eau d’été non libre.");
                var a = Ground(world, FirstBank.x, FirstBank.y);
                var b = Ground(world, SecondBank.x, SecondBank.y);
                object[] args = { water, a, b, false, null, null, null };
                Need((bool)typeof(WaterCrossing).GetMethod("TryPlan", Any).Invoke(null, args),
                    "Pont refusé après ajout du décor " + args[6]);
                typeof(WaterCrossing).GetMethod("Build", Any).Invoke(null, new[] { (object)a, b, args[4], args[5], false });
                Stage(6, .7f);
                return;
            }
            if (stage == 6)
            {
                var corridors = (IEnumerable)Field(scenery, "corridors");
                bool clear = corridors.Cast<ValueTuple<Vector3,Vector3,float>>().Any(segment =>
                    Vector2.Distance(new Vector2(segment.Item1.x,segment.Item1.z),FirstBank) < .1f &&
                    Vector2.Distance(new Vector2(segment.Item2.x,segment.Item2.z),SecondBank) < .1f);
                Need(clear, "Le pont n’est pas pris en compte par les exclusions du décor.");
                Need(DemoFlow263.SaveManagement(), "Sauvegarde bloquée " + DemoFlow263.Notice);
                Debug.Log("AURON_PASS pont, dégagement du décor et sauvegarde.");
                Stage(7);
                EditorApplication.ExitPlaymode();
            }
        }
        catch (Exception error)
        {
            Debug.LogError($"AURON_FAIL étape {stage} · {error}");
            Finish(1);
        }
    }

    private static void ValidateScenery(TerrainGenerationWorld world, Component scenery)
    {
        var terrain = world.Terrain;
        int count = Property<int>(scenery, "InstanceCount");
        int surfaces = Property<int>(scenery, "SurfaceCount");
        var appearance = terrain.GetComponent<TerrainGeneratedAppearance>();
        var convertedMaterials = (IDictionary)Field(scenery, "materialCopies");
        int textured = convertedMaterials.Values.Cast<Material>().Count(mat => mat.GetTexture("_BaseMap") != null);
        var root = GameObject.Find("Décor géographique — Auron");
        Need(count > 27000 && surfaces == 270 && terrain.terrainData.alphamapResolution == 512,
            $"Décor incomplet · {count} instances, {surfaces} surfaces.");
        Need(appearance.ResolvedId == "AURON_PURE_NATURE" && textured >= 6,
            $"Profil ou textures Pure Nature 2 absents · {appearance.ResolvedId}, {textured} matériaux.");
        Need(root != null && root.transform.IsChildOf(terrain.transform) && root.GetComponentsInChildren<Collider>().Length == 0,
            "Le décor remplace les collisions natives.");
        Need(world.Recipe.sizeX == 8040 && world.Recipe.sizeZ == 8400, "Emprise Auron modifiée.");
        Debug.Log($"AURON_PASS rendu Pure Nature 2, {count} instances, {surfaces} surfaces, {textured} matériaux texturés.");
    }

    private static void SetView(Vector3 focus, Vector3 offset)
    {
        var controller = UnityEngine.Object.FindFirstObjectByType<PlayableCameraController>();
        if (controller != null) controller.enabled = false;
        Camera.main.transform.position = focus + offset;
        Camera.main.transform.LookAt(focus);
    }

    private static void Capture(Component scenery, string name)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        var target = RenderTexture.GetTemporary(1440, 900, 24);
        var previous = RenderTexture.active;
        var texture = new Texture2D(1440, 900, TextureFormat.RGB24, false);
        try
        {
            var cam = Camera.main;
            Call(scenery, "DrawInstances", cam);
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(cam,
                new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = target });
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 1440, 900), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.GetFullPath(name), texture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.Destroy(texture);
        }
    }

    private static void Finish(int code)
    {
        SessionState.SetBool(Key + "Active", false);
        EditorApplication.Exit(code);
    }
}
