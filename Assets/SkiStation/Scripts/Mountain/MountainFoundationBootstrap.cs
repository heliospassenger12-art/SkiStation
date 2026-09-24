using System.Collections;
using UnityEngine;

namespace SkiStation
{
    public sealed class MountainFoundationBootstrap : MonoBehaviour
    {
        [SerializeField] private bool createMountainIfMissing = true;
        [SerializeField] private MountainDefinition definition;
        [SerializeField] private bool autoFrameCamera = true;
        private MountainRuntime mountain;

        private void Awake()
        {
            mountain = MountainRuntime.I;
            if (mountain == null && createMountainIfMissing)
            {
                var go = new GameObject("SkiStation_Mountain");
                mountain = go.AddComponent<MountainRuntime>();
            }
            if (mountain == null) return;
            if (definition != null) mountain.Configure(definition);
            mountain.Generate();
            EnsureMountainVisible();
            EnsureLighting();
            if (autoFrameCamera) FrameCamera();
            StartCoroutine(FrameAgainAfterStartup());
        }

        private IEnumerator FrameAgainAfterStartup()
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            if (mountain == null) mountain = MountainRuntime.I;
            if (mountain == null) yield break;
            EnsureMountainVisible();
            EnsureLighting();
            if (autoFrameCamera)
            {
                // Keep the visual camera authoritative during the first second, after every other Start().
                for (int i = 0; i < 30; i++) { FrameCamera(); yield return null; }
            }
            Debug.Log("SkiStation 1.65: Display 1 + full viewport forced; mountain + camera forced visible. Altitude " + mountain.MinAltitude.ToString("0") + "–" + mountain.MaxAltitude.ToString("0") + " m.");
        }

        private void EnsureMountainVisible()
        {
            if (mountain == null) return;
            var renderer = mountain.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = true;
                // 2.15: use a lit snow material so relief, ridges and shadows are finally readable.
                var lit = Shader.Find("Universal Render Pipeline/Lit");
                if (lit == null) lit = Shader.Find("Standard");
                if (lit != null)
                {
                    var mat = new Material(lit);
                    mat.name = "SkiStation_2_15_LitAlpineSnow";
                    mat.color = new Color(0.88f, 0.93f, 0.98f, 1f);
                    if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", .18f);
                    if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
                    renderer.sharedMaterial = mat;
                }
                else if (renderer.sharedMaterial == null)
                    renderer.sharedMaterial = RuntimeMaterialFactory.Create(new Color(0.88f, 0.93f, 0.98f));
            }
            mountain.gameObject.SetActive(true);
            mountain.transform.position = Vector3.zero;
            mountain.transform.rotation = Quaternion.identity;
            mountain.transform.localScale = Vector3.one;
        }

        private void EnsureLighting()
        {
            RenderSettings.ambientLight = new Color(0.62f, 0.66f, 0.72f, 1f);
            Light sun = null;
            foreach (var light in FindObjectsOfType<Light>())
            {
                if (light != null && light.type == LightType.Directional) { sun = light; break; }
            }
            if (sun == null)
            {
                var go = new GameObject("SkiStation_Sun");
                sun = go.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.enabled = true;
            sun.intensity = 1.25f;
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        }

        private void FrameCamera()
        {
            if (mountain == null || !mountain.IsGenerated) return;

            Camera cam = Camera.main;
            if (cam == null) cam = FindObjectOfType<Camera>();
            if (cam == null)
            {
                var cameraGo = new GameObject("SkiStation_VisualCamera");
                cameraGo.tag = "MainCamera";
                cam = cameraGo.AddComponent<Camera>();
            }

            // 1.64 visual isolation: one camera only, all layers, no render texture.
            foreach (var other in FindObjectsOfType<Camera>())
                if (other != null && other != cam) other.enabled = false;

            float size = mountain.Definition != null ? mountain.Definition.size : 1200f;
            float relief = Mathf.Max(200f, mountain.MaxAltitude - mountain.MinAltitude);
            Vector3 target = new Vector3(0f, mountain.MinAltitude + relief * 0.42f, 0f);

            cam.enabled = true;
            cam.targetTexture = null;
            cam.targetDisplay = 0;
            cam.rect = new Rect(0f, 0f, 1f, 1f);
            cam.cullingMask = ~0;
            cam.depth = 100f;
            cam.orthographic = false;
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 10000f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.48f, 0.72f, 0.92f, 1f);
            cam.transform.position = target + new Vector3(-size * 0.72f, relief * 1.55f + 260f, -size * 0.88f);
            cam.transform.LookAt(target, Vector3.up);

            // 2.15: diagnostic summit beacon removed.
        }
    }
}
