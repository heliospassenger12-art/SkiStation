using System;
using System.Collections.Generic;
using SkiStation.AlpineEmpires;
using UnityEngine;

namespace SkiStation
{
    public sealed partial class StationServiceRuntime : MonoBehaviour
    {
        public static readonly List<StationServiceRuntime> All = new();
        public StationServiceDefinition definition;
        public string AlpineCompanyId { get; internal set; }
        public string AlpineTerritoryId { get; internal set; }
        internal bool UsesAlliedAccount => !string.IsNullOrEmpty(AlpineCompanyId) && AlpineCompanyId != "company-1";

        internal bool PostAlliedReceipt(AlpineActionKind kind, float amount, FinanceCategory category,
            CustomerProfile profile, int day, float hour)
        {
            if (!UsesAlliedAccount || !AlpineSession.Finite(amount) || amount < 0f || amount > 20000000f)
                return false;
            var world = SkiStationStartMenu249.Owner263?.ActiveAlpineWorld263;
            if (world == null || !world.TryGetManager(AlpineCompanyId, out var company)) return false;
            decimal value = (decimal)amount;
            bool operating = kind == AlpineActionKind.ServiceOperatingCost;
            if (operating) value = Math.Min(value, company.ExactBalance);
            int cents = (int)Math.Round(value * 100m, MidpointRounding.AwayFromZero);
            if (cents == 0) return true;

            var serviceId = GetId();
            var command = new AlpineCommand
            {
                kind = kind, companyId = company.Id, managerId = company.ManagerId,
                operationId = operating ? $"service.opex.{serviceId}.{day}" : Guid.NewGuid().ToString("N"),
                subjectId = serviceId, sectorId = AlpineTerritoryId, cents = cents,
                day = day, hour = hour, category = category, customer = profile
            };
            var result = world.PostServiceReceipt(command);
            if (result == AlpineActionResult.Applied || result == AlpineActionResult.AlreadyApplied) return true;
            Arcade.DemoDiagnostics263.Record("alpine.finance.refused", serviceId + " / " + result);
            return false;
        }
        [SerializeField] private int activeCustomers;
        [SerializeField] private List<SkierRuntime> queue = new();
        private readonly Dictionary<SkierRuntime, float> startHour = new();
        private readonly Dictionary<SkierRuntime, float> queueHour = new();
        private StationServiceDailyStats stats;
        private float lastGameHour = -1f;
        private float lastElapsedMinutes = -1f;
        private int lastDay = -1;

        public int ActiveCustomers => activeCustomers;
        public int QueueCount => queue.Count;
        public int EffectiveCapacity => definition == null ? 0 : (definition.staffAvailable >= definition.staffRequired ? definition.capacity : 0);
        public int AvailableCapacity => Mathf.Max(0, EffectiveCapacity - activeCustomers);
        public float Occupancy01 => definition == null ? 0f : Mathf.Clamp01(activeCustomers / (float)Mathf.Max(1, definition.capacity));
        public float QueuePressure01 => definition == null ? 0f : Mathf.Clamp01(queue.Count / (float)Mathf.Max(1, definition.maxQueue));
        public StationServiceDailyStats Stats => stats;
        public event Action Changed;

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() { All.Remove(this); }
        private void Awake()
        {
            ManagementElementIdentity261.Ensure(this, "building");
            lastGameHour = GameClock.I != null ? GameClock.I.CurrentHour : -1f;
            lastElapsedMinutes = GameClock.I != null ? GameClock.I.ElapsedGameMinutes : -1f;
            lastDay = GameClock.I != null ? GameClock.I.CurrentDay : -1;
        }
        private void Update() {
            if (SkiStation.Arcade.ArcadeSessionTime2611.BlocksManagement(this)) return; ProcessClock(); }

        public bool IsOpen()
        {
            if (definition == null || GameClock.I == null) return false;
            var structureStatus = GetComponent<StationStructureStatus249>();
            if (structureStatus != null && !structureStatus.CanWelcomeVisitors) return false;
            if (definition.staffAvailable < definition.staffRequired) return false;
            float h = GameClock.I.CurrentHour;
            if (GameClock.I.IsNight) return definition.openAtNight;
            if (definition.openingHour <= definition.closingHour) return h >= definition.openingHour && h < definition.closingHour;
            return h >= definition.openingHour || h < definition.closingHour;
        }

        public bool CanRequest(SkierRuntime skier)
        {
            if (skier == null || definition == null || !IsOpen()) return false;
            if (!definition.unlimitedStock && definition.stock <= 0) return false;
            if (Contains(skier)) return false;
            return activeCustomers < EffectiveCapacity || (activeCustomers + queue.Count) < EffectiveCapacity + definition.maxQueue;
        }

