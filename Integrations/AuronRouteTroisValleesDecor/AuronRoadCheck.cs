using System;
using System.Linq;
using System.Reflection;
using SkiStation;
using SkiStation.Arcade;
using SkiStation.TerrainGeneration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class AuronRoadCheck
{
    const string Key = "AuronRoadCheck.";
    const string Domain = "lab.gestion.fr.auron";
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static AuronRoadCheck() => EditorApplication.update += Tick;
    public static void Run()
    {
        PlayerSettings.productName = "SkiStation_AuronRoad_Smoke";
        SessionState.SetBool(Key + "Active", true);
        Restart(0);
    }
    static void Restart(int phase)
    {
        SessionState.SetInt(Key + "Stage", phase);
        SessionState.SetFloat(Key + "Deadline", (float)EditorApplication.timeSinceStartup + 180f);
        EditorSceneManager.OpenScene("Assets/Scenes/SkiStation_Bootstrap.unity");
        EditorApplication.EnterPlaymode();
    }
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Any).Invoke(target,args);
    static void Need(bool condition, string reason) { if (!condition) throw new InvalidOperationException(reason); }
    static object Invoke(Type type, string name, Type[] signature, params object[] args) =>
        type.GetMethod(name, Any, null, signature, null).Invoke(null, args);

    static void Tick()
    {
        if (!SessionState.GetBool(Key + "Active", false)) return;
        int stage = SessionState.GetInt(Key + "Stage", -1);
        try
        {
            Need(EditorApplication.timeSinceStartup < SessionState.GetFloat(Key + "Deadline", 0), "Délai dépassé.");
            if (!EditorApplication.isPlaying)
            {
                if (stage == 2 && !EditorApplication.isPlayingOrWillChangePlaymode) Restart(3);
                return;
            }
            var menu = SkiStationStartMenu249.Owner263;
            if (menu == null) return;
            if (stage == 0 || stage == 3)
            {
                if (!SkiStationStartMenu249.HomeOpen263 || SceneManager.GetActiveScene().name != "SkiStation_Bootstrap") return;
                var record = DemoFlow263.ValidReprises(out var notice).Find(d => d.domainId == Domain);
                Need(record != null, "Sauvegarde de test absente " + notice);
                Call(menu, "ContinueChoiceNow263", record);
                SessionState.SetInt(Key + "Stage", stage + 1);
                return;
            }
            var world = TerrainGenerationWorld.Current;
            if (world == null || world.domainId != Domain || SkiStationStartMenu249.HomeOpen263 || ArrivalHub250.I == null || !ArrivalHub250.I.Ready) return;
            var water = world.GetComponents<Component>().FirstOrDefault(c => c.GetType().Name == "RealTerrainWater");
            if (water == null || GameClock.I == null) return;
            Need(GameClock.I.CurrentDay == 95 && UnityEngine.Object.FindObjectsByType<WaterCrossing>(FindObjectsSortMode.None).Length == 2,
                "La sauvegarde d’origine a perdu la saison ou les deux ponts.");
            var road = StationRoadNetwork247.I;
            var points = road.GetPermanentRoadPath();
            Need(points.Length == 71 && Mathf.Abs(points[0].x) < .01f && Mathf.Abs(points[^1].x) < .01f
                && Mathf.Abs(points[0].z + 3150f) < .01f && Mathf.Abs(points[^1].z + 2630f) < .01f,
                "Les accès de la route ont bougé.");
            for (int i = 1; i < points.Length; i++)
                Need(!(bool)Call(water,"Intersects",points[i-1],points[i],StationRoadNetwork247.MainRoadHalfWidth,false),
                    "L’emprise de la route rencontre encore de l’eau " + points[i]);

            var type = typeof(ManagementSaveData261).Assembly.GetType("SkiStation.AlpineMaterialCheckpoint261");
            string hash = (string)Invoke(type, "RoadHash", new[] {typeof(StationRoadNetwork247)}, road);
            string old = (string)typeof(StationRoadNetwork247).GetProperty("RetiredGeographyHash", Any).GetValue(road);
            Need(hash != old && old?.Length == 64, "Empreinte précédente absente.");
            var match = new[] {typeof(StationRoadNetwork247),typeof(ManagementSaveData261),typeof(string)};
            Need((bool)Invoke(type, "MatchesRoad", match, road, new ManagementSaveData261 {domainId = Domain}, old),
                "L’empreinte de la route d’origine est refusée.");
            Need(!(bool)Invoke(type, "MatchesRoad", match, road, new ManagementSaveData261 {domainId = "lab.gestion.orcieres"}, old),
                "La compatibilité Auron est appliquée à un autre domaine.");
            if (stage == 1)
            {
                Need(DemoFlow263.SaveManagement(), "Réenregistrement après migration refusé " + DemoFlow263.Notice);
                Debug.Log("AURON_ROUTE_PASS ancienne sauvegarde, 2 ponts, route entièrement hors d’eau et réenregistrement.");
                SessionState.SetInt(Key + "Stage",2);
                EditorApplication.ExitPlaymode();
                return;
            }
            Debug.Log("AURON_ROUTE_PASS reprise après réenregistrement avec la route corrigée et 2 ponts.");
            Finish(0);
        }
        catch (Exception error) { Debug.LogError("AURON_ROUTE_FAIL " + error); Finish(1); }
    }
    static void Finish(int code) { SessionState.SetBool(Key + "Active", false); EditorApplication.Exit(code); }
}
