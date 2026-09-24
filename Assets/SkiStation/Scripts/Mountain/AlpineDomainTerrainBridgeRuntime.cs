using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    // 1.74 — bridge between the alpine terrain authority and the existing piste/lift domain.
    // Keeps construction systems independent while giving them one deterministic source of terrain truth.
    public sealed class AlpineDomainTerrainBridgeRuntime : MonoBehaviour
    {
        public static AlpineDomainTerrainBridgeRuntime I { get; private set; }
        private AlpineTerrainFoundationRuntime terrain;
        private AlpineTerrainIntelligenceRuntime intelligence;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        public void Initialize(AlpineTerrainFoundationRuntime foundation, AlpineTerrainIntelligenceRuntime terrainIntelligence)
        {
            terrain = foundation;
            intelligence = terrainIntelligence;
            Debug.Log("SkiStation 1.74: ALPINE DOMAIN BRIDGE READY — pistes and lifts now share the massif terrain authority.");
        }

        public List<Vector3> ConformSlopePoints(IReadOnlyList<Vector3> source, float surfaceOffset)
        {
            var result = new List<Vector3>();
            EnsureReferences();
            if (source == null) return result;
            for (int i = 0; i < source.Count; i++)
            {
                if (terrain == null) { result.Add(source[i]); continue; }
                AlpineTerrainSample sample = terrain.Sample(source[i]);
                result.Add(sample.position + sample.normal * Mathf.Max(0.05f, surfaceOffset));
            }
            return result;
        }

        public AlpineCorridorAssessment AssessSlope(IReadOnlyList<Vector3> route)
        {
            EnsureReferences();
            if (route == null || route.Count < 2 || intelligence == null)
                return new AlpineCorridorAssessment(false, 0f, 0f, 0f, 0f, 0f, 0f, 0f, AlpineSkiDifficulty.Unsuitable);
            return AssessPolyline(route, AlpineCorridorPurpose.SkiRun);
        }

        public AlpineCorridorAssessment AssessLift(IReadOnlyList<Vector3> route)
        {
            EnsureReferences();
            if (route == null || route.Count < 2 || intelligence == null)
                return new AlpineCorridorAssessment(false, 0f, 0f, 0f, 0f, 0f, 0f, 0f, AlpineSkiDifficulty.Unsuitable);
            return AssessPolyline(route, AlpineCorridorPurpose.LiftLine);
        }

        public bool DifficultyMatches(SlopeDefinition definition, AlpineCorridorAssessment assessment)
        {
            if (definition == null || !assessment.suitable) return false;
            AlpineSkiDifficulty expected = DefinitionDifficulty(definition);
            if (assessment.skiDifficulty == AlpineSkiDifficulty.Unsuitable) return false;
            return Mathf.Abs((int)expected - (int)assessment.skiDifficulty) <= 1;
        }

        public Vector3 SnapLiftStation(Vector3 position)
        {
            EnsureReferences();
            return terrain != null ? terrain.Sample(position).position : position;
        }

        private AlpineCorridorAssessment AssessPolyline(IReadOnlyList<Vector3> route, AlpineCorridorPurpose purpose)
        {
            float weightedScore = 0f, length = 0f, drop = 0f, slope = 0f, maxSlope = 0f, build = 0f, ski = 0f;
            int segments = 0;
            AlpineSkiDifficulty hardest = AlpineSkiDifficulty.Beginner;
            bool suitable = true;
            for (int i = 1; i < route.Count; i++)
            {
                AlpineCorridorAssessment a = intelligence.AssessCorridor(route[i - 1], route[i], purpose, 16);
                float w = Mathf.Max(1f, a.length);
                weightedScore += a.score * w; slope += a.averageSlope * w; build += a.buildableFraction * w; ski += a.skiableFraction * w;
                length += w; drop += a.verticalDrop; maxSlope = Mathf.Max(maxSlope, a.maximumSlope); segments++;
                suitable &= a.suitable;
                if (a.skiDifficulty != AlpineSkiDifficulty.Unsuitable && (int)a.skiDifficulty > (int)hardest) hardest = a.skiDifficulty;
                if (a.skiDifficulty == AlpineSkiDifficulty.Unsuitable && purpose == AlpineCorridorPurpose.SkiRun) hardest = AlpineSkiDifficulty.Unsuitable;
            }
            if (segments == 0 || length <= 0f)
                return new AlpineCorridorAssessment(false, 0f, 0f, 0f, 0f, 0f, 0f, 0f, AlpineSkiDifficulty.Unsuitable);
            return new AlpineCorridorAssessment(suitable, weightedScore / length, length, drop, slope / length, maxSlope, build / length, ski / length, hardest);
        }

        private static AlpineSkiDifficulty DefinitionDifficulty(SlopeDefinition definition)
        {
            if (definition == null) return AlpineSkiDifficulty.Unsuitable;
            string id = (definition.id ?? string.Empty).ToLowerInvariant();
            if (id.Contains("green") || id.Contains("verte")) return AlpineSkiDifficulty.Beginner;
            if (id.Contains("blue") || id.Contains("bleue")) return AlpineSkiDifficulty.Intermediate;
            if (id.Contains("red") || id.Contains("rouge")) return AlpineSkiDifficulty.Advanced;
            return AlpineSkiDifficulty.Expert;
        }

        private void EnsureReferences()
        {
            if (terrain == null) terrain = AlpineTerrainFoundationRuntime.I;
            if (intelligence == null) intelligence = AlpineTerrainIntelligenceRuntime.I;
        }
    }
}
