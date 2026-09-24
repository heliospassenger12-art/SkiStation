using UnityEngine;

namespace SkiStation
{
    public enum SlopeDifficulty { Green, Blue, Red, Black, Extreme }
    public enum SlopeEndConnection { None, Lift, Slope, ResortPoint, Building }

    [CreateAssetMenu(menuName = "SkiStation/Slope Definition")]
    public sealed class SlopeDefinition : ScriptableObject
    {
        public string id = "blue";
        public string displayName = "Piste bleue";
        [Range(2f, 30f)] public float defaultWidth = 10f;
        public float buildCostPerMeter = 90f;
        [Range(0f, 45f)] public float minAngle = 5f;
        [Range(0f, 45f)] public float maxAngle = 22f;
        public bool requiresGrooming = true;
        public bool placeSigns = true;
    }
}
