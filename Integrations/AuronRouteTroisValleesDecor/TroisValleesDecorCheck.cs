using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using SkiStation;
using SkiStation.Arcade;
using SkiStation.TerrainGeneration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

[InitializeOnLoad]
public static class TroisValleesDecorCheck
{
    const string Key = "TroisValleesDecorCheck.";
    const string Domain = "lab.gestion.fr.les-trois-vallees-complet";
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    static TroisValleesDecorCheck() => EditorApplication.update += Tick;

    public static void RunBaseline() => Run(false);
    public static void RunDecor() => Run(true);

    static void Run(bool decorated)
    {
        PlayerSettings.productName = "SkiStation_3V";
        SessionState.SetBool(Key + "Active", true);
        SessionState.SetBool(Key + "Decor", decorated);
        Stage(0);
        Restart();
    }

    static void Restart()
    {
        SessionState.SetFloat(Key + "Deadline", (float)EditorApplication.timeSinceStartup + 220f);
        EditorSceneManager.OpenScene("Assets/Scenes/SkiStation_Bootstrap.unity");
        EditorApplication.EnterPlaymode();
    }

    static void Stage(int stage, float delay = 0f)
    {
        SessionState.SetInt(Key + "Stage", stage);
        SessionState.SetFloat(Key + "At", (float)EditorApplication.timeSinceStartup + delay);
    }
    static object Call(object value, string name, params object[] args) => value.GetType().GetMethod(name, Any).Invoke(value, args);
    static object Field(object value, string name) => value.GetType().GetField(name, Any).GetValue(value);
    static object Prop(object value, string name) => value.GetType().GetProperty(name, Any).GetValue(value);
    static void Need(bool condition, string detail) { if (!condition) throw new InvalidOperationException(detail); }

    static void Tick()
    {
        if (!SessionState.GetBool(Key + "Active", false)) return;
        int stage = SessionState.GetInt(Key + "Stage", -1);
        bool decorated = SessionState.GetBool(Key + "Decor", false);
        try
        {
            double now = EditorApplication.timeSinceStartup;
            Need(now < SessionState.GetFloat(Key + "Deadline", 0), "Délai dépassé à l’étape " + stage);
            if (now < SessionState.GetFloat(Key + "At", 0)) return;
            if (!EditorApplication.isPlaying)
            {
                if (stage == 5 && !EditorApplication.isPlayingOrWillChangePlaymode) { Stage(6); Restart(); }
                return;
            }
            var menu = SkiStationStartMenu249.Owner263;
            if (menu == null) return;
            if (stage == 0 || stage == 6)
            {
                if (!SkiStationStartMenu249.HomeOpen263 || SceneManager.GetActiveScene().name != "SkiStation_Bootstrap") return;
                if (stage == 0)
                {
                    Action<SkiStationStartMenu249> launch = loaded => Call(Field(loaded, "alpinePreview"), "CreateSession");
                    Call(menu, "LoadValAlpin263", launch, Domain);
                    Stage(1);
                }
                else
                {
                    var save = DemoFlow263.ValidReprises(out var notice).Find(record => record.domainId == Domain);
                    Need(save != null, "Reprise des Trois Vallées absente " + notice);
                    Call(menu, "ContinueChoiceNow263", save);
                    Stage(7);
                }
                return;
            }
            var world = TerrainGenerationWorld.Current;
            if (world == null || world.domainId != Domain || ArrivalHub250.I == null || !ArrivalHub250.I.Ready) return;
            if (stage == 1)
            {
                var preview = Field(menu, "alpinePreview");
                if (Field(preview, "session") == null) return;
                Call(menu, "LaunchAlpineStation263");
                Stage(2, 1f);
                return;
            }
            if (SkiStationStartMenu249.HomeOpen263) return;
            var nature = world.GetComponents<Component>().FirstOrDefault(c => c.GetType().Name == "RealTerrainNature");
            var water = world.GetComponents<Component>().FirstOrDefault(c => c.GetType().Name == "RealTerrainWater");
            if (nature == null || water == null || Camera.main == null) return;
            if (stage == 2)
            {
                Check(world, nature, decorated);
                Benchmark(world, nature, decorated);
                if (!decorated) { Finish(0); return; }
                GameClock.I.RestoreCheckpoint261(95, 11f);
                Call(water, "Refresh");
                Need(Prop(water, "Lakes").ToString() == "Open", "Dégel interrompu.");
                var a = Ground(world, -108.50f, -2937.34f);
                var b = Ground(world, -108.50f, -2891.35f);
                object[] args = { water, a, b, false, null, null, null };
                Need((bool)typeof(WaterCrossing).GetMethod("TryPlan", Any).Invoke(null, args), "Pont ski refusé " + args[6]);
                typeof(WaterCrossing).GetMethod("Build", Any).Invoke(null, new[] { (object)a, b, args[4], args[5], false });
                Stage(3, .65f);
                return;
            }
            if (stage == 3)
            {
                Need(DemoFlow263.SaveManagement(), "Sauvegarde bloquée " + DemoFlow263.Notice);
                Stage(5);
                EditorApplication.ExitPlaymode();
                return;
            }
            if (stage == 7)
            {
                Check(world, nature, true);
                Need(UnityEngine.Object.FindFirstObjectByType<WaterCrossing>() != null && GameClock.I.CurrentDay == 95,
                    "Construction ou saison absente après redémarrage.");
                Debug.Log("TROIS_VALLEES_PASS décor, pont et saison restaurés.");
                Finish(0);
            }
        }
        catch (Exception error)
        {
            Debug.LogError($"TROIS_VALLEES_FAIL étape {stage} · {error}");
            Finish(1);
        }
    }