        public bool RequestService(SkierRuntime skier)
        {
            if (skier == null || definition == null) return false;
            stats.totalRequests++;
            if (!CanRequest(skier)) { stats.refused++; Changed?.Invoke(); return false; }
            if (activeCustomers < EffectiveCapacity)
            {
                if (!BeginService(skier)) { stats.refused++; Changed?.Invoke(); return false; }
            }
            else
            {
                if (queue.Count >= definition.maxQueue) { stats.refused++; Changed?.Invoke(); return false; }
                queue.Add(skier);
                queueHour[skier] = GameClock.I != null ? GameClock.I.CurrentHour : 0f;
                stats.queuePeak = Mathf.Max(stats.queuePeak, queue.Count);
                skier.SetState(SkierState.Resting);
                Changed?.Invoke();
            }
            return true;
        }

        public bool Contains(SkierRuntime skier) => skier != null && (queue.Contains(skier) || startHour.ContainsKey(skier));
        public bool IsBeingServed(SkierRuntime skier) => skier != null && startHour.ContainsKey(skier);
        public bool IsSuspended348
        {
            get
            {
                var status = GetComponent<StationStructureStatus249>();
                return status != null && status.IsInMaintenance;
            }
        }

        /// <summary>
        /// 3.47: atomic family handoff. Every child gets a real service place and one shared
        /// group lesson gets a real instructor, or nothing is booked and no revenue can occur.
        /// </summary>
        public bool TryRequestFamilyLesson347(IReadOnlyList<SkierRuntime> children, out string reason)
        {
            reason = "Ecole indisponible";
            if (children == null || children.Count == 0 || definition == null ||
                definition.type != StationServiceType.SkiSchool || !IsOpen()) return false;
            if (!definition.unlimitedStock && definition.stock < children.Count)
            { reason = "Places vendables insuffisantes"; return false; }
            if (queue.Count > 0 || AvailableCapacity < children.Count)
            { reason = "Capacite insuffisante pour tous les enfants"; return false; }
            var school = GetComponent<SkiSchoolRuntime>();
            if (school == null || !school.CanAcceptFamilyGroup347(children, out reason)) return false;
            for (int i = 0; i < children.Count; i++)
                if (children[i] == null || Contains(children[i]) || !CanRequest(children[i]))
                { reason = "Un enfant ne peut pas etre admis"; return false; }
            if (!school.TryStartFamilyGroup347(children, out reason)) return false;
            for (int i = 0; i < children.Count; i++)
            {
                stats.totalRequests++;
                if (!BeginService(children[i], false))
                {
                    // Prechecks above make this unreachable; retain atomic rollback if the building changes mid-call.
                    for (int j = 0; j < i; j++) CancelVisit(children[j]);
                    school.CancelStudents347(children);
                    reason = "Prise en charge interrompue";
                    return false;
                }
            }
            reason = "Moniteur et places confirmes";
            return true;
        }

        public void CancelAll349()
        {
            var clients = new List<SkierRuntime>(startHour.Keys);
            foreach (var skier in queue) if (!clients.Contains(skier)) clients.Add(skier);
            GetComponent<SkiSchoolRuntime>()?.CancelStudents347(clients);
            queue.Clear(); queueHour.Clear(); startHour.Clear(); activeCustomers = 0;
            Changed?.Invoke();
        }

        public void CancelVisit(SkierRuntime skier)
        {
            queue.Remove(skier); queueHour.Remove(skier);
            if (startHour.Remove(skier)) activeCustomers = Mathf.Max(0, activeCustomers - 1);
            Changed?.Invoke();
        }

        public bool TryComplete(SkierRuntime skier)
        {
            if (skier == null || definition == null || IsSuspended348 || !startHour.ContainsKey(skier)) return false;
            float serviceMinutes = definition.serviceMinutes;
            float elapsed = ElapsedGameMinutes(startHour[skier], GameClock.I != null ? GameClock.I.CurrentHour : 0f);
            if (elapsed < serviceMinutes) return false;
            CompleteService(skier);
            return true;
        }

