using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum ManagedZoneEquipmentType { MagicCarpet, BeginnerTow, LearningArea, RestArea, InformationSign }

    [Serializable]
    public sealed class ManagedZoneEquipmentSnapshot
    {
        public ManagedZoneEquipmentType type;
        public string zoneName;
        public float capacityPerHour;
        public float qualityBonus;
    }

    /// <summary>
    /// Lightweight functional equipment layer for player-defined management zones.
    /// It keeps zones readable without forcing the player into detailed utility networks.
    /// </summary>
    public sealed class ManagedZoneEquipmentRuntime : MonoBehaviour
    {
        public static ManagedZoneEquipmentRuntime I { get; private set; }
        public IReadOnlyList<ManagedZoneEquipmentSnapshot> Equipment => equipment;
        public event Action Changed;
        private readonly List<ManagedZoneEquipmentSnapshot> equipment = new List<ManagedZoneEquipmentSnapshot>();

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
        }

        public bool CanPlace(ManagedZoneType zoneType, Vector3 position)
        {
            return ManagedZoneRuntime.I != null && ManagedZoneRuntime.I.IsPointInside(position, zoneType);
        }

        public bool Place(ManagedZoneType zoneType, ManagedZoneEquipmentType type, Vector3 position)
        {
            if (!CanPlace(zoneType, position)) return false;
            var go = GameObject.CreatePrimitive(type == ManagedZoneEquipmentType.MagicCarpet ? PrimitiveType.Cube : PrimitiveType.Cylinder);
            go.name = "Equipement_Zone_" + type;
            go.transform.position = position + Vector3.up * .35f;
            go.transform.localScale = type == ManagedZoneEquipmentType.MagicCarpet ? new Vector3(2.2f,.25f,6f) : new Vector3(.55f,.7f,.55f);
            var data = new ManagedZoneEquipmentSnapshot { type = type, zoneName = zoneType.ToString(), capacityPerHour = Capacity(type), qualityBonus = Bonus(type) };
            equipment.Add(data); Changed?.Invoke(); return true;
        }

        public float Capacity(ManagedZoneEquipmentType type) =>
            type == ManagedZoneEquipmentType.MagicCarpet ? 900f :
            type == ManagedZoneEquipmentType.BeginnerTow ? 700f : 0f;

        public float Bonus(ManagedZoneEquipmentType type) =>
            type == ManagedZoneEquipmentType.LearningArea ? .25f :
            type == ManagedZoneEquipmentType.RestArea ? .12f :
            type == ManagedZoneEquipmentType.InformationSign ? .08f : .05f;
    }
}
