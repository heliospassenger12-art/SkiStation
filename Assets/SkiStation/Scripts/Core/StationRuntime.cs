using System;
using UnityEngine;

namespace SkiStation
{
    [Serializable]
    public class StationState
    {
        public int day = 1;
        public int visitorsToday;
        public float money = 5000000f;
    }

    public class StationRuntime : MonoBehaviour
    {
        public static StationRuntime I { get; private set; }
        public static StationRuntime Instance => I;
        public StationState State { get; private set; } = new StationState();

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool Spend(float amount, FinanceCategory category = FinanceCategory.Other, string sourceId = null, string note = null)
        {
            if (amount < 0f) return false;
            bool unlimited = EconomyModeRuntime.I != null && EconomyModeRuntime.I.UnlimitedFunds;
            if (!unlimited && State.money < amount) return false;
            if (!unlimited) State.money -= amount;
            FinanceRuntime.I?.RecordExpense(amount, category, sourceId, note);
            return true;
        }

        public void Earn(float amount, FinanceCategory category = FinanceCategory.Other,
            CustomerProfile customer = CustomerProfile.Tourist, string sourceId = null, string note = null)
        {
            amount = Mathf.Max(0f, amount);
            State.money += amount;
            if (amount > 0f) FinanceRuntime.I?.RecordRevenue(amount, category, customer, sourceId, note);
        }

        private void Update()
        {
            if (GameClock.I != null) State.day = GameClock.I.CurrentDay;
        }

        public void AddVisitor(CustomerProfile profile = CustomerProfile.Tourist, float satisfaction = 75f, float spend = 0f)
        {
            State.visitorsToday++;
            StationStatisticsRuntime.I?.RecordVisitor(profile, satisfaction, spend);
        }
    }
}