        private bool BeginService(SkierRuntime skier, bool startSkiLesson = true)
        {
            activeCustomers++;
            startHour[skier] = GameClock.I != null ? GameClock.I.CurrentHour : 0f;
            skier.SetState(SkierState.Resting);
            if (startSkiLesson && definition.type == StationServiceType.SkiSchool)
            {
                var school = GetComponent<SkiSchoolRuntime>();
                if (school == null)
                {
                    startHour.Remove(skier);
                    activeCustomers = Mathf.Max(0, activeCustomers - 1);
                    return false;
                }
                else
                {
                    SkiLessonFormat format = SkiLessonFormat.Individual;
                    if (definition.lessonFormat == SkiSchoolServiceFormat.Group) format = SkiLessonFormat.Group;
                    else if (definition.lessonFormat == SkiSchoolServiceFormat.Flexible)
                    {
                        var group = VisitorGroupRuntime.I != null ? VisitorGroupRuntime.I.Get(skier) : default;
                        format = group.memberCount >= 2 && UnityEngine.Random.value < .72f ? SkiLessonFormat.Group : SkiLessonFormat.Individual;
                    }
                    if (!school.TryStartLesson(skier, format, GetOrCreateProgress(skier).CurrentLevel))
                    {
                        startHour.Remove(skier);
                        activeCustomers = Mathf.Max(0, activeCustomers - 1);
                        return false;
                    }
                }
            }
            Changed?.Invoke();
            return true;
        }

        private void CompleteService(SkierRuntime skier)
        {
            float waitedBeforeService = WaitMinutes(skier);
            startHour.Remove(skier);
            queueHour.Remove(skier);
            activeCustomers = Mathf.Max(0, activeCustomers - 1);
            if (!definition.unlimitedStock) definition.stock = Mathf.Max(0, definition.stock - 1);

            CustomerProfile profile = skier.PopulationProfile != null ? skier.PopulationProfile.ToCustomerProfile() : CustomerProfile.Tourist;
            float price = PricingRuntime.I != null ? PricingRuntime.I.GetPrice(GetId(), ToFinanceCategory(), definition.basePrice, profile) : definition.basePrice;
            float waitingPenalty = Mathf.Clamp(waitedBeforeService * .65f, 0f, 40f);
            float quality = Mathf.Clamp(definition.quality - waitingPenalty, 0f, 100f);
            // 3.33: satisfaction personnalisee apres consommation reelle.
            float experience01 = VisitorServicePreference333.CompletedExperience01(this, skier, price, waitedBeforeService);
            float satisfactionDelta = VisitorServicePreference333.SatisfactionDelta(this, skier, price, waitedBeforeService);
            // 3.41: une consommation terminee alimente la reputation propre de cet etablissement.
            VisitorEstablishmentReputation341.Ensure().Record(this, experience01);
            // 3.42: une consommation reelle devient une habitude potentielle; jamais une simple intention.
            VisitorServiceMemory342.RegisterCompleted(skier, this);
            skier.ChangeSatisfaction(satisfactionDelta);
            if (VisitorFeedbackSatisfactionRuntime.I != null)
            {
                VisitorFeedbackSatisfactionRuntime.I.Record(skier, VisitorExperienceArea.Services, experience01);
                VisitorFeedbackSatisfactionRuntime.I.Record(skier, VisitorExperienceArea.Waiting, VisitorFeedbackSatisfactionRuntime.WaitQuality392(waitedBeforeService));
            }

            if (VisitorServiceConsumptionRuntime.I != null)
                VisitorServiceConsumptionRuntime.I.RecordServiceTransaction(skier, ToVisitorServiceType(), GetId(), definition.serviceName, price, quality, this);
            else if (UsesAlliedAccount)
                PostAlliedReceipt(AlpineActionKind.ServiceRevenue, price, ToFinanceCategory(), profile,
                    GameClock.I != null ? GameClock.I.CurrentDay : 1, GameClock.I != null ? GameClock.I.CurrentHour : 0f);
            else StationRuntime.I?.Earn(price, ToFinanceCategory(), profile, GetId(), definition.serviceName);
            if (VisitorDayPlanRuntime.I != null) VisitorDayPlanRuntime.I.RegisterServiceCompleted(skier, definition.type, definition.serviceMinutes);

            skier.GetComponent<VisitorDayDecision329>()?.ServiceCompleted(this);
            stats.served++; stats.revenue += price; stats.satisfaction += quality; stats.satisfactionSamples++;
            int pi = Mathf.Clamp((int)profile, 0, 8);
            if (stats.servedByProfile == null || stats.servedByProfile.Length != 9) stats.servedByProfile = new int[9];
            if (stats.revenueByProfile == null || stats.revenueByProfile.Length != 9) stats.revenueByProfile = new float[9];
            if (stats.satisfactionByProfile == null || stats.satisfactionByProfile.Length != 9) stats.satisfactionByProfile = new float[9];
            stats.servedByProfile[pi]++; stats.revenueByProfile[pi] += price; stats.satisfactionByProfile[pi] += quality;
            AdmitWaiting();
            Changed?.Invoke();
        }

