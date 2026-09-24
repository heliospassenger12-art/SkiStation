using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    // Management-only data catalog. This does not instantiate buildings or alter arcade gameplay.
    public static class StationStructureCatalog249
    {
        public enum Category { Accommodation, FoodAndDrink, Retail, SkiServices, Wellness, Activities, Transport, Operations }
        public enum Kind
        {
            Chalet, Hotel, ApartmentResidence, MountainRefuge,
            Restaurant, MountainRestaurant, Cafe, Bar,
            WinterSportsShop, Grocery, SouvenirShop,
            SkiSchool, ChildrensSkiGarden, FirstAid,
            Spa, SwimmingPool,
            SleddingArea, CrossCountryCenter, SnowshoeCenter,
            Parking, ShuttleStop,
            GroomingGarage, LiftMaintenance, SnowmakingStation, ApartmentBuilding
        }
        public sealed class Definition
        {
            public readonly Kind Type;
            public readonly Category Group;
            public readonly string DisplayName;
            public readonly int IndicativeCost;
            public readonly int Capacity;
            public readonly Vector2 Footprint;
            public readonly bool RequiresPedestrianAccess;
            public readonly bool RequiresRoadAccess;
            public readonly bool HasVisitorService;
            public Definition(Kind type, Category group, string name, int cost, int capacity,
                float width, float depth, bool pedestrian, bool road, bool service)
            {
                Type = type; Group = group; DisplayName = name; IndicativeCost = cost;
                Capacity = capacity; Footprint = new Vector2(width, depth);
                RequiresPedestrianAccess = pedestrian; RequiresRoadAccess = road; HasVisitorService = service;
            }
        }
        private static readonly Definition[] entries =
        {
            new Definition(Kind.Chalet, Category.Accommodation, "Chalet", 85000, 8, 15, 12, true, true, true),
            new Definition(Kind.Hotel, Category.Accommodation, "Hotel", 450000, 60, 28, 22, true, true, true),
            new Definition(Kind.ApartmentResidence, Category.Accommodation, "Residence de tourisme", 350000, 48, 28, 22, true, true, true),
            new Definition(Kind.ApartmentBuilding, Category.Accommodation, "Immeuble d appartements", 720000, 96, 30, 25, true, true, true),
            new Definition(Kind.MountainRefuge, Category.Accommodation, "Refuge", 150000, 20, 18, 14, true, false, true),
            new Definition(Kind.Restaurant, Category.FoodAndDrink, "Restaurant", 170000, 75, 22, 18, true, true, true),
            new Definition(Kind.MountainRestaurant, Category.FoodAndDrink, "Restaurant d'altitude", 220000, 90, 25, 20, true, false, true),
            new Definition(Kind.Cafe, Category.FoodAndDrink, "Cafe", 90000, 35, 16, 13, true, true, true),
            new Definition(Kind.Bar, Category.FoodAndDrink, "Bar", 110000, 45, 18, 14, true, true, true),
            new Definition(Kind.WinterSportsShop, Category.Retail, "Magasin de sports d'hiver", 120000, 24, 20, 16, true, true, true),
            new Definition(Kind.Grocery, Category.Retail, "Epicerie", 95000, 28, 17, 14, true, true, true),
            new Definition(Kind.SouvenirShop, Category.Retail, "Boutique souvenirs", 80000, 20, 15, 12, true, true, true),
            new Definition(Kind.SkiSchool, Category.SkiServices, "Ecole de ski", 140000, 50, 20, 16, true, true, true),
            new Definition(Kind.ChildrensSkiGarden, Category.SkiServices, "Jardin des Piou-Piou", 110000, 30, 26, 22, true, true, true),
            new Definition(Kind.FirstAid, Category.SkiServices, "Poste de secours", 160000, 12, 18, 15, true, true, true),
            new Definition(Kind.Spa, Category.Wellness, "Spa", 240000, 35, 24, 19, true, true, true),
            new Definition(Kind.SwimmingPool, Category.Wellness, "Piscine", 550000, 90, 36, 28, true, true, true),
            new Definition(Kind.SleddingArea, Category.Activities, "Espace luge", 65000, 60, 28, 22, true, false, true),
            new Definition(Kind.CrossCountryCenter, Category.Activities, "Centre de ski de fond", 150000, 55, 22, 18, true, true, true),
            new Definition(Kind.SnowshoeCenter, Category.Activities, "Centre raquettes", 80000, 30, 18, 14, true, true, true),
            new Definition(Kind.Parking, Category.Transport, "Parking", 180000, 120, 40, 32, true, true, true),
            new Definition(Kind.ShuttleStop, Category.Transport, "Arret navette", 30000, 35, 12, 8, true, true, true),
            new Definition(Kind.GroomingGarage, Category.Operations, "Garage dameuses", 220000, 12, 28, 22, false, true, false),
            new Definition(Kind.LiftMaintenance, Category.Operations, "Atelier remontees", 180000, 10, 24, 18, false, true, false),
            new Definition(Kind.SnowmakingStation, Category.Operations, "Local enneigement", 120000, 8, 18, 14, false, true, false)
        };
        public static IReadOnlyList<Definition> All { get { return entries; } }
        public static Definition Get(Kind kind)
        {
            foreach (Definition item in entries) if (item.Type == kind) return item;
            throw new ArgumentOutOfRangeException("kind", kind, "Type de structure inconnu");
        }
    }
}
