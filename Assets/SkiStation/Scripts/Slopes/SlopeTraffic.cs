using UnityEngine;

namespace SkiStation
{
    public class SlopeTraffic : MonoBehaviour
    {
        [SerializeField] private float skiersPerHour = 180f;
        public float CapacityPerHour => skiersPerHour;

        public bool CanEnter(int currentUsers)
        {
            return currentUsers < Mathf.RoundToInt(skiersPerHour);
        }
    }
}
