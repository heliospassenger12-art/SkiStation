using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 0.70 - Presentation-neutral inspection model for a single lift.
/// Uses reflection for optional LiftRuntime fields so the inspector remains
/// compatible while the underlying lift simulation evolves.
/// </summary>
public class LiftInspectionRuntime : MonoBehaviour
{
    [Serializable]
    public class LiftInspectionSnapshot
    {
        public string id;
        public string displayName;
        public string type;
        public bool open;
        public bool faulted;
        public bool underground;
        public float length;
        public float verticalDrop;
        public float speed;
        public float capacity;
        public float occupancy;
        public float queue;
        public float utilization;
        public float pressure;
        public float estimatedWaitMinutes;
        public float theoreticalThroughput;
        public float realThroughput;
        public int pylonCount;
        public int vehicleCount;
        public float condition01;
    }

    public static LiftInspectionRuntime Instance { get; private set; }

    [SerializeField] private float refreshSeconds = 1f;
    private float nextRefresh;
    private object selectedLift;
    private LiftInspectionSnapshot snapshot;

    public LiftInspectionSnapshot Snapshot => snapshot;
    public bool HasSelection => selectedLift != null;

    public event Action<LiftInspectionSnapshot> Changed;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (selectedLift == null || Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + Mathf.Max(0.2f, refreshSeconds);
        RebuildSnapshot();
    }

    public bool Select(object lift)
    {
        if (lift == null) return false;
        selectedLift = lift;
        RebuildSnapshot();
        return true;
    }

    public bool SelectComponent(Component component)
    {
        return Select(component != null ? component.gameObject : null);
    }

    public bool SelectGameObject(GameObject go)
    {
        if (go == null) return false;
        selectedLift = go;
        RebuildSnapshot();
        return true;
    }

    public void ClearSelection()
    {
        selectedLift = null;
        snapshot = null;
        Changed?.Invoke(null);
    }

    public void Refresh()
    {
        if (selectedLift != null) RebuildSnapshot();
    }

    private void RebuildSnapshot()
    {
        snapshot = new LiftInspectionSnapshot();
        snapshot.displayName = ReadString(selectedLift, "displayName", "name", "Name") ?? "Remontée";
        snapshot.id = ReadString(selectedLift, "id", "Id", "identifier") ?? snapshot.displayName;
        snapshot.type = ReadString(selectedLift, "type", "liftType", "Type") ?? "Lift";

        snapshot.open = ReadBool(selectedLift, true, "isOpen", "IsOpen", "open");
        snapshot.faulted = ReadBool(selectedLift, false, "faulted", "isFaulted", "Faulted");
        snapshot.underground = ReadBool(selectedLift, false, "underground", "isUnderground", "Underground");

        snapshot.length = ReadFloat(selectedLift, 0f, "length", "lineLength", "Length");
        snapshot.verticalDrop = ReadFloat(selectedLift, 0f, "verticalDrop", "verticalDifference", "VerticalDrop");
        snapshot.speed = ReadFloat(selectedLift, 0f, "speed", "currentSpeed", "Speed");
        snapshot.capacity = ReadFloat(selectedLift, 0f, "capacity", "hourlyCapacity", "Capacity");
        snapshot.occupancy = ReadFloat(selectedLift, 0f, "occupancy", "passengers", "currentPassengers");
        snapshot.queue = ReadFloat(selectedLift, 0f, "queue", "queueCount", "queuedPassengers");
        snapshot.utilization = Mathf.Clamp01(ReadFloat(selectedLift, 0f, "utilization", "utilization01"));
        snapshot.pressure = Mathf.Clamp01(ReadFloat(selectedLift, 0f, "pressure", "pressure01"));
        snapshot.estimatedWaitMinutes = ReadFloat(selectedLift, 0f, "estimatedWaitMinutes", "waitMinutes");
        snapshot.theoreticalThroughput = ReadFloat(selectedLift, 0f, "theoreticalThroughput", "theoreticalCapacity");
        snapshot.realThroughput = ReadFloat(selectedLift, 0f, "realThroughput", "actualThroughput");
        snapshot.pylonCount = Mathf.Max(0, ReadInt(selectedLift, 0, "pylonCount", "PylonCount"));
        snapshot.vehicleCount = Mathf.Max(0, ReadInt(selectedLift, 0, "vehicleCount", "VehicleCount"));
        snapshot.condition01 = Mathf.Clamp01(ReadFloat(selectedLift, 1f, "condition01", "condition", "health01"));

        Changed?.Invoke(snapshot);
    }

    private static object ReadMember(object target, params string[] names)
    {
        if (target == null) return null;
        if (target is GameObject go)
        {
            foreach (var n in names)
            {
                var c = go.GetComponent(n);
                if (c != null) return c;
            }
        }

        var t = target.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (var n in names)
        {
            var p = t.GetProperty(n, flags);
            if (p != null && p.CanRead) try { return p.GetValue(target, null); } catch { }
            var f = t.GetField(n, flags);
            if (f != null) try { return f.GetValue(target); } catch { }
        }
        return null;
    }

    private static string ReadString(object o, params string[] n)
    {
        var v = ReadMember(o, n);
        return v?.ToString();
    }

    private static bool ReadBool(object o, bool d, params string[] n)
    {
        var v = ReadMember(o, n);
        return v is bool b ? b : d;
    }

    private static int ReadInt(object o, int d, params string[] n)
    {
        var v = ReadMember(o, n);
        try { return v == null ? d : Convert.ToInt32(v); } catch { return d; }
    }

    private static float ReadFloat(object o, float d, params string[] n)
    {
        var v = ReadMember(o, n);
        try { return v == null ? d : Convert.ToSingle(v); } catch { return d; }
    }
}
