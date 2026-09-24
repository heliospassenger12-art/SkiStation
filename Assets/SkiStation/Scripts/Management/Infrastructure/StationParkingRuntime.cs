using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum ParkingType { DayVisitor, HotelGuest, Staff, CoachBus }

    [Serializable]
    public class ParkingArea
    {
        public string name;
        public ParkingType type;
        public int spaces;
        public int occupiedSpaces;
        public float dailyPrice;
        public bool open = true;
        public int FreeSpaces => Mathf.Max(0, spaces - occupiedSpaces);
    }

    public sealed class StationParkingRuntime : MonoBehaviour
    {
        public static StationParkingRuntime I { get; private set; }
        public List<ParkingArea> areas = new List<ParkingArea>();
        public int TotalSpaces { get; private set; }
        public int OccupiedSpaces { get; private set; }
        public int FreeSpaces => Mathf.Max(0, TotalSpaces - OccupiedSpaces);
        public event Action Changed;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject); Recalculate();
        }
        private void OnDestroy() { if (I == this) I = null; }

        public void AddArea(string areaName, ParkingType type, int spaces, float dailyPrice)
        {
            if (spaces <= 0) return;
            areas.Add(new ParkingArea { name = areaName, type = type, spaces = spaces, dailyPrice = Mathf.Max(0f, dailyPrice) });
            Recalculate();
        }
        public bool TryPark(ParkingType type)
        {
            for (int i = 0; i < areas.Count; i++)
            {
                var area = areas[i];
                if (!area.open || area.type != type || area.FreeSpaces <= 0) continue;
                area.occupiedSpaces++; Recalculate(); Changed?.Invoke(); return true;
            }
            return false;
        }
        public void Leave(ParkingType type)
        {
            for (int i = areas.Count - 1; i >= 0; i--)
            {
                var area = areas[i];
                if (area.type != type || area.occupiedSpaces <= 0) continue;
                area.occupiedSpaces--; Recalculate(); Changed?.Invoke(); return;
            }
        }
        public void Recalculate()
        {
            TotalSpaces = 0; OccupiedSpaces = 0;
            foreach (var area in areas)
            {
                if (area == null) continue;
                area.occupiedSpaces = Mathf.Clamp(area.occupiedSpaces, 0, Mathf.Max(0, area.spaces));
                TotalSpaces += Mathf.Max(0, area.spaces); OccupiedSpaces += area.occupiedSpaces;
            }
        }
    }
}
