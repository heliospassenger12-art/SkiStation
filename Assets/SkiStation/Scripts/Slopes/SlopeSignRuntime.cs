using UnityEngine;

namespace SkiStation
{
    public sealed class SlopeSignRuntime : MonoBehaviour
    {
        [SerializeField] private float sideOffset = 1.2f;
        [SerializeField] private float height = 1.8f;
        [SerializeField] private GameObject signPrefab;

        public void Place(SlopeRuntime slope)
        {
            if (slope == null || slope.Points.Count < 2) return;
            PlacePair(slope, 0); PlacePair(slope, slope.Points.Count - 1);
        }
        private void PlacePair(SlopeRuntime slope, int index)
        {
            Vector3 tangent = index == 0 ? slope.Points[1] - slope.Points[0] : slope.Points[index] - slope.Points[index - 1];
            tangent.y = 0f; tangent.Normalize(); Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
            Spawn(slope.Points[index] + side * (slope.GetWidthAt(index) * .5f + sideOffset));
            Spawn(slope.Points[index] - side * (slope.GetWidthAt(index) * .5f + sideOffset));
        }
        private void Spawn(Vector3 p)
        {
            var go = signPrefab != null ? Instantiate(signPrefab, p, Quaternion.identity) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "SlopeSign"; go.transform.position = p + Vector3.up * (height * .5f); go.transform.localScale = new Vector3(.12f, height, .12f);
        }
    }
}
