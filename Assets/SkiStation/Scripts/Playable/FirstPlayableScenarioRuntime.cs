using UnityEngine;

namespace SkiStation
{
    public enum FirstScenarioStage { BuildLift, BuildSlope, OpenDomain, WelcomeVisitors, EarnRevenue, Completed }

    /// <summary>First guided objective chain: a tiny resort that becomes operational.</summary>
    public sealed class FirstPlayableScenarioRuntime : MonoBehaviour
    {
        public static FirstPlayableScenarioRuntime I { get; private set; }
        public FirstScenarioStage Stage { get; private set; } = FirstScenarioStage.BuildLift;
        public string Objective { get; private set; } = "Construisez votre premiere remontee.";
        public float Progress01 { get; private set; }
        public bool Completed => Stage == FirstScenarioStage.Completed;
        private float startingRevenue;

        private void Awake() { if (I != null && I != this) { Destroy(gameObject); return; } I = this; DontDestroyOnLoad(gameObject); }
        private void Start() { startingRevenue = FinanceRuntime.I != null ? FinanceRuntime.I.TotalRevenue : 0f; }

        private void Update()
        {
            var loop = PlayableLoopRuntime.I;
            if (loop == null) return;
            switch (Stage)
            {
                case FirstScenarioStage.BuildLift:
                    Progress01 = Mathf.Clamp01(loop.LiftCount); Objective = "Construisez votre premiere remontee.";
                    if (loop.LiftCount > 0) Advance(FirstScenarioStage.BuildSlope, "Tracez une piste desservie par la remontee."); break;
                case FirstScenarioStage.BuildSlope:
                    Progress01 = Mathf.Clamp01(loop.SlopeCount); Objective = "Tracez votre premiere piste.";
                    if (loop.SlopeCount > 0) Advance(FirstScenarioStage.OpenDomain, "Votre domaine est presque pret."); break;
                case FirstScenarioStage.OpenDomain:
                    Progress01 = loop.DomainReady ? 1f : 0f; Objective = "Ouvrez un petit domaine coherent.";
                    if (loop.DomainReady) Advance(FirstScenarioStage.WelcomeVisitors, "Accueillez vos premiers skieurs."); break;
                case FirstScenarioStage.WelcomeVisitors:
                    int visitors = StationRuntime.I != null ? StationRuntime.I.State.visitorsToday : 0;
                    Progress01 = Mathf.Clamp01(visitors / 5f); Objective = "Accueillez 5 visiteurs (" + visitors + "/5).";
                    if (visitors >= 5) { startingRevenue = FinanceRuntime.I != null ? FinanceRuntime.I.TotalRevenue : 0f; Advance(FirstScenarioStage.EarnRevenue, "Generez vos premieres recettes."); } break;
                case FirstScenarioStage.EarnRevenue:
                    float revenue = FinanceRuntime.I != null ? Mathf.Max(0f, FinanceRuntime.I.TotalRevenue - startingRevenue) : 0f;
                    Progress01 = Mathf.Clamp01(revenue / 100f); Objective = "Generez 100 EUR de recettes (" + revenue.ToString("N0") + "/100).";
                    if (revenue >= 100f) Advance(FirstScenarioStage.Completed, "Premier domaine operationnel ! Continuez librement a developper la station."); break;
                case FirstScenarioStage.Completed: Progress01 = 1f; break;
            }
        }

        private void Advance(FirstScenarioStage next, string message)
        {
            Stage = next; Progress01 = 0f; Objective = message; PlayableLoopRuntime.I?.SetMessage(message);
        }
    }

    public static class FirstPlayableScenarioBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (Object.FindFirstObjectByType<FirstPlayableScenarioRuntime>() == null)
                new GameObject("First Playable Scenario 1.92").AddComponent<FirstPlayableScenarioRuntime>();
        }
    }
}
