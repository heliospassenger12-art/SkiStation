using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    [Serializable]
    public struct ResortReputationSnapshot
    {
        public CustomerProfile profile;
        [Range(0f,1f)] public float reputation01;
        [Range(0f,1f)] public float loyalty01;
        [Range(0f,1f)] public float returnIntent01;
        [Range(0f,1f)] public float wordOfMouth01;
        public int observedVisitors;
        public string strongestPoint;
        public string mainComplaint;
    }

    /// <summary>
    /// 1.88 - Converts visitor experience into slow-moving resort reputation, loyalty,
    /// return intent and word of mouth. This is a management signal only: it does not
    /// spawn visitors or override the existing simulation. 1.89 can consume it safely.
    /// </summary>
    public sealed class ResortReputationLoyaltyRuntime : MonoBehaviour
    {
        public static ResortReputationLoyaltyRuntime I { get; private set; }
        [SerializeField, Min(2f)] private float refreshSeconds = 8f;
        [SerializeField, Range(.005f,.25f)] private float reputationLearningRate = .045f;
        private readonly Dictionary<CustomerProfile, ResortReputationSnapshot> profiles = new Dictionary<CustomerProfile, ResortReputationSnapshot>();
        private float timer;
        public IReadOnlyDictionary<CustomerProfile, ResortReputationSnapshot> Profiles => profiles;
        public event Action Changed;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
            foreach (CustomerProfile p in Enum.GetValues(typeof(CustomerProfile)))
                profiles[p] = NewSnapshot(p);
        }

        private void Start() { Refresh(); }
        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < Mathf.Max(2f, refreshSeconds)) return;
            timer = 0f; Refresh();
        }

        public bool TryGet(CustomerProfile profile, out ResortReputationSnapshot snapshot) { return profiles.TryGetValue(profile, out snapshot); }

        public ResortReputationSnapshot GetStationSummary()
        {
            ResortReputationSnapshot result = new ResortReputationSnapshot();
            float weight = 0f;
            foreach (KeyValuePair<CustomerProfile, ResortReputationSnapshot> pair in profiles)
            {
                float w = Mathf.Max(1, pair.Value.observedVisitors);
                weight += w; result.observedVisitors += pair.Value.observedVisitors;
                result.reputation01 += pair.Value.reputation01 * w;
                result.loyalty01 += pair.Value.loyalty01 * w;
                result.returnIntent01 += pair.Value.returnIntent01 * w;
                result.wordOfMouth01 += pair.Value.wordOfMouth01 * w;
            }
            if (weight > 0f) { result.reputation01/=weight; result.loyalty01/=weight; result.returnIntent01/=weight; result.wordOfMouth01/=weight; }
            return result;
        }

        public void Refresh()
        {
            VisitorFeedbackSatisfactionRuntime feedback = VisitorFeedbackSatisfactionRuntime.I;
            foreach (CustomerProfile p in Enum.GetValues(typeof(CustomerProfile)))
            {
                ResortReputationSnapshot s;
                if (!profiles.TryGetValue(p, out s)) s = NewSnapshot(p);
                VisitorFeedbackSnapshot f;
                if (feedback != null && feedback.TryGet(p, out f) && f.visitors > 0)
                {
                    float experience = Mathf.Clamp01(f.overall01);
                    float loyalty = EstimateLoyalty(p, experience);
                    float returnIntent = Mathf.Clamp01(experience * .55f + loyalty * .45f);
                    float advocacy = Mathf.Clamp01((experience - .45f) / .55f);
                    float targetReputation = Mathf.Clamp01(experience * .68f + advocacy * .20f + loyalty * .12f);
                    s.reputation01 = Mathf.Lerp(s.reputation01, targetReputation, reputationLearningRate);
                    s.loyalty01 = Mathf.Lerp(s.loyalty01, loyalty, reputationLearningRate * 1.25f);
                    s.returnIntent01 = Mathf.Lerp(s.returnIntent01, returnIntent, reputationLearningRate * 1.25f);
                    s.wordOfMouth01 = Mathf.Lerp(s.wordOfMouth01, advocacy, reputationLearningRate);
                    s.observedVisitors = f.visitors;
                    s.strongestPoint = f.strongestPoint;
                    s.mainComplaint = f.mainComplaint;
                }
                profiles[p] = s;
            }
            Changed?.Invoke();
        }

        private static float EstimateLoyalty(CustomerProfile profile, float fallback)
        {
            VisitorLoyaltyRuntime loyalty = VisitorLoyaltyRuntime.I;
            if (loyalty == null || loyalty.Live.Count == 0) return fallback * .75f;
            float sum = 0f; int count = 0;
            foreach (KeyValuePair<SkierRuntime, VisitorLoyaltySnapshot> pair in loyalty.Live)
            {
                SkierRuntime skier = pair.Key;
                if (skier == null) continue;
                CustomerProfile p = skier.PopulationProfile != null ? skier.PopulationProfile.ToCustomerProfile() : CustomerProfile.Tourist;
                if (p != profile) continue;
                sum += pair.Value.loyalty01; count++;
            }
            return count > 0 ? Mathf.Clamp01(sum / count) : fallback * .75f;
        }

        private static ResortReputationSnapshot NewSnapshot(CustomerProfile profile)
        {
            return new ResortReputationSnapshot { profile=profile, reputation01=.55f, loyalty01=.40f, returnIntent01=.52f, wordOfMouth01=.50f, strongestPoint="-", mainComplaint="-" };
        }
    }

    public static class ResortReputationLoyaltyBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (ResortReputationLoyaltyRuntime.I != null) return;
            new GameObject("Resort Reputation & Loyalty 1.88").AddComponent<ResortReputationLoyaltyRuntime>();
        }
    }
}
