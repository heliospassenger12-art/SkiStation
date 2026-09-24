using UnityEngine;

namespace SkiStation.Arcade
{
    // Optional standalone HUD. Attach to the player avatar in the test scene.
    public sealed class ArcadeComboHUD : MonoBehaviour
    {
        public ArcadeSkiController skier;
        void Awake() { if (skier == null) skier = GetComponent<ArcadeSkiController>(); }
        void OnGUI()
        {
            if (skier == null || !skier.enabled || skier.ComboCount < 1) return;
            GUI.Box(new Rect(Screen.width - 230f, 100f, 210f, 65f),
                "FIGURES x" + skier.ComboCount + "  |  GAIN x" + skier.ComboMultiplier.ToString("0.0")
                + "\n" + skier.ComboTimeRemaining.ToString("0.0") + " s pour enchainer");
        }
    }
}
