using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SkiStation;
using SkiStation.TerrainGeneration;

[InitializeOnLoad]
public static class CameraGestionCheck
{
    const string Key = "CameraGestionCheck.";
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly string[] Domains = { "lab.gestion.fr.les-trois-vallees-complet", "lab.gestion.orcieres" };
    static readonly Type Frame = typeof(PlayableCameraController).GetNestedType("NavigationFrame", Any);
    static readonly MethodInfo Navigate = typeof(PlayableCameraController).GetMethod("Navigate", Any);
    static CameraGestionCheck() => EditorApplication.update += Tick;

    public static void Run()
    {
        PlayerSettings.productName = "SkiStation_CameraGestion_Smoke";
        SessionState.SetBool(Key + "Run", true);
        SessionState.SetInt(Key + "Index", 0);
        StartPlay();
    }

    static void StartPlay()
    {
        SessionState.SetInt(Key + "Stage", 0);
        SessionState.SetFloat(Key + "Deadline", (float)EditorApplication.timeSinceStartup + 140f);
        EditorSceneManager.OpenScene("Assets/Scenes/SkiStation_Bootstrap.unity");
        EditorApplication.EnterPlaymode();
    }

    static object Call(object obj, string method, params object[] args) => obj.GetType().GetMethod(method, Any).Invoke(obj, args);
    static object Field(object obj, string name) => obj.GetType().GetField(name, Any).GetValue(obj);

    static void Tick()
    {
        if (!SessionState.GetBool(Key + "Run", false)) return;
        int index = SessionState.GetInt(Key + "Index", 0);
        int stage = SessionState.GetInt(Key + "Stage", 0);
        string domain = Domains[index];
        try
        {
            Need(EditorApplication.timeSinceStartup < SessionState.GetFloat(Key + "Deadline", 0), "Délai dépassé.");
            if (!EditorApplication.isPlaying)
            {
                if (stage == 3 && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetInt(Key + "Index", index + 1);
                    StartPlay();
                }
                return;
            }
            var menu = SkiStationStartMenu249.Owner263;
            if (menu == null) return;
            if (stage == 0 && SkiStationStartMenu249.HomeOpen263 && SceneManager.GetActiveScene().name == "SkiStation_Bootstrap")
            {
                Action<SkiStationStartMenu249> create = loaded => Call(Field(loaded, "alpinePreview"), "CreateSession");
                Call(menu, "LoadValAlpin263", create, domain);
                SessionState.SetInt(Key + "Stage", 1);
                return;
            }
            var world = TerrainGenerationWorld.Current;
            if (world == null || world.domainId != domain || ArrivalHub250.I == null || !ArrivalHub250.I.Ready) return;
            if (stage == 1)
            {
                if (Field(Field(menu, "alpinePreview"), "session") == null) return;
                Call(menu, "LaunchAlpineStation263");
                SessionState.SetInt(Key + "Stage", 2);
                return;
            }
            if (stage != 2 || SkiStationStartMenu249.HomeOpen263) return;
            var controls = UnityEngine.Object.FindFirstObjectByType<PlayableCameraController>();
            var cam = controls != null ? Field(controls, "targetCamera") as Camera : null;
            var water = world.GetComponents<Component>().FirstOrDefault(c => c.GetType().Name == "RealTerrainWater");
            var tool = world.GetComponents<Component>().FirstOrDefault(c => c.GetType().Name == "WaterCrossingTool");
            if (cam == null || !cam.isActiveAndEnabled || cam.pixelWidth < 16 || water == null || tool == null) return;
            TestCamera(world, controls, cam, water, tool, index == 0);
            Debug.Log("CAMERA_PASS " + domain + " · contrôles vérifiés.");
            if (index == Domains.Length - 1) Finish(0);
            else
            {
                SessionState.SetInt(Key + "Stage", 3);
                EditorApplication.ExitPlaymode();
            }
        }
        catch (Exception error)
        {
            Debug.LogError($"CAMERA_FAIL {domain}, étape {stage} · {error}");
            Finish(1);
        }
    }

    static Vector3 Focus(PlayableCameraController controls) => (Vector3)Field(controls, "focus");

