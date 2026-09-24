using System;
using UnityEngine;

namespace SkiStation
{
    public enum StationServiceType { Shop, Restaurant, Bar, EquipmentRental, SkiSchool, Activity, Accommodation }
    public enum SkiSchoolServiceFormat { Individual, Group, Flexible }
    public enum ServiceRange { Economy, Standard, Premium, Luxury, Pro, Expert }

    [CreateAssetMenu(menuName = "SkiStation/Services/Station Service Definition")]
    public sealed class StationServiceDefinition : ScriptableObject
    {
        public string serviceId = "service";
        public string serviceName = "Service";
        public StationServiceType type = StationServiceType.Shop;
        public ServiceRange range = ServiceRange.Standard;
        [Min(1)] public int capacity = 10;
        [Min(1)] public int maxQueue = 30;
        [Min(0.1f)] public float serviceMinutes = 8f;
        [Min(0f)] public float basePrice = 20f;
        [Range(0f,100f)] public float quality = 75f;
        [Min(0)] public int staffRequired = 1;
        [Min(0)] public int staffAvailable = 1;
        [Min(0)] public int stock = 9999;
        public bool unlimitedStock = true;
        [Range(0f,1f)] public float familySpecialization;
        [Range(0f,1f)] public float beginnerSpecialization;
        [Range(0f,1f)] public float sportSpecialization;
        [Range(0f,1f)] public float premiumSpecialization;
        [Range(0f,1f)] public float expertSpecialization;
        [Range(0f,1f)] public float youthSpecialization;
        public SkiSchoolServiceFormat lessonFormat = SkiSchoolServiceFormat.Flexible;
        public SkiSchoolZoneType skiSchoolZoneType = SkiSchoolZoneType.StandardSchool;
        [Min(2)] public int lessonMaxGroupSize = 8;
        public bool familyFriendly;
        public bool openAtNight;
        [Range(0f,24f)] public float openingHour = 8f;
        [Range(0f,24f)] public float closingHour = 19f;
        [Min(0.1f)] public float arrivalRadius = 3f;
    }

    [Serializable]
    public struct StationServiceDailyStats
    {
        public int served;
        public int refused;
        public int totalRequests;
        public float revenue;
        public float waitingMinutes;
        public float satisfaction;
        public int satisfactionSamples;
        public int lastDay;
        public int queuePeak;
        public int[] servedByProfile;
        public float[] revenueByProfile;
        public float[] satisfactionByProfile;

        public StationServiceDailyStats(int day)
        {
            served = 0; refused = 0; totalRequests = 0; revenue = 0f; waitingMinutes = 0f;
            satisfaction = 0f; satisfactionSamples = 0; lastDay = day; queuePeak = 0;
            servedByProfile = new int[9];
            revenueByProfile = new float[9];
            satisfactionByProfile = new float[9];
        }
    }
}
