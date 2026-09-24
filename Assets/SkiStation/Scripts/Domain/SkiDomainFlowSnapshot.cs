using UnityEngine;

namespace SkiStation
{
    /// <summary>0.96 lightweight snapshot used by management UI. No artificial population is created.</summary>
    public readonly struct SkiDomainFlowSnapshot
    {
        public readonly int skiers;
        public readonly int skiing;
        public readonly int waitingForLift;
        public readonly int ridingLift;
        public readonly int resting;
        public readonly int arriving;
        public readonly int leaving;
        public readonly int openSlopes;
        public readonly int openLifts;
        public readonly int activeVehicles;
        public readonly float averageSatisfaction;

        public SkiDomainFlowSnapshot(int skiers, int skiing, int waitingForLift, int ridingLift,
            int resting, int arriving, int leaving, int openSlopes, int openLifts, int activeVehicles,
            float averageSatisfaction)
        {
            this.skiers = skiers; this.skiing = skiing; this.waitingForLift = waitingForLift;
            this.ridingLift = ridingLift; this.resting = resting; this.arriving = arriving;
            this.leaving = leaving; this.openSlopes = openSlopes; this.openLifts = openLifts;
            this.activeVehicles = activeVehicles; this.averageSatisfaction = averageSatisfaction;
        }
    }
}
