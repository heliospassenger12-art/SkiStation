using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum AccommodationType { Hotel, Residence, Apartment, Chalet, Hostel }

    [Serializable]
    public class AccommodationBuilding
    {
        public string name;
        public AccommodationType type;
        public int rooms;
        public int occupiedRooms;
        public float nightlyPrice = 120f;
        public bool open = true;
        public int FreeRooms => Mathf.Max(0, rooms - occupiedRooms);
    }

    /// <summary>Basic lodging capacity for residents and short-stay visitors.</summary>
    public sealed class StationAccommodationRuntime : MonoBehaviour
    {
        public static StationAccommodationRuntime I { get; private set; }
        public List<AccommodationBuilding> buildings = new List<AccommodationBuilding>();

        public int TotalRooms { get; private set; }
        public int OccupiedRooms { get; private set; }
        public int FreeRooms => Mathf.Max(0, TotalRooms - OccupiedRooms);

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
            RecalculateCapacity();
        }

        public void AddBuilding(string name, AccommodationType type, int rooms, float nightlyPrice)
        {
            if (rooms <= 0) return;
            buildings.Add(new AccommodationBuilding { name = name, type = type, rooms = rooms, nightlyPrice = Mathf.Max(0f, nightlyPrice) });
            RecalculateCapacity();
        }

        public bool TryCheckIn(int rooms)
        {
            if (rooms <= 0 || FreeRooms < rooms) return false;
            int left = rooms;
            for (int i = 0; i < buildings.Count && left > 0; i++)
            {
                AccommodationBuilding b = buildings[i];
                int add = Mathf.Min(left, b.FreeRooms);
                b.occupiedRooms += add;
                left -= add;
            }
            RecalculateCapacity();
            return left == 0;
        }

        public void CheckOut(int rooms)
        {
            int left = Mathf.Max(0, rooms);
            for (int i = buildings.Count - 1; i >= 0 && left > 0; i--)
            {
                AccommodationBuilding b = buildings[i];
                int remove = Mathf.Min(left, b.occupiedRooms);
                b.occupiedRooms -= remove;
                left -= remove;
            }
            RecalculateCapacity();
        }

        public void RecalculateCapacity()
        {
            TotalRooms = 0; OccupiedRooms = 0;
            for (int i = 0; i < buildings.Count; i++)
            {
                TotalRooms += Mathf.Max(0, buildings[i].rooms);
                OccupiedRooms += Mathf.Clamp(buildings[i].occupiedRooms, 0, buildings[i].rooms);
            }
        }
    }
}
