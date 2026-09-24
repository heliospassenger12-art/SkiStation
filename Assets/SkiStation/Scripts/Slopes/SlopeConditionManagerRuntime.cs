using UnityEngine;

namespace SkiStation
{
    public sealed class SlopeConditionManagerRuntime : MonoBehaviour
    {
        public static SlopeConditionManagerRuntime I { get; private set; }
        private float scan;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            scan += Time.unscaledDeltaTime;
            if (scan < 1f) return;
            scan = 0f;
            foreach (var slope in Object.FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None))
                if (slope != null && slope.GetComponent<SlopeConditionRuntime>() == null)
                    slope.gameObject.AddComponent<SlopeConditionRuntime>();
        }
    }
}
