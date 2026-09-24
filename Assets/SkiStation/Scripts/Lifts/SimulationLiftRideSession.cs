using System;
using UnityEngine;

namespace SkiStation
{
    [Serializable]
    public sealed class SimulationLiftRideSession
    {
        public Transform rider;
        public LiftDefinition lift;
        public float startedAtGameHour;

        public SimulationLiftRideSession() { }
        public SimulationLiftRideSession(Transform rider, LiftDefinition lift, float startedAtGameHour)
        {
            this.rider = rider;
            this.lift = lift;
            this.startedAtGameHour = startedAtGameHour;
        }
    }
}
