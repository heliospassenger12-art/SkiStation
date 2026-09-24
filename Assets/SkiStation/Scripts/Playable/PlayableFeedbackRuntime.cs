using UnityEngine;

namespace SkiStation
{
    /// <summary>Compact player feedback for the first playable: detects meaningful milestones without adding debug panels.</summary>
    public sealed class PlayableFeedbackRuntime : MonoBehaviour
    {
        public static PlayableFeedbackRuntime I { get; private set; }
        public string Message { get; private set; } = "";
        public float VisibleUntil { get; private set; }
        public bool Visible => !string.IsNullOrEmpty(Message) && Time.unscaledTime < VisibleUntil;

        private int slopes = -1, lifts = -1, visitors = -1;
        private bool domainReady;
        private FirstScenarioStage stage;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            var loop = PlayableLoopRuntime.I;
            if (loop != null) { slopes = loop.SlopeCount; lifts = loop.LiftCount; domainReady = loop.DomainReady; }
            visitors = StationRuntime.I != null ? StationRuntime.I.State.visitorsToday : 0;
            stage = FirstPlayableScenarioRuntime.I != null ? FirstPlayableScenarioRuntime.I.Stage : FirstScenarioStage.BuildLift;
            Show("Bienvenue. Construisez un petit domaine pour accueillir vos premiers skieurs.", 5f);
        }

        private void Update()
        {
            var loop = PlayableLoopRuntime.I;
            if (loop != null)
            {
                if (slopes >= 0 && loop.SlopeCount > slopes) Show("Nouvelle piste construite.");
                if (lifts >= 0 && loop.LiftCount > lifts) Show("Nouvelle remontee construite.");
                if (!domainReady && loop.DomainReady) Show("Domaine actif : les premiers visiteurs peuvent arriver !", 5f);
                slopes = loop.SlopeCount; lifts = loop.LiftCount; domainReady = loop.DomainReady;
            }

            int nowVisitors = StationRuntime.I != null ? StationRuntime.I.State.visitorsToday : 0;
            if (visitors >= 0 && visitors == 0 && nowVisitors > 0) Show("Vos premiers skieurs sont arrives !", 4f);
            visitors = nowVisitors;

            var scenario = FirstPlayableScenarioRuntime.I;
            if (scenario != null && scenario.Stage != stage)
            {
                stage = scenario.Stage;
                if (stage == FirstScenarioStage.Completed) Show("Premier objectif termine ! Le domaine est maintenant libre.", 6f);
            }
        }

        public void Show(string message, float seconds = 3.2f)
        {
            if (string.IsNullOrEmpty(message)) return;
            Message = message; VisibleUntil = Time.unscaledTime + Mathf.Max(1f, seconds);
        }
    }

    public static class PlayableFeedbackBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (Object.FindFirstObjectByType<PlayableFeedbackRuntime>() == null)
                new GameObject("Playable Feedback 1.94").AddComponent<PlayableFeedbackRuntime>();
        }
    }
}
