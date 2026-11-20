using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SkiStation;
using SkiStation.Arcade;
using SkiStation.TerrainGeneration;

[InitializeOnLoad]
public static class HydroFranceCheck
{
    const string Key = "HydroFranceCheck.";
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static readonly string[] Maps = { "auron", "les-trois-vallees-complet" };
    static HydroFranceCheck() => EditorApplication.update += Tick;

    public static void Run()
    {
        PlayerSettings.productName = "SkiStation_HydroFrance_Smoke";
        SessionState.SetBool(Key + "Run", true);
        SessionState.SetInt(Key + "Map", 0);
        Stage(0);
        Restart();
    }

    static void Stage(int stage) => SessionState.SetInt(Key + "Stage", stage);

    static void Restart()
    {
        SessionState.SetFloat(Key + "Deadline", (float)EditorApplication.timeSinceStartup + 180);
        EditorSceneManager.OpenScene("Assets/Scenes/SkiStation_Bootstrap.unity");
        EditorApplication.EnterPlaymode();
    }

    static object Call(object value, string name, params object[] args) =>
        value.GetType().GetMethod(name, Any).Invoke(value, args);

    static object Prop(Component value, string name) =>
        value.GetType().GetProperty(name, Any).GetValue(value);

    static void Need(bool condition, string error)
    {
        if (!condition) throw new InvalidOperationException(error);
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Key + "Run", false)) return;
        int index = SessionState.GetInt(Key + "Map", 0);
        string key = Maps[index], domain = "lab.gestion.fr." + key;
        int stage = SessionState.GetInt(Key + "Stage", -1);
        try
        {
            Need(EditorApplication.timeSinceStartup <= SessionState.GetFloat(Key + "Deadline", 0),
                $"Délai dépassé dans {SceneManager.GetActiveScene().name}.");
            if (!EditorApplication.isPlaying)
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (stage == 3) { Stage(4); Restart(); }
                if (stage == 6) { SessionState.SetInt(Key + "Map", 1); Stage(0); Restart(); }
                return;
            }
            var menu = SkiStationStartMenu249.Owner263;
            if (menu == null) return;
            if (stage == 0 || stage == 4)
            {
                if (!SkiStationStartMenu249.HomeOpen263 || SceneManager.GetActiveScene().name != "SkiStation_Bootstrap") return;
                if (stage == 0)
                {
                    Action<SkiStationStartMenu249> launch = loaded => Call(Preview(loaded), "CreateSession");
                    Call(menu, "LoadValAlpin263", launch, domain);
                    Stage(1);
                }
                else
                {
                    var save = DemoFlow263.ValidReprises(out var notice).Find(entry => entry.domainId == domain);
                    Need(save != null, "Reprise absente " + notice);
                    Call(menu, "ContinueChoiceNow263", save);
                    Stage(5);
                }
                return;
            }
            var world = TerrainGenerationWorld.Current;
            if (world == null || world.domainId != domain || ArrivalHub250.I == null || !ArrivalHub250.I.Ready) return;
            if (stage == 1)
            {
                var preview = Preview(menu);
                if (preview.GetType().GetField("session", Any).GetValue(preview) == null) return;
                Call(menu, "LaunchAlpineStation263");
                Stage(2);
                return;
            }
            if (SkiStationStartMenu249.HomeOpen263 || GameClock.I == null) return;
            if (stage != 2 && stage != 5) return;
            var components = world.GetComponents<Component>();
            var hydro = components.FirstOrDefault(c => c.GetType().Name == "RealTerrainWater");
            var nature = components.FirstOrDefault(c => c.GetType().Name == "RealTerrainNature");
            // Décor géographique starts two frames after the native terrain.
            if (hydro == null || nature == null) return;
            Need(world.Recipe.sizeX == (index == 0 ? 8040 : 19560) && world.Recipe.sizeZ == (index == 0 ? 8400 : 31200),
                "Dimensions différentes du relief attendu.");
            int count = (int)Prop(nature, "SurfaceCount");
            Need(count == (index == 0 ? 234 : 1458), "Nombre de surfaces inattendu " + count);
            string lakeName = (string)Prop(nature, "FocusWater");
            object[] focus = { lakeName, null };
            Need((bool)Call(nature, "TryGetWater", focus), "Plan d’eau de référence introuvable.");
            Need(!(bool)Prop(hydro, "HasActivities"), "Activité aquatique fictive ajoutée.");
            var data = JsonUtility.FromJson<Map>(Resources.Load<TextAsset>("TerrainGeneration/Nature/" + key).text);
            var lakePolygon = data.waters.First(s => s.name == lakeName);
            var lake2 = TriangleCenter(lakePolygon, 0);
            var lake = new Vector3(lake2.x, world.ClampedHeight(lake2.x, lake2.y), lake2.y);
            Need(ManagementSurface.TryGet(lake, out _), "Plan d’eau hors du terrain.");
            if (stage == 2)
            {
                Need(Prop(hydro, "Lakes").ToString() != "Open", "Pas de gel hivernal.");
                CheckFootprint(lake);
                GameClock.I.RestoreCheckpoint261(95, 11);
                Call(hydro, "Refresh");
                Need(Prop(hydro, "Lakes").ToString() == "Open" && Prop(hydro, "Streams").ToString() == "Open",
                    "Pas de dégel estival.");
                Need(SeasonWeatherRuntime363.I.GetForecast(95).temperatureC >= 13f, "Météo estivale absente.");
                CheckFootprint(lake);
                var ski = BuildBridge(world, hydro, data.streams, false, null);
                var bike = BuildBridge(world, hydro, data.streams, true, ski.Item1);
                Need(Allowed(hydro, ski.Item1, ski.Item2, 8, true), "Piste refusée sur le pont ski.");
                Need(!Allowed(hydro, ski.Item1, ski.Item2, 8, false), "Route au sol autorisée sur l’eau.");
                Need(!Allowed(hydro, bike.Item1, bike.Item2, 3, true), "Piste autorisée sur la passerelle VTT.");
                Need(DemoFlow263.SaveManagement(), "Enregistrement refusé " + DemoFlow263.Notice);
                Debug.Log($"HYDRO_PASS {key} · {count} surfaces · {lakeName} · hiver/été · emprises · pont ski et passerelle VTT enregistrés.");
                Stage(3);
                EditorApplication.ExitPlaymode();
            }
            else
            {
                var bridges = UnityEngine.Object.FindObjectsByType<WaterCrossing>(FindObjectsSortMode.None);
                Need(bridges.Length == 2 && bridges.Any(b => b.name == "Passerelle VTT") && bridges.Any(b => b.name == "Pont de piste"),
                    "Les deux ponts n’ont pas été restaurés.");
                var deck = bridges.First(b => b.name == "Pont de piste").transform.Find("Tablier").position;
                Need(ManagementSurface.TryGet(deck, out var hill) && ManagementSurface.TryTrail(deck, out var trail) && trail.y > hill.y + .5f,
                    "Le tablier restauré ne porte pas la piste.");
                Call(hydro, "Refresh");
                Need(GameClock.I.CurrentDay == 95 && Prop(hydro, "Lakes").ToString() == "Open",
                    "L’été n’a pas été restauré.");
                Debug.Log($"HYDRO_PASS {key} · Continuer à froid · ponts, tablier et saison restaurés.");
                if (index == Maps.Length - 1) Finish(0);
                else { Stage(6); EditorApplication.ExitPlaymode(); }
            }
        }
        catch (Exception error)
        {
            Debug.LogError($"HYDRO_FAIL {key} étape {stage} · {error}");
            Finish(1);
        }
    }

    static object Preview(SkiStationStartMenu249 menu) => menu.GetType().GetField("alpinePreview", Any).GetValue(menu);

    static void CheckFootprint(Vector3 lake)
    {
        Need(!SkiStation.Altitude.AltitudeConstructionPolicy.ValidateFootprint(lake, new Vector2(4, 4), out var why)
            && why.Contains("d’eau"), "L’emprise aquatique n’est pas protégée " + why);
    }

    static bool Allowed(Component hydro, Vector3 a, Vector3 b, float width, bool trail) =>
        (bool)hydro.GetType().GetMethod("AllowsRoute", Any).Invoke(null, new object[] { new[] { a, b }, width, null, trail });

    [Serializable] sealed class Map { public Surface[] waters, streams; }
    [Serializable] sealed class Surface { public string name; public Vector2[] vertices; public int[] triangles; }

    static Vector2 TriangleCenter(Surface surface, int offset) =>
        (surface.vertices[surface.triangles[offset]] + surface.vertices[surface.triangles[offset + 1]]
            + surface.vertices[surface.triangles[offset + 2]]) / 3f;

    static (Vector3, Vector3) BuildBridge(TerrainGenerationWorld world, Component hydro,
        Surface[] streams, bool bike, Vector3? other)
    {
        var plan = typeof(WaterCrossing).GetMethod("TryPlan", Any);
        var build = typeof(WaterCrossing).GetMethod("Build", Any);
        var entrance = world.FrontDeNeige.position;
        var centers = streams.SelectMany(s => Enumerable.Range(0, s.triangles.Length / 18)
            .Select(i => TriangleCenter(s, 18 * i)))
            .OrderBy(p => (p - new Vector2(entrance.x, entrance.z)).sqrMagnitude);
        foreach (var pos in centers)
        {
            var center = new Vector3(pos.x, world.ClampedHeight(pos.x, pos.y), pos.y);
            if (!world.Contains(pos.x, pos.y) || (other.HasValue && Vector3.Distance(other.Value, center) < 100)) continue;
            for (int n = 0; n < 12; n++)
            {
                var direction = Quaternion.Euler(0, n * 15, 0) * Vector3.forward;
                foreach (int reach in new[] { 19, 23, 31, 40 })
                {
                    if (!ManagementSurface.TryGet(center - direction * reach, out var a)
                        || !ManagementSurface.TryGet(center + direction * reach, out var b)
                        || Mathf.Abs(a.y - b.y) / (2 * reach) > .22f) continue;
                    object[] args = { hydro, a, b, bike, null, null, null };
                    if (!(bool)plan.Invoke(null, args) || Allowed(hydro, a, b, bike ? 3 : 8, true)) continue;
                    build.Invoke(null, new[] { (object)a, b, args[4], args[5], bike });
                    Debug.Log($"HYDRO_PASS {(bike ? "passerelle VTT" : "pont ski")} · {a} vers {b}");
                    return (a, b);
                }
            }
        }
        throw new InvalidOperationException("Franchissement compatible introuvable.");
    }

    static void Finish(int code)
    {
        SessionState.SetBool(Key + "Run", false);
        EditorApplication.Exit(code);
    }
}
