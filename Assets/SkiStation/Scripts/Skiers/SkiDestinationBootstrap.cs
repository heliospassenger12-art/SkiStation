using UnityEngine;
namespace SkiStation
{
    /// <summary>1.80 non-destructive installer for concrete skier destinations.</summary>
    public sealed class SkiDestinationBootstrap : MonoBehaviour
    {
        private float timer;
        private void Update()
        {
            timer += Time.unscaledDeltaTime; if (timer < 3f) return; timer = 0f;
            SkierRuntime[] skiers = Object.FindObjectsOfType<SkierRuntime>();
            for (int i = 0; i < skiers.Length; i++)
                if (skiers[i] != null && skiers[i].GetComponent<SkiDestinationRuntime>() == null)
                    skiers[i].gameObject.AddComponent<SkiDestinationRuntime>();
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (Object.FindObjectOfType<SkiDestinationBootstrap>() != null) return;
            new GameObject("SkiDestinationBootstrap_1.80").AddComponent<SkiDestinationBootstrap>();
        }
    }
}
