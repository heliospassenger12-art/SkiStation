using UnityEngine;
namespace SkiStation.Arcade
{
    // Unscaled HUD time: a big-air landing remains readable during the optional micro slow-motion.
    public sealed class ArcadeLandingCelebrationHUD : MonoBehaviour
    {
        public ArcadeSkiController skier;
        float expiresAt;
        string message = "";
        void Awake() { if (skier == null) skier = GetComponent<ArcadeSkiController>(); }
        void OnEnable() { if (skier != null) skier.Landed += OnLanded; }
        void OnDisable() { if (skier != null) skier.Landed -= OnLanded; }
        void OnLanded(float impact)
        {
            if (skier == null || !skier.LastLandingHadSuccessfulTrick) return;
            message = skier.LastTrickName + "  |  Combo x" + skier.ComboCount +
                "  |  Boost " + Mathf.RoundToInt(skier.BoostFraction * 100f) + "%";
            expiresAt = Time.unscaledTime + 2.3f;
        }
        void OnGUI()
        {
            if (skier == null || !skier.enabled || Time.unscaledTime > expiresAt ||
                PlayerPrefs.GetInt("SkiStation.Arcade.ShowTrickNames", 1) == 0) return;
            GUI.Box(new Rect(Screen.width * 0.5f - 195f, Screen.height - 112f, 390f, 29f), message);
        }
    }
}
