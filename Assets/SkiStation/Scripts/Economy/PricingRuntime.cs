using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>Central price authority. Systems may use an override while retaining their own definitions.</summary>
    public sealed class PricingRuntime : MonoBehaviour
    {
        public static PricingRuntime I { get; private set; }
        private readonly Dictionary<string, PricePoint> prices = new(StringComparer.OrdinalIgnoreCase);
        public event Action<PricePoint> PriceChanged;
        public IReadOnlyDictionary<string, PricePoint> Prices => prices;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
        }

        public float GetPrice(string id, FinanceCategory category, float basePrice, CustomerProfile profile = CustomerProfile.Tourist)
        {
            if (string.IsNullOrWhiteSpace(id)) return Mathf.Max(0f, basePrice);
            if (!prices.TryGetValue(id, out var point))
            {
                point = new PricePoint(id, category, basePrice);
                prices[id] = point;
            }
            return Mathf.Clamp(point.currentPrice, point.minimumPrice, Mathf.Max(point.minimumPrice, point.maximumPrice));
        }

        public bool SetPrice(string id, FinanceCategory category, float basePrice, float price, float elasticity = 1f)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            var point = prices.TryGetValue(id, out var existing)
                ? existing
                : new PricePoint(id, category, basePrice, elasticity);
            point.category = category;
            point.basePrice = Mathf.Max(0f, basePrice);
            point.elasticity = Mathf.Clamp(elasticity, .1f, 5f);
            point.currentPrice = Mathf.Clamp(price, point.minimumPrice, Mathf.Max(point.minimumPrice, point.maximumPrice));
            prices[id] = point;
            PriceChanged?.Invoke(point);
            return true;
        }

        public void ResetPrice(string id)
        {
            if (!prices.TryGetValue(id, out var point)) return;
            point.currentPrice = point.basePrice;
            prices[id] = point;
            PriceChanged?.Invoke(point);
        }

        public float GetPriceRatio(string id, float basePrice)
        {
            if (basePrice <= 0f) return 1f;
            return GetPrice(id, FinanceCategory.Other, basePrice) / basePrice;
        }
    }
}
