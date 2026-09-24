using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum SkiDestinationKind { BaseVillage, MountainRestaurant, FamilySector, SportSector, SkiSchool, Panorama, Accommodation, Service }

    [Serializable]
    public struct SkiDestinationCandidate
    {
        public SkiDestinationKind kind;
        public Vector3 position;
        public StationServiceRuntime service;
        public string label;
        public float score;
    }

    /// <summary>
    /// 1.80 destination layer: turns services, managed sectors and alpine landmarks into
    /// concrete places a skier can deliberately travel to. Advisory only; existing movement
    /// and service runtimes remain authoritative until the Unity validation pass.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkiDestinationRuntime : MonoBehaviour
    {
        [SerializeField] private SkierRuntime skier;
        [SerializeField] private IntelligentSkiDayRuntime dayMind;
        [SerializeField, Min(2f)] private float refreshSeconds = 6f;
        [SerializeField, Min(20f)] private float arrivalRadius = 45f;

        private float timer;
        private SkiDestinationCandidate current;
        private bool hasDestination;
        private float sharedLockUntil;
        public bool HasDestination => hasDestination;
        public SkiDestinationCandidate Current => current;
        public event Action DestinationChanged;

        private void Awake()
        {
            if (skier == null) skier = GetComponent<SkierRuntime>();
            if (dayMind == null) dayMind = GetComponent<IntelligentSkiDayRuntime>();
        }

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < refreshSeconds) return;
            timer = 0f;
            if (hasDestination && Vector3.Distance(transform.position, current.position) <= arrivalRadius)
            {
                if (dayMind != null && (current.kind == SkiDestinationKind.MountainRestaurant || current.kind == SkiDestinationKind.Service))
                    dayMind.RegisterMealOrRest(current.kind == SkiDestinationKind.MountainRestaurant);
                hasDestination = false;
                DestinationChanged?.Invoke();
            }
            if (Time.unscaledTime >= sharedLockUntil) SelectBestDestination();
        }

        public void SetSharedDestination(SkiDestinationCandidate candidate, float lockSeconds)
        {
            bool changed = !hasDestination || current.kind != candidate.kind || Vector3.Distance(current.position, candidate.position) > 5f;
            current = candidate;
            hasDestination = true;
            sharedLockUntil = Mathf.Max(sharedLockUntil, Time.unscaledTime + Mathf.Max(1f, lockSeconds));
            if (changed) DestinationChanged?.Invoke();
        }

        public bool SelectBestDestination()
        {
            List<SkiDestinationCandidate> candidates = BuildCandidates();
            if (candidates.Count == 0) return false;
            SkiDestinationCandidate best = candidates[0];
            for (int i = 1; i < candidates.Count; i++) if (candidates[i].score > best.score) best = candidates[i];
            bool changed = !hasDestination || Vector3.Distance(best.position, current.position) > 5f || best.kind != current.kind;
            current = best; hasDestination = true;
            if (changed) DestinationChanged?.Invoke();
            return true;
        }

        private List<SkiDestinationCandidate> BuildCandidates()
        {
            List<SkiDestinationCandidate> list = new List<SkiDestinationCandidate>();
            SkiDayGoal goal = dayMind != null ? dayMind.Mind.goal : SkiDayGoal.Explore;
            for (int i = 0; i < StationServiceRuntime.All.Count; i++)
            {
                StationServiceRuntime s = StationServiceRuntime.All[i];
                if (s == null || s.definition == null || !s.IsOpen()) continue;
                SkiDestinationKind kind = KindForService(s.definition.type);
                float score = Score(kind, s.transform.position, goal);
                if (goal == SkiDayGoal.Lunch && (s.definition.type == StationServiceType.Restaurant || s.definition.type == StationServiceType.Bar)) score += 180f;
                if (goal == SkiDayGoal.Recover && (s.definition.type == StationServiceType.Restaurant || s.definition.type == StationServiceType.Accommodation)) score += 100f;
                list.Add(new SkiDestinationCandidate { kind = kind, position = s.transform.position, service = s, label = s.definition.serviceName, score = score });
            }

            AddZoneCandidate(list, ManagedZoneType.Family, SkiDestinationKind.FamilySector, goal == SkiDayGoal.FamilyCruise ? 130f : 10f);
            AddZoneCandidate(list, ManagedZoneType.Sport, SkiDestinationKind.SportSector, goal == SkiDayGoal.Training || goal == SkiDayGoal.Vertical ? 120f : 5f);
            AddZoneCandidate(list, ManagedZoneType.Beginner, SkiDestinationKind.SkiSchool, goal == SkiDayGoal.Progress || goal == SkiDayGoal.WarmUp ? 100f : 5f);

            IntegratedSkiDomainNetworkRuntime net = IntegratedSkiDomainNetworkRuntime.I;
            if (net != null && net.Links != null && net.Links.Count > 0)
            {
                Vector3 low = net.Links[0].from; Vector3 high = low;
                for (int i = 0; i < net.Links.Count; i++)
                {
                    Vector3 a = net.Links[i].from; Vector3 b = net.Links[i].to;
                    if (a.y < low.y) low = a; if (b.y < low.y) low = b;
                    if (a.y > high.y) high = a; if (b.y > high.y) high = b;
                }
                float baseBonus = goal == SkiDayGoal.ReturnToBase ? 250f : 0f;
                list.Add(new SkiDestinationCandidate { kind = SkiDestinationKind.BaseVillage, position = low, label = "Front de neige", score = Score(SkiDestinationKind.BaseVillage, low, goal) + baseBonus });
                float panoBonus = goal == SkiDayGoal.Explore || goal == SkiDayGoal.Vertical ? 80f : 0f;
                list.Add(new SkiDestinationCandidate { kind = SkiDestinationKind.Panorama, position = high, label = "Point panoramique", score = Score(SkiDestinationKind.Panorama, high, goal) + panoBonus });
            }
            return list;
        }

        private void AddZoneCandidate(List<SkiDestinationCandidate> list, ManagedZoneType type, SkiDestinationKind kind, float bonus)
        {
            ManagedZoneRuntime z = ManagedZoneRuntime.I;
            if (z == null || z.Zones == null) return;
            for (int i = 0; i < z.Zones.Count; i++)
            {
                ManagedZoneSnapshot snap = z.Zones[i];
                if (snap == null || snap.type != type) continue;
                ManagedZoneType ignored; List<Vector3> points;
                if (!z.TryGetZoneData(snap.name, out ignored, out points) || points == null || points.Count == 0) continue;
                Vector3 center = Vector3.zero; for (int p = 0; p < points.Count; p++) center += points[p]; center /= points.Count;
                list.Add(new SkiDestinationCandidate { kind = kind, position = center, label = snap.name, score = Score(kind, center, dayMind != null ? dayMind.Mind.goal : SkiDayGoal.Explore) + bonus });
            }
        }

        private float Score(SkiDestinationKind kind, Vector3 p, SkiDayGoal goal)
        {
            float distance = Vector3.Distance(transform.position, p);
            float score = -distance * .015f;
            if (goal == SkiDayGoal.ReturnToBase && kind == SkiDestinationKind.BaseVillage) score += 200f;
            if (goal == SkiDayGoal.Explore && kind == SkiDestinationKind.Panorama) score += 80f;
            if (goal == SkiDayGoal.FamilyCruise && kind == SkiDestinationKind.FamilySector) score += 100f;
            if (goal == SkiDayGoal.Training && kind == SkiDestinationKind.SportSector) score += 100f;
            return score;
        }

        private static SkiDestinationKind KindForService(StationServiceType type)
        {
            switch (type)
            {
                case StationServiceType.Restaurant:
                case StationServiceType.Bar: return SkiDestinationKind.MountainRestaurant;
                case StationServiceType.SkiSchool: return SkiDestinationKind.SkiSchool;
                case StationServiceType.Accommodation: return SkiDestinationKind.Accommodation;
                default: return SkiDestinationKind.Service;
            }
        }
    }
}
