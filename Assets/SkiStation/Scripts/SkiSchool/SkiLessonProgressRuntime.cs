using UnityEngine;

namespace SkiStation
{
    /// <summary>Progression élève basée sur les badges de ski : Ourson, étoiles, Étoile d'Or, Flèche et Chamois.</summary>
    public sealed class SkiLessonProgressRuntime : MonoBehaviour
    {
        public float Skill01 { get; private set; }
        public int LessonsCompleted { get; private set; }
        public bool PiouPiouGraduated { get; private set; }

        public SkiLessonLevel CurrentLevel
        {
            get
            {
                if (Skill01 < 0.10f) return SkiLessonLevel.Ourson;
                if (Skill01 < 0.24f) return SkiLessonLevel.Flocon;
                if (Skill01 < 0.40f) return SkiLessonLevel.PremiereEtoile;
                if (Skill01 < 0.56f) return SkiLessonLevel.DeuxiemeEtoile;
                if (Skill01 < 0.70f) return SkiLessonLevel.TroisiemeEtoile;
                if (Skill01 < 0.82f) return SkiLessonLevel.EtoileOr;
                if (Skill01 < 0.90f) return SkiLessonLevel.Fleche;
                if (Skill01 < 0.97f) return SkiLessonLevel.Chamois;
                return SkiLessonLevel.ChamoisOr;
            }
        }

        public string CurrentBadgeName => SkiLessonLevelUtility.BadgeName(CurrentLevel);

        public void CompleteLesson(SkiLessonFormat format, SkiSchoolZoneType zone)
        {
            float gain = format == SkiLessonFormat.Individual ? .105f : .075f;
            if (zone == SkiSchoolZoneType.PiouPiou) gain *= 1.08f;
            Skill01 = Mathf.Clamp01(Skill01 + gain);
            LessonsCompleted++;

            var profile = GetComponent<SkierPopulationProfile>();
            if (profile != null && profile.IsChild)
                PiouPiouGraduated = Skill01 >= 0.10f || profile.AgeYears >= 8;
        }
    }
}
