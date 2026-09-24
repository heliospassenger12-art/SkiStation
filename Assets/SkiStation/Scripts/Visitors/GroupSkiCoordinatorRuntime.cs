using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// 1.81 group decision layer. Families, couples, friends and elite teams share a destination,
    /// wait/reunite when cohesion is lost, and keep the weakest member in mind when travelling.
    /// Movement itself remains owned by the existing skier/network runtimes.
    /// </summary>
    public sealed class GroupSkiCoordinatorRuntime : MonoBehaviour
    {
        public static GroupSkiCoordinatorRuntime I { get; private set; }

        [SerializeField, Min(1f)] private float refreshSeconds = 4f;
        [SerializeField, Min(10f)] private float regroupDistance = 55f;
        [SerializeField, Min(5f)] private float reunitedDistance = 24f;

        private readonly Dictionary<string, GroupState> states = new Dictionary<string, GroupState>();
        private float timer;

        [Serializable]
        private sealed class GroupState
        {
            public string id;
            public SkierRuntime leader;
            public readonly List<SkierRuntime> members = new List<SkierRuntime>();
            public bool regrouping;
            public SkiDestinationCandidate sharedDestination;
            public bool hasSharedDestination;
        }

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start() { RefreshGroups(); Coordinate(); }

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < refreshSeconds) return;
            timer = 0f;
            RefreshGroups();
            Coordinate();
        }

        private void RefreshGroups()
        {
            states.Clear();
            SkierRuntime[] skiers = FindObjectsOfType<SkierRuntime>();
            for (int i = 0; i < skiers.Length; i++)
            {
                SkierRuntime skier = skiers[i];
                if (skier == null || skier.State == SkierState.Leaving || skier.PopulationProfile == null) continue;
                string id = skier.PopulationProfile.GroupId;
                if (string.IsNullOrEmpty(id)) continue;
                GroupState state;
                if (!states.TryGetValue(id, out state))
                {
                    state = new GroupState { id = id };
                    states.Add(id, state);
                }
                state.members.Add(skier);
            }

            foreach (KeyValuePair<string, GroupState> pair in states)
                pair.Value.leader = ChooseLeader(pair.Value.members);
        }

        private void Coordinate()
        {
            foreach (KeyValuePair<string, GroupState> pair in states)
            {
                GroupState group = pair.Value;
                if (group.members.Count < 2 || group.leader == null) continue;

                float spread = MaximumDistanceFrom(group.leader, group.members);
                if (spread >= regroupDistance) group.regrouping = true;
                else if (spread <= reunitedDistance) group.regrouping = false;

                SkiDestinationRuntime leaderDestination = group.leader.GetComponent<SkiDestinationRuntime>();
                if (leaderDestination == null) continue;

                if (!leaderDestination.HasDestination) leaderDestination.SelectBestDestination();
                if (leaderDestination.HasDestination)
                {
                    group.sharedDestination = leaderDestination.Current;
                    group.hasSharedDestination = true;
                }

                for (int i = 0; i < group.members.Count; i++)
                {
                    SkierRuntime member = group.members[i];
                    if (member == null || member == group.leader) continue;
                    SkiDestinationRuntime destination = member.GetComponent<SkiDestinationRuntime>();
                    if (destination == null) continue;

                    // A separated member first heads toward the leader's current reachable area;
                    // otherwise everybody receives the same semantic destination.
                    if (group.regrouping)
                    {
                        SkiDestinationCandidate reunion = new SkiDestinationCandidate
                        {
                            kind = SkiDestinationKind.Service,
                            position = group.leader.transform.position,
                            service = null,
                            label = "Regroupement " + pair.Key,
                            score = 1000f
                        };
                        destination.SetSharedDestination(reunion, refreshSeconds * 2.5f);
                    }
                    else if (group.hasSharedDestination)
                    {
                        destination.SetSharedDestination(group.sharedDestination, refreshSeconds * 2.5f);
                    }

                    MultiStepSkiJourneyPlanner planner = member.GetComponent<MultiStepSkiJourneyPlanner>();
                    if (planner != null) planner.ReplanFrom(member.transform.position);
                }
            }
        }

        private static SkierRuntime ChooseLeader(List<SkierRuntime> members)
        {
            if (members == null || members.Count == 0) return null;
            for (int i = 0; i < members.Count; i++)
                if (members[i] != null && members[i].PopulationProfile != null && !members[i].PopulationProfile.IsChild)
                    return members[i];
            return members[0];
        }

        private static float MaximumDistanceFrom(SkierRuntime leader, List<SkierRuntime> members)
        {
            float max = 0f;
            for (int i = 0; i < members.Count; i++)
                if (members[i] != null) max = Mathf.Max(max, Vector3.Distance(leader.transform.position, members[i].transform.position));
            return max;
        }
    }
}
