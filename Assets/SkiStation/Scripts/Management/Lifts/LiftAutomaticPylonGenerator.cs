using System.Collections.Generic;
using UnityEngine;
using SkiStation;

/// <summary>
/// Fast lift-line implantation: station-to-station route with pylons automatically
/// distributed and their tower height adapted to sampled terrain.
/// </summary>
public class LiftAutomaticPylonGenerator : MonoBehaviour
{
    [SerializeField, Min(20f)] private float targetSpan = 55f;
    [SerializeField, Min(3f)] private float minimumHeight = 7f;
    [SerializeField, Min(0.5f)] private float cableClearance = 5f;
    [SerializeField, Min(0f)] private float baseOffset = 0.15f;
    [SerializeField, Min(1f)] private float anchorHeight = 10f;

    public float TargetSpan => targetSpan;

    public List<Vector3> BuildCableRoute(Vector3 bottom, Vector3 top)
    {
        int count = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(
            new Vector3(bottom.x, 0f, bottom.z),
            new Vector3(top.x, 0f, top.z)) / Mathf.Max(1f, targetSpan)));
        var route = new List<Vector3>(count + 1);
        TerrainQuery terrain = FindObjectOfType<TerrainQuery>();

        for (int i = 0; i <= count; i++)
        {
            float t = i / (float)count;
            Vector3 p = Vector3.Lerp(bottom, top, t);
            float terrainY = p.y;
            if (terrain != null && terrain.TrySample(p, out var terrainPoint, out _, out _))
                terrainY = terrainPoint.y + baseOffset;

            float nominalCableY = Mathf.Lerp(bottom.y + anchorHeight, top.y + anchorHeight, t);
            p.y = Mathf.Max(nominalCableY, terrainY + minimumHeight + cableClearance);
            route.Add(p);
        }
        return route;
    }

    public List<LiftPylonRuntime> GeneratePylons(Transform parent, Vector3 bottom, Vector3 top)
    {
        var cableRoute = BuildCableRoute(bottom, top);
        var result = new List<LiftPylonRuntime>();
        TerrainQuery terrain = FindObjectOfType<TerrainQuery>();

        for (int i = 1; i < cableRoute.Count - 1; i++)
        {
            Vector3 cable = cableRoute[i];
            Vector3 basePoint = cable;
            float terrainY = basePoint.y - minimumHeight - cableClearance;
            if (terrain != null && terrain.TrySample(basePoint, out var terrainPoint, out _, out _))
                terrainY = terrainPoint.y + baseOffset;

            float height = Mathf.Max(minimumHeight, cable.y - terrainY);
            GameObject go = new GameObject("Pylon_Auto_" + i.ToString("00"));
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(cable.x, terrainY, cable.z);

            var pylon = go.AddComponent<LiftPylonRuntime>();
            pylon.SetTechnicalData(height, 0f, 1f);
            result.Add(pylon);
        }
        return result;
    }
}
