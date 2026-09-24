using UnityEngine;

namespace SkiStation
{
    public class StationEntranceRuntime : MonoBehaviour
    {
        public static StationEntranceRuntime I { get; private set; }
        [SerializeField] private StationEntranceDefinition definition;
        [SerializeField] private Transform arrivalPoint;
        [SerializeField] private Transform dropOffPoint;
        public int ArrivalsThisSession { get; private set; }

        public StationEntranceDefinition Definition => definition;
        public Transform ArrivalPoint => arrivalPoint;
        public Transform DropOffPoint => dropOffPoint;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        public void Configure(StationEntranceDefinition def, Transform arrival, Transform dropOff)
        {
            definition = def;
            arrivalPoint = arrival;
            dropOffPoint = dropOff;
        }

        public void RegisterArrival(int count)
        {
            ArrivalsThisSession += Mathf.Max(0, count);
        }
    }
}
