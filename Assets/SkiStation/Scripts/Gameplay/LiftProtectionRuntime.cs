using UnityEngine;

namespace SkiStation
{
    /// <summary>Automatic, no-micromanagement protection around aerial lift pylons.</summary>
    public sealed class LiftProtectionRuntime : MonoBehaviour
    {
        public static bool AnyProtectedPylons { get; private set; }
        [SerializeField] private float radius = 1.15f;
        [SerializeField] private float height = 2.1f;
        private LiftRuntime lift;
        private float nextScan;

        public void Bind(LiftRuntime value) { lift = value; }

        private void Update()
        {
            if (lift == null) lift = GetComponent<LiftRuntime>();
            if (lift == null || lift.IsUnderground || Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + 1.5f;
            BuildProtectionIfNeeded();
        }

        private void BuildProtectionIfNeeded()
        {
            bool found = false;
            foreach (Transform child in transform)
                if (child.name.StartsWith("Pylon") && child.Find("ProtectiveCushion") != null) found = true;
            if (found) { AnyProtectedPylons = true; return; }

            foreach (Transform child in transform)
            {
                if (!child.name.StartsWith("Pylon")) continue;
                var cushion = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cushion.name = "ProtectiveCushion";
                cushion.transform.SetParent(child, false);
                cushion.transform.localPosition = Vector3.zero;
                cushion.transform.localScale = new Vector3(radius, height * .5f, radius);
                var r = cushion.GetComponent<Renderer>();
                if (r != null) r.material = CreateSoftMaterial();
                var collider = cushion.GetComponent<Collider>();
                if (collider != null) collider.isTrigger = true;
                found = true;
            }
            AnyProtectedPylons |= found;
        }

        private static Material CreateSoftMaterial()
        {
            var m = RuntimeMaterialFactory.Create(new Color(.95f, .18f, .08f, 1f));
            if (m != null) m.name = "Safety_Cushion_Material";
            return m;
        }
    }
}
