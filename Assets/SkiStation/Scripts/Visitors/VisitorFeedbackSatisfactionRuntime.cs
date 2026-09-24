using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum VisitorExperienceArea
    {
        SkiArea,
        Lifts,
        Services,
        Waiting,
        SnowQuality,
        Overall
    }

    [Serializable]
    public struct VisitorFeedbackSnapshot
    {
        public CustomerProfile profile;
        public int visitors;
        [Range(0f, 1f)] public float overall01;
        [Range(0f, 1f)] public float skiArea01;
        [Range(0f, 1f)] public float lifts01;
        [Range(0f, 1f)] public float services01;
        [Range(0f, 1f)] public float waiting01;
        [Range(0f, 1f)] public float snowQuality01;
        public int positiveSignals;
        public int negativeSignals;
        public string strongestPoint;
        public string mainComplaint;
    }

    /// <summary>
    /// 1.87 - Lightweight visitor feedback layer. It observes existing satisfaction and
    /// service pressure, accepts precise experience signals from gameplay systems, and
    /// exposes compact profile summaries for management/reputation. It never drives movement.
    /// </summary>
    public sealed class VisitorFeedbackSatisfactionRuntime : MonoBehaviour
    {
        public static VisitorFeedbackSatisfactionRuntime I { get; private set; }

        [SerializeField, Min(1f)] private float refreshSeconds = 5f;
        [SerializeField, Range(.01f, .5f)] private float signalLearningRate = .16f;

        private sealed class LiveFeedback
        {
            public float skiArea = .72f;
            public float lifts = .72f;
            public float services = .72f;
            public float waiting = .72f;
            public float snow = .72f;
            public int positive;
            public int negative;
        }

        private readonly Dictionary<SkierRuntime, LiveFeedback> live = new Dictionary<SkierRuntime, LiveFeedback>();
        private readonly Dictionary<CustomerProfile, VisitorFeedbackSnapshot> profiles = new Dictionary<CustomerProfile, VisitorFeedbackSnapshot>();
        private float timer;

        public IReadOnlyDictionary<CustomerProfile, VisitorFeedbackSnapshot> Profiles => profiles;
        public event Action Changed;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start() { Refresh(); }

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < Mathf.Max(1f, refreshSeconds)) return;
            timer = 0f;
            Refresh();
        }

        public void Record(SkierRuntime skier, VisitorExperienceArea area, float quality01)
        {
            if (skier == null) return;
            LiveFeedback feedback = Ensure(skier);
            float value = Mathf.Clamp01(quality01);
            switch (area)
            {
                case VisitorExperienceArea.SkiArea: feedback.skiArea = Learn(feedback.skiArea, value); break;
                case VisitorExperienceArea.Lifts: feedback.lifts = Learn(feedback.lifts, value); break;
                case VisitorExperienceArea.Services: feedback.services = Learn(feedback.services, value); break;
                case VisitorExperienceArea.Waiting: feedback.waiting = Learn(feedback.waiting, value); break;
                case VisitorExperienceArea.SnowQuality: feedback.snow = Learn(feedback.snow, value); break;
                case VisitorExperienceArea.Overall:
                    feedback.skiArea = Learn(feedback.skiArea, value);
                    feedback.lifts = Learn(feedback.lifts, value);
                    feedback.services = Learn(feedback.services, value);
                    break;
            }
            if (value >= .72f) feedback.positive++;
            if (value <= .42f) feedback.negative++;
            if (VisitorLoyaltyRuntime.I != null) VisitorLoyaltyRuntime.I.RecordExperience(skier, value);
        }

        public bool TryGet(CustomerProfile profile, out VisitorFeedbackSnapshot snapshot)
        {
            return profiles.TryGetValue(profile, out snapshot);
        }

        public VisitorFeedbackSnapshot GetStationSummary()
        {
            VisitorFeedbackSnapshot total = new VisitorFeedbackSnapshot();
            if (profiles.Count == 0) return total;
            float weight = 0f;
            foreach (KeyValuePair<CustomerProfile, VisitorFeedbackSnapshot> pair in profiles)
            {
                float w = Mathf.Max(1, pair.Value.visitors);
                weight += w;
                total.visitors += pair.Value.visitors;
                total.overall01 += pair.Value.overall01 * w;
                total.skiArea01 += pair.Value.skiArea01 * w;
                total.lifts01 += pair.Value.lifts01 * w;
                total.services01 += pair.Value.services01 * w;
                total.waiting01 += pair.Value.waiting01 * w;
                total.snowQuality01 += pair.Value.snowQuality01 * w;
                total.positiveSignals += pair.Value.positiveSignals;
                total.negativeSignals += pair.Value.negativeSignals;
            }
            if (weight > 0f)
            {
                total.overall01 /= weight; total.skiArea01 /= weight; total.lifts01 /= weight;
                total.services01 /= weight; total.waiting01 /= weight; total.snowQuality01 /= weight;
            }
            SetHighlights(ref total);
            return total;
        }

        public void Refresh()
        {
            profiles.Clear();
            SkierRuntime[] skiers = FindObjectsOfType<SkierRuntime>();
            HashSet<SkierRuntime> active = new HashSet<SkierRuntime>();
            Dictionary<CustomerProfile, float[]> sums = new Dictionary<CustomerProfile, float[]>();

            for (int i = 0; i < skiers.Length; i++)
            {
                SkierRuntime skier = skiers[i];
                if (skier == null || skier.State == SkierState.Leaving) continue;
                active.Add(skier);
                LiveFeedback f = Ensure(skier);
                CustomerProfile profile = skier.PopulationProfile != null ? skier.PopulationProfile.ToCustomerProfile() : CustomerProfile.Tourist;
                float[] s;
                if (!sums.TryGetValue(profile, out s)) { s = new float[9]; sums[profile] = s; }
                float direct = Mathf.Clamp01(skier.Satisfaction / 100f);
                float servicePressure = CurrentServiceComfort();
                f.services = Mathf.Lerp(f.services, servicePressure, .05f);
                f.waiting = Mathf.Lerp(f.waiting, servicePressure, .04f);
                float overall = Mathf.Clamp01(direct * .42f + f.skiArea * .20f + f.lifts * .12f + f.services * .12f + f.waiting * .08f + f.snow * .06f);
                s[0] += 1f; s[1] += overall; s[2] += f.skiArea; s[3] += f.lifts; s[4] += f.services; s[5] += f.waiting; s[6] += f.snow; s[7] += f.positive; s[8] += f.negative;
            }

            List<SkierRuntime> stale = new List<SkierRuntime>();
            foreach (KeyValuePair<SkierRuntime, LiveFeedback> pair in live)
                if (pair.Key == null || !active.Contains(pair.Key)) stale.Add(pair.Key);
            for (int i = 0; i < stale.Count; i++) live.Remove(stale[i]);

            foreach (KeyValuePair<CustomerProfile, float[]> pair in sums)
            {
                float[] s = pair.Value; float n = Mathf.Max(1f, s[0]);
                VisitorFeedbackSnapshot snapshot = new VisitorFeedbackSnapshot
                {
                    profile = pair.Key, visitors = Mathf.RoundToInt(s[0]), overall01 = s[1] / n,
                    skiArea01 = s[2] / n, lifts01 = s[3] / n, services01 = s[4] / n,
                    waiting01 = s[5] / n, snowQuality01 = s[6] / n,
                    positiveSignals = Mathf.RoundToInt(s[7]), negativeSignals = Mathf.RoundToInt(s[8])
                };
                SetHighlights(ref snapshot);
                profiles[pair.Key] = snapshot;
            }
            Changed?.Invoke();
        }

        private LiveFeedback Ensure(SkierRuntime skier)
        {
            LiveFeedback value;
            if (!live.TryGetValue(skier, out value)) { value = new LiveFeedback(); live[skier] = value; }
            return value;
        }

        private float Learn(float oldValue, float newValue) { return Mathf.Lerp(oldValue, newValue, signalLearningRate); }

        private static float CurrentServiceComfort()
        {
            if (ServiceDemandIntelligenceRuntime.I == null) return .72f;
            float pressure = 0f; int count = 0;
            foreach (KeyValuePair<StationServiceType, ServiceDemandSnapshot> pair in ServiceDemandIntelligenceRuntime.I.Snapshots)
            { pressure += pair.Value.unmetDemand01; count++; }
            return count > 0 ? 1f - Mathf.Clamp01(pressure / count) : .72f;
        }

        private static void SetHighlights(ref VisitorFeedbackSnapshot s)
        {
            float best = s.skiArea01; s.strongestPoint = "Domaine skiable";
            if (s.lifts01 > best) { best = s.lifts01; s.strongestPoint = "Remontées"; }
            if (s.services01 > best) { best = s.services01; s.strongestPoint = "Services"; }
            if (s.waiting01 > best) { best = s.waiting01; s.strongestPoint = "Fluidité"; }
            if (s.snowQuality01 > best) { s.strongestPoint = "Neige"; }

            float worst = s.skiArea01; s.mainComplaint = "Domaine skiable";
            if (s.lifts01 < worst) { worst = s.lifts01; s.mainComplaint = "Remontées"; }
            if (s.services01 < worst) { worst = s.services01; s.mainComplaint = "Services"; }
            if (s.waiting01 < worst) { worst = s.waiting01; s.mainComplaint = "Attente"; }
            if (s.snowQuality01 < worst) { s.mainComplaint = "Qualité de neige"; }
        }
    }

    public static class VisitorFeedbackSatisfactionBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (VisitorFeedbackSatisfactionRuntime.I != null) return;
            GameObject go = new GameObject("Visitor Feedback & Satisfaction 1.87");
            go.AddComponent<VisitorFeedbackSatisfactionRuntime>();
        }
    }
}
