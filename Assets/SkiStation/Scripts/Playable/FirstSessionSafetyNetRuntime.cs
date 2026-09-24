using UnityEngine;

namespace SkiStation
{
    /// <summary>Observes the first playable loop and exposes one concise recovery hint when progress stalls.</summary>
    public sealed class FirstSessionSafetyNetRuntime : MonoBehaviour
    {
        public static FirstSessionSafetyNetRuntime I { get; private set; }
        public bool NeedsAttention { get; private set; }
        public string Message { get; private set; } = "";
        private float stalledSince = -1f;
        private int lastVisitors;

        private void Awake() { if (I != null && I != this) { Destroy(gameObject); return; } I = this; }
        private void Update() { if (Time.unscaledTime % 1f > .03f) return; Evaluate(); }

        private void Evaluate()
        {
            NeedsAttention = false; Message = "";
            var station = StationRuntime.I; var loop = PlayableLoopRuntime.I;
            if (station == null || loop == null) return;

            if (station.State.money < 0f) { Warn("Tresorerie negative : evitez de nouvelles constructions et laissez le domaine generer des recettes."); return; }
            if (loop.LiftCount > 0 && loop.SlopeCount == 0 && station.State.money < PlayableLoopRuntime.SlopeBaseCost)
            { Warn("Budget trop faible pour la premiere piste : la session risque d'etre bloquee."); return; }
            if (loop.DomainReady && station.State.visitorsToday == 0)
            {
                if (stalledSince < 0f) stalledSince = Time.unscaledTime;
                if (Time.unscaledTime - stalledSince > 45f) Warn("Aucun visiteur : " + (StationVisitorDemandRuntime.I != null ? StationVisitorDemandRuntime.I.ArrivalDiagnostic : "systeme des visiteurs absent"));
                return;
            }
            if (station.State.visitorsToday != lastVisitors) { lastVisitors = station.State.visitorsToday; stalledSince = -1f; }
            else if (!loop.DomainReady) stalledSince = -1f;
        }
        private void Warn(string text) { NeedsAttention = true; Message = text; }
    }

    public static class FirstSessionSafetyNetBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (Object.FindFirstObjectByType<FirstSessionSafetyNetRuntime>() == null)
                new GameObject("First Session Safety Net 1.97").AddComponent<FirstSessionSafetyNetRuntime>();
        }
    }
}
