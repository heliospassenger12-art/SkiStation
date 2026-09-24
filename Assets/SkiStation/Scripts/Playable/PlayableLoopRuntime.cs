using UnityEngine;

namespace SkiStation
{
    /// <summary>Small, non-invasive coordinator for the first playable management loop.</summary>
    public sealed class PlayableLoopRuntime : MonoBehaviour
    {
        public static PlayableLoopRuntime I { get; private set; }
        public const float SlopeBaseCost = 12000f;
        public const float SlopeCostPerMeter = 38f;
        public string LastMessage { get; private set; } = "Construisez une remontee et une piste pour demarrer le domaine.";
        public int SlopeCount
        {
            get
            {
                int count = 0;
                foreach (var slope in Object.FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None))
                    if (slope != null && slope.name.StartsWith("Piste_")) count++;
                return count;
            }
        }
        public int LiftCount
        {
            get
            {
                int count = 0;
                foreach (var lift in Object.FindObjectsByType<LiftRuntime>(FindObjectsSortMode.None))
                    if (lift != null && lift.LiftId != "starter_panorama_6seat" && !lift.name.Contains("Démonstration") && !lift.name.Contains("Demonstration")) count++;
                return count;
            }
        }
        public bool DomainReady => SlopeCount > 0 && LiftCount > 0;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        public float EstimateSlopeCost(Vector3[] route)
        {
            float length = 0f;
            if (route != null) for (int i = 1; i < route.Length; i++) length += Vector3.Distance(route[i - 1], route[i]);
            return SlopeBaseCost + length * SlopeCostPerMeter;
        }

        public bool PayForSlope(Vector3[] route)
        {
            float cost = EstimateSlopeCost(route);
            if (StationRuntime.I == null || !StationRuntime.I.Spend(cost, FinanceCategory.Construction, "slope_route", "Slope construction"))
            {
                LastMessage = "Budget insuffisant pour construire cette piste.";
                return false;
            }
            LastMessage = "Piste construite : " + cost.ToString("N0") + " EUR.";
            return true;
        }

        public void SetMessage(string value) { if (!string.IsNullOrEmpty(value)) LastMessage = value; }
    }
}
