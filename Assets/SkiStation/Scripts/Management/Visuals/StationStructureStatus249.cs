using UnityEngine;

namespace SkiStation
{
    // Management status for constructed buildings. Orange is reserved for maintenance.
    public sealed class StationStructureStatus249 : MonoBehaviour
    {
        public enum State { Open, Closed, Intervention }
        public State Current = State.Open;
        public bool CanWelcomeVisitors => Current == State.Open;
        public Color StatusColor => Current == State.Open ? Color.green :
            Current == State.Closed ? Color.red : new Color(1f, .55f, 0f);
        public void SetOpen(bool open)
        {
            if (Current == State.Intervention) return;
            Current = open ? State.Open : State.Closed;
            // Keep the legacy shop flag synchronized with the universal status.
            var shop = GetComponent<StationEquipmentShop249>();
            if (shop != null) shop.IsOpen = open;
        }
        public void RequireIntervention() { Current = State.Intervention; }
    }
}
