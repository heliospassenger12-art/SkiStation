using UnityEngine;

namespace SkiStation
{
    /// <summary>Attaches the 1.77 network-aware navigator to skier agents without scene migration.</summary>
    public sealed class IntegratedSkierDomainBootstrap : MonoBehaviour
    {
        [SerializeField] private float refreshSeconds = 2f;
        private float timer;

        private void Start() { Attach(); }
        private void Update()
        {
            timer -= Time.unscaledDeltaTime;
            if (timer > 0f) return;
            timer = Mathf.Max(.5f, refreshSeconds);
            Attach();
        }

        private static void Attach()
        {
            SkierRuntime[] skiers = Object.FindObjectsOfType<SkierRuntime>();
            for (int i = 0; i < skiers.Length; i++)
            {
                SkierRuntime skier = skiers[i];
                if (skier != null && skier.GetComponent<IntegratedSkierDomainNavigator>() == null)
                    skier.gameObject.AddComponent<IntegratedSkierDomainNavigator>();
            }
        }
    }
}
