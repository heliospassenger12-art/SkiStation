using System;
using UnityEngine;

namespace SkiStation
{
    [Serializable]
    public struct PricePoint
    {
        public string id;
        public FinanceCategory category;
        public float basePrice;
        public float currentPrice;
        [Range(0.1f, 5f)] public float elasticity;
        [Min(0f)] public float minimumPrice;
        [Min(0f)] public float maximumPrice;

        public PricePoint(string id, FinanceCategory category, float basePrice, float elasticity = 1f)
        {
            this.id = id; this.category = category;
            this.basePrice = Mathf.Max(0f, basePrice);
            this.currentPrice = this.basePrice;
            this.elasticity = Mathf.Clamp(elasticity, .1f, 5f);
            this.minimumPrice = 0f;
            this.maximumPrice = Mathf.Max(this.basePrice * 10f, 1f);
        }
    }
}
