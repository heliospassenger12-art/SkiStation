using UnityEngine;

namespace SkiStation
{
    /// <summary>Global construction preference. Instant mode is the default so the player is never forced to wait.</summary>
    public sealed class ConstructionPolicyRuntime : MonoBehaviour
    {
        public static ConstructionPolicyRuntime I { get; private set; }
        [SerializeField] bool instantConstruction = true;
        public bool InstantConstruction => instantConstruction;
        public event System.Action Changed;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.I)) ToggleInstant();
        }

        public void SetInstant(bool value) { instantConstruction = value; Changed?.Invoke(); }
        public void ToggleInstant() => SetInstant(!instantConstruction);
    }
}
