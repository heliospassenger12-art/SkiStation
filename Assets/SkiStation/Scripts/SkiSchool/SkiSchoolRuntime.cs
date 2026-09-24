using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum SkiLessonFormat { Individual, Group }
    public enum SkiLessonAudience { Adults, Children, AllAges }
    public enum SkiLessonLevel { Ourson, Flocon, PremiereEtoile, DeuxiemeEtoile, TroisiemeEtoile, EtoileOr, Fleche, Chamois, ChamoisOr }

    public static class SkiLessonLevelUtility
    {
        public static string BadgeName(SkiLessonLevel level)
        {
            switch (level)
            {
                case SkiLessonLevel.Ourson: return "Ourson";
                case SkiLessonLevel.Flocon: return "Flocon";
                case SkiLessonLevel.PremiereEtoile: return "1re étoile";
                case SkiLessonLevel.DeuxiemeEtoile: return "2e étoile";
                case SkiLessonLevel.TroisiemeEtoile: return "3e étoile";
                case SkiLessonLevel.EtoileOr: return "Étoile d'Or";
                case SkiLessonLevel.Fleche: return "Flèche";
                case SkiLessonLevel.Chamois: return "Chamois";
                default: return "Chamois d'Or";
            }
        }

        public static int BadgeIndex(SkiLessonLevel level) => (int)level;
    }
    public enum SkiSchoolZoneType { StandardSchool, PiouPiou }

    [Serializable]
    public struct SkiLessonOffer
    {
        public SkiLessonFormat format;
        public SkiLessonAudience audience;
        public SkiLessonLevel level;
        public int minAge;
        public int maxAge;
        public int minGroupSize;
        public int maxGroupSize;
        public float price;
    }

    [Serializable]
    public struct SkiLessonSnapshot
    {
        public string serviceId;
        public string serviceName;
        public SkiSchoolZoneType zoneType;
        public int activeLessons;
        public int individualLessons;
        public int groupLessons;
        public int childrenLessons;
        public int adultLessons;
        public int activeStudents;
        public float utilization01;
    }

    /// <summary>1.55 ski-school layer: individual/group lessons, all ages, and a separate Piou-Piou child zone.</summary>
    public sealed class SkiSchoolRuntime : MonoBehaviour
    {
        public static readonly List<SkiSchoolRuntime> All = new();
        public StationServiceRuntime service;
        public SkiSchoolZoneType zoneType = SkiSchoolZoneType.StandardSchool;
        public bool individualLessons = true;
        public bool groupLessons = true;
        [Min(1)] public int maxGroupSize = 8;
        [Min(1)] public int instructors = 2;
        [Min(3)] public int piouPiouMaxAge = 7;
        [Min(1)] public int adultMinAge = 18;
        [SerializeField] private int activeLessons;
        [SerializeField] private int activeStudents;
        private readonly List<SkiLessonSession> sessions = new();
        public IReadOnlyList<SkiLessonSession> Sessions => sessions;
        public event Action Changed;

        [Serializable]
        public sealed class SkiLessonSession
        {
            public SkiLessonFormat format;
            public SkiLessonAudience audience;
            public SkiLessonLevel level;
            public int age;
            public int students;
            public float startHour;
            public float durationMinutes;
            public bool active;
            public string instructorId;
            public int leadStudentInstanceId;
            public List<int> studentInstanceIds = new List<int>();
            public string trainingSlopeName;
        }

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() { All.Remove(this); }
        private void Update() { TickSessions(); }

        public bool CanAccept(SkierRuntime skier, SkiLessonFormat format)
        {
            if (skier == null || service == null || service.definition == null || !service.IsOpen()) return false;
            if (zoneType == SkiSchoolZoneType.PiouPiou && !IsChild(skier)) return false;
            if (zoneType == SkiSchoolZoneType.StandardSchool && IsChild(skier) && ChildWantsPiouPiou(skier)) return false;
            if (format == SkiLessonFormat.Individual && !individualLessons) return false;
            if (format == SkiLessonFormat.Group && !groupLessons) return false;
            return FindFreeInstructor() != null;
        }

        public bool TryStartLesson(SkierRuntime skier, SkiLessonFormat format, SkiLessonLevel level = SkiLessonLevel.Ourson)
        {
            if (!CanAccept(skier, format)) return false;
            int age = GetAge(skier);
            int students = format == SkiLessonFormat.Individual ? 1 : Mathf.Clamp(EstimateGroupSize(skier), 2, maxGroupSize);
            var instructor = FindFreeInstructor();
            if (instructor == null) return false;
            var session = new SkiLessonSession {
                format = format,
                audience = IsChild(skier) ? SkiLessonAudience.Children : SkiLessonAudience.Adults,
                level = level,
                age = age,
                students = students,
                startHour = GameClock.I != null ? GameClock.I.CurrentHour : 0f,
                durationMinutes = format == SkiLessonFormat.Individual ? 55f : 60f,
                active = true,
                instructorId = instructor.GetInstanceID().ToString(),
                leadStudentInstanceId = skier.GetInstanceID()
            };
            instructor.TryAssign((format == SkiLessonFormat.Individual ? "Cours individuel" : "Cours collectif") + " — " + (IsChild(skier) ? "enfants" : "adultes"));
            var leadProgress = skier.GetComponent<SkiLessonProgressRuntime>() ?? skier.gameObject.AddComponent<SkiLessonProgressRuntime>();
            session.level = leadProgress.CurrentLevel;
            CollectSessionStudents(skier, format, session.studentInstanceIds);
            session.students = Mathf.Max(1, session.studentInstanceIds.Count);
            session.trainingSlopeName = FindTrainingSlopeName(session.level, skier);
            sessions.Add(session);
            activeLessons++;
            activeStudents += students;
            Changed?.Invoke();
            return true;
        }

        public SkiLessonSnapshot GetSnapshot()
        {
            int individual = 0, group = 0, children = 0, adults = 0;
            foreach (var s in sessions) {
                if (!s.active) continue;
                if (s.format == SkiLessonFormat.Individual) individual++; else group++;
                if (s.audience == SkiLessonAudience.Children) children++; else adults++;
            }
            return new SkiLessonSnapshot {
                serviceId = service != null && service.definition != null ? service.definition.serviceId : "",
                serviceName = service != null && service.definition != null ? service.definition.serviceName : name,
                zoneType = zoneType, activeLessons = activeLessons, individualLessons = individual,
                groupLessons = group, childrenLessons = children, adultLessons = adults,
                activeStudents = activeStudents,
                utilization01 = service == null ? 0f : Mathf.Clamp01(activeStudents / (float)Mathf.Max(1, service.EffectiveCapacity))
            };
        }

        public static SkiSchoolRuntime FindBest(SkierRuntime skier, SkiLessonFormat format, bool preferPiouPiou)
        {
            SkiSchoolRuntime best = null; float bestScore = float.MinValue;
            foreach (var school in All) {
                if (school == null || !school.CanAccept(skier, format)) continue;
                float score = 0f;
                bool child = IsChild(skier);
                if (child && school.zoneType == SkiSchoolZoneType.PiouPiou) score += preferPiouPiou ? 2f : 1.2f;
                if (!child && school.zoneType == SkiSchoolZoneType.StandardSchool) score += 1.2f;
                score -= school.GetSnapshot().utilization01 * .5f;
                float distance = Vector3.Distance(skier.transform.position, school.transform.position) / 100f;
                score -= Mathf.Clamp01(distance) * .25f;
                if (score > bestScore) { bestScore = score; best = school; }
            }
            return best;
        }

        private SkiInstructorRuntime FindFreeInstructor()
        {
            EnsureInstructorPool();
            for (int i = 0; i < instructorPool.Count; i++) if (instructorPool[i] != null && !instructorPool[i].Busy) return instructorPool[i];
            return null;
        }

        private readonly List<SkiInstructorRuntime> instructorPool = new();

        private void EnsureInstructorPool()
        {
            instructorPool.RemoveAll(x => x == null);
            while (instructorPool.Count < Mathf.Max(1, instructors))
            {
                var go = new GameObject("Moniteur_de_ski");
                go.transform.SetParent(transform);
                var instructor = go.AddComponent<SkiInstructorRuntime>();
                instructor.Bind(this);
                instructorPool.Add(instructor);
            }
        }

        private void ReleaseInstructor(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            for (int i = 0; i < instructorPool.Count; i++)
                if (instructorPool[i] != null && instructorPool[i].GetInstanceID().ToString() == id) { instructorPool[i].Release(); return; }
        }

        private static SkierRuntime FindStudent(int instanceId)
        {
            var all = FindObjectsOfType<SkierRuntime>();
            for (int i = 0; i < all.Length; i++) if (all[i] != null && all[i].GetInstanceID() == instanceId) return all[i];
            return null;
        }

        private void TickSessions()
        {
            if (GameClock.I == null || sessions.Count == 0) return;
            float now = GameClock.I.CurrentHour;
            for (int i = sessions.Count - 1; i >= 0; i--) {
                var s = sessions[i]; if (!s.active) { sessions.RemoveAt(i); continue; }
                float elapsed = now - s.startHour; if (elapsed < 0f) elapsed += 24f;
                if (elapsed * 60f >= s.durationMinutes) {
                    s.active = false; activeLessons = Mathf.Max(0, activeLessons - 1); activeStudents = Mathf.Max(0, activeStudents - s.students);
                    ReleaseInstructor(s.instructorId);
                    for (int studentIndex = 0; studentIndex < s.studentInstanceIds.Count; studentIndex++)
                    {
                        var student = FindStudent(s.studentInstanceIds[studentIndex]);
                        if (student == null) continue;
                        var progress = student.GetComponent<SkiLessonProgressRuntime>() ?? student.gameObject.AddComponent<SkiLessonProgressRuntime>();
                        progress.CompleteLesson(s.format, zoneType);
                        student.ChangeSatisfaction(s.format == SkiLessonFormat.Individual ? 2.2f : 1.6f);
                    }
                    sessions.RemoveAt(i);
                }
            }
            Changed?.Invoke();
        }


        private void CollectSessionStudents(SkierRuntime lead, SkiLessonFormat format, List<int> ids)
        {
            ids.Clear();
            if (lead == null) return;
            ids.Add(lead.GetInstanceID());
            if (format != SkiLessonFormat.Group || VisitorGroupRuntime.I == null) return;
            var snapshot = VisitorGroupRuntime.I.Get(lead);
            if (snapshot.memberCount < 2) return;
            var all = FindObjectsOfType<SkierRuntime>();
            string groupId = snapshot.groupId;
            for (int i = 0; i < all.Length && ids.Count < maxGroupSize; i++)
            {
                var candidate = all[i];
                if (candidate == null || candidate == lead || candidate.State == SkierState.Leaving) continue;
                var candidateSnapshot = VisitorGroupRuntime.I.Get(candidate);
                if (candidateSnapshot.groupId != groupId) continue;
                if (GetAge(candidate) != GetAge(lead) && IsChild(candidate) != IsChild(lead)) continue;
                ids.Add(candidate.GetInstanceID());
            }
        }

        private static string FindTrainingSlopeName(SkiLessonLevel level, SkierRuntime skier)
        {
            SlopeDifficulty wanted = SlopeDifficulty.Green;
            switch (level)
            {
                case SkiLessonLevel.PremiereEtoile:
                case SkiLessonLevel.DeuxiemeEtoile: wanted = SlopeDifficulty.Blue; break;
                case SkiLessonLevel.TroisiemeEtoile:
                case SkiLessonLevel.EtoileOr: wanted = SlopeDifficulty.Red; break;
                case SkiLessonLevel.Fleche:
                case SkiLessonLevel.Chamois:
                case SkiLessonLevel.ChamoisOr: wanted = SlopeDifficulty.Black; break;
            }
            SlopeRuntime best = null; float bestScore = float.MaxValue;
            var slopes = FindObjectsOfType<SlopeRuntime>();
            for (int i = 0; i < slopes.Length; i++)
            {
                var slope = slopes[i];
                if (slope == null || !slope.Open) continue;
                float score = Mathf.Abs((int)slope.Difficulty - (int)wanted);
                if (score < bestScore) { bestScore = score; best = slope; }
            }
            return best != null ? best.name : "Zone débutants";
        }

        private int EstimateGroupSize(SkierRuntime skier)
        {
            if (VisitorGroupRuntime.I == null) return 4;
            var snap = VisitorGroupRuntime.I.Get(skier);
            return Mathf.Max(2, snap.memberCount);
        }

        private static bool IsChild(SkierRuntime s) => s != null && s.PopulationProfile != null && s.PopulationProfile.IsChild;
        private static bool ChildWantsPiouPiou(SkierRuntime s) => IsChild(s) && GetAge(s) <= 7;
        public static int GetAge(SkierRuntime s) => s != null && s.PopulationProfile != null ? s.PopulationProfile.AgeYears : 30;
    }
}
