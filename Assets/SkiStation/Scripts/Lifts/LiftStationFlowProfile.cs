using UnityEngine;

namespace SkiStation
{
    public enum LiftBoardingStyle { QueueAndBoard, Continuous, CabinDoor, FunicularDoor, CarpetEntry }

    [CreateAssetMenu(menuName = "SkiStation/Lifts/Station Flow Profile")]
    public sealed class LiftStationFlowProfile : ScriptableObject
    {
        public LiftBoardingStyle boardingStyle = LiftBoardingStyle.QueueAndBoard;
        [Min(1)] public int boardingSlots = 1;
        [Min(1)] public int unloadingSlots = 1;
        [Min(.1f)] public float approachRadius = 5f;
        [Min(.1f)] public float dockingSeconds = 1f;
        [Min(.1f)] public float releaseSeconds = 1f;
        public bool supportsWalkThrough = false;

        public static LiftStationFlowProfile For(LiftDefinition def)
        {
            var p = CreateInstance<LiftStationFlowProfile>();
            if (def == null) return p;
            string n = def.displayName == null ? string.Empty : def.displayName.ToLowerInvariant();
            if (n.Contains("funiculaire") || n.Contains("funicular")) { p.boardingStyle = LiftBoardingStyle.FunicularDoor; p.boardingSlots = 2; p.unloadingSlots = 2; }
            else if (n.Contains("télécabine") || n.Contains("gondole") || n.Contains("cabin")) { p.boardingStyle = LiftBoardingStyle.CabinDoor; p.boardingSlots = 2; p.unloadingSlots = 2; }
            else if (n.Contains("tapis") || n.Contains("carpet")) { p.boardingStyle = LiftBoardingStyle.CarpetEntry; p.boardingSlots = 1; p.unloadingSlots = 1; p.supportsWalkThrough = true; }
            else if (n.Contains("téléski") || n.Contains("tow")) { p.boardingStyle = LiftBoardingStyle.Continuous; p.boardingSlots = 1; p.unloadingSlots = 1; }
            return p;
        }
    }
}
