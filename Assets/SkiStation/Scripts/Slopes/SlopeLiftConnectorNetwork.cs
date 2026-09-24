using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public sealed class SlopeLiftConnectorNetwork : MonoBehaviour
    {
        public static SlopeLiftConnectorNetwork I { get; private set; }
        public IReadOnlyList<SlopeLiftConnectorRuntime> Connectors => connectors;
        private readonly List<SlopeLiftConnectorRuntime> connectors = new List<SlopeLiftConnectorRuntime>();

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        public void Register(SlopeLiftConnectorRuntime connector)
        { if (connector != null && !connectors.Contains(connector)) connectors.Add(connector); }

        public void Unregister(SlopeLiftConnectorRuntime connector)
        { if (connector != null) connectors.Remove(connector); }

        public SlopeLiftConnectorRuntime FindUsable(Vector3 position, float radius = 4f)
        {
            SlopeLiftConnectorRuntime best = null;
            float bestDistance = radius * radius;
            for (int i = 0; i < connectors.Count; i++)
            {
                var c = connectors[i];
                if (c == null || !c.CanUse(position, radius)) continue;
                float d = (c.EntryPoint - position).sqrMagnitude;
                if (d < bestDistance) { bestDistance = d; best = c; }
            }
            return best;
        }
    }
}
