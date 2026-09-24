using System;
using UnityEngine;

namespace SkiStation
{
    [Serializable]
    public struct VisitorPreferenceProfile
    {
        [Range(0f,1f)] public float family;
        [Range(0f,1f)] public float sport;
        [Range(0f,1f)] public float adventure;
        [Range(0f,1f)] public float comfort;
        [Range(0f,1f)] public float panorama;
        [Range(0f,1f)] public float variety;
        [Range(0f,1f)] public float lowDifficulty;
        [Range(0f,1f)] public float highDifficulty;

        public static VisitorPreferenceProfile For(CustomerProfile profile)
        {
            switch (profile)
            {
                case CustomerProfile.Beginner: return new VisitorPreferenceProfile { comfort=.65f, lowDifficulty=.95f, variety=.35f, panorama=.30f };
                case CustomerProfile.Intermediate: return new VisitorPreferenceProfile { sport=.55f, lowDifficulty=.45f, highDifficulty=.45f, variety=.65f };
                case CustomerProfile.Advanced: return new VisitorPreferenceProfile { sport=.75f, adventure=.65f, highDifficulty=.70f, variety=.80f };
                case CustomerProfile.Expert: return new VisitorPreferenceProfile { sport=.85f, adventure=.85f, highDifficulty=.95f, variety=.85f };
                case CustomerProfile.Family: return new VisitorPreferenceProfile { family=.95f, comfort=.80f, lowDifficulty=.85f, panorama=.55f, variety=.60f };
                case CustomerProfile.Youth: return new VisitorPreferenceProfile { family=.70f, adventure=.65f, lowDifficulty=.55f, variety=.85f };
                case CustomerProfile.Athlete: return new VisitorPreferenceProfile { sport=1f, adventure=.80f, highDifficulty=1f, variety=.75f };
                case CustomerProfile.Premium: return new VisitorPreferenceProfile { comfort=1f, panorama=.90f, variety=.75f, family=.35f };
                default: return new VisitorPreferenceProfile { comfort=.55f, panorama=.55f, variety=.75f, lowDifficulty=.45f, highDifficulty=.35f };
            }
        }
    }
}