        private void AdmitWaiting()
        {
            if (!IsOpen()) return;
            while (queue.Count > 0 && activeCustomers < EffectiveCapacity)
            {
                var next = queue[0]; queue.RemoveAt(0);
                if (next == null || next.State == SkierState.Leaving) continue;
                stats.waitingMinutes += Mathf.Max(0f, WaitMinutes(next));
                if (!BeginService(next)) { stats.refused++; queueHour.Remove(next); }
            }
        }

        private void ProcessClock()
        {
            if (definition == null || GameClock.I == null) return;
            int day = GameClock.I.CurrentDay;
            float hour = GameClock.I.CurrentHour;
            float elapsed = GameClock.I.ElapsedGameMinutes;
            float deltaMinutes = lastElapsedMinutes < 0f ? 0f : Mathf.Max(0f, elapsed - lastElapsedMinutes);
            if (day != lastDay) { stats = new StationServiceDailyStats(day); lastDay = day; }
            if (IsSuspended348)
            {
                ShiftTimers348(deltaMinutes);
                GetComponent<SkiSchoolRuntime>()?.PauseActiveSessions348(deltaMinutes);
                lastElapsedMinutes = elapsed;
                lastGameHour = hour;
                return;
            }
            lastElapsedMinutes = elapsed;
            lastGameHour = hour;

            var active = new List<SkierRuntime>(startHour.Keys);
            for (int i = 0; i < active.Count; i++) if (active[i] != null) TryComplete(active[i]);

            for (int i = queue.Count - 1; i >= 0; i--)
            {
                if (queue[i] == null || queue[i].State == SkierState.Leaving) { if (queue[i] != null) queueHour.Remove(queue[i]); queue.RemoveAt(i); }
            }
            AdmitWaiting();
        }

        private void ShiftTimers348(float gameMinutes)
        {
            if (gameMinutes <= 0f) return;
            float hours = gameMinutes / 60f;
            var active = new List<SkierRuntime>(startHour.Keys);
            for (int i = 0; i < active.Count; i++) startHour[active[i]] = WrapHour348(startHour[active[i]] + hours);
            var waiting = new List<SkierRuntime>(queueHour.Keys);
            for (int i = 0; i < waiting.Count; i++) queueHour[waiting[i]] = WrapHour348(queueHour[waiting[i]] + hours);
        }

        private static float WrapHour348(float hour)
        { while (hour >= 24f) hour -= 24f; while (hour < 0f) hour += 24f; return hour; }

        private float WaitMinutes(SkierRuntime skier)
        {
            if (skier == null || GameClock.I == null || !queueHour.TryGetValue(skier, out var from)) return 0f;
            return ElapsedGameMinutes(from, GameClock.I.CurrentHour);
        }

        private float ElapsedGameMinutes(float fromHour, float toHour)
        {
            float d = toHour - fromHour; if (d < 0f) d += 24f; return d * 60f;
        }

        private string GetId() => definition != null && !string.IsNullOrWhiteSpace(definition.serviceId) ? definition.serviceId : gameObject.GetInstanceID().ToString();

        private FinanceCategory ToFinanceCategory()
        {
            switch (definition.type)
            {
                case StationServiceType.Shop: return FinanceCategory.Shops;
                case StationServiceType.Restaurant:
                case StationServiceType.Bar: return FinanceCategory.Food;
                case StationServiceType.EquipmentRental: return FinanceCategory.Rentals;
                case StationServiceType.SkiSchool: return FinanceCategory.Lessons;
                case StationServiceType.Activity: return FinanceCategory.Leisure;
                case StationServiceType.Accommodation: return FinanceCategory.Hotel;
                default: return FinanceCategory.Other;
            }
        }

        private VisitorServiceType ToVisitorServiceType()
        {
            switch (definition.type)
            {
                case StationServiceType.Shop: return VisitorServiceType.Shop;
                case StationServiceType.Restaurant: return VisitorServiceType.Restaurant;
                case StationServiceType.Bar: return VisitorServiceType.Bar;
                case StationServiceType.EquipmentRental: return VisitorServiceType.EquipmentRental;
                case StationServiceType.SkiSchool: return VisitorServiceType.SkiSchool;
                case StationServiceType.Activity: return VisitorServiceType.MountainActivity;
                case StationServiceType.Accommodation: return VisitorServiceType.Hotel;
                default: return VisitorServiceType.Leisure;
            }
        }
        private static SkiLessonProgressRuntime GetOrCreateProgress(SkierRuntime skier)
        {
            if (skier == null) return null;
            return skier.GetComponent<SkiLessonProgressRuntime>() ?? skier.gameObject.AddComponent<SkiLessonProgressRuntime>();
        }

    }
}
