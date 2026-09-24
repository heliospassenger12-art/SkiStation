using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>Phase 46: career catalogue. Only a classic drag lift and a fixed two-seat chair are available initially.</summary>
    public sealed class LiftCareerCatalog249 : MonoBehaviour
    {
        private struct Model
        {
            public string id, name;
            public LiftType type;
            public int tier, seats;
            public float maxLength, speed, cost;
            public bool detachable;
            public float maintenancePerDay;
            public Model(string id, string name, LiftType type, int tier, int seats, float maxLength, float speed, float cost, bool detachable = false)
            { this.id=id; this.name=name; this.type=type; this.tier=tier; this.seats=seats; this.maxLength=maxLength; this.speed=speed; this.cost=cost; this.detachable=detachable; maintenancePerDay=Mathf.Round(cost * .0025f); }
        }
        private static readonly Model[] Models = {
            new Model("tow_classic", "Téléski classique", LiftType.Platter, 0, 1, 1000f, 7f, 65000f),
            new Model("chair_fixed_2", "Télésiège fixe 2 places", LiftType.FixedChair, 0, 2, 1500f, 8f, 125000f),
            new Model("tow_tbar", "Téléski à archet 2 places", LiftType.TBar, 1, 2, 850f, 9f, 125000f),
            new Model("chair_fixed_4", "Télésiège fixe 4 places", LiftType.FixedChair, 1, 4, 1100f, 9f, 240000f),
            new Model("carpet", "Tapis roulant débutants", LiftType.MagicCarpet, 1, 1, 180f, 2f, 70000f),
            new Model("chair_detachable_4", "Télésiège débrayable 4 places", LiftType.DetachableChair, 2, 4, 1700f, 17f, 520000f, true),
            new Model("chair_detachable_6", "Télésiège débrayable 6 places", LiftType.DetachableChair, 3, 6, 2400f, 18f, 850000f, true),
            new Model("gondola_8", "Télécabine 8 places", LiftType.Gondola, 3, 8, 3200f, 20f, 1450000f, true),
            new Model("chair_bubble_8", "Télésiège à bulles 8 places", LiftType.BubbleChair, 4, 8, 3000f, 18f, 1300000f, true),
            new Model("gondola_10", "Télécabine 10 places", LiftType.Gondola, 4, 10, 3800f, 21f, 2000000f, true),
            new Model("cablecar", "Téléphérique", LiftType.CableCar, 5, 50, 7500f, 28f, 4800000f),
            new Model("three_s", "Télécabine 3S", LiftType.ThreeS, 6, 30, 8500f, 27f, 8500000f, true),
            new Model("funicular", "Funiculaire", LiftType.Funicular, 6, 80, 9000f, 25f, 9500000f)
        };
        private static readonly HashSet<string> Researched = new HashSet<string>();
        private static readonly int[] ResearchCosts = { 0, 30000, 90000, 180000, 350000, 750000, 1500000 };
        private static int currentTier;
        // Explicit session selection until the game-mode/save system is connected.
        // Career never receives the sandbox override.
        private static bool sandboxSession;
        private static bool sandboxAllUnlocked;
        public static bool SandboxSession => sandboxSession;
        public static bool SandboxAllUnlocked => sandboxSession && sandboxAllUnlocked;
        public static void SetSandboxSession(bool enabled)
        {
            sandboxSession = enabled;
            if (!enabled) sandboxAllUnlocked = false;
        }
        public static void SetSandboxResearchRules(bool allUnlocked)
        {
            if (sandboxSession) sandboxAllUnlocked = allUnlocked;
        }
        // Called only after the player confirms the phase 54 start screen.
        public static void StartNewSession(bool isSandbox, bool unlockEverything)
        {
            currentTier = 0;
            Researched.Clear();
            sandboxSession = isSandbox;
            sandboxAllUnlocked = isSandbox && unlockEverything;
        }
        private bool visible;
        private Vector2 scroll;
        private LiftConstructionPlanner planner;
        private void Awake() { planner = FindFirstObjectByType<LiftConstructionPlanner>(); }
        public static bool IsUnlocked(string id)
        {
            foreach (var m in Models) if (m.id == id) return SandboxAllUnlocked || m.tier <= currentTier;
            // Do not silently authorize legacy high-capacity models as starting equipment.
            return false;
        }
        // A researched engineering tier increases the allowable length of newly planned lines.
        // The sandbox catalogue override unlocks models, but does not silently purchase research upgrades.
        public static float EffectiveMaxLength(string id, float baseLength)
        {
            if (baseLength <= 0f) return 0f;
            int unlockedAt = 0;
            foreach (var model in Models) if (model.id == id) { unlockedAt = model.tier; break; }
            int improvements = Mathf.Max(0, currentTier - unlockedAt);
            return Mathf.Round(baseLength * (1f + 0.12f * improvements));
        }

        public static void Configure(LiftDefinition def, string id)
        {
            foreach (var m in Models) if (m.id == id)
            {
                def.id=m.id; def.displayName=m.name; def.type=m.type; def.researchLevel=m.tier;
                def.seatsPerVehicle=m.seats; def.maxLineLengthMeters=m.maxLength; def.speedKmh=m.speed;
                def.buildCost=m.cost; def.maintenancePerDay=m.maintenancePerDay; def.detachable=m.detachable; def.variableSpeed=m.detachable;
                def.spacingSeconds=m.type==LiftType.MagicCarpet ? 3f : m.type==LiftType.CableCar || m.type==LiftType.Funicular ? 180f : m.detachable ? 8f : m.seats>=4 ? 12f : 16f;
                def.vehicles=Mathf.Max(2,Mathf.CeilToInt(2f*m.maxLength / Mathf.Max(.5f,m.speed/3.6f) / def.spacingSeconds));
                def.loadingSeconds=m.detachable ? 7f : 4f;
                return;
            }
        }
        private void Update() { if (Input.GetKeyDown(KeyCode.F8)) visible=!visible; }
        private void OnGUI()
        {
            if (!visible) return;
            Rect r=new Rect(Mathf.Max(8,Screen.width-410),70,400,Mathf.Min(570,Screen.height-85));
            GUI.Box(r,"REMONTÉES / RECHERCHE — F8");
            GUILayout.BeginArea(new Rect(r.x+12,r.y+30,r.width-24,r.height-42));
            GUILayout.Label("Mode : " + (sandboxSession ? "BAC À SABLE" : "CARRIÈRE") + (SandboxAllUnlocked ? " — catalogue libre" : " — laboratoire"));
            GUILayout.Label("Règles définies dans le menu de début de partie.");
            GUILayout.Label("Palier de recherche : " + currentTier + " / 6" + (SandboxAllUnlocked ? " (catalogue libre)" : ""));
            if (!SandboxAllUnlocked && currentTier<6)
            {
                int next=currentTier+1;
                if (GUILayout.Button("Rechercher palier " + next + " — " + ResearchCosts[next].ToString("N0") + " €",GUILayout.Height(32)))
                {
                    if (StationRuntime.I == null || StationRuntime.I.Spend(ResearchCosts[next],FinanceCategory.Construction,"lift_research","Recherche remontées palier " + next)) currentTier=next;
                }
            }
            GUILayout.Label("Recherche : chaque palier augmente de 12 % la portée des modèles déjà débloqués (nouvelles constructions).");
            GUILayout.Label("Fiches indicatives de gameplay (non homologuées). Débit théorique : places × 3600 / intervalle.");
            GUILayout.Label("Sélectionner un modèle puis construire avec l'outil remontée.");
            scroll=GUILayout.BeginScrollView(scroll);
            foreach (var m in Models)
            {
                bool unlocked=IsUnlocked(m.id);
                float interval=m.type==LiftType.MagicCarpet ? 3f : m.type==LiftType.CableCar || m.type==LiftType.Funicular ? 180f : m.detachable ? 8f : m.seats>=4 ? 12f : 16f;
                int theoreticalFlow=Mathf.RoundToInt(m.seats*3600f/interval);
                GUILayout.Label(m.name + " | palier " + m.tier + (unlocked ? " — disponible" : " — verrouillé"));
                GUILayout.Label("  " + m.seats + " place(s) | " + EffectiveMaxLength(m.id, m.maxLength).ToString("N0") + " m max | " + m.speed.ToString("0.#") + " km/h");
                GUILayout.Label("  Débit théorique " + theoreticalFlow.ToString("N0") + " pers./h | coût " + m.cost.ToString("N0") + " € | entretien " + m.maintenancePerDay.ToString("N0") + " €/j");
                GUI.enabled=unlocked;
                if (GUILayout.Button("Choisir — " + m.cost.ToString("N0") + " €"))
                {
                    if (planner==null) planner=FindFirstObjectByType<LiftConstructionPlanner>();
                    if (planner!=null) { var d=ScriptableObject.CreateInstance<LiftDefinition>(); Configure(d,m.id); planner.SetDefinition(d); if (planner.IsActive && planner.HasStations && !planner.AwaitingConfirmation) planner.Begin(); }
                }
                GUI.enabled=true;
            }
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
    }
}
