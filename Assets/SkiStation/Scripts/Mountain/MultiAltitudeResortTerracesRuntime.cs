using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum ResortAltitudeTier { Valley, MidMountain, Alpine }

    public struct ResortTerrace
    {
        public string id;
        public string displayName;
        public ResortAltitudeTier tier;
        public Vector3 center;
        public float radius;
        public float slopeDegrees;
        public float score;
        public bool buildable;
    }

    // 2.19 — discovers several naturally buildable resort hubs without flattening the mountain.
    // These are planning anchors only: the player remains free to ignore them and build one or many villages.
    public sealed class MultiAltitudeResortTerracesRuntime : MonoBehaviour
    {
        public static MultiAltitudeResortTerracesRuntime I { get; private set; }
        public IReadOnlyList<ResortTerrace> Terraces => terraces;
        private readonly List<ResortTerrace> terraces = new List<ResortTerrace>();

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        private void Start() { Rebuild(); }

        public void Rebuild()
        {
            terraces.Clear();
            var terrain = AlpineTerrainFoundationRuntime.I;
            if (terrain == null) return;

            AddBest(terrain, "valley", "Front de neige — Vallée", ResortAltitudeTier.Valley,
                new Vector3(0f,0f,-1500f), 900f, 55f, 420f, 8.5f);
            AddBest(terrain, "mid_west", "Village d'altitude — Ouest", ResortAltitudeTier.MidMountain,
                new Vector3(-820f,0f,-150f), 720f, 180f, 850f, 10.5f);
            AddBest(terrain, "mid_east", "Village d'altitude — Est", ResortAltitudeTier.MidMountain,
                new Vector3(820f,0f,-80f), 720f, 180f, 900f, 10.5f);
            AddBest(terrain, "alpine", "Station d'altitude", ResortAltitudeTier.Alpine,
                new Vector3(0f,0f,720f), 820f, 480f, 1250f, 11.5f);

            Debug.Log("SkiStation 2.19: MULTI-ALTITUDE RESORT TERRACES READY — " + terraces.Count + " optional buildable hubs discovered.");
        }

        private void AddBest(AlpineTerrainFoundationRuntime terrain, string id, string name, ResortAltitudeTier tier,
            Vector3 target, float searchRadius, float minAltitude, float maxAltitude, float maxSlope)
        {
            ResortTerrace best = new ResortTerrace { id=id, displayName=name, tier=tier, score=float.MaxValue, buildable=false };
            const float step = 70f;
            for (float z = target.z-searchRadius; z <= target.z+searchRadius; z += step)
            for (float x = target.x-searchRadius; x <= target.x+searchRadius; x += step)
            {
                var s = terrain.Sample(new Vector3(x,0f,z));
                if (s.altitude < minAltitude || s.altitude > maxAltitude || s.slopeDegrees > maxSlope) continue;
                float clearance = LocalFlatness(terrain, x, z, 90f);
                if (clearance > maxSlope + 2.5f) continue;
                float distance = Vector2.Distance(new Vector2(x,z), new Vector2(target.x,target.z)) / searchRadius;
                float score = s.slopeDegrees * 2.2f + clearance * 1.2f + distance * 8f;
                if (score >= best.score) continue;
                best.center = s.position + Vector3.up * 1.5f;
                best.radius = tier == ResortAltitudeTier.Valley ? 220f : (tier == ResortAltitudeTier.MidMountain ? 150f : 125f);
                best.slopeDegrees = s.slopeDegrees;
                best.score = score;
                best.buildable = true;
            }
            if (best.buildable) terraces.Add(best);
        }

        private static float LocalFlatness(AlpineTerrainFoundationRuntime terrain, float x, float z, float r)
        {
            float worst = 0f;
            Vector2[] offsets = { Vector2.zero, Vector2.right*r, Vector2.left*r, Vector2.up*r, Vector2.down*r,
                new Vector2(r*.7f,r*.7f), new Vector2(-r*.7f,r*.7f), new Vector2(r*.7f,-r*.7f), new Vector2(-r*.7f,-r*.7f) };
            for (int i=0;i<offsets.Length;i++)
                worst = Mathf.Max(worst, terrain.Sample(new Vector3(x+offsets[i].x,0f,z+offsets[i].y)).slopeDegrees);
            return worst;
        }

        public ResortTerrace FindNearest(Vector3 world, ResortAltitudeTier? tier = null)
        {
            ResortTerrace best = default(ResortTerrace); float dBest = float.MaxValue;
            for (int i=0;i<terraces.Count;i++)
            {
                if (tier.HasValue && terraces[i].tier != tier.Value) continue;
                float d = (terraces[i].center-world).sqrMagnitude;
                if (d < dBest) { dBest=d; best=terraces[i]; }
            }
            return best;
        }
    }

    public static class MultiAltitudeResortTerracesBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (Object.FindFirstObjectByType<MultiAltitudeResortTerracesRuntime>() != null) return;
            new GameObject("SkiStation_2.19_MULTI_ALTITUDE_RESORT_TERRACES").AddComponent<MultiAltitudeResortTerracesRuntime>();
        }
    }
}
