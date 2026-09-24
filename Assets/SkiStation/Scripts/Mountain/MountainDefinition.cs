using UnityEngine;

namespace SkiStation
{
    [CreateAssetMenu(menuName = "SkiStation/Mountain Definition", fileName = "MountainDefinition")]
    public sealed class MountainDefinition : ScriptableObject
    {
        [Header("Dimensions")]
        [Min(32)] public int resolution = 129;
        [Min(100f)] public float size = 1200f;
        [Min(50f)] public float maximumAltitude = 1800f;
        [Min(0f)] public float baseAltitude = 900f;

        [Header("Shape")]
        [Min(0.1f)] public float mainFrequency = 1.15f;
        [Range(0f, 1f)] public float secondaryNoise = 0.22f;
        [Range(0f, 1f)] public float ridgeStrength = 0.32f;
        [Range(0f, 1f)] public float valleyStrength = 0.28f;
        [Min(0.1f)] public float plateauRadius = 150f;
        [Min(0f)] public float baseFlatRadius = 260f;
        [Min(0f)] public float baseFlatBlend = 0.75f;
        public int seed = 1707;

        [Header("Gameplay zones")]
        [Min(0f)] public float buildableMaxSlope = 18f;
        [Min(0f)] public float forestStartAltitude = 980f;
        [Min(0f)] public float forestLimitAltitude = 1550f;
        [Min(0f)] public float rockStartAltitude = 1500f;
        [Min(0f)] public float summitAltitude = 1700f;

        public float BaseTopographicAltitude => baseAltitude + maximumAltitude;
    }
}
