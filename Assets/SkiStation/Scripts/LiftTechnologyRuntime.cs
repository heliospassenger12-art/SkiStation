using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum LiftTechnologyType
    {
        ImprovedBraking,
        DetachableGrip,
        HighSpeedDrive,
        HigherCapacityVehicles,
        ModernCable,
        EnergyEfficientMotor,
        AdvancedSafety,
        LongSpanPylons
    }

    [Serializable]
    public class LiftTechnologyDefinition
    {
        public LiftTechnologyType technology;
        public string displayName;
        public int researchTier;
        public float researchCost = 10000f;
        public float speedMultiplier = 1f;
        public float capacityMultiplier = 1f;
        public float wearMultiplier = 1f;
        public float safetyBonus01 = 0f;
        public float maxSpanMultiplier = 1f;
    }

    public class LiftTechnologyRuntime : MonoBehaviour
    {
        public static LiftTechnologyRuntime Instance { get; private set; }

        public LiftTechnologyDefinition[] technologies;
        private readonly HashSet<LiftTechnologyType> unlocked = new();
        private readonly Dictionary<string, HashSet<LiftTechnologyType>> applied = new();

        public event Action Changed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            EnsureDefaults();
        }

        private void EnsureDefaults()
        {
            if (technologies != null && technologies.Length > 0) return;

            technologies = new[]
            {
                new LiftTechnologyDefinition { technology=LiftTechnologyType.ImprovedBraking, displayName="Freinage amélioré", researchTier=1, researchCost=8000, safetyBonus01=.08f },
                new LiftTechnologyDefinition { technology=LiftTechnologyType.DetachableGrip, displayName="Pinces débrayables", researchTier=2, researchCost=18000, speedMultiplier=1.18f, wearMultiplier=.92f },
                new LiftTechnologyDefinition { technology=LiftTechnologyType.HighSpeedDrive, displayName="Motorisation haute vitesse", researchTier=2, researchCost=22000, speedMultiplier=1.28f, wearMultiplier=1.08f },
                new LiftTechnologyDefinition { technology=LiftTechnologyType.HigherCapacityVehicles, displayName="Véhicules grande capacité", researchTier=2, researchCost=20000, capacityMultiplier=1.22f },
                new LiftTechnologyDefinition { technology=LiftTechnologyType.ModernCable, displayName="Câble nouvelle génération", researchTier=3, researchCost=30000, wearMultiplier=.78f, maxSpanMultiplier=1.15f },
                new LiftTechnologyDefinition { technology=LiftTechnologyType.EnergyEfficientMotor, displayName="Motorisation éco-énergétique", researchTier=3, researchCost=24000, wearMultiplier=.88f },
                new LiftTechnologyDefinition { technology=LiftTechnologyType.AdvancedSafety, displayName="Sécurité avancée", researchTier=4, researchCost=40000, safetyBonus01=.20f },
                new LiftTechnologyDefinition { technology=LiftTechnologyType.LongSpanPylons, displayName="Portées longues", researchTier=4, researchCost=45000, maxSpanMultiplier=1.35f }
            };
        }

        public bool IsUnlocked(LiftTechnologyType t) => unlocked.Contains(t);

        public bool Research(LiftTechnologyType t)
        {
            var def = Find(t);
            if (def == null || unlocked.Contains(t)) return false;

            if (!CareerTierGateAllows(def.researchTier))
                return false;

            if (StationRuntime.Instance != null && !StationRuntime.Instance.Spend(def.researchCost))
                return false;

            unlocked.Add(t);
            Changed?.Invoke();
            return true;
        }

        private bool CareerTierGateAllows(int tier)
        {
            var type = Type.GetType("SkiStation.CareerProgressionRuntime");
            if (type == null) return true;
            var prop = type.GetProperty("Instance");
            var inst = prop != null ? prop.GetValue(null, null) : null;
            if (inst == null) return true;
            var method = type.GetMethod("IsTierUnlocked");
            if (method == null) return true;
            try { return (bool)method.Invoke(inst, new object[] { tier }); }
            catch { return true; }
        }

        public bool ApplyToLift(string liftId, LiftTechnologyType t)
        {
            if (!unlocked.Contains(t) || string.IsNullOrEmpty(liftId)) return false;
            if (!applied.TryGetValue(liftId, out var set))
                applied[liftId] = set = new HashSet<LiftTechnologyType>();
            if (!set.Add(t)) return false;
            var lift = FindObjectsOfType<LiftRuntime>();
            for (int i = 0; i < lift.Length; i++)
                if (lift[i] != null && lift[i].LiftId == liftId)
                    lift[i].RefreshPerformance();
            Changed?.Invoke();
            return true;
        }

        public float GetSpeedMultiplier(string liftId)
        {
            float result = 1f;
            if (applied.TryGetValue(liftId, out var set))
                foreach (var t in set) result *= Find(t)?.speedMultiplier ?? 1f;
            return result;
        }

        public float GetCapacityMultiplier(string liftId)
        {
            float result = 1f;
            if (applied.TryGetValue(liftId, out var set))
                foreach (var t in set) result *= Find(t)?.capacityMultiplier ?? 1f;
            return result;
        }

        public float GetWearMultiplier(string liftId)
        {
            float result = 1f;
            if (applied.TryGetValue(liftId, out var set))
                foreach (var t in set) result *= Find(t)?.wearMultiplier ?? 1f;
            return result;
        }

        public float GetSafetyBonus(string liftId)
        {
            float result = 0f;
            if (applied.TryGetValue(liftId, out var set))
                foreach (var t in set) result += Find(t)?.safetyBonus01 ?? 0f;
            return Mathf.Clamp01(result);
        }

        public float GetMaxSpanMultiplier(string liftId)
        {
            float result = 1f;
            if (applied.TryGetValue(liftId, out var set))
                foreach (var t in set) result *= Find(t)?.maxSpanMultiplier ?? 1f;
            return result;
        }

        private LiftTechnologyDefinition Find(LiftTechnologyType t)
        {
            foreach (var d in technologies)
                if (d != null && d.technology == t) return d;
            return null;
        }
    }
}
