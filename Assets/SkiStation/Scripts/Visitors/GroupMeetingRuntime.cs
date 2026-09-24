using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum GroupMeetingPurpose { Lunch, SkiSchoolPickup, AfternoonRegroup, ReturnHome }

    [Serializable]
    public sealed class GroupMeetingPlan
    {
        public string groupId;
        public GroupMeetingPurpose purpose;
        public float targetHour;
        public SkiDestinationCandidate destination;
        public bool completed;
    }

    /// <summary>
    /// 1.82 scheduled group rendezvous. Groups may split for lessons or individual skiing,
    /// then receive a shared destination shortly before a planned meeting time.
    /// </summary>
    public sealed class GroupMeetingRuntime : MonoBehaviour
    {
        public static GroupMeetingRuntime I { get; private set; }
        [SerializeField, Min(5f)] private float refreshSeconds = 8f;
        [SerializeField, Range(0.05f, 1f)] private float anticipationHours = .30f;
        private readonly List<GroupMeetingPlan> plans = new List<GroupMeetingPlan>();
        private float timer;
        public IList<GroupMeetingPlan> Plans { get { return plans; } }

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
        }

        private void Start() { BuildDailyPlans(); }

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < refreshSeconds) return;
            timer = 0f;
            if (plans.Count == 0) BuildDailyPlans();
            ApplyDueMeetings();
        }

        public void BuildDailyPlans()
        {
            plans.Clear();
            Dictionary<string, List<SkierRuntime>> groups = CollectGroups();
            foreach (KeyValuePair<string, List<SkierRuntime>> pair in groups)
            {
                if (pair.Value.Count < 2) continue;
                SkiDestinationCandidate lunch;
                if (TryFindGroupDestination(pair.Value, SkiDestinationKind.MountainRestaurant, out lunch))
                    plans.Add(NewPlan(pair.Key, GroupMeetingPurpose.Lunch, 12.5f, lunch));

                if (ContainsChild(pair.Value))
                {
                    SkiDestinationCandidate school;
                    if (TryFindGroupDestination(pair.Value, SkiDestinationKind.SkiSchool, out school))
                        plans.Add(NewPlan(pair.Key, GroupMeetingPurpose.SkiSchoolPickup, 16f, school));
                }

                SkiDestinationCandidate home;
                if (TryFindGroupDestination(pair.Value, SkiDestinationKind.BaseVillage, out home))
                    plans.Add(NewPlan(pair.Key, GroupMeetingPurpose.ReturnHome, 17.25f, home));
            }
        }

        private void ApplyDueMeetings()
        {
            if (GameClock.I == null) return;
            float hour = GameClock.I.CurrentHour;
            Dictionary<string, List<SkierRuntime>> groups = CollectGroups();
            for (int i = 0; i < plans.Count; i++)
            {
                GroupMeetingPlan plan = plans[i];
                if (plan.completed || hour < plan.targetHour - anticipationHours) continue;
                List<SkierRuntime> members;
                if (!groups.TryGetValue(plan.groupId, out members)) continue;
                for (int m = 0; m < members.Count; m++)
                {
                    SkiDestinationRuntime destination = members[m] != null ? members[m].GetComponent<SkiDestinationRuntime>() : null;
                    if (destination != null) destination.SetSharedDestination(plan.destination, Mathf.Max(30f, refreshSeconds * 4f));
                    MultiStepSkiJourneyPlanner planner = members[m] != null ? members[m].GetComponent<MultiStepSkiJourneyPlanner>() : null;
                    if (planner != null) planner.ReplanFrom(members[m].transform.position);
                }
                if (hour >= plan.targetHour + .35f) plan.completed = true;
            }
        }

        private static GroupMeetingPlan NewPlan(string id, GroupMeetingPurpose purpose, float hour, SkiDestinationCandidate destination)
        {
            return new GroupMeetingPlan { groupId = id, purpose = purpose, targetHour = hour, destination = destination };
        }

        private static Dictionary<string, List<SkierRuntime>> CollectGroups()
        {
            Dictionary<string, List<SkierRuntime>> result = new Dictionary<string, List<SkierRuntime>>();
            SkierRuntime[] all = FindObjectsOfType<SkierRuntime>();
            for (int i = 0; i < all.Length; i++)
            {
                SkierRuntime s = all[i];
                if (s == null || s.PopulationProfile == null || string.IsNullOrEmpty(s.PopulationProfile.GroupId)) continue;
                List<SkierRuntime> list;
                if (!result.TryGetValue(s.PopulationProfile.GroupId, out list)) { list = new List<SkierRuntime>(); result.Add(s.PopulationProfile.GroupId, list); }
                list.Add(s);
            }
            return result;
        }

        private static bool ContainsChild(List<SkierRuntime> members)
        {
            for (int i = 0; i < members.Count; i++) if (members[i] != null && members[i].PopulationProfile != null && members[i].PopulationProfile.IsChild) return true;
            return false;
        }

        private static bool TryFindGroupDestination(List<SkierRuntime> members, SkiDestinationKind kind, out SkiDestinationCandidate candidate)
        {
            candidate = new SkiDestinationCandidate();
            for (int i = 0; i < members.Count; i++)
            {
                SkiDestinationRuntime d = members[i] != null ? members[i].GetComponent<SkiDestinationRuntime>() : null;
                if (d == null) continue;
                if (!d.HasDestination) d.SelectBestDestination();
                if (d.HasDestination && d.Current.kind == kind) { candidate = d.Current; return true; }
            }
            if (kind == SkiDestinationKind.BaseVillage && IntegratedSkiDomainNetworkRuntime.I != null && IntegratedSkiDomainNetworkRuntime.I.Links.Count > 0)
            {
                Vector3 low = IntegratedSkiDomainNetworkRuntime.I.Links[0].from;
                for (int i = 0; i < IntegratedSkiDomainNetworkRuntime.I.Links.Count; i++)
                {
                    Vector3 a = IntegratedSkiDomainNetworkRuntime.I.Links[i].from, b = IntegratedSkiDomainNetworkRuntime.I.Links[i].to;
                    if (a.y < low.y) low = a; if (b.y < low.y) low = b;
                }
                candidate = new SkiDestinationCandidate { kind = kind, position = low, label = "Front de neige", score = 100f }; return true;
            }
            return false;
        }
    }
}
