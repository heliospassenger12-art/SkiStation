using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    [Serializable]
    public struct VisitorGroupSnapshot
    {
        public string groupId;
        public SkierPopulationArchetype archetype;
        public int memberCount;
        public string leaderName;
        public string sharedIntent;
        [Range(0f, 1f)] public float cohesion01;
        [Range(0f, 1f)] public float reunionPressure01;
        [Range(0f, 1f)] public float togetherness01;
        [Range(0f, 1f)] public float averageSatisfaction01;
        public string favoriteSlopeId;
        public string favoriteLiftId;
        public string favoriteServiceId;
        public int sharedDecisions;
        public int temporarySplits;
    }

    /// <summary>
    /// Coordinates real management visitors that belong to an explicit group.
    /// A group never creates or moves visitors by itself: it only influences decisions,
    /// records shared choices and observes whether members are physically together.
    /// Player simulation is deliberately excluded.
    /// </summary>
    public sealed class VisitorGroupRuntime : MonoBehaviour
    {
        public static VisitorGroupRuntime I { get; private set; }

        [SerializeField] private float refreshSeconds = 2.5f;
        [SerializeField, Range(0f, 1f)] private float defaultCohesion = .78f;
        [SerializeField, Range(0f, 1f)] private float sharedChoiceWeight = .22f;
        [SerializeField] private float togetherDistanceMeters = 18f;
        [SerializeField] private float splitDistanceMeters = 45f;

        private readonly Dictionary<string, List<SkierRuntime>> groups = new();
        private readonly Dictionary<string, VisitorGroupSnapshot> snapshots = new();
        private readonly Dictionary<string, string> slopeFavorites = new();
        private readonly Dictionary<string, string> liftFavorites = new();
        private readonly Dictionary<string, string> serviceFavorites = new();
        private readonly Dictionary<string, int> slopeChoiceCounts = new();
        private readonly Dictionary<string, int> liftChoiceCounts = new();
        private readonly Dictionary<string, int> serviceChoiceCounts = new();
        private readonly HashSet<string> currentlySeparated = new();
        private float timer;

        public IReadOnlyDictionary<string, VisitorGroupSnapshot> Snapshots => snapshots;
        public event Action Changed;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start() => Refresh();

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < Mathf.Max(.5f, refreshSeconds)) return;
            timer = 0f;
            Refresh();
        }

        public bool IsGrouped(SkierRuntime skier)
        {
            string id = GetGroupId(skier);
            return !string.IsNullOrEmpty(id);
        }

        public VisitorGroupSnapshot Get(SkierRuntime skier)
        {
            string id = GetGroupId(skier);
            if (string.IsNullOrEmpty(id) || !snapshots.TryGetValue(id, out var snapshot)) return default;
            return snapshot;
        }

        /// <summary>Returns a small score adjustment encouraging a group to make a compatible choice.</summary>
        public float ScoreSlope(SkierRuntime skier, SlopeRuntime slope)
        {
            if (skier == null || slope == null) return 0f;
            string id = GetGroupId(skier);
            if (string.IsNullOrEmpty(id) || !groups.TryGetValue(id, out var members) || members.Count < 2) return 0f;
            float score = 0f;
            if (slopeFavorites.TryGetValue(id, out var favorite) && favorite == slope.name)
                score += sharedChoiceWeight;
            score += SharedDifficultyFit(members, slope) * .22f;
            return score;
        }

        public float ScoreLift(SkierRuntime skier, LiftRuntime lift)
        {
            if (skier == null || lift == null) return 0f;
            string id = GetGroupId(skier);
            if (string.IsNullOrEmpty(id) || !groups.TryGetValue(id, out var members) || members.Count < 2) return 0f;
            return liftFavorites.TryGetValue(id, out var favorite) && favorite == lift.name ? sharedChoiceWeight : 0f;
        }

        public float ScoreService(SkierRuntime skier, string serviceId)
        {
            if (skier == null || string.IsNullOrEmpty(serviceId)) return 0f;
            string id = GetGroupId(skier);
            if (string.IsNullOrEmpty(id) || !groups.TryGetValue(id, out var members) || members.Count < 2) return 0f;
            return serviceFavorites.TryGetValue(id, out var favorite) && favorite == serviceId ? sharedChoiceWeight : 0f;
        }

        public void RegisterSlopeChoice(SkierRuntime skier, SlopeRuntime slope)
        {
            if (skier == null || slope == null) return;
            RegisterChoice(skier, slope.name, slopeFavorites, slopeChoiceCounts);
        }

        public void RegisterLiftChoice(SkierRuntime skier, LiftRuntime lift)
        {
            if (skier == null || lift == null) return;
            RegisterChoice(skier, lift.name, liftFavorites, liftChoiceCounts);
        }

        public void RegisterServiceChoice(SkierRuntime skier, string serviceId)
        {
            if (skier == null || string.IsNullOrEmpty(serviceId)) return;
            RegisterChoice(skier, serviceId, serviceFavorites, serviceChoiceCounts);
        }

        public void Refresh()
        {
            groups.Clear();
            var skiers = FindObjectsOfType<SkierRuntime>();
            for (int i = 0; i < skiers.Length; i++)
            {
                var skier = skiers[i];
                if (skier == null || skier.State == SkierState.Leaving) continue;
                string id = GetGroupId(skier);
                if (string.IsNullOrEmpty(id)) continue;
                if (!groups.TryGetValue(id, out var list))
                {
                    list = new List<SkierRuntime>();
                    groups[id] = list;
                }
                list.Add(skier);
            }

            var alive = new HashSet<string>(groups.Keys);
            var dead = new List<string>();
            foreach (var key in snapshots.Keys) if (!alive.Contains(key)) dead.Add(key);
            foreach (var key in dead)
            {
                snapshots.Remove(key);
                currentlySeparated.Remove(key);
            }

            foreach (var pair in groups)
                snapshots[pair.Key] = BuildSnapshot(pair.Key, pair.Value);

            Changed?.Invoke();
        }

        private VisitorGroupSnapshot BuildSnapshot(string id, List<SkierRuntime> members)
        {
            var first = members.Count > 0 ? members[0] : null;
            var profile = first != null ? first.PopulationProfile : null;
            float avgSatisfaction = 0f;
            float maxDistance = 0f;
            string leader = profile != null ? profile.DisplayName : "Groupe";
            for (int i = 0; i < members.Count; i++)
            {
                avgSatisfaction += members[i].Satisfaction / 100f;
                for (int j = i + 1; j < members.Count; j++)
                    maxDistance = Mathf.Max(maxDistance, Vector3.Distance(members[i].transform.position, members[j].transform.position));
            }
            avgSatisfaction /= Mathf.Max(1, members.Count);

            bool separated = maxDistance >= splitDistanceMeters;
            bool together = maxDistance <= togetherDistanceMeters;
            if (separated && currentlySeparated.Add(id)) { }
            if (!separated) currentlySeparated.Remove(id);

            float togetherness = 1f - Mathf.Clamp01((maxDistance - togetherDistanceMeters) / Mathf.Max(1f, splitDistanceMeters - togetherDistanceMeters));
            float reunion = separated ? .55f + defaultCohesion * .45f : 0f;

            return new VisitorGroupSnapshot
            {
                groupId = id,
                archetype = profile != null ? profile.Archetype : SkierPopulationArchetype.Solo,
                memberCount = members.Count,
                leaderName = leader,
                sharedIntent = ResolveSharedIntent(members),
                cohesion01 = defaultCohesion,
                reunionPressure01 = Mathf.Clamp01(reunion),
                togetherness01 = Mathf.Clamp01(togetherness),
                averageSatisfaction01 = Mathf.Clamp01(avgSatisfaction),
                favoriteSlopeId = slopeFavorites.TryGetValue(id, out var s) ? s : null,
                favoriteLiftId = liftFavorites.TryGetValue(id, out var l) ? l : null,
                favoriteServiceId = serviceFavorites.TryGetValue(id, out var f) ? f : null,
                sharedDecisions = GetCount(slopeChoiceCounts, id) + GetCount(liftChoiceCounts, id) + GetCount(serviceChoiceCounts, id),
                temporarySplits = separated ? 1 : 0
            };
        }

        private void RegisterChoice(SkierRuntime skier, string value, Dictionary<string, string> favorites, Dictionary<string, int> counts)
        {
            string id = GetGroupId(skier);
            if (string.IsNullOrEmpty(id) || !groups.ContainsKey(id)) return;
            int count = GetCount(counts, id) + 1;
            counts[id] = count;
            if (!favorites.ContainsKey(id) || count == 1) favorites[id] = value;
            else if (UnityEngine.Random.value < .35f) favorites[id] = value;
            if (snapshots.TryGetValue(id, out var snapshot))
            {
                snapshot.sharedDecisions++;
                if (favorites == slopeFavorites) snapshot.favoriteSlopeId = favorites[id];
                else if (favorites == liftFavorites) snapshot.favoriteLiftId = favorites[id];
                else snapshot.favoriteServiceId = favorites[id];
                snapshots[id] = snapshot;
            }
            Changed?.Invoke();
        }

        private static int GetCount(Dictionary<string, int> dict, string key) => dict.TryGetValue(key, out var value) ? value : 0;

        private static string GetGroupId(SkierRuntime skier)
        {
            return skier != null && skier.PopulationProfile != null ? skier.PopulationProfile.GroupId : null;
        }

        private static float SharedDifficultyFit(List<SkierRuntime> members, SlopeRuntime slope)
        {
            if (members == null || members.Count == 0 || slope == null) return 0f;
            float total = 0f;
            for (int i = 0; i < members.Count; i++)
            {
                var p = members[i].PopulationProfile;
                CustomerProfile profile = p != null ? p.ToCustomerProfile() : CustomerProfile.Tourist;
                switch (profile)
                {
                    case CustomerProfile.Family:
                    case CustomerProfile.Beginner:
                    case CustomerProfile.Youth:
                        total += slope.Difficulty == SlopeDifficulty.Green || slope.Difficulty == SlopeDifficulty.Blue ? 1f : -.35f;
                        break;
                    case CustomerProfile.Expert:
                    case CustomerProfile.Athlete:
                        total += slope.Difficulty == SlopeDifficulty.Red || slope.Difficulty == SlopeDifficulty.Black ? 1f : .10f;
                        break;
                    default:
                        total += .35f;
                        break;
                }
            }
            return Mathf.Clamp(total / members.Count, -1f, 1f);
        }

        private static string ResolveSharedIntent(List<SkierRuntime> members)
        {
            if (members == null || members.Count == 0) return "";
            var intents = new Dictionary<VisitorIntent, int>();
            for (int i = 0; i < members.Count; i++)
            {
                var skier = members[i];
                VisitorIntent intent = VisitorIntent.SkiDay;
                if (VisitorIntentRuntime.I != null) intent = VisitorIntentRuntime.I.GetIntent(skier);
                intents[intent] = intents.TryGetValue(intent, out var count) ? count + 1 : 1;
            }
            VisitorIntent best = VisitorIntent.SkiDay;
            int bestCount = -1;
            foreach (var pair in intents) if (pair.Value > bestCount) { best = pair.Key; bestCount = pair.Value; }
            return best.ToString();
        }
    }
}
