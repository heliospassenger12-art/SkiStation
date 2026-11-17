using UnityEngine;

namespace SkiStation
{
    /// <summary>Phase 54: management-mode start screen. Saves the selected rules, not the resort world.</summary>
    public sealed class SkiStationStartMenu249 : MonoBehaviour
    {
        private const string ModeKey = "SkiStation.P54.Mode";
        private const string FreeKey = "SkiStation.P54.SandboxFree";
        private bool showing = true;
        private bool confirming;
        private bool sandbox;
        private bool freeResearch = true;
        private float previousTimeScale = 1f;
        private GUIStyle titleStyle, subtitleStyle, cardStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<SkiStationStartMenu249>() != null) return;
            new GameObject("SkiStation_StartMenu_Phase54").AddComponent<SkiStationStartMenu249>();
        }

        private void Awake()
        {
            sandbox = PlayerPrefs.GetInt(ModeKey, 0) == 1;
            freeResearch = PlayerPrefs.GetInt(FreeKey, 1) == 1;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        private void OnDestroy()
        {
            if (showing) Time.timeScale = previousTimeScale;
        }

        private void OnGUI()
        {
            if (!showing) return;
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                subtitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                cardStyle = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.UpperCenter, wordWrap = true };
            }
            Color old = GUI.color;
            GUI.color = new Color(.035f, .09f, .15f, .97f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            float width = Mathf.Min(650f, Screen.width - 24f);
            float height = Mathf.Min(530f, Screen.height - 20f);
            Rect panel = new Rect((Screen.width - width) * .5f, (Screen.height - height) * .5f, width, height);
            GUI.Box(panel, "");
            GUILayout.BeginArea(new Rect(panel.x + 18f, panel.y + 14f, panel.width - 36f, panel.height - 28f));
            GUILayout.Label("SKISTATION", titleStyle, GUILayout.Height(46));
            GUILayout.Label("Crée ta station de ski", subtitleStyle, GUILayout.Height(30));
            GUILayout.Space(10);
            if (!confirming)
            {
                GUILayout.Label("MODE DE JEU", GUILayout.Height(24));
                if (GUILayout.Button((!sandbox ? "✓  " : "") + "Carrière — progression et laboratoire obligatoires", GUILayout.Height(45))) sandbox = false;
                if (GUILayout.Button((sandbox ? "✓  " : "") + "Bac à sable — construis librement ta station", GUILayout.Height(45))) sandbox = true;
                GUILayout.Space(10);
                if (sandbox)
                {
                    GUILayout.Label("RECHERCHE EN BAC À SABLE", GUILayout.Height(24));
                    if (GUILayout.Button((freeResearch ? "✓  " : "") + "Toutes les remontées disponibles dès le départ", GUILayout.Height(40))) freeResearch = true;
                    if (GUILayout.Button((!freeResearch ? "✓  " : "") + "Progression avec laboratoire de recherche", GUILayout.Height(40))) freeResearch = false;
                }
                else GUILayout.Label("Départ : téléski classique et télésiège fixe 2 places.\nLes autres remontées se débloquent au laboratoire.", cardStyle, GUILayout.Height(80));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Continuer — vérifier les paramètres", GUILayout.Height(45))) confirming = true;
            }
            else
            {
                GUILayout.Label("RÉCAPITULATIF AVANT CRÉATION", GUILayout.Height(30));
                GUILayout.Label("Mode : " + (sandbox ? "Bac à sable" : "Carrière"), GUILayout.Height(28));
                GUILayout.Label("Remontées : " + (sandbox && freeResearch ? "Catalogue entièrement débloqué" : "Progression avec laboratoire"), GUILayout.Height(28));
                GUILayout.Label(sandbox && freeResearch ? "Toutes les remontées du catalogue sont disponibles." : "Au départ : téléski classique et télésiège fixe 2 places.", cardStyle, GUILayout.Height(70));
                GUILayout.Label("Ces paramètres seront mémorisés sur cet ordinateur. La sauvegarde du monde de la station n'est pas encore intégrée.", GUILayout.Height(52));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Modifier", GUILayout.Height(38))) confirming = false;
                if (GUILayout.Button("Confirmer et commencer", GUILayout.Height(46))) StartGame();
            }
            GUILayout.EndArea();
            GUI.color = old;
        }

        private void StartGame()
        {
            LiftCareerCatalog249.StartNewSession(sandbox, sandbox && freeResearch);
            PlayerPrefs.SetInt(ModeKey, sandbox ? 1 : 0);
            PlayerPrefs.SetInt(FreeKey, freeResearch ? 1 : 0);
            PlayerPrefs.Save();
            showing = false;
            Time.timeScale = previousTimeScale;
            enabled = false;
        }
    }
}
