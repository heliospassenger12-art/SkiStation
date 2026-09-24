using System.Collections.Generic;
using UnityEngine;

public class LiftPylonLinePlan
{
    public Vector3 BottomStation;
    public Vector3 TopStation;
    public LiftPylonPlacementMode Mode;
    public readonly List<LiftPylonRuntime> Pylons = new List<LiftPylonRuntime>();

    public void Clear()
    {
        Pylons.Clear();
    }
}
