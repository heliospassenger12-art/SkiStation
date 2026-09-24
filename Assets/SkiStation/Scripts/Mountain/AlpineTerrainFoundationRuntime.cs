using UnityEngine;

namespace SkiStation
{
    public enum AlpineTerrainClass
    {
        ResortFloor,
        GentleSkiSlope,
        SkiSlope,
        SteepSlope,
        RockFace,
        RidgeOrSummit
    }

    public struct AlpineTerrainSample
    {
        public Vector3 position;
        public Vector3 normal;
        public float altitude;
        public float slopeDegrees;
        public AlpineTerrainClass terrainClass;
        public bool buildable;
        public bool skiable;
        public bool forestSuitable;

        public AlpineTerrainSample(Vector3 p, Vector3 n, float h, float slope, AlpineTerrainClass type, bool canBuild, bool canSki, bool forest)
        {
            position = p; normal = n; altitude = h; slopeDegrees = slope; terrainClass = type;
            buildable = canBuild; skiable = canSki; forestSuitable = forest;
        }
    }

    // 1.71 — single terrain-query authority for pistes, lifts, buildings, forests and future biomes.
    // It samples the exact same procedural height function as the visible 1.70 massif.
    public sealed class AlpineTerrainFoundationRuntime : MonoBehaviour
    {
        public static AlpineTerrainFoundationRuntime I { get; private set; }
        public const float MassifHalfSize = 2100f;
        public const float ResortFloorMaxSlope = 8f;
        public const float BuildingMaxSlope = 12f;
        public const float SkiableMaxSlope = 48f;

        public float MinAltitude { get; private set; }
        public float MaxAltitude { get; private set; }
        public Vector3 SuggestedResortCenter { get; private set; }
        public Vector3 SuggestedBeginnerArea { get; private set; }

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        public AlpineTerrainSample Sample(Vector3 worldPosition)
        {
            float x = Mathf.Clamp(worldPosition.x, -AlpineMassifGeneratorV2.HalfWidth, AlpineMassifGeneratorV2.HalfWidth);
            float z = Mathf.Clamp(worldPosition.z, AlpineMassifGeneratorV2.MinZ, MassifHalfSize);
            const float e = 12f;
            float h = SkiStationGuaranteedMountainVisual.Height(x, z);
            float hx0 = SkiStationGuaranteedMountainVisual.Height(x - e, z);
            float hx1 = SkiStationGuaranteedMountainVisual.Height(x + e, z);
            float hz0 = SkiStationGuaranteedMountainVisual.Height(x, z - e);
            float hz1 = SkiStationGuaranteedMountainVisual.Height(x, z + e);
            Vector3 normal = new Vector3(hx0 - hx1, 2f * e, hz0 - hz1).normalized;
            float slope = Vector3.Angle(normal, Vector3.up);
            AlpineTerrainClass type = Classify(h, slope, z);
            bool buildable = slope <= BuildingMaxSlope && type != AlpineTerrainClass.RockFace && type != AlpineTerrainClass.RidgeOrSummit;
            // Flat snow on the resort floor is valid for beginner pistes and run-outs.
            // A 2-degree minimum previously rejected broad, visibly snowy areas.
            bool skiable = slope <= SkiableMaxSlope && type != AlpineTerrainClass.RockFace && type != AlpineTerrainClass.RidgeOrSummit;
            bool forest = h >= 110f && h <= 900f && slope <= 34f && z < 1050f && type != AlpineTerrainClass.ResortFloor;
            return new AlpineTerrainSample(new Vector3(x, h, z), normal, h, slope, type, buildable, skiable, forest);
        }

        public float HeightAt(float x, float z) { return SkiStationGuaranteedMountainVisual.Height(x, z); }
        public bool IsInsideMassif(Vector3 p) { return Mathf.Abs(p.x) <= AlpineMassifGeneratorV2.HalfWidth && p.z >= AlpineMassifGeneratorV2.MinZ && p.z <= MassifHalfSize; }

        public void RebuildSummary()
        {
            MinAltitude = float.MaxValue; MaxAltitude = float.MinValue;
            for (int z = -3400; z <= 2100; z += 100)
            for (int x = -4000; x <= 4000; x += 200)
            {
                float h = HeightAt(x, z);
                MinAltitude = Mathf.Min(MinAltitude, h);
                MaxAltitude = Mathf.Max(MaxAltitude, h);
            }
            SuggestedResortCenter = FindBestFlatPoint(new Vector3(0f, 0f, -2400f), 650f);
            SuggestedBeginnerArea = FindBestFlatPoint(new Vector3(0f, 0f, -1050f), 500f);
            Debug.Log("SkiStation 1.71: ALPINE TERRAIN FOUNDATION READY — altitude " + MinAltitude.ToString("0") + "–" + MaxAltitude.ToString("0") + " m; terrain classes/buildability/skiability available.");
        }

        private Vector3 FindBestFlatPoint(Vector3 center, float radius)
        {
            AlpineTerrainSample best = Sample(center);
            float bestScore = best.slopeDegrees;
            for (float z = center.z - radius; z <= center.z + radius; z += 80f)
            for (float x = center.x - radius; x <= center.x + radius; x += 80f)
            {
                var s = Sample(new Vector3(x, 0f, z));
                float distancePenalty = Vector2.Distance(new Vector2(x,z), new Vector2(center.x,center.z)) / radius * 2f;
                float score = s.slopeDegrees + distancePenalty + (s.buildable ? 0f : 100f);
                if (score < bestScore) { best = s; bestScore = score; }
            }
            return best.position;
        }

        private static AlpineTerrainClass Classify(float altitude, float slope, float z)
        {
            if (z < -850f && slope <= ResortFloorMaxSlope) return AlpineTerrainClass.ResortFloor;
            if (altitude > 1500f || (altitude > 1200f && slope > 38f)) return AlpineTerrainClass.RidgeOrSummit;
            if (slope > SkiableMaxSlope) return AlpineTerrainClass.RockFace;
            if (slope > 36f) return AlpineTerrainClass.SteepSlope;
            if (slope > 18f) return AlpineTerrainClass.SkiSlope;
            return AlpineTerrainClass.GentleSkiSlope;
        }
    }
}
