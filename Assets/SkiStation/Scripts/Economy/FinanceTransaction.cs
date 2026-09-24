using System;

namespace SkiStation
{
    public enum FinanceTransactionType { Revenue, Expense }

    public enum FinanceCategory
    {
        SkiPass, Rentals, Shops, Food, Hotel, Leisure, Lessons, Events,
        Construction, Staff, Maintenance, Marketing, Transport, Research,
        Taxes, Other
    }

    [Serializable]
    public struct FinanceTransaction
    {
        public FinanceTransactionType type;
        public FinanceCategory category;
        public CustomerProfile customer;
        public float amount;
        public int day;
        public float hour;
        public string sourceId;
        public string note;

        public FinanceTransaction(FinanceTransactionType type, FinanceCategory category,
            CustomerProfile customer, float amount, int day, float hour,
            string sourceId, string note)
        {
            this.type = type;
            this.category = category;
            this.customer = customer;
            this.amount = amount;
            this.day = day;
            this.hour = hour;
            this.sourceId = sourceId;
            this.note = note;
        }
    }
}