    static void FrameInput(PlayableCameraController controls, Vector2 pointer, float wheel = 0f, string pressed = "None",
        bool held = false, bool blocked = false, Vector2 move = default, bool fast = false, float dt = 1f / 60f)
    {
        var frame = Activator.CreateInstance(Frame);
        Set("pointer", pointer);
        Set("wheel", wheel);
        Set("pressed", Enum.Parse(Frame.GetField("pressed").FieldType, pressed));
        Set("held", held);
        Set("blocked", blocked);
        Set("move", move);
        Set("fast", fast);
        Navigate.Invoke(controls, new[] { frame, (object)dt });
        void Set(string name, object value) => Frame.GetField(name).SetValue(frame, value);
    }

    static void TestCamera(TerrainGenerationWorld world, PlayableCameraController controls, Camera cam,
        Component water, Component tool, bool big)
    {
        var rect = cam.pixelRect;
        Vector2 center = rect.center;
        controls.Overview();
        var bounds = world.Terrain.terrainData.bounds;
        bounds.center += world.Terrain.transform.position;
        float filled = 0f;
        for (int corner = 0; corner < 8; corner++)
        {
            var point = bounds.center + Vector3.Scale(bounds.extents,
                new Vector3((corner & 1) != 0 ? 1 : -1, (corner & 2) != 0 ? 1 : -1, (corner & 4) != 0 ? 1 : -1));
            var view = cam.WorldToViewportPoint(point);
            Need(view.x is > 0 and < 1 && view.y is > 0 and < 1 && view.z > 0 && view.z < cam.farClipPlane,
                "Coin de la carte hors de la vue générale " + view);
            filled = Mathf.Max(filled, Mathf.Abs(view.x - .5f) * 2f, Mathf.Abs(view.y - .5f) * 2f);
        }
        Need(filled is > .82f and < .94f, "Vue générale mal ajustée.");
        if (big) Capture(cam, "camera-trois-vallees-vue-generale.png");
        controls.FocusEntrance();
        Need(Vector3.Distance(world.FrontDeNeige.position, Focus(controls)) < 50, "Retour au front de neige incorrect.");
        Need(cam.transform.forward.y < -.3f, "Angle de départ trop près de l’horizon.");

        Vector2 aim = center + new Vector2(rect.width * .12f, rect.height * .11f);
        var anchor = Pick(cam, aim);
        var initialRange = Vector3.Distance(cam.transform.position, Focus(controls));
        FrameInput(controls, aim, wheel: 2);
        for (int step = 0; step < 70; step++) FrameInput(controls, aim);
        Need(Vector3.Distance(cam.transform.position, Focus(controls)) < initialRange * .8f, "Zoom avant insuffisant.");
        Need(Vector2.Distance(aim, cam.WorldToScreenPoint(anchor)) < 2, "Ancre perdue au zoom avant.");
        FrameInput(controls, aim, wheel: -2);
        for (int step = 0; step < 70; step++) FrameInput(controls, aim);
        Need(Mathf.Abs(Vector3.Distance(cam.transform.position, Focus(controls)) - initialRange) < 1,
            "Zoom avant et arrière asymétriques.");
        Need(Vector2.Distance(aim, cam.WorldToScreenPoint(anchor)) < 2, "Ancre perdue au zoom arrière.");

        controls.FocusEntrance();
        anchor = Pick(cam, aim);
        var draggedTo = aim + new Vector2(60, -35);
        FrameInput(controls, aim, pressed: "Pan", held: true);
        FrameInput(controls, draggedTo, held: true);
        Need(Vector2.Distance(draggedTo, cam.WorldToScreenPoint(anchor)) < 2, "Glisser décroché du relief.");
        FrameInput(controls, draggedTo);

        controls.FocusEntrance();
        var rotation = cam.transform.rotation;
        anchor = Pick(cam, center);
        FrameInput(controls, center, pressed: "OrbitRight", held: true);
        FrameInput(controls, center + new Vector2(100, -35), held: true);
        Need(Quaternion.Angle(rotation, cam.transform.rotation) > 12, "Orbite au clic droit trop faible.");
        Need(Vector2.Distance(center, cam.WorldToScreenPoint(anchor)) < 2, "L’orbite perd le point central.");
        FrameInput(controls, center);
        var position = cam.transform.position;
        rotation = cam.transform.rotation;
        FrameInput(controls, center, wheel: 4, pressed: "Pan", held: true, blocked: true);
        FrameInput(controls, center + Vector2.right * 200, held: true, blocked: true);
        Need(Vector3.Distance(position, cam.transform.position) < .001f && Quaternion.Angle(rotation, cam.transform.rotation) < .01f,
            "La souris actionne la caméra au-dessus d’un panneau.");

        Need((bool)Call(controls, "CanStartRightOrbit"), "Le clic droit ne peut pas démarrer l’orbite.");
        Call(tool, "Begin", water, false);
        Need(!(bool)Call(controls, "CanStartRightOrbit"), "Le clic droit intercepte un outil de construction.");
        FrameInput(controls, center, pressed: "OrbitMiddle", held: true);
        FrameInput(controls, center + new Vector2(-70, 25), held: true);
        Need(Quaternion.Angle(rotation, cam.transform.rotation) > 10, "Maj + clic molette ne tourne pas pendant la construction.");
        Call(tool, "Cancel");
        FrameInput(controls, center);

        var ground = world.ClampedHeight(position.x, position.z);
        cam.transform.position = new Vector3(position.x, ground - 20f, position.z);
        FrameInput(controls, center);
        Need(cam.transform.position.y >= ground + 4.9f, "La caméra traverse le relief.");
        float sixty = Travel(controls, center, 60);
        float thirty = Travel(controls, center, 30);
        Need(sixty > 70 && Mathf.Abs(sixty - thirty) < sixty * .03f,
            $"Déplacement dépendant de la cadence : {sixty} m contre {thirty} m.");
        if (big)
        {
            var start = new Vector3(-108.50f, 2269.80f, -2937.34f);
            var end = new Vector3(-108.50f, 2267.84f, -2891.35f);
            controls.FocusDomainElement((start + end) * .5f, 32);
            var picked = new Vector3[2];
            int index = 0;
            foreach (var bank in new[] { start, end })
            {
                var projected = cam.WorldToScreenPoint(bank);
                Need(projected.z > 0 && rect.Contains(projected), "Une berge est hors champ.");
                picked[index] = Pick(cam, projected);
                Need(Vector3.Distance(picked[index++], bank) < 3, "Une berge est masquée par le relief.");
            }
            object[] plan = { water, picked[0], picked[1], false, null, null, null };
            Need((bool)typeof(WaterCrossing).GetMethod("TryPlan", Any).Invoke(null, plan),
                "Franchissement refusé aux berges visées : " + plan[6]);
            typeof(WaterCrossing).GetMethod("Build", Any).Invoke(null,
                new[] { (object)picked[0], picked[1], plan[4], plan[5], false });
            Capture(cam, "camera-trois-vallees-franchissement.png");
            controls.Overview();
            for (int i = 0; i < 190; i++) FrameInput(controls, center, move: Vector2.up, fast: true);
            var pivot = Focus(controls);
            Need(pivot.z <= world.Recipe.originZ + world.Recipe.sizeZ + .1f, "Déplacement au-delà de la limite nord.");
        }
        Debug.Log($"CAMERA_PASS cadrage, molette avant/arrière, glisser ancré, orbite, panneau, anti-collision et cadence ({sixty:F1} / {thirty:F1} m).");
    }

    static float Travel(PlayableCameraController controls, Vector2 pointer, int fps)
    {
        controls.FocusEntrance();
        Vector3 start = Focus(controls);
        for (int i = 0; i < fps; i++) FrameInput(controls, pointer, move: Vector2.right, dt: 1f / fps);
        var delta = Focus(controls) - start;
        return new Vector2(delta.x, delta.z).magnitude;
    }

    static Vector3 Pick(Camera cam, Vector2 point)
    {
        Need(ManagementSurface.TryRaycast349(cam.ScreenPointToRay(point), out var ground), "Point du relief introuvable.");
        return ground;
    }

    static void Capture(Camera cam, string filename)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        var target = RenderTexture.GetTemporary(1440, 810, 24);
        var previous = RenderTexture.active;
        var pixels = new Texture2D(1440, 810, TextureFormat.RGB24, false);
        try
        {
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(cam,
                new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = target });
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 1440, 810), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(Path.GetFullPath(filename), pixels.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.Destroy(pixels);
        }
    }

    static void Need(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    static void Finish(int code) { SessionState.SetBool(Key + "Run", false); EditorApplication.Exit(code); }
}
