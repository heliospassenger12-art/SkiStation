using System;
using UnityEngine;

namespace SkiStation
{
    public enum LeisureFacilityType { SnowVolleyball, IceKart, NaturalLakeSkating, Sledding, KidsZone, IceRink, Climbing, Other }
    public enum CompetitionLevel { None, Local, Regional, National, International }

    [CreateAssetMenu(menuName = "SkiStation/Leisure/Facility Definition")]
    public class LeisureFacilityDefinition : ScriptableObject
    {
        public string facilityName = "Base de loisirs";
        public LeisureFacilityType type = LeisureFacilityType.Other;
        [Min(1)] public int capacity = 30;
        [Min(1)] public int sessionsPerHour = 2;
        [Min(0)] public float entryPrice = 10f;
        public bool familyFriendly = true;
        public bool playableByPlayer = true;
        public CompetitionLevel maximumCompetitionLevel = CompetitionLevel.None;
        public bool seasonal;
        public bool requiresFrozenSurface;
    }

    public sealed class LeisureFacilityRuntime : MonoBehaviour
    {
        public static readonly System.Collections.Generic.List<LeisureFacilityRuntime> All = new();
        public LeisureFacilityDefinition definition;
        [SerializeField] private int occupancy;
        [SerializeField] private CompetitionLevel currentCompetition;
        public int Occupancy => occupancy;
        public int AvailableCapacity => definition == null ? 0 : Mathf.Max(0, definition.capacity - occupancy);
        public CompetitionLevel CurrentCompetition => currentCompetition;
        public event Action<int> OccupancyChanged;

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() { All.Remove(this); }
        public bool TryEnter(int amount = 1)
        {
            if (definition == null || amount < 1 || occupancy + amount > definition.capacity) return false;
            occupancy += amount; OccupancyChanged?.Invoke(occupancy); return true;
        }
        public void Exit(int amount = 1) { occupancy = Mathf.Max(0, occupancy - Mathf.Max(1, amount)); OccupancyChanged?.Invoke(occupancy); }
        public bool StartCompetition(CompetitionLevel level)
        {
            if (definition == null || level > definition.maximumCompetitionLevel || level == CompetitionLevel.None) return false;
            currentCompetition = level; return true;
        }
        public void EndCompetition() => currentCompetition = CompetitionLevel.None;
    }
}
