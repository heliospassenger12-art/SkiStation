using UnityEngine;

namespace SkiStation
{
    /// <summary>3.29: arbitrate at the base only; physical ski legs retain their destination and owner.</summary>
    [DisallowMultipleComponent]
    public sealed class VisitorDayDecision329 : MonoBehaviour
    {
        public VisitorDayAction329 Action { get; private set; } = VisitorDayAction329.Ski;
        public string Reason { get; private set; } = "Decision au prochain passage au front de neige";
        public string Label => Action == VisitorDayAction329.Eat ? "Manger" : Action == VisitorDayAction329.Rest ? "Se reposer" : Action == VisitorDayAction329.Home ? "Rentrer" : "Skier";
        public bool OwnsMovement => active;
        public bool IsResting => active && Action == VisitorDayAction329.Rest;
        public bool IsWalkingToMeal => active && Action == VisitorDayAction329.Eat && !requested;
        SkierRuntime skier;
        IntelligentSkiDayRuntime mind;
        StationServiceRuntime restaurant;
        bool active, requested;
        float started, deadline, mealRetryAfter;

        void Awake()
        {
            skier = GetComponent<SkierRuntime>();
            mind = GetComponent<IntelligentSkiDayRuntime>() ?? gameObject.AddComponent<IntelligentSkiDayRuntime>();
        }
        public static VisitorDayDecision329 Ensure(SkierRuntime skier)
        {
            if (skier == null || Arcade.ArcadeSceneIsolation.IsActive) return null;
            return skier.GetComponent<VisitorDayDecision329>() ?? skier.gameObject.AddComponent<VisitorDayDecision329>();
        }

        public bool TryChooseAtBase(float skiMinutes)
        {
            if (active) return true;
            if (GameClock.I == null || skier == null || !ManagementJourneyAccess.CanExitToVillage(transform.position)) return false;
            var arrival = GetComponent<StationVisitorRoadArrival248>();
            if (arrival != null && arrival.IsArriving) return false;
            var budget = VisitorDayPlan328.Ensure(skier);
            float mealMinutes;
            restaurant = FindMeal(budget, out mealMinutes);
            Action = VisitorDecisionRules329.Choose(budget.MinutesRemaining, budget.ReservedMinutes,
                skiMinutes, mind.Mind.hunger01, mind.Mind.energy01, restaurant != null);
            switch (Action)
            {
                case VisitorDayAction329.Home:
                    Reason = "Le temps restant ne permet plus un cycle avec sa marge de retour";
                    skier.StopMoving();
                    // Existing lifecycle handles real exit or overnight accommodation.
                    var life = GetComponent<VisitorLifecycleRuntime>() ?? gameObject.AddComponent<VisitorLifecycleRuntime>();
                    life.RequestDayReturn329();
                    return true;
                case VisitorDayAction329.Rest:
                    Reason = mind.Mind.energy01 <= .30f ? "Energie basse : pause de 10 minutes de jeu" : "Aucun trajet disponible : pause puis nouvelle recherche";
                    active = true; started = GameClock.I.ElapsedGameMinutes;
                    deadline = started + 10f; skier.StopMoving(); skier.SetState(SkierState.Resting);
                    return true;
                case VisitorDayAction329.Eat:
                    Reason = "Faim : " + restaurant.definition.serviceName + "; marche et attente estimees incluses";
                    active = true; requested = false; started = GameClock.I.ElapsedGameMinutes;
                    deadline = started + Mathf.Min(mealMinutes + 5f, budget.MinutesRemaining - budget.ReservedMinutes);
                    skier.SetState(SkierState.Resting); skier.MoveTo(restaurant.transform.position);
                    return true;
                default:
                    Reason = mind.Mind.hunger01 >= .66f && restaurant == null ? "Aucun repas proche disponible dans le temps restant ; ski possible" : "Trajet compatible avec le temps restant";
                    return false;
            }
        }

        StationServiceRuntime FindMeal(VisitorDayPlan328 budget, out float duration)
        {
            duration = float.PositiveInfinity;
            if (GameClock.I.ElapsedGameMinutes < mealRetryAfter || mind.Mind.hunger01 < .66f) return null;
            StationServiceRuntime best = null; float score = float.PositiveInfinity;
            foreach (var service in StationServiceRuntime.All)
            {
                if (service == null || service.definition == null || service.definition.type != StationServiceType.Restaurant || !service.CanRequest(skier)) continue;
                Vector3 target = service.transform.position;
                // Conservative first step: local services only. No straight-line climb to mountain restaurants.
                if (!ManagementJourneyAccess.CanExitToVillage(target) || !ManagementJourneyAccess.CanWalk(transform.position, target, 250f)) continue;
                float walk = GameClock.I.RealSecondsToGameMinutes(Vector3.Distance(transform.position, target) / 6f);
                float wait = service.AvailableCapacity > 0 && service.QueueCount == 0 ? 0f : Mathf.Ceil((service.ActiveCustomers + service.QueueCount) / (float)Mathf.Max(1, service.EffectiveCapacity)) * service.definition.serviceMinutes;
                float total = walk + wait + service.definition.serviceMinutes;
                if (total + budget.ReservedMinutes > budget.MinutesRemaining) continue;
                float close = service.definition.closingHour;
                if (close < service.definition.openingHour && GameClock.I.CurrentHour >= service.definition.openingHour) close += 24f;
                if (GameClock.I.CurrentHour + total / 60f >= close) continue;
                float candidate = total;
                if (skier.PopulationProfile != null && skier.PopulationProfile.IsChild && !service.definition.familyFriendly) candidate += 10f;
                if (candidate < score) { score = candidate; best = service; duration = total; }
            }
            return best;
        }

        void Update()
        {
            if (!active || GameClock.I == null || skier == null || Time.deltaTime <= 0f) return;
            float now = GameClock.I.ElapsedGameMinutes;
            if (IsResting)
            {
                if (now >= deadline)
                {
                    mind.RegisterMealOrRest(false); active = false;
                    Reason = "Pause terminee : nouvelle decision au front de neige";
                }
                return;
            }
            if (restaurant == null || restaurant.definition == null || !restaurant.IsOpen() || now >= deadline || VisitorDayPlan328.Ensure(skier).ReturnWindowStarted)
            { CancelMeal(); return; }
            if (requested) return;
            if (Vector3.Distance(transform.position, restaurant.transform.position) > restaurant.definition.arrivalRadius) return;
            skier.StopMoving();
            requested = restaurant.RequestService(skier);
            if (!requested) CancelMeal();
            else Reason = "Au restaurant : file ou repas en cours";
        }

        public void ServiceCompleted(StationServiceRuntime service)
        {
            if (!active || Action != VisitorDayAction329.Eat || service != restaurant || !requested) return;
            mind.RegisterMealOrRest(true);
            active = false; requested = false; restaurant = null;
            Reason = "Repas effectivement termine";
        }
        void CancelMeal()
        {
            if (restaurant != null) restaurant.CancelVisit(skier);
            restaurant = null; requested = false; active = false;
            mealRetryAfter = GameClock.I != null ? GameClock.I.ElapsedGameMinutes + 15f : 0f;
            skier.StopMoving(); skier.SetState(SkierState.Resting);
            Reason = "Repas indisponible ou trop long : nouvelle decision sans consommation fictive";
        }
        void OnDestroy() { if (restaurant != null && !ReferenceEquals(skier, null)) restaurant.CancelVisit(skier); }
    }
}
