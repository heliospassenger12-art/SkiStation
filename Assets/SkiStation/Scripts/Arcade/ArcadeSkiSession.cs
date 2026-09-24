using UnityEngine;

namespace SkiStation.Arcade
{
    // Attach only to a dedicated player avatar, NOT to any SkierRuntime / visitor.
    // Deliberately opt-in: no auto-instantiation, no demo domain, no changes to AI.
    public sealed class ArcadeSkiSession : MonoBehaviour
    {
        public ArcadeSkiController player;
        public Camera playerCamera;
        public Camera managementCamera;
        public MonoBehaviour managementCameraControls;
        public bool IsPlaying { get; private set; }

        private void Awake()
        {
            if (player != null) player.StopPlaying();
            if (playerCamera != null) playerCamera.enabled = false;
        }

        public bool EnterWorld(Vector3 safeSpawnPosition, Quaternion facing)
        {
            if (player == null || playerCamera == null) return false;
            if (managementCamera != null) managementCamera.enabled = false;
            if (managementCameraControls != null) managementCameraControls.enabled = false;
            player.gameObject.SetActive(true);
            player.BeginAt(safeSpawnPosition, facing);
            playerCamera.enabled = true;
            IsPlaying = true;
            return true;
        }

        public void ExitWorld()
        {
            if (!IsPlaying) return;
            if (player != null) player.StopPlaying();
            if (playerCamera != null) playerCamera.enabled = false;
            if (managementCamera != null) managementCamera.enabled = true;
            if (managementCameraControls != null) managementCameraControls.enabled = true;
            IsPlaying = false;
        }

        private void Update()
        {
            if (IsPlaying && Input.GetKeyDown(KeyCode.Escape)) ExitWorld();
        }
    }
}
