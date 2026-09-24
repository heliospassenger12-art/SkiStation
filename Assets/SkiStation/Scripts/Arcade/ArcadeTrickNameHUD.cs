using UnityEngine;
namespace SkiStation.Arcade
{
    // PlayerPrefs setting can later be exposed in the game's Settings menu.
    public sealed class ArcadeTrickNameHUD : MonoBehaviour
    {
        public ArcadeSkiController skier;
        public bool ShowTrickNames { get { return PlayerPrefs.GetInt("SkiStation.Arcade.ShowTrickNames", 1) != 0; }
            set { PlayerPrefs.SetInt("SkiStation.Arcade.ShowTrickNames", value ? 1 : 0); PlayerPrefs.Save(); } }
        string nameToShow = "";
        float hideAt;
        void Awake() { if (skier == null) skier = GetComponent<ArcadeSkiController>(); }
        void OnEnable() { if (skier != null) skier.TrickLanded += OnTrick; }
        void OnDisable() { if (skier != null) skier.TrickLanded -= OnTrick; }
        void OnTrick(string trick) { nameToShow = trick; hideAt = Time.unscaledTime + 2.5f; }
        void OnGUI()
        {
            if (skier == null || !skier.enabled) return;
            // Temporary test-scene settings control, later replaced by the game settings panel.
            if (GUI.Button(new Rect(18, Screen.height - 40, 185, 28),
                ShowTrickNames ? "Noms figures : OUI" : "Noms figures : NON")) ShowTrickNames = !ShowTrickNames;
            if (!ShowTrickNames || Time.unscaledTime >= hideAt) return;
            GUI.Box(new Rect(Screen.width * 0.5f - 115f, Screen.height - 76f, 230f, 28f), nameToShow);
        }
    }
}
