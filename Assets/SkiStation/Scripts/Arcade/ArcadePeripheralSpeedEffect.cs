using UnityEngine;

namespace SkiStation.Arcade
{
    // Lightweight optional speed feedback. URP uses edge streak overlay; built-in pipeline
    // additionally uses a peripheral multi-tap blur shader when OnRenderImage is supported.
    [RequireComponent(typeof(Camera))]
    public sealed class ArcadePeripheralSpeedEffect : MonoBehaviour
    {
        public ArcadeSkiController controller;
        public Shader peripheralBlurShader;
        [Range(0f, 1f)] public float maximumIntensity = 0.78f;
        public float effectBeginsAtSpeed = 9f;
        public float fullEffectAtSpeed = 40f;
        public float response = 4f;
        public bool showPeripheralStreaks = true;
        public bool showBlurWhenSupported = true;
        public bool softenEffectsInAir = true;
        float intensity;
        Material blurMaterial;
        Texture2D streakTexture;
        Camera attachedCamera;

        void Awake()
        {
            attachedCamera = GetComponent<Camera>();
            if (peripheralBlurShader == null)
                peripheralBlurShader = Shader.Find("Hidden/SkiStation/ArcadePeripheralBlur");
            if (peripheralBlurShader != null && peripheralBlurShader.isSupported)
                blurMaterial = new Material(peripheralBlurShader) { hideFlags = HideFlags.HideAndDontSave };
            streakTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            streakTexture.SetPixel(0, 0, Color.white);
            streakTexture.Apply();
        }
        void Update()
        {
            float speed = controller == null ? 0f : controller.Speed;
            float denominator = Mathf.Max(0.1f, fullEffectAtSpeed - effectBeginsAtSpeed);
            float target = Mathf.SmoothStep(0f, maximumIntensity,
                Mathf.Clamp01((speed - effectBeginsAtSpeed) / denominator));
            if (softenEffectsInAir && controller != null && controller.IsAirborne) target *= 0.35f;
            if (controller != null && controller.IsCrashed) target = 0f;
            intensity = Mathf.Lerp(intensity, target, 1f - Mathf.Exp(-response * Time.deltaTime));
        }
        // Unity built-in render pipeline only. URP does not call OnRenderImage; edge streaks
        // and the widened FOV remain active there, without pretending a real blur is applied.
        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!showBlurWhenSupported || blurMaterial == null || intensity < 0.005f)
            {
                Graphics.Blit(source, destination); return;
            }
            blurMaterial.SetFloat("_Intensity", intensity);
            Graphics.Blit(source, destination, blurMaterial);
        }
        void OnGUI()
        {
            if (!showPeripheralStreaks || intensity < 0.015f || streakTexture == null ||
                attachedCamera == null || !attachedCamera.enabled) return;
            // Deterministic screen-space peripheral streaks; center/avatar remains untouched.
            float w = Screen.width, h = Screen.height;
            float alpha = intensity * 0.18f;
            Color previous = GUI.color;
            for (int i = 0; i < 18; i++)
            {
                float phase = Mathf.Repeat(Time.time * (0.65f + intensity * 1.7f) + i * 0.618034f, 1f);
                float y = h * (0.12f + Mathf.Repeat(i * 0.173f + phase * 0.38f, 0.76f));
                float length = w * (0.015f + intensity * 0.075f) * (0.4f + phase);
                float edge = w * (0.025f + Mathf.Repeat(i * 0.131f, 0.13f));
                GUI.color = new Color(0.91f, 0.97f, 1f, alpha * (0.35f + phase * 0.65f));
                GUI.DrawTexture(new Rect(edge, y, length, 1.4f + intensity * 2f), streakTexture);
                GUI.DrawTexture(new Rect(w - edge - length, y, length, 1.4f + intensity * 2f), streakTexture);
            }
            for (int i = 0; i < 9; i++)
            {
                float x = w * (0.12f + i * 0.095f);
                float phase = Mathf.Repeat(Time.time * (0.9f + intensity * 2f) + i * 0.211f, 1f);
                GUI.color = new Color(0.92f, 0.98f, 1f, alpha * 0.45f * phase);
                GUI.DrawTexture(new Rect(x, h * (0.91f + phase * 0.07f), 1.5f, h * intensity * 0.08f), streakTexture);
            }
            GUI.color = previous;
        }
        void OnDestroy()
        {
            if (blurMaterial != null) Destroy(blurMaterial);
            if (streakTexture != null) Destroy(streakTexture);
        }
    }
}
