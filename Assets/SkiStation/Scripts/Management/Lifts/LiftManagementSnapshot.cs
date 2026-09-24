using System;
using UnityEngine;

namespace SkiStation
{
    [Serializable]
    public struct LiftManagementSnapshot
    {
        public string id;
        public string displayName;
        public LiftType type;
        public bool open;
        public bool faulted;
        public float length;
        public float verticalDrop;
        public float speedKmh;
        public float capacityPerHour;
        public int queue;
        public int passengersOnLine;
        public float utilization01;
        public float pressure01;
        public float travelMinutes;
        public bool underground;
    }
}
