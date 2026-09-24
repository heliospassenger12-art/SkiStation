using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// Lightweight piste flow model. Converts live traffic, width and difficulty into
    /// a congestion pressure used by visitor route choice. No manual micromanagement.
    /// </summary>
    public sealed class SlopeCongestionRuntime : MonoBehaviour
    {
        public static SlopeCongestionRuntime I { get; private set; }

        [SerializeField] private float severeThreshold = 0.82f;
        [SerializeField] private float highThreshold = 0.62f;
        [SerializeField] private float mediumThreshold = 0.40f;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        public float GetPressure01(SlopeRuntime slope)
        {
            if (slope == null) return 0f;
            float width = Mathf.Max(2f, slope.AverageWidth);
            float density = slope.TrafficCount / Mathf.Max(1f, slope.Length * (width / 10f));
            float raw = Mathf.Clamp01(density * 7.5f);
            raw *= Mathf.Lerp(0.88f, 1.12f, Mathf.InverseLerp(0f, 4f, (int)slope.Difficulty));
            return Mathf.Clamp01(raw);
        }

        public string GetLabel(SlopeRuntime slope)
        {
            float p = GetPressure01(slope);
            if (p >= severeThreshold) return "Très chargée";
            if (p >= highThreshold) return "Chargée";
            if (p >= mediumThreshold) return "Modérée";
            return "Fluide";
        }

        public float GetRoutePenalty01(SlopeRuntime slope)
        {
            float p = GetPressure01(slope);
            return p * p;
        }

        public bool ShouldRedirect(SlopeRuntime slope) => GetPressure01(slope) >= severeThreshold;
    }
}
