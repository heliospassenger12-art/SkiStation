using UnityEngine;

namespace SkiStation
{
    /// <summary>Creates a financial + construction project from a completed piste design.</summary>
    public sealed class SlopeConstructionProjectManager : MonoBehaviour
    {
        public static SlopeConstructionProjectManager I { get; private set; }
        public SlopeConstructionProjectRuntime ActiveProject { get; private set; }

        [SerializeField] float baseCostPerMeter = 900f;
        [SerializeField] float widthMultiplier = 55f;
        [SerializeField] float workMinutesPer100m = 18f;
        [SerializeField] float reliefMultiplier = 1.4f;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        public bool CanStart(SlopeRuntime slope)
        {
            return slope != null && slope.Points.Count >= 2 && ActiveProject == null;
        }

        public SlopeConstructionProjectRuntime BeginProject(SlopeRuntime slope)
        {
            return BeginProject(slope, SlopeConstructionInvestment.Standard);
        }

        public SlopeConstructionProjectRuntime BeginProject(SlopeRuntime slope, SlopeConstructionInvestment investment)
        {
            if (!CanStart(slope)) return null;
            float avgWidth = 0f;
            for (int i = 0; i < slope.Points.Count; i++) avgWidth += slope.GetWidthAt(i);
            avgWidth /= Mathf.Max(1, slope.Points.Count);
            float relief = 1f + Mathf.Clamp(slope.AverageAngle / 20f, 0f, 1.5f) * reliefMultiplier;
            float investmentMultiplier = investment == SlopeConstructionInvestment.Economy ? 0.82f :
                investment == SlopeConstructionInvestment.Premium ? 1.32f : 1f;
            float cost = Mathf.Max(25000f, slope.Length * (baseCostPerMeter + avgWidth * widthMultiplier) * relief * investmentMultiplier);
            float duration = Mathf.Max(5f, slope.Length / 100f * workMinutesPer100m * relief);

            var quality = slope.GetComponent<SlopeConstructionQualityRuntime>();
            if (quality == null) quality = slope.gameObject.AddComponent<SlopeConstructionQualityRuntime>();
            quality.Evaluate(investment);

            if (StationRuntime.I != null && !StationRuntime.I.Spend(cost, FinanceCategory.Construction, slope.name, "Construction de piste — " + investment))
                return null;

            ActiveProject = SlopeConstructionProjectRuntime.StartProject(slope, cost, duration);
            return ActiveProject;
        }

        void Update()
        {
            if (ActiveProject != null && ActiveProject.State == SlopeConstructionProjectState.Completed)
                ActiveProject = null;
            if (ActiveProject != null && ActiveProject.State == SlopeConstructionProjectState.Cancelled)
                ActiveProject = null;
        }
    }
}
