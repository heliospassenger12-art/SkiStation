using System;
using UnityEngine;

namespace SkiStation
{
    public enum MountainActivityType
    {
        MountainRestaurant, Refuge, Dogsled, Paragliding, Zipline, Snowmobile,
        Viewpoint, FestivalStage, Concert, IceSkating, Snowpark, Other
    }

    [Flags]
    public enum ActivityAccessMode
    {
        None = 0, FromSlope = 1, FromLift = 2, Pedestrian = 4, SpecializedTransport = 8
    }

    [CreateAssetMenu(menuName = "SkiStation/Activities/Mountain Activity Definition")]
    public class MountainActivityDefinition : ScriptableObject
    {
        public string activityName = "Mountain Activity";
        public MountainActivityType type = MountainActivityType.Other;
        public ActivityAccessMode access = ActivityAccessMode.Pedestrian;
        [Min(1)] public int capacity = 20;
        [Min(1)] public int throughputPerHour = 20;
        [Min(0)] public float price = 20f;
        [Range(0, 100)] public float reputationImpact = 2f;
        public bool playableByPlayer = true;
        public bool requiresGoodWeather;
        [Range(-40, 40)] public float minTemperatureC = -20f;
        [Range(-40, 50)] public float maxTemperatureC = 35f;
    }

    public sealed class MountainActivityRuntime : MonoBehaviour
    {
        public static readonly System.Collections.Generic.List<MountainActivityRuntime> All = new();
        public MountainActivityDefinition definition;
        [SerializeField] private int currentOccupancy;
        public int CurrentOccupancy => currentOccupancy;
        public int AvailableCapacity => Mathf.Max(0, (definition != null ? definition.capacity : 0) - currentOccupancy);
        public event Action<int> OccupancyChanged;

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() { All.Remove(this); }
        public bool TryConsumeByVisitor(SkierRuntime skier)
        {
            return VisitorServiceConsumptionRuntime.I != null &&
                   VisitorServiceConsumptionRuntime.I.TryConsumeMountainActivity(skier, this);
        }

        public bool TryEnter(int amount = 1)
        {
            if (definition == null || amount < 1 || currentOccupancy + amount > definition.capacity) return false;
            currentOccupancy += amount; OccupancyChanged?.Invoke(currentOccupancy); return true;
        }
        public void Exit(int amount = 1)
        {
            currentOccupancy = Mathf.Max(0, currentOccupancy - Mathf.Max(1, amount));
            OccupancyChanged?.Invoke(currentOccupancy);
        }
        public float HourlyPressure(float expectedVisitors)
        {
            if (definition == null || definition.throughputPerHour <= 0) return 1f;
            return Mathf.Clamp01(expectedVisitors / definition.throughputPerHour);
        }
    }
}
