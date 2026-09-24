using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    [Serializable]
    public struct SkiDomainRouteDecision
    {
        public SkierRuntime skier;
        public SlopeRuntime currentSlope;
        public LiftRuntime nextLift;
        public SlopeRuntime nextSlope;
        public float score;
        public string reason;
        public float timestamp;
    }

    /// <summary>
    /// 1.00 bridge between the real management skier, the real lift network and
    /// the real slope graph. It makes route decisions only; it never creates
    /// visitors, vehicles, traffic or player avatars, and it never moves a skier.
    /// </summary>
    public sealed class SkiDomainRouteDecisionRuntime : MonoBehaviour
    {
        public static SkiDomainRouteDecisionRuntime I { get; private set; }

        [SerializeField, Min(0.25f)] private float refreshSeconds = 1f;
        [SerializeField, Min(5f)] private float nearestSlopeRadius = 45f;
        [SerializeField, Min(0f)] private float liftPreferenceWeight = 1f;

        private readonly Dictionary<SkierRuntime, SkiDomainRouteDecision> decisions = new();
        private float timer;

        public IReadOnlyDictionary<SkierRuntime, SkiDomainRouteDecision> Decisions => decisions;
        public event Action Changed;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < refreshSeconds) return;
            timer = 0f;
            RecalculateAll();
        }

        public bool TryDecide(SkierRuntime skier, out SkiDomainRouteDecision decision)
        {
            decision = default;
            if (skier == null || skier.State == SkierState.Leaving) return false;
            if (ResortRouteNetwork.I == null) return false;

            SlopeRuntime current = FindNearestSlope(skier.transform.position);
            LiftRuntime nextLift = null;
            SlopeRuntime nextSlope = null;
            float score = 0f;
            string reason;

            if (current != null && (skier.State == SkierState.Skiing || skier.State == SkierState.Resting))
            {
                nextLift = VisitorBehaviorRuntime.I != null
                    ? VisitorBehaviorRuntime.I.ChooseLift(skier, current.BottomPoint, current.TopPoint)
                    : ResortRouteNetwork.I.FindLiftFromSlope(current);

                if (nextLift != null)
                {
                    nextSlope = VisitorBehaviorRuntime.I != null
                        ? VisitorBehaviorRuntime.I.ChooseSlope(skier, nextLift)
                        : ResortRouteNetwork.I.FindSlopeFromLift(nextLift);
                    score = nextLift.EffectiveSpeedKmh * liftPreferenceWeight;
                    reason = nextSlope != null
                        ? "Piste actuelle → remontée → prochaine piste"
                        : "Piste actuelle → remontée";
                }
                else
                {
                    reason = "Aucune remontée compatible à proximité";
                }
            }
            else if (skier.State == SkierState.WaitingForLift || skier.State == SkierState.RidingLift)
            {
                nextLift = FindNearestUsableLift(skier.transform.position);
                if (nextLift != null)
                {
                    nextSlope = VisitorBehaviorRuntime.I != null
                        ? VisitorBehaviorRuntime.I.ChooseSlope(skier, nextLift)
                        : ResortRouteNetwork.I.FindSlopeFromLift(nextLift);
                    score = nextLift.EffectiveSpeedKmh * liftPreferenceWeight;
                    reason = nextSlope != null
                        ? "Remontée → choix de piste selon le profil"
                        : "Remontée → aucune piste compatible détectée";
                }
                else reason = "Aucune remontée disponible";
            }
            else
            {
                reason = "État hors décision montagne";
            }

            decision = new SkiDomainRouteDecision
            {
                skier = skier,
                currentSlope = current,
                nextLift = nextLift,
                nextSlope = nextSlope,
                score = score,
                reason = reason,
                timestamp = Time.time
            };
            decisions[skier] = decision;
            return nextLift != null || nextSlope != null;
        }

        public void RecalculateAll()
        {
            var skiers = FindObjectsOfType<SkierRuntime>();
            var live = new HashSet<SkierRuntime>(skiers);
            foreach (var skier in skiers)
            {
                if (skier == null || skier.State == SkierState.Leaving) continue;
                TryDecide(skier, out _);
            }

            var dead = new List<SkierRuntime>();
            foreach (var pair in decisions)
                if (!live.Contains(pair.Key)) dead.Add(pair.Key);
            foreach (var skier in dead) decisions.Remove(skier);
            Changed?.Invoke();
        }

        private SlopeRuntime FindNearestSlope(Vector3 position)
        {
            SlopeRuntime best = null;
            float bestDistance = nearestSlopeRadius;
            foreach (var slope in ResortRouteNetwork.I.Slopes)
            {
                if (slope == null || !slope.Open || slope.Points.Count < 2) continue;
                float distance = Mathf.Min(Vector3.Distance(position, slope.TopPoint), Vector3.Distance(position, slope.BottomPoint));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = slope;
                }
            }
            return best;
        }

        private LiftRuntime FindNearestUsableLift(Vector3 position)
        {
            LiftRuntime best = null;
            float bestDistance = nearestSlopeRadius;
            foreach (var lift in ResortRouteNetwork.I.Lifts)
            {
                if (lift == null || !lift.Open || lift.Faulted || lift.Definition == null) continue;
                float distance = Vector3.Distance(position, lift.Bottom);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = lift;
                }
            }
            return best;
        }
    }
}
