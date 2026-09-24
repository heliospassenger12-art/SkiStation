using UnityEngine;

namespace SkiStation
{
    /// <summary>Compact readiness monitor for the first playable session. It observes only; existing systems stay authoritative.</summary>
    public sealed class FirstSessionReadinessRuntime : MonoBehaviour
    {
        public static FirstSessionReadinessRuntime I { get; private set; }
        public string Status { get; private set; } = "Pret a construire";
        public string Hint { get; private set; } = "Commencez par une remontee, puis tracez une piste.";
        public bool HasBlockingIssue { get; private set; }
        public int ActiveSkiers { get; private set; }
        public float Satisfaction01 { get; private set; }
        private float nextRefresh;

        private void Awake() { if (I != null && I != this) { Destroy(gameObject); return; } I = this; }
        private void Update() { if (Time.unscaledTime < nextRefresh) return; nextRefresh = Time.unscaledTime + 1f; Refresh(); }

        private void Refresh()
        {
            var loop = PlayableLoopRuntime.I;
            var station = StationRuntime.I;
            ActiveSkiers = Object.FindObjectsByType<SkierRuntime>(FindObjectsSortMode.None).Length;
            HasBlockingIssue = false;
            Satisfaction01 = 0f;
            var skiers = Object.FindObjectsByType<SkierRuntime>(FindObjectsSortMode.None);
            if (skiers.Length > 0) { float sum = 0f; foreach (var s in skiers) sum += s.Satisfaction / 100f; Satisfaction01 = sum / skiers.Length; }

            if (station == null) { Status = "Initialisation"; Hint = "La station se prepare."; return; }
            if (loop == null) { Status = "Initialisation"; Hint = "Les outils de jeu se preparent."; return; }
            if (loop.LiftCount == 0) { Status = "Domaine ferme"; Hint = "Construisez votre premiere remontee [L]."; return; }
            if (loop.SlopeCount == 0) { Status = "Remontee prete"; Hint = "Tracez maintenant une piste [B]."; return; }
            if (!loop.DomainReady) { Status = "Connexion a verifier"; Hint = "Reliez la piste et la remontee pour ouvrir le domaine."; HasBlockingIssue = true; return; }
            if (station.State.visitorsToday == 0) { Status = "Domaine ouvert"; Hint = "Les premiers visiteurs peuvent maintenant arriver."; return; }
            if (ActiveSkiers == 0) { Status = "Visiteurs accueillis"; Hint = "La simulation attend les prochains skieurs."; return; }
            Status = "Station en activite";
            Hint = Satisfaction01 < .55f ? "La satisfaction baisse : observez files d'attente et qualite du domaine." : "Le domaine fonctionne. Continuez a developper la station.";
        }
    }

    public static class FirstSessionReadinessBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (Object.FindFirstObjectByType<FirstSessionReadinessRuntime>() == null)
                new GameObject("First Session Readiness 1.95").AddComponent<FirstSessionReadinessRuntime>();
        }
    }
}
