using UnityEngine;

namespace SkiStation
{
    /// <summary>Attaches the 1.78 day-mind to real skier agents without scene editing.</summary>
    public sealed class IntelligentSkiDayBootstrap : MonoBehaviour
    {
        [SerializeField] private float refreshSeconds = 2f;
        private float timer;
        private void Start() { Attach(); }
        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < Mathf.Max(.5f, refreshSeconds)) return;
            timer = 0f;
            Attach();
        }
        private static void Attach()
        {
            SkierRuntime[] skiers = Object.FindObjectsOfType<SkierRuntime>();
            for (int i = 0; i < skiers.Length; i++)
                if (skiers[i] != null && skiers[i].GetComponent<IntelligentSkiDayRuntime>() == null)
                    skiers[i].gameObject.AddComponent<IntelligentSkiDayRuntime>();
        }
    }
}
