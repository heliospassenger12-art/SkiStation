using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// 3.02: keeps the unified ski-domain graph synchronized with the live resort.
    /// New/removed pistes and lifts, closures and lift faults become visible to the
    /// multi-lift planner without restarting Play Mode. Read-only: it never changes
    /// construction or infrastructure state.
    /// </summary>
    public sealed class LiveSkiDomainNetwork302 : MonoBehaviour
    {
        public static LiveSkiDomainNetwork302 I { get; private set; }
        [SerializeField, Min(1f)] private float refreshSeconds = 3f;
        private float timer;
        private int lastFingerprint = int.MinValue;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;
            if (I == null) new GameObject("SkiStation_302_Reseau_Domaine_Vivant").AddComponent<LiveSkiDomainNetwork302>();
        }

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start() { ForceRefresh(); }

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < refreshSeconds) return;
            timer = 0f;
            RefreshIfChanged();
        }

        public void ForceRefresh()
        {
            var network = IntegratedSkiDomainNetworkRuntime.I;
            if (network == null) return;
            network.Rebuild();
            lastFingerprint = ComputeFingerprint();
        }

        private void RefreshIfChanged()
        {
            var network = IntegratedSkiDomainNetworkRuntime.I;
            if (network == null) return;
            int fingerprint = ComputeFingerprint();
            if (fingerprint == lastFingerprint) return;
            network.Rebuild();
            lastFingerprint = fingerprint;
            Debug.Log("SkiStation 3.02: reseau du domaine recalcule apres modification d'infrastructure.");
        }

        private static int ComputeFingerprint()
        {
            unchecked
            {
                int hash = 17;
                var slopes = Object.FindObjectsOfType<SlopeRuntime>();
                hash = hash * 31 + slopes.Length;
                for (int i = 0; i < slopes.Length; i++)
                {
                    var s = slopes[i];
                    if (s == null) continue;
                    hash = hash * 31 + s.GetInstanceID();
                    hash = hash * 31 + (s.Open ? 1 : 0);
                    hash = hash * 31 + (s.Points != null ? s.Points.Count : 0);
                }

                var lifts = Object.FindObjectsOfType<LiftRuntime>();
                hash = hash * 31 + lifts.Length;
                for (int i = 0; i < lifts.Length; i++)
                {
                    var l = lifts[i];
                    if (l == null) continue;
                    hash = hash * 31 + l.GetInstanceID();
                    hash = hash * 31 + (l.Open ? 1 : 0);
                    hash = hash * 31 + (l.Faulted ? 1 : 0);
                }
                return hash;
            }
        }
    }
}