    static Vector3 Ground(TerrainGenerationWorld world, float x, float z) => new Vector3(x,world.ClampedHeight(x,z),z);
    static void Check(TerrainGenerationWorld world, Component nature, bool decorated)
    {
        int count = (int)Prop(nature, "InstanceCount");
        Need(world.Recipe.sizeX == 19560 && world.Recipe.sizeZ == 31200, "Emprise inattendue.");
        if (!decorated) { Need(count == 0, "La référence contient déjà le décor."); return; }
        Need(count > 50000 && (int)Prop(nature, "SurfaceCount") > 1458, "Couches absentes.");
        var materials = (IDictionary)Field(nature, "materialCopies");
        Need(materials.Values.Cast<Material>().Count(m => m.GetTexture("_BaseMap") != null) >= 6,
            "Modèles Pure Nature 2 non texturés.");
        Need(world.Terrain.GetComponent<TerrainGeneratedAppearance>().ResolvedId == "lab.gestion.fr.les-trois-vallees-complet.pure-nature",
            "Profil décor absent.");
        Debug.Log("TROIS_VALLEES_PASS " + count + " modèles géographiques, profil Pure Nature 2.");
    }

    static void Benchmark(TerrainGenerationWorld world, Component nature, bool decorated)
    {
        var controls = UnityEngine.Object.FindFirstObjectByType<PlayableCameraController>();
        var camera = Camera.main;
        var request = new UnityEngine.Rendering.RenderPipeline.StandardRequest();
        var buffer = RenderTexture.GetTemporary(1440, 900, 24);
        var previous = RenderTexture.active;
        var pixel = new Texture2D(1, 1, TextureFormat.RGB24, false);
        request.destination = buffer;
        var draw = nature.GetType().GetMethod("DrawInstances", Any);
        object[] drawArgs = {camera};
        string label = decorated ? "decor" : "reference";

        try
        {
            controls.Overview();
            Measure("carte");
            object[] focus = {"Lac de Tuéda", null};
            Need((bool)Call(nature, "TryGetWater", focus), "Tuéda introuvable.");
            Vector3 lake = (Vector3)focus[1];
            View(lake, new Vector3(-545f, 480f, -760f));
            Measure("vallee");
            View(lake + new Vector3(170f, 0f, 30f), new Vector3(30f, 50f, -140f));
            Measure("sol");
            controls.FocusEntrance();
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(buffer);
            UnityEngine.Object.Destroy(pixel);
        }

        void View(Vector3 focus, Vector3 offset)
        {
            focus.y = world.ClampedHeight(focus.x,focus.z);
            Vector3 eye = focus + offset;
            eye.y = Mathf.Max(eye.y,world.ClampedHeight(eye.x,eye.z)+22f);
            camera.transform.position = eye;
            camera.transform.LookAt(focus);
        }

        void Measure(string shot)
        {
            var time = new double[24];
            for (int i = -6; i < time.Length; i++)
            {
                long start = Stopwatch.GetTimestamp();
                if (decorated) draw.Invoke(nature, drawArgs);
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = buffer;
                // Synchronous 1 px transfer makes the timed GPU commands complete before sampling.
                pixel.ReadPixels(new Rect(720, 450, 1, 1), 0, 0, false);
                if (i >= 0) time[i] = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
            }
            Array.Sort(time);
            Debug.Log(FormattableString.Invariant(
                $"TROIS_VALLEES_BENCH {label} {shot} median={time[12]:F2}ms p95={time[22]:F2}ms n={time.Length} 1440x900 {SystemInfo.graphicsDeviceType}"));
            if (!decorated) return;
            var image = new Texture2D(buffer.width, buffer.height, TextureFormat.RGB24, false);
            try
            {
                image.ReadPixels(new Rect(0,0,buffer.width,buffer.height),0,0);
                image.Apply();
                File.WriteAllBytes(Path.GetFullPath("trois-vallees-" + shot + ".png"), image.EncodeToPNG());
            }
            finally { UnityEngine.Object.Destroy(image); }
        }
    }

    static void Finish(int code) { SessionState.SetBool(Key + "Active", false); EditorApplication.Exit(code); }
}
