// SkiStation — point d'entrée de la scène de gestion fusionnée. S'installe tout seul dans toute scène dont le nom commence
// par « SkiStation_Gestion » (les anciens systèmes ChatGPT y sont désactivés par ArcadeSceneIsolation), crée la caméra,
// la lumière, le relief, le rendu, les outils, l'interface et le pont arcade, puis fait avancer l'horloge du noyau.
using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using SkiStation.Gestion;

namespace SkiStation.GestionUnity
{
    public sealed class GestionBootstrap : MonoBehaviour
    {
        public const string SCENE_PREFIX = "SkiStation_Gestion";
        GestionWorld world; GestionRender render; GestionTools tools; GestionUI ui; GestionCamera camCtl; GestionArcade arcade; Camera cam; Light sun;
        static string SavePath { get { return Path.Combine(Application.persistentDataPath, "skistation_gestion.json"); } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (!SceneManager.GetActiveScene().name.StartsWith(SCENE_PREFIX, StringComparison.Ordinal)) return;
            if (FindFirstObjectByType<GestionBootstrap>() != null) return;
            new GameObject("SkiStation_Gestion").AddComponent<GestionBootstrap>();
        }

        void Awake()
        {
            Scenarios.LoadRecords = k => PlayerPrefs.HasKey(k) ? PlayerPrefs.GetString(k) : null;
            Scenarios.SaveRecords = (k, v) => { PlayerPrefs.SetString(k, v); PlayerPrefs.Save(); };
            Game.AutoSave = () => Save();
            Rand.Seed(Environment.TickCount);

            foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None)) c.gameObject.SetActive(false);
            var camGo = new GameObject("Camera de gestion"); camGo.tag = "MainCamera"; cam = camGo.AddComponent<Camera>(); camGo.AddComponent<AudioListener>();
            camCtl = camGo.AddComponent<GestionCamera>();
            tools = camGo.AddComponent<GestionTools>();
            var sunGo = new GameObject("Soleil"); sun = sunGo.AddComponent<Light>(); sun.type = LightType.Directional; sun.shadows = LightShadows.Soft; sun.shadowStrength = .7f;
            QualitySettings.shadowDistance = 2500;

            world = gameObject.AddComponent<GestionWorld>();
            render = gameObject.AddComponent<GestionRender>(); render.Build(sun);
            var arcGo = new GameObject("Arcade (rider ChatGPT)"); arcGo.transform.SetParent(transform); arcade = arcGo.AddComponent<GestionArcade>(); arcade.managementCamera = cam; arcade.managementControls = camCtl;
            ui = gameObject.AddComponent<GestionUI>(); ui.boot = this; ui.tools = tools; ui.camCtl = camCtl; ui.arcade = arcade; ui.render = render; ui.world = world;
            Game.Bus += (ev, a, b) => { if (ev == "season") { world.SetSummer(Game.G.summer); render.RebuildAll(); } };

            // relief de fond pour le menu : la montagne classique
            Game.T.Init(1337); Game.T.GenTrees(); world.Build(); camCtl.Focus(0, 2300, 3200); camCtl.pitch = 30;
        }

        void Update()
        {
            if (Game.G != null && !arcade.Playing) Sim.Tick(Time.unscaledDeltaTime);
            else if (Game.G == null) camCtl.yaw += Time.unscaledDeltaTime * 3;         // menu : la caméra tourne lentement autour du massif
            render.UpdateSky(cam);
        }

        // ---------- parties ----------
        public void NewGame(NewGameOptions o)
        {
            Game.NewGame(o); AfterLoad();
            camCtl.Focus(0, 2350, 2200); camCtl.pitch = 42; camCtl.yaw = 0;
            Game.Log(o.scn != null ? "Scénario « " + Catalog.Scenario(o.scn).name + " » : bonne chance !" : "Bac à sable : construisez librement.", "info");
        }
        void AfterLoad()
        {
            world.summerLook = Game.G.summer; world.RebuildAll(); render.RebuildAll(); tools.Cancel(); tools.Select(null);
            Game.G.paused = true;
        }
        public bool HasSave() { try { return File.Exists(SavePath); } catch (Exception) { return false; } }
        public void Continue()
        {
            try { var g = SaveLoad.Deserialize(File.ReadAllText(SavePath)); if (g == null) { ui.Toast("Sauvegarde illisible."); return; } SaveLoad.Restore(g); AfterLoad(); camCtl.Focus(0, 2350, 2200); }
            catch (Exception e) { Debug.LogException(e); ui.Toast("Sauvegarde illisible."); }
        }
        public bool Import(string code) { var g = SaveLoad.ImportCode(code); if (g == null) return false; SaveLoad.Restore(g); AfterLoad(); camCtl.Focus(0, 2350, 2200); return true; }
        public bool Save()
        {
            if (Game.G == null) return false;
            try { File.WriteAllText(SavePath, SaveLoad.Serialize(Game.G)); return true; } catch (Exception e) { Debug.LogWarning("SkiStation : sauvegarde impossible — " + e.Message); return false; }
        }
        public void ToMenu() { Game.G = null; Sim.Visitors.Clear(); Game.T.Init(1337); Game.T.GenTrees(); world.summerLook = false; world.RebuildAll(); render.ClearAll(); camCtl.Focus(0, 2300, 3200); }
        void OnApplicationQuit() { Save(); }
    }
}
