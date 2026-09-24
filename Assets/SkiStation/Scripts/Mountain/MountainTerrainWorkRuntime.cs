using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// Non-destructive terrain work layer. Stores height deltas on the procedural mountain
    /// and rebuilds the visible/collision mesh after edits.
    /// </summary>
    public sealed class MountainTerrainWorkRuntime : MonoBehaviour
    {
        public static MountainTerrainWorkRuntime I { get; private set; }
        [SerializeField, Min(0.1f)] float maxDelta = 40f;
        [SerializeField] bool rebuildCollider = true;
        float[,] deltas;
        int resolution;

        public float MaxDelta => maxDelta;
        public bool IsReady => deltas != null && MountainRuntime.I != null && MountainRuntime.I.IsGenerated;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        public void Initialize(int targetResolution)
        {
            resolution = Mathf.Max(2, targetResolution);
            deltas = new float[resolution, resolution];
        }

        public float GetDelta(int x, int z)
        {
            if (deltas == null || x < 0 || z < 0 || x >= resolution || z >= resolution) return 0f;
            return deltas[x, z];
        }

        public void ApplyBrush(Vector3 worldPosition, float radius, float strength, bool flatten, float targetHeight = 0f)
        {
            var mountain = MountainRuntime.I;
            if (mountain == null || !mountain.IsGenerated || radius <= 0f || deltas == null) return;
            Vector3 local = mountain.transform.InverseTransformPoint(worldPosition);
            float size = mountain.Definition.size;
            int centerX = Mathf.RoundToInt((local.x / size + 0.5f) * (resolution - 1));
            int centerZ = Mathf.RoundToInt((local.z / size + 0.5f) * (resolution - 1));
            float cell = size / (resolution - 1);
            int cells = Mathf.CeilToInt(radius / cell);

            for (int z = Mathf.Max(0, centerZ - cells); z <= Mathf.Min(resolution - 1, centerZ + cells); z++)
            for (int x = Mathf.Max(0, centerX - cells); x <= Mathf.Min(resolution - 1, centerX + cells); x++)
            {
                float px = (x / (float)(resolution - 1) - 0.5f) * size;
                float pz = (z / (float)(resolution - 1) - 0.5f) * size;
                float distance = Vector2.Distance(new Vector2(px, pz), new Vector2(local.x, local.z));
                if (distance > radius) continue;
                float falloff = 1f - Mathf.Clamp01(distance / radius);
                falloff = falloff * falloff * (3f - 2f * falloff);
                float current = mountain.GetBaseHeightAtGrid(x, z) + deltas[x, z];
                float delta = flatten ? (targetHeight - current) * strength * falloff : strength * falloff;
                deltas[x, z] = Mathf.Clamp(deltas[x, z] + delta, -maxDelta, maxDelta);
            }
            mountain.RebuildFromTerrainWorks(this, rebuildCollider);
        }

        public void ClearAll()
        {
            if (deltas == null) return;
            System.Array.Clear(deltas, 0, deltas.Length);
            if (MountainRuntime.I != null) MountainRuntime.I.RebuildFromTerrainWorks(this, rebuildCollider);
        }
    }
}
