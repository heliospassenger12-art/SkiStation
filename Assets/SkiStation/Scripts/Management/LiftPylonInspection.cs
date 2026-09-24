using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 0.70 - Clickable technical probe for lift infrastructure.
/// It does not move or replace pylons yet; it only identifies a pylon and
/// exposes its transform/height/span data when available.
/// </summary>
public class LiftPylonInspection : MonoBehaviour
{
    [Serializable]
    public struct PylonSnapshot
    {
        public string name;
        public float altitude;
        public float height;
        public float estimatedLoad;
        public float condition01;
    }

    public PylonSnapshot Read(Component pylon)
    {
        PylonSnapshot s = new PylonSnapshot();
        if (pylon == null) return s;

        s.name = pylon.name;
        s.altitude = pylon.transform.position.y;
        s.height = ReadFloat(pylon, 0f, "height", "towerHeight", "Height");
        s.estimatedLoad = ReadFloat(pylon, 0f, "estimatedLoad", "load", "Load");
        s.condition01 = Mathf.Clamp01(ReadFloat(pylon, 1f, "condition01", "condition", "health01"));
        return s;
    }

    private static float ReadFloat(object target, float d, params string[] names)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var t = target.GetType();
        foreach (var n in names)
        {
            var p = t.GetProperty(n, flags);
            if (p != null && p.CanRead) try { return Convert.ToSingle(p.GetValue(target, null)); } catch { }
            var f = t.GetField(n, flags);
            if (f != null) try { return Convert.ToSingle(f.GetValue(target)); } catch { }
        }
        return d;
    }
}
