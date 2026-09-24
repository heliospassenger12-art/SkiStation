using System.Collections;
using UnityEngine;

namespace SkiStation.Arcade
{
    // Isolated arcade test visuals: crater is a temporary decal-like mesh, not terrain deformation.
    public sealed class ArcadeBigAirLanding : MonoBehaviour
    {
        public ArcadeSkiController skier;
        public float minimumJumpDrop = 20f;
        public float minimumImpactSpeed = 26f;
        public float slowMotionScale = 0.52f;
        public float slowMotionRealSeconds = 0.19f;
        public float craterLifetime = 7f;
        public bool SlowMotionEnabled { get; private set; }
        const string SettingKey = "SkiStation.Arcade.BigAirSlowMotion";
        Coroutine slowMotion;
        float previousTimeScale = 1f;
        bool ownsTimeScale;
        Material craterMaterial;

        void Awake()
        {
            if (skier == null) skier = GetComponent<ArcadeSkiController>();
            SlowMotionEnabled = PlayerPrefs.GetInt(SettingKey, 1) != 0;
        }
        void OnEnable() { if (skier != null) skier.Landed += OnLanding; }
        void OnDisable()
        {
            if (skier != null) skier.Landed -= OnLanding;
            if (slowMotion != null) StopCoroutine(slowMotion);
            RestoreTime();
        }
        public void SetSlowMotionEnabled(bool enabled)
        {
            SlowMotionEnabled = enabled;
            PlayerPrefs.SetInt(SettingKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            if (!enabled) { if (slowMotion != null) StopCoroutine(slowMotion); slowMotion = null; RestoreTime(); }
        }
        void OnLanding(float impact)
        {
            if (skier == null || !skier.LastLandingHadSuccessfulTrick ||
                skier.LastJumpDrop < minimumJumpDrop || impact < minimumImpactSpeed) return;
            // Crater and slow motion are tied to the SAME successful exceptional landing.
            CreateTemporaryCrater(impact);
            if (SlowMotionEnabled && slowMotion == null) slowMotion = StartCoroutine(BriefSlowMotion());
        }
        IEnumerator BriefSlowMotion()
        {
            previousTimeScale = Time.timeScale;
            ownsTimeScale = true;
            Time.timeScale = Mathf.Min(previousTimeScale, Mathf.Clamp(slowMotionScale, 0.1f, 1f));
            yield return new WaitForSecondsRealtime(slowMotionRealSeconds);
            RestoreTime();
            slowMotion = null;
        }
        void RestoreTime()
        {
            if (!ownsTimeScale) return;
            Time.timeScale = previousTimeScale;
            ownsTimeScale = false;
        }
        void CreateTemporaryCrater(float impact)
        {
            if (craterMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader == null) return;
                craterMaterial = new Material(shader);
                craterMaterial.color = new Color(0.43f, 0.64f, 0.75f, 1f);
            }
            // Flat, darkened oval + snow rim: a visual impression of a crater.
            // No terrain/collider edits; the skier can continue without a physics snag.
            GameObject crater = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crater.name = "Temporary Big Air snow crater (visual only)";
            crater.transform.position = skier.transform.position + Vector3.down * 0.14f;
            float radius = Mathf.Clamp(impact * 0.075f, 1.8f, 5f);
            crater.transform.localScale = new Vector3(radius, 0.012f, radius);
            crater.GetComponent<Renderer>().sharedMaterial = craterMaterial;
            Collider collider = crater.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            // Raised rim made of non-colliding snow segments. Visual only; no terrain deformation.
            Material rimMaterial = new Material(craterMaterial);
            rimMaterial.color = new Color(0.87f, 0.95f, 1f, 1f);
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI * 2f / 12f;
                GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rim.name = "Temporary crater snow rim";
                rim.transform.SetParent(crater.transform, false);
                rim.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.48f, 1.8f, Mathf.Sin(angle) * 0.48f);
                rim.transform.localScale = new Vector3(0.24f, 12f, 0.24f);
                rim.GetComponent<Renderer>().sharedMaterial = rimMaterial;
                Collider rimCollider = rim.GetComponent<Collider>();
                if (rimCollider != null) Destroy(rimCollider);
            }
            Destroy(rimMaterial, craterLifetime + 0.2f);
            Destroy(crater, craterLifetime);
        }
    }
}
