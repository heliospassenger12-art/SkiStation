using UnityEngine;

namespace SkiStation
{
    /// <summary>Beginner tool: P toggles profile mode; 1-6 choose a profile; click applies it to the nearest slope.</summary>
    public sealed class SlopeProfileTool : MonoBehaviour
    {
        [SerializeField] Camera buildCamera;
        [SerializeField, Min(1f)] float selectionRadius = 35f;
        [SerializeField] float amplitude = 0.55f;
        [SerializeField] float wavelength = 7f;
        bool active;
        SlopeProfileMode mode = SlopeProfileMode.Natural;

        public bool IsActive => active;
        public SlopeProfileMode Mode => mode;

        void Awake() { if (!buildCamera) buildCamera = Camera.main; }

        void Update()
        {
            if (!buildCamera) buildCamera = Camera.main;
            if (Input.GetKeyDown(KeyCode.P)) active = !active;
            if (!active) return;
            if (Input.GetKeyDown(KeyCode.Alpha1)) mode = SlopeProfileMode.Natural;
            if (Input.GetKeyDown(KeyCode.Alpha2)) mode = SlopeProfileMode.Graded;
            if (Input.GetKeyDown(KeyCode.Alpha3)) mode = SlopeProfileMode.Bumps;
            if (Input.GetKeyDown(KeyCode.Alpha4)) mode = SlopeProfileMode.Moguls;
            if (Input.GetKeyDown(KeyCode.Alpha5)) mode = SlopeProfileMode.Compression;
            if (Input.GetKeyDown(KeyCode.Alpha6)) mode = SlopeProfileMode.Wall;
            if (Input.GetKeyDown(KeyCode.Minus)) amplitude = Mathf.Max(0.1f, amplitude - 0.1f);
            if (Input.GetKeyDown(KeyCode.Equals)) amplitude = Mathf.Min(3f, amplitude + 0.1f);
            if (Input.GetMouseButtonDown(0)) ApplyNearest();
        }

        void ApplyNearest()
        {
            if (!buildCamera || SlopeNetwork.I == null) return;
            Ray ray = buildCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, 5000f)) return;
            SlopeRuntime best = null; float bestDistance = selectionRadius;
            foreach (var slope in SlopeNetwork.I.Slopes)
            {
                if (slope == null || slope.Points.Count == 0) continue;
                float d = DistanceToSlope(hit.point, slope);
                if (d < bestDistance) { bestDistance = d; best = slope; }
            }
            if (best == null) return;
            var profile = best.GetComponent<SlopeProfileRuntime>() ?? best.gameObject.AddComponent<SlopeProfileRuntime>();
            profile.SetMode(mode); profile.SetAmplitude(amplitude); profile.SetWavelength(wavelength); profile.Apply();
        }

        static float DistanceToSlope(Vector3 p, SlopeRuntime slope)
        {
            float best = float.MaxValue;
            for (int i = 0; i < slope.Points.Count; i++) best = Mathf.Min(best, Vector3.Distance(p, slope.Points[i]));
            return best;
        }
    }
}
