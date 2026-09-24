using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum CustomerProfile { Beginner, Intermediate, Advanced, Expert, Family, Youth, Athlete, Premium, Tourist }
    public enum RevenueCategory { SkiPass, Rentals, Shops, Food, Hotel, Leisure, Lessons, Events, Other }

    [Serializable] public struct RevenueRecord { public RevenueCategory category; public CustomerProfile customer; public float amount; public int day; public RevenueRecord(RevenueCategory c, CustomerProfile p, float a, int d) { category=c; customer=p; amount=a; day=d; } }
    [Serializable] public struct VisitorRecord { public CustomerProfile profile; public float satisfaction; public float spend; public int day; public VisitorRecord(CustomerProfile p,float s,float sp,int d){profile=p;satisfaction=s;spend=sp;day=d;} }

    public class StationStatisticsRuntime : MonoBehaviour
    {
        public static StationStatisticsRuntime I { get; private set; }
        [SerializeField] private int historyLimit = 5000;
        private readonly List<RevenueRecord> revenue = new List<RevenueRecord>();
        private readonly List<VisitorRecord> visitors = new List<VisitorRecord>();
        private readonly Dictionary<string,int> shopSales = new Dictionary<string,int>();
        private readonly Dictionary<string,float> shopRevenue = new Dictionary<string,float>();
        private readonly Dictionary<CustomerProfile,int> visitorMix = new Dictionary<CustomerProfile,int>();
        private readonly Dictionary<CustomerProfile,float> satisfactionSum = new Dictionary<CustomerProfile,float>();
        private readonly Dictionary<CustomerProfile,float> spendSum = new Dictionary<CustomerProfile,float>();

        public IReadOnlyList<RevenueRecord> RevenueHistory => revenue;
        public IReadOnlyList<VisitorRecord> VisitorHistory => visitors;
        public float TotalRevenue { get; private set; }
        public float TotalExpenses { get; private set; }
        public int TotalVisitors { get; private set; }
        public float AverageSatisfaction => TotalVisitors == 0 ? 0f : AverageSatisfactionInternal();
        public event Action Changed;

        public void RecordFinanceTransaction(FinanceTransaction transaction)
        {
            if (transaction.type == FinanceTransactionType.Revenue)
            {
                var category = ToRevenueCategory(transaction.category);
                RecordRevenueInternal(category, transaction.customer, transaction.amount, transaction.sourceId, transaction.day);
            }
            else
            {
                RecordExpenseInternal(transaction.amount);
            }
        }

        private void Awake(){ if(I!=null && I!=this){Destroy(gameObject);return;} I=this; DontDestroyOnLoad(gameObject); }

        public void RecordRevenue(RevenueCategory category, CustomerProfile profile, float amount, string shopId = null)
        {
            if (amount <= 0f) return;
            RecordRevenueInternal(category, profile, amount, shopId, StationRuntime.I != null ? StationRuntime.I.State.day : 1);
            Changed?.Invoke();
        }

        public void RecordExpense(float amount)
        {
            RecordExpenseInternal(amount);
            Changed?.Invoke();
        }

        private void RecordRevenueInternal(RevenueCategory category, CustomerProfile profile, float amount, string shopId, int day)
        {
            if (amount <= 0f) return;
            revenue.Add(new RevenueRecord(category, profile, amount, day));
            TotalRevenue += amount;
            if (shopId != null)
            {
                if (!shopSales.ContainsKey(shopId)) shopSales[shopId] = 0;
                if (!shopRevenue.ContainsKey(shopId)) shopRevenue[shopId] = 0f;
                shopSales[shopId]++;
                shopRevenue[shopId] += amount;
            }
            Trim();
        }

        private void RecordExpenseInternal(float amount)
        {
            if (amount <= 0f) return;
            TotalExpenses += amount;
        }

        private static RevenueCategory ToRevenueCategory(FinanceCategory category)
        {
            switch (category)
            {
                case FinanceCategory.SkiPass: return RevenueCategory.SkiPass;
                case FinanceCategory.Rentals: return RevenueCategory.Rentals;
                case FinanceCategory.Shops: return RevenueCategory.Shops;
                case FinanceCategory.Food: return RevenueCategory.Food;
                case FinanceCategory.Hotel: return RevenueCategory.Hotel;
                case FinanceCategory.Leisure: return RevenueCategory.Leisure;
                case FinanceCategory.Lessons: return RevenueCategory.Lessons;
                case FinanceCategory.Events: return RevenueCategory.Events;
                default: return RevenueCategory.Other;
            }
        }

        public void RecordVisitor(CustomerProfile profile,float satisfaction,float spend)
        {
            int day=StationRuntime.I!=null?StationRuntime.I.State.day:1; visitors.Add(new VisitorRecord(profile,Mathf.Clamp(satisfaction,0,100),Mathf.Max(0,spend),day)); TotalVisitors++;
            if(!visitorMix.ContainsKey(profile)) visitorMix[profile]=0; if(!satisfactionSum.ContainsKey(profile)) satisfactionSum[profile]=0; if(!spendSum.ContainsKey(profile)) spendSum[profile]=0;
            visitorMix[profile]++; satisfactionSum[profile]+=Mathf.Clamp(satisfaction,0,100); spendSum[profile]+=Mathf.Max(0,spend); Trim(); Changed?.Invoke();
        }
        public int GetVisitors(CustomerProfile p)=>visitorMix.TryGetValue(p,out var n)?n:0;
        public float GetAverageSatisfaction(CustomerProfile p)=>visitorMix.TryGetValue(p,out var n)&&n>0?satisfactionSum[p]/n:0f;
        public float GetAverageSpend(CustomerProfile p)=>visitorMix.TryGetValue(p,out var n)&&n>0?spendSum[p]/n:0f;
        public int GetShopSales(string id)=>id!=null&&shopSales.TryGetValue(id,out var n)?n:0;
        public float GetShopRevenue(string id)=>id!=null&&shopRevenue.TryGetValue(id,out var v)?v:0f;
        private float AverageSatisfactionInternal(){float s=0; foreach(var v in visitors)s+=v.satisfaction; return visitors.Count==0?0:s/visitors.Count;}
        private void Trim(){while(revenue.Count>historyLimit)revenue.RemoveAt(0);while(visitors.Count>historyLimit)visitors.RemoveAt(0);}
    }
}
