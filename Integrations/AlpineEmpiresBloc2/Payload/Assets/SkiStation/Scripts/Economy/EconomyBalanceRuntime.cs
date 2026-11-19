using System;
using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// 3.53 - Balancing layer for the career economy.
    /// Keeps the economy readable and rewarding without replacing the authoritative ledger.
    /// </summary>
    public sealed class EconomyBalanceRuntime : MonoBehaviour
    {
        public static EconomyBalanceRuntime I { get; private set; }

        [Header("Career pacing")]
        [SerializeField] private float careerStartingCash = 650000f;
        [SerializeField] private float sandboxStartingCash = 5000000f;
        [SerializeField] private float safetyReserve = 90000f;
        [SerializeField] private float targetUpgradeDays = 3.5f;

        [Header("Daily operating costs")]
        [SerializeField] private float serviceBaseDailyCost = 180f;
        [SerializeField] private float staffDailyCost = 115f;

        private int lastSettledDay = -1;
        private float yesterdayRevenue;
        private float yesterdayExpenses;
        private float yesterdayProfit;
        private float dailyOperatingCost;
        private string lastSettlement = "Économie prête.";

        public float YesterdayRevenue => yesterdayRevenue;
        public float YesterdayExpenses => yesterdayExpenses;
        public float YesterdayProfit => yesterdayProfit;
        public float EstimatedDailyOperatingCost => EstimateDailyOperatingCost();
        public float SafetyReserve => safetyReserve;
        public string LastSettlement => lastSettlement;
        public float ObserveLiftDailyCost390(LiftRuntime lift)=>lift!=null&&lift.Definition!=null?Mathf.Max(0,lift.Definition.maintenancePerDay):0;
        public float ObserveServiceDailyCost390(StationServiceRuntime service)
        {
            if(service==null||service.definition==null)return 0;var d=service.definition;
            float factor=d.range==ServiceRange.Luxury?1.8f:d.range==ServiceRange.Premium?1.4f:d.range==ServiceRange.Economy ? .8f : 1f;
            return serviceBaseDailyCost*factor+Mathf.Max(0,d.staffAvailable)*staffDailyCost;
        }

        public float TicketPriceRatio
        {
            get
            {
                const float basePrice = 52f;
                return PricingRuntime.I != null ? PricingRuntime.I.GetPriceRatio("ski_pass_day", basePrice) : 1f;
            }
        }

        /// <summary>Demand reacts softly to the day-pass price. Reputation remains handled by the visitor-demand runtime.</summary>
        public float DemandMultiplier
        {
            get
            {
                float ratio = Mathf.Clamp(TicketPriceRatio, .55f, 1.8f);
                if (ratio <= 1f) return Mathf.Lerp(1.18f, 1f, Mathf.InverseLerp(.55f, 1f, ratio));
                return Mathf.Lerp(1f, .62f, Mathf.InverseLerp(1f, 1.8f, ratio));
            }
        }

        public float CashAfterReserve => Mathf.Max(0f, (StationRuntime.I != null ? StationRuntime.I.State.money : 0f) - safetyReserve);

        public float RecentDailyProfit
        {
            get
            {
                if (FinanceRuntime.I == null || StationRuntime.I == null) return 0f;
                int day = StationRuntime.I.State.day;
                float revenue = FinanceRuntime.I.GetRevenueForDay(day);
                float expenses = FinanceRuntime.I.GetExpensesForDay(day);
                if (Mathf.Abs(revenue) + Mathf.Abs(expenses) < .01f) return yesterdayProfit;
                return revenue - expenses;
            }
        }

        public string ProfitabilityLabel
        {
            get
            {
                float p = RecentDailyProfit;
                if (p > 5000f) return "Rentable";
                if (p >= 0f) return "Équilibre positif";
                if (p > -5000f) return "À consolider";
                return "Déficitaire";
            }
        }

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
            lastSettledDay = GameClock.I != null ? GameClock.I.CurrentDay : 1;
        }

        private void Update()
        {
            if (GameClock.I == null) return;
            int day = GameClock.I.CurrentDay;
            if (lastSettledDay < 0) lastSettledDay = day;
            if (day <= lastSettledDay) return;

            // Charge once per elapsed day. Normally only one day passes at a time, but this is robust to skips.
            while (lastSettledDay < day)
            {
                SettleDay(lastSettledDay);
                lastSettledDay++;
            }
        }

        public void StartNewSession353(bool sandbox)
        {
            if (StationRuntime.I != null)
                StationRuntime.I.State.money = sandbox ? sandboxStartingCash : careerStartingCash;
            if (EconomyModeRuntime.I != null)
                EconomyModeRuntime.I.SetFundingMode(sandbox ? FundingMode.Unlimited : FundingMode.Standard);
            lastSettledDay = GameClock.I != null ? GameClock.I.CurrentDay : 1;
            yesterdayRevenue = yesterdayExpenses = yesterdayProfit = 0f;
            lastSettlement = sandbox
                ? "Bac à sable : fonds illimités, économie suivie à titre indicatif."
                : "Carrière : capital initial " + careerStartingCash.ToString("N0") + " €.";
        }

        internal void SyncAlpineRestoredDay()
        {
            lastSettledDay = GameClock.I != null ? GameClock.I.CurrentDay : 1;
        }

        private void SettleDay(int day)
        {
            var finance = FinanceRuntime.I;
            if (finance == null) return;

            // Capture trading result before adding the new daily operating charges.
            yesterdayRevenue = finance.GetRevenueForDay(day);
            float before = finance.GetExpensesForDay(day);
            dailyOperatingCost = EstimateDailyOperatingCost();
            foreach (var service in StationServiceRuntime.All)
                if (service != null && service.UsesAlliedAccount)
                    service.PostAlliedReceipt(AlpineEmpires.AlpineActionKind.ServiceOperatingCost,
                        ObserveServiceDailyCost390(service), FinanceCategory.Maintenance, CustomerProfile.Tourist, day, 23.99f);

            bool unlimited = EconomyModeRuntime.I != null && EconomyModeRuntime.I.UnlimitedFunds;
            float charged = dailyOperatingCost;
            if (!unlimited && dailyOperatingCost > 0f && StationRuntime.I != null)
            {
                charged = Mathf.Min(dailyOperatingCost, Mathf.Max(0f, StationRuntime.I.State.money));
                StationRuntime.I.State.money -= charged;
                if (charged > 0f) finance.RecordExpenseForDay(charged, day, FinanceCategory.Maintenance,
                    "daily_operations_353", "Exploitation quotidienne de la station");
            }
            else if (unlimited && dailyOperatingCost > 0f)
            {
                finance.RecordExpenseForDay(dailyOperatingCost, day, FinanceCategory.Maintenance,
                    "daily_operations_353", "Coût indicatif d'exploitation quotidienne");
            }

            yesterdayExpenses = before + charged;
            yesterdayProfit = yesterdayRevenue - yesterdayExpenses;
            lastSettlement = "Jour " + day + " : recettes " + yesterdayRevenue.ToString("N0") + " € | dépenses " +
                yesterdayExpenses.ToString("N0") + " € | résultat " + yesterdayProfit.ToString("+N0;-N0;0") + " €.";
        }

        private float EstimateDailyOperatingCost()
        {
            float total = 0f;
            foreach (var lift in FindObjectsByType<LiftRuntime>(FindObjectsSortMode.None))
            {
                if (lift == null || lift.Definition == null) continue;
                total += ObserveLiftDailyCost390(lift);
            }
            foreach (var service in StationServiceRuntime.All)
            {
                if (service == null || service.definition == null || service.UsesAlliedAccount) continue;
                total += ObserveServiceDailyCost390(service);
            }
            return Mathf.Round(total);
        }

        public float EstimatePaybackDays(float investment)
        {
            if (investment <= 0f) return 0f;
            float profit = Mathf.Max(0f, RecentDailyProfit);
            return profit <= 1f ? float.PositiveInfinity : investment / profit;
        }

        public float UpgradeProgress01(float investment)
        {
            if (investment <= 0f) return 1f;
            return Mathf.Clamp01(CashAfterReserve / investment);
        }

        public string DescribeNextInvestment(float investment)
        {
            if (investment <= 0f) return "Aucun investissement requis.";
            float available = CashAfterReserve;
            if (available >= investment) return "Investissement finançable tout en gardant la réserve de sécurité.";
            float missing = investment - available;
            float profit = Mathf.Max(0f, RecentDailyProfit);
            if (profit > 1f)
            {
                float days = missing / profit;
                return "Encore " + missing.ToString("N0") + " € à générer (~" + Mathf.Max(1f, days).ToString("0.0") + " j au rythme récent).";
            }
            return "Encore " + missing.ToString("N0") + " € à générer ; améliorez fréquentation ou recettes.";
        }

        public bool IsUpgradePaceHealthy(float investment)
        {
            float profit = Mathf.Max(0f, RecentDailyProfit);
            if (investment <= 0f || CashAfterReserve >= investment) return true;
            return profit > 1f && (investment - CashAfterReserve) / profit <= targetUpgradeDays;
        }
    }
}
