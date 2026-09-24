using UnityEngine;

namespace SkiStation
{
    /// <summary>0.80: gives each real lift vehicle stable docking/boarding/unloading anchors without teleporting the management simulation.</summary>
    public sealed class LiftVehicleStationDockingRuntime : MonoBehaviour
    {
        public Transform DockingAnchor { get; private set; }
        public Transform BoardingAnchor { get; private set; }
        public Transform UnloadingAnchor { get; private set; }
        public Transform ExitAnchor { get; private set; }

        private void Awake() { Build(); }

        public void Build()
        {
            DockingAnchor = Anchor("DockingAnchor", Vector3.zero);
            BoardingAnchor = Anchor("BoardingAnchor", new Vector3(0f, .7f, 0f));
            UnloadingAnchor = Anchor("UnloadingAnchor", new Vector3(0f, .1f, 1.4f));
            ExitAnchor = Anchor("ExitAnchor", new Vector3(0f, .1f, 2.4f));
        }

        private Transform Anchor(string id, Vector3 local)
        {
            var t = transform.Find(id);
            if (t == null)
            {
                var go = new GameObject(id);
                t = go.transform; t.SetParent(transform, false);
            }
            t.localPosition = local;
            return t;
        }
    }
}
