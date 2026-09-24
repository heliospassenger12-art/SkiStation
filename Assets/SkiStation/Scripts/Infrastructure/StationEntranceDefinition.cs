using UnityEngine;

namespace SkiStation
{
    [CreateAssetMenu(menuName = "SkiStation/Infrastructure/Station Entrance Definition")]
    public class StationEntranceDefinition : ScriptableObject
    {
        [Min(1)] public int hourlyCapacity = 240;
        [Min(0f)] public float arrivalRadius = 5f;
        public bool acceptsPedestrians = true;
        public bool acceptsCars = true;
        public bool acceptsBuses = true;
    }
}
