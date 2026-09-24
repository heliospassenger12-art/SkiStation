using UnityEngine;

namespace SkiStation.Arcade
{
    // Purely visual animation on child objects: gameplay collider/root never ragdolls.
    // A failed aerial trick triggers the only crash; retry immediately restores equipment.
    public sealed class ArcadeSkiTrickVisual : MonoBehaviour
    {
        public ArcadeSkiController controller;
        public Transform helmet;
        public Transform leftSki;
        public Transform rightSki;
        public Transform bodyVisual;
        public float tumbleDegreesPerSecond = 540f;
        Vector3 leftHome, rightHome;
        Quaternion leftRotation, rightRotation, bodyRotation;
        float crashTime;
        bool crashed;

        void Start()
        {
            if (leftSki != null) { leftHome = leftSki.localPosition; leftRotation = leftSki.localRotation; }
            if (rightSki != null) { rightHome = rightSki.localPosition; rightRotation = rightSki.localRotation; }
            if (bodyVisual != null) bodyRotation = bodyVisual.localRotation;
            if (controller != null)
            {
                controller.Crashed += OnCrash;
                controller.Retried += ResetVisual;
            }
        }
        void OnDestroy()
        {
            if (controller == null) return;
            controller.Crashed -= OnCrash;
            controller.Retried -= ResetVisual;
        }
        void OnCrash() { crashed = true; crashTime = 0f; }
        void Update()
        {
            if (controller == null) return;
            if (crashed)
            {
                crashTime += Time.deltaTime;
                float angle = Mathf.Min(680f, crashTime * tumbleDegreesPerSecond);
                if (bodyVisual != null) bodyVisual.localRotation = bodyRotation * Quaternion.Euler(angle, 0f, 0f);
                if (leftSki != null)
                {
                    leftSki.localPosition = leftHome + new Vector3(-crashTime * 1.6f, crashTime * 0.6f, crashTime * 0.8f);
                    leftSki.localRotation = leftRotation * Quaternion.Euler(angle * 1.4f, angle * 0.7f, 0f);
                }
                if (rightSki != null)
                {
                    rightSki.localPosition = rightHome + new Vector3(crashTime * 1.4f, crashTime * 0.8f, -crashTime * 0.5f);
                    rightSki.localRotation = rightRotation * Quaternion.Euler(-angle, angle, 0f);
                }
                return;
            }
            // ArcadeCarvingVisual owns normal-play lean and aerial rotation.
            // This component owns only the crash tumble and detached skis.
        }
        public void ResetVisual()
        {
            crashed = false; crashTime = 0f;
            if (bodyVisual != null) bodyVisual.localRotation = bodyRotation;
            if (leftSki != null) { leftSki.localPosition = leftHome; leftSki.localRotation = leftRotation; }
            if (rightSki != null) { rightSki.localPosition = rightHome; rightSki.localRotation = rightRotation; }
        }
    }
}
