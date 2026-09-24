using System;
namespace SkiStation
{
    public static class GameEvent
    {
        public static event Action BuildingPlaced;
        public static event Action DayChanged;
        public static void RaiseBuildingPlaced() => BuildingPlaced?.Invoke();
        public static void RaiseDayChanged() => DayChanged?.Invoke();
    }
}
