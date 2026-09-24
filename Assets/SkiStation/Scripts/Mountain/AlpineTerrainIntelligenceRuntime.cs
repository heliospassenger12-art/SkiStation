using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum AlpineSkiDifficulty { Beginner, Intermediate, Advanced, Expert, Unsuitable }
    public enum AlpineCorridorPurpose { SkiRun, LiftLine, BuildingCluster, Forest }

    public struct AlpineCorridorAssessment
    {
        public bool suitable;
        public float score;
        public float length;
        public float verticalDrop;
        public float averageSlope;
        public float maximumSlope;
        public float buildableFraction;
        public float skiableFraction;
        public AlpineSkiDifficulty skiDifficulty;

        public AlpineCorridorAssessment(bool ok, float s, float len, float drop, float avg, float max,
            float buildable, float skiable, AlpineSkiDifficulty difficulty)
        {
            suitable = ok; score = s; length = len; verticalDrop = drop; averageSlope = avg;
            maximumSlope = max; buildableFraction = buildable; skiableFraction = skiable;
            skiDifficulty = difficulty;
        }
    }

    // 1.73 — gameplay interpretation layer above the deterministic mountain.
    // Does not change rendering. Pistes, lifts and construction can all ask the same terrain authority.
    public sealed class AlpineTerrainIntelligenceRuntime : MonoBehaviour
    {
        public static AlpineTerrainIntelligenceRuntime I { get; private set; }
        public Vector3 BestResortHub { get; private set; }
        public Vector3 BestBeginnerHub { get; private set; }
        public Vector3 BestLiftBase { get; private set; }
        public Vector3 BestLiftTop { get; private set; }

        private AlpineTerrainFoundationRuntime terrain;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        public void Initialize(AlpineTerrainFoundationRuntime foundation)
        {
            terrain = foundation;
            if (terrain == null) return;
            BestResortHub = FindBestBuildSite(new Vector3(0f, 0f, -1550f), 700f, 90f);
            BestBeginnerHub = FindBestSkiHub(new Vector3(0f, 0f, -900f), 650f, AlpineSkiDifficulty.Beginner);
            BestLiftBase = FindBestLiftStation(new Vector3(0f, 0f, -1050f), 650f);
            BestLiftTop = FindBestLiftStation(new Vector3(0f, 0f, 1050f), 850f);
            Debug.Log("SkiStation 1.73: ALPINE TERRAIN INTELLIGENCE READY — piste difficulty, corridors, lift stations and construction sites available.");
        }

        public AlpineSkiDifficulty DifficultyAt(Vector3 worldPosition)
        {
            EnsureTerrain();
            if (terrain == null) return AlpineSkiDifficulty.Unsuitable;
            AlpineTerrainSample s = terrain.Sample(worldPosition);
            if (!s.skiable) return AlpineSkiDifficulty.Unsuitable;
            if (s.slopeDegrees <= 14f) return AlpineSkiDifficulty.Beginner;
            if (s.slopeDegrees <= 24f) return AlpineSkiDifficulty.Intermediate;
            if (s.slopeDegrees <= 34f) return AlpineSkiDifficulty.Advanced;
            if (s.slopeDegrees <= AlpineTerrainFoundationRuntime.SkiableMaxSlope) return AlpineSkiDifficulty.Expert;
            return AlpineSkiDifficulty.Unsuitable;
        }

        public float BuildingSuitability(Vector3 worldPosition, float desiredRadius)
        {
            EnsureTerrain();
            if (terrain == null) return 0f;
            float radius = Mathf.Max(10f, desiredRadius);
            float score = 0f; int count = 0;
            for (int iz = -2; iz <= 2; iz++)
            for (int ix = -2; ix <= 2; ix++)
            {
                Vector3 p = worldPosition + new Vector3(ix * radius * .45f, 0f, iz * radius * .45f);
                AlpineTerrainSample s = terrain.Sample(p);
                float local = s.buildable ? Mathf.Clamp01(1f - s.slopeDegrees / AlpineTerrainFoundationRuntime.BuildingMaxSlope) : 0f;
                score += local; count++;
            }
            return count > 0 ? score / count : 0f;
        }

        public float LiftStationSuitability(Vector3 worldPosition)
        {
            EnsureTerrain();
            if (terrain == null) return 0f;
            AlpineTerrainSample center = terrain.Sample(worldPosition);
            if (!center.buildable) return 0f;
            float flat = BuildingSuitability(worldPosition, 55f);
            float clearance = center.terrainClass == AlpineTerrainClass.ResortFloor || center.terrainClass == AlpineTerrainClass.GentleSkiSlope ? 1f : .72f;
            return Mathf.Clamp01(flat * clearance);
        }

        public AlpineCorridorAssessment AssessCorridor(Vector3 start, Vector3 end, AlpineCorridorPurpose purpose, int sampleCount)
        {
            EnsureTerrain();
            if (terrain == null) return new AlpineCorridorAssessment(false, 0f, 0f, 0f, 0f, 0f, 0f, 0f, AlpineSkiDifficulty.Unsuitable);
            int n = Mathf.Clamp(sampleCount, 8, 128);
            float slopeSum = 0f, maxSlope = 0f; int buildable = 0, skiable = 0;
            int beginner = 0, intermediate = 0, advanced = 0, expert = 0, unsuitable = 0;
            AlpineTerrainSample first = terrain.Sample(start);
            AlpineTerrainSample last = terrain.Sample(end);
            for (int i = 0; i < n; i++)
            {
                float t = n == 1 ? 0f : (float)i / (n - 1);
                AlpineTerrainSample s = terrain.Sample(Vector3.Lerp(start, end, t));
                slopeSum += s.slopeDegrees; maxSlope = Mathf.Max(maxSlope, s.slopeDegrees);
                if (s.buildable) buildable++;
                if (s.skiable) skiable++;
                AlpineSkiDifficulty d = DifficultyAt(s.position);
                if (d == AlpineSkiDifficulty.Beginner) beginner++;
                else if (d == AlpineSkiDifficulty.Intermediate) intermediate++;
                else if (d == AlpineSkiDifficulty.Advanced) advanced++;
                else if (d == AlpineSkiDifficulty.Expert) expert++;
                else unsuitable++;
            }
            float avg = slopeSum / n;
            float buildFraction = (float)buildable / n;
            float skiFraction = (float)skiable / n;
            float length = Vector2.Distance(new Vector2(start.x, start.z), new Vector2(end.x, end.z));
            float drop = first.altitude - last.altitude;
            AlpineSkiDifficulty routeDifficulty = DominantDifficulty(beginner, intermediate, advanced, expert, unsuitable);
            float score;
            bool suitable;
            if (purpose == AlpineCorridorPurpose.SkiRun)
            {
                score = skiFraction * 72f + Mathf.Clamp01(Mathf.Abs(drop) / 450f) * 18f + Mathf.Clamp01(length / 1800f) * 10f;
                suitable = skiFraction >= .82f && unsuitable <= Mathf.Max(1, n / 10);
            }
            else if (purpose == AlpineCorridorPurpose.LiftLine)
            {
                float baseStation = LiftStationSuitability(start);
                float topStation = LiftStationSuitability(end);
                score = (baseStation + topStation) * 30f + Mathf.Clamp01(Mathf.Abs(drop) / 700f) * 30f + Mathf.Clamp01(length / 2200f) * 10f;
                suitable = baseStation >= .35f && topStation >= .35f && length >= 180f;
            }
            else if (purpose == AlpineCorridorPurpose.BuildingCluster)
            {
                score = buildFraction * 100f; suitable = buildFraction >= .8f;
            }
            else
            {
                score = ForestFraction(start, end, n) * 100f; suitable = score >= 55f;
            }
            return new AlpineCorridorAssessment(suitable, Mathf.Clamp(score, 0f, 100f), length, drop, avg, maxSlope, buildFraction, skiFraction, routeDifficulty);
        }

        public List<Vector3> FindSkiablePath(Vector3 start, Vector3 target, AlpineSkiDifficulty desiredDifficulty, int steps)
        {
            EnsureTerrain();
            var path = new List<Vector3>();
            if (terrain == null) return path;
            Vector3 current = terrain.Sample(start).position; path.Add(current);
            int count = Mathf.Clamp(steps, 8, 96);
            for (int step = 0; step < count; step++)
            {
                Vector2 toTarget = new Vector2(target.x - current.x, target.z - current.z);
                if (toTarget.magnitude < 65f) break;
                Vector2 forward = toTarget.normalized;
                Vector3 best = current; float bestScore = float.MaxValue;
                for (int k = -4; k <= 4; k++)
                {
                    float angle = k * 12f;
                    Vector2 dir = Rotate(forward, angle);
                    Vector3 candidate = terrain.Sample(current + new Vector3(dir.x * 85f, 0f, dir.y * 85f)).position;
                    AlpineSkiDifficulty d = DifficultyAt(candidate);
                    if (d == AlpineSkiDifficulty.Unsuitable) continue;
                    float uphillPenalty = candidate.y > current.y + 5f ? (candidate.y - current.y) * 6f : 0f;
                    float difficultyPenalty = Mathf.Abs((int)d - (int)desiredDifficulty) * 22f;
                    float targetPenalty = Vector2.Distance(new Vector2(candidate.x, candidate.z), new Vector2(target.x, target.z)) * .015f;
                    float candidateScore = uphillPenalty + difficultyPenalty + targetPenalty + Mathf.Abs(angle) * .05f;
                    if (candidateScore < bestScore) { bestScore = candidateScore; best = candidate; }
                }
                if (best == current) break;
                current = best; path.Add(current);
            }
            return path;
        }

        private Vector3 FindBestBuildSite(Vector3 center, float radius, float footprint)
        {
            Vector3 best = terrain.Sample(center).position; float bestScore = -1f;
            for (float z = center.z - radius; z <= center.z + radius; z += 90f)
            for (float x = center.x - radius; x <= center.x + radius; x += 90f)
            {
                Vector3 p = terrain.Sample(new Vector3(x, 0f, z)).position;
                float score = BuildingSuitability(p, footprint) - Vector2.Distance(new Vector2(x,z), new Vector2(center.x,center.z)) / radius * .08f;
                if (score > bestScore) { bestScore = score; best = p; }
            }
            return best;
        }

        private Vector3 FindBestSkiHub(Vector3 center, float radius, AlpineSkiDifficulty desired)
        {
            Vector3 best = terrain.Sample(center).position; float bestScore = float.MaxValue;
            for (float z = center.z - radius; z <= center.z + radius; z += 80f)
            for (float x = center.x - radius; x <= center.x + radius; x += 80f)
            {
                Vector3 p = terrain.Sample(new Vector3(x, 0f, z)).position;
                AlpineSkiDifficulty d = DifficultyAt(p);
                if (d == AlpineSkiDifficulty.Unsuitable) continue;
                float score = Mathf.Abs((int)d - (int)desired) * 30f + Vector2.Distance(new Vector2(x,z), new Vector2(center.x,center.z)) / radius * 4f;
                if (score < bestScore) { bestScore = score; best = p; }
            }
            return best;
        }

        private Vector3 FindBestLiftStation(Vector3 center, float radius)
        {
            Vector3 best = terrain.Sample(center).position; float bestScore = -1f;
            for (float z = center.z - radius; z <= center.z + radius; z += 90f)
            for (float x = center.x - radius; x <= center.x + radius; x += 90f)
            {
                Vector3 p = terrain.Sample(new Vector3(x, 0f, z)).position;
                float score = LiftStationSuitability(p) - Vector2.Distance(new Vector2(x,z), new Vector2(center.x,center.z)) / radius * .06f;
                if (score > bestScore) { bestScore = score; best = p; }
            }
            return best;
        }

        private float ForestFraction(Vector3 start, Vector3 end, int n)
        {
            int ok = 0;
            for (int i = 0; i < n; i++)
            {
                float t = n == 1 ? 0f : (float)i / (n - 1);
                if (terrain.Sample(Vector3.Lerp(start, end, t)).forestSuitable) ok++;
            }
            return (float)ok / n;
        }

        private void EnsureTerrain()
        {
            if (terrain == null) terrain = AlpineTerrainFoundationRuntime.I;
        }

        private static AlpineSkiDifficulty DominantDifficulty(int b, int i, int a, int e, int u)
        {
            if (u > b && u > i && u > a && u > e) return AlpineSkiDifficulty.Unsuitable;
            if (e >= a && e >= i && e >= b) return AlpineSkiDifficulty.Expert;
            if (a >= i && a >= b) return AlpineSkiDifficulty.Advanced;
            if (i >= b) return AlpineSkiDifficulty.Intermediate;
            return AlpineSkiDifficulty.Beginner;
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad; float c = Mathf.Cos(r); float s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
    }
}
