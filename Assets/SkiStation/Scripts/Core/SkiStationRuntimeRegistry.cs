using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public sealed class SkiStationRuntimeRegistry : MonoBehaviour
    {
        public static SkiStationRuntimeRegistry I { get; private set; }

        private readonly List<Component> registered = new List<Component>();
        public int RegisteredCount => registered.Count;

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Register(Component component)
        {
            if (component == null || registered.Contains(component)) return;
            registered.Add(component);
        }

        public void Unregister(Component component)
        {
            if (component == null) return;
            registered.Remove(component);
        }

        public IReadOnlyList<Component> Snapshot() => registered;
    }
}
