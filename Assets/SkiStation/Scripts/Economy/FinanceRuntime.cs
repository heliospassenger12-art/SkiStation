using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// Single financial ledger for the management simulation.
    /// It records money movements but deliberately does not own the station balance:
    /// StationRuntime remains the compatibility facade for the current prototype.
    /// </summary>
    public class FinanceRuntime : MonoBehaviour
    {
        public static FinanceRuntime I { get; private set; }

        [SerializeField] private int historyLimit = 10000;
        private readonly List<FinanceTransaction> transactions = new List<FinanceTransaction>();
        private readonly Dictionary<FinanceCategory, float> revenueByCategory = new Dictionary<FinanceCategory, float>();
        private readonly Dictionary<FinanceCategory, float> expenseByCategory = new Dictionary<FinanceCategory, float>();

        public IReadOnlyList<FinanceTransaction> Transactions => transactions;
        public float Balance => StationRuntime.I != null ? StationRuntime.I.State.money : 0f;
        public float TotalRevenue { get; private set; }
        public float TotalExpenses { get; private set; }
        public float NetCashFlow => TotalRevenue - TotalExpenses;

        public event Action<FinanceTransaction> TransactionRecorded;
        public event Action Changed;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool CanAfford(float amount)
        {
            return amount >= 0f && Balance >= amount;
        }

        public void RecordRevenue(float amount, FinanceCategory category = FinanceCategory.Other,
            CustomerProfile customer = CustomerProfile.Tourist, string sourceId = null, string note = null)
        {
            if (amount <= 0f) return;
            Record(new FinanceTransaction(FinanceTransactionType.Revenue, category, customer,
                amount, CurrentDay(), CurrentHour(), sourceId, note));
        }

        public void RecordExpense(float amount, FinanceCategory category = FinanceCategory.Other,
            string sourceId = null, string note = null)
        {
            if (amount <= 0f) return;
            Record(new FinanceTransaction(FinanceTransactionType.Expense, category,
                CustomerProfile.Tourist, amount, CurrentDay(), CurrentHour(), sourceId, note));
        }

        public float GetRevenue(FinanceCategory category)
        {
            return revenueByCategory.TryGetValue(category, out var value) ? value : 0f;
        }

        public float GetExpenses(FinanceCategory category)
        {
            return expenseByCategory.TryGetValue(category, out var value) ? value : 0f;
        }

        public FinanceTransaction[] GetRecent(int count = 50)
        {
            count = Mathf.Clamp(count, 0, transactions.Count);
            var result = new FinanceTransaction[count];
            for (int i = 0; i < count; i++)
                result[i] = transactions[transactions.Count - 1 - i];
            return result;
        }

        private void Record(FinanceTransaction transaction)
        {
            transactions.Add(transaction);
            if (transaction.type == FinanceTransactionType.Revenue)
            {
                TotalRevenue += transaction.amount;
                Add(revenueByCategory, transaction.category, transaction.amount);
            }
            else
            {
                TotalExpenses += transaction.amount;
                Add(expenseByCategory, transaction.category, transaction.amount);
            }

            StationStatisticsRuntime.I?.RecordFinanceTransaction(transaction);
            Trim();
            TransactionRecorded?.Invoke(transaction);
            Changed?.Invoke();
        }

        private static void Add(Dictionary<FinanceCategory, float> map, FinanceCategory category, float amount)
        {
            if (!map.ContainsKey(category)) map[category] = 0f;
            map[category] += amount;
        }

        private int CurrentDay() => StationRuntime.I != null ? StationRuntime.I.State.day : 1;
        private float CurrentHour() => GameClock.I != null ? GameClock.I.CurrentHour : 0f;

        private void Trim()
        {
            while (transactions.Count > Mathf.Max(100, historyLimit)) transactions.RemoveAt(0);
        }
    }
}
