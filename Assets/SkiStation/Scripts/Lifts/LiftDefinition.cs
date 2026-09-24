using UnityEngine;

namespace SkiStation
{
    public enum LiftType
    {
        Platter,
        TBar,
        Fork,
        FixedChair,
        DetachableChair,
        BubbleChair,
        Gondola,
        CableCar,
        ThreeS,
        Funicular,
        MagicCarpet
    }

    public enum LiftConstructionMode
    {
        Surface,
        Underground,
        Auto
    }

    [CreateAssetMenu(menuName = "SkiStation/Lift Definition")]
    public class LiftDefinition : ScriptableObject
    {
        public string id = "chair_detachable_6";
        public string displayName = "Télésiège débrayable 6 places";
        public LiftType type = LiftType.DetachableChair;

        [Header("Construction")]
        public LiftConstructionMode constructionMode = LiftConstructionMode.Surface;
        [Tooltip("Pour un funiculaire en mode Auto, le jeu choisit le tunnel au-delà de ce dénivelé.")]
        [Min(10f)] public float autoTunnelVerticalDrop = 500f;
        [Tooltip("Profondeur minimale du tunnel sous le terrain au milieu de la ligne.")]
        [Min(3f)] public float tunnelDepth = 18f;
        [Min(2f)] public float tunnelRadius = 4.5f;
        [Min(8)] public int tunnelSamples = 64;
        public bool emergencyAccess = true;
        public bool tunnelLighting = true;

        [Header("Surface tow routing")]
        [Tooltip("Autorise les changements de direction réalistes pour les téléskis. En construction, Shift + clic ajoute un point de virage.")]
        public bool allowTowDirectionChanges = true;
        [Min(1)] public int maxTowDirectionPoints = 3;
        [Min(2)] public int towSamplesPerSegment = 10;
        [Min(0f)] public float towDirectionChangeCostMultiplier = .08f;
        [Min(1f)] public float towCornerRadius = 8f;

        [Header("Economy")]
        public float buildCost = 250000f;
        public float maintenancePerDay = 1200f;
        public int researchLevel = 0;
        [Header("Career construction limits")]
        [Min(50f)] public float maxLineLengthMeters = 600f;

        [Header("Performance")]
        [Min(1f)] public float speedKmh = 12f;
        [Min(1)] public int seatsPerVehicle = 6;
        [Min(1)] public int vehicles = 80;
        [Min(.5f)] public float spacingSeconds = 8f;
        public bool detachable;
        public bool variableSpeed;
        [Min(0.1f)] public float loadingSeconds = 6f;
        [Min(0.1f)] public float unloadingSeconds = 4f;
        [Min(1f)] public float vehicleLength = 2.2f;
        [Min(0.2f)] public float lineWidth = 0.9f;

        [Header("Presentation")]
        public bool heatedSeats;
        public bool tintedWindows;
        public bool windProtection;
        public bool premiumCabins;
        public bool showTechnicalDetails = true;
    }
}
