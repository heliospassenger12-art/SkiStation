using UnityEngine;

namespace SkiStation
{
    /// <summary>Small runtime data component attached to a tow corner.</summary>
    public class LiftDirectionChangeMarker : MonoBehaviour
    {
        public float TurnAngle { get; set; }
        public bool IsSharpTurn => Mathf.Abs(TurnAngle) > 25f;
    }
}
