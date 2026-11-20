using System;
using UnityEngine;

namespace SkiStation
{
    public enum MountainActivityType
    {
        MountainRestaurant, Refuge, Dogsled, Paragliding, Zipline, Snowmobile,
        Viewpoint, FestivalStage, Concert, IceSkating, Snowpark, Other, IceDiving, LakeSwimming
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
        private Func<bool> seasonalAvailability;
        private readonly System.Collections.Generic.Queue<float> visits = new();
        internal bool IsSeasonalSite => seasonalAvailability != null;
        internal bool OpenForVisitors => seasonalAvailability?.Invoke() ?? true;

        internal void ConfigureSeasonalSite(MountainActivityType type, string label, int capacity, float price, Func<bool> available)
        {
            definition = ScriptableObject.CreateInstance<MountainActivityDefinition>();
            definition.type=type;
            definition.activityName=label;
            definition.capacity=capacity;
            definition.throughputPerHour=capacity;
            definition.price=price;
            definition.playableByPlayer=false;
            seasonalAvailability=available;
        }

        private static float ClockHours => GameClock.I != null ? GameClock.I.CurrentDay*24 + GameClock.I.CurrentHour : 0;

        private void Update()
        {
            while (visits.Count>0 && (!OpenForVisitors || visits.Peek()<=ClockHours))
            {
                visits.Dequeue();
                Exit();
            }
        }

        private void OnDestroy()
        {
            if (IsSeasonalSite && definition != null) Destroy(definition);
        }
        public int CurrentOccupancy => currentOccupancy;
        public int AvailableCapacity => OpenForVisitors ? Mathf.Max(0, (definition != null ? definition.capacity : 0) - currentOccupancy) : 0;
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
            if (definition == null || amount < 1 || amount > AvailableCapacity) return false;
            if (IsSeasonalSite) for (int i=0;i<amount;i++) visits.Enqueue(ClockHours+1f);
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
