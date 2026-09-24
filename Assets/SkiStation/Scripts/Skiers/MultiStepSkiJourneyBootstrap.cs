using UnityEngine;

namespace SkiStation
{
    /// <summary>1.79 non-destructive auto installer for multi-step journey planning.</summary>
    public sealed class MultiStepSkiJourneyBootstrap : MonoBehaviour
    {
        private float timer;
        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < 3f) return;
            timer = 0f;
            SkierRuntime[] skiers = Object.FindObjectsOfType<SkierRuntime>();
            for (int i = 0; i < skiers.Length; i++)
                if (skiers[i] != null && skiers[i].GetComponent<MultiStepSkiJourneyPlanner>() == null)
                    skiers[i].gameObject.AddComponent<MultiStepSkiJourneyPlanner>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (Object.FindObjectOfType<MultiStepSkiJourneyBootstrap>() != null) return;
            new GameObject("MultiStepSkiJourneyBootstrap_1.79").AddComponent<MultiStepSkiJourneyBootstrap>();
        }
    }
}
