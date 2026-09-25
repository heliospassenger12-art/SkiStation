using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// Phase 2.76 - finition de presentation de la camera de gestion uniquement.
    /// Ne touche ni aux colliders ni au gameplay. Arcade explicitement exclu.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ManagementCameraPresentation276 : MonoBehaviour
    {
        [SerializeField] private float fieldOfView = 43f;
        [SerializeField] private float nearClip = .3f;
        [SerializeField] private float farClip = 14000f;
        [SerializeField] private float minimumHeight = 5f;
        private Camera cam;

        private void Start() { Apply(); }

        private void LateUpdate()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) { enabled = false; return; }
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            // Une focale un peu plus longue donne une lecture plus "maquette alpine"
            // sans changer les commandes ni la position choisie par le joueur.
            if (!cam.orthographic) cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fieldOfView, 1f - Mathf.Exp(-5f * Time.unscaledDeltaTime));
            cam.nearClipPlane = Mathf.Min(cam.nearClipPlane, nearClip);
            cam.farClipPlane = Mathf.Max(cam.farClipPlane, farClip);

            // Garde-fou visuel seulement : evite que la camera traverse la neige.
            if (ManagementSurface.TryGet(cam.transform.position, out var ground) && cam.transform.position.y < ground.y + minimumHeight)
                cam.transform.position = new Vector3(cam.transform.position.x, ground.y + minimumHeight, cam.transform.position.z);
        }

        private void Apply()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) { enabled = false; return; }
            cam = Camera.main;
            if (cam == null) return;
            cam.orthographic = false;
            cam.fieldOfView = fieldOfView;
            cam.nearClipPlane = nearClip;
            cam.farClipPlane = Mathf.Max(cam.farClipPlane, farClip);
            cam.allowHDR = true;
        }
    }

    public static class ManagementCameraPresentation276Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;
            if (Object.FindFirstObjectByType<ManagementCameraPresentation276>() == null)
                new GameObject("SkiStation_CameraPresentation_276").AddComponent<ManagementCameraPresentation276>();
        }
    }
}
