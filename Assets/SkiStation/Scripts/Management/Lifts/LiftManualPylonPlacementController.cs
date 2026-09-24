using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Technical implantation mode: place pylons one by one on the terrain and choose
/// their tower height. The list becomes the authored lift line.
/// </summary>
public class LiftManualPylonPlacementController : MonoBehaviour
{
    [SerializeField] private Camera constructionCamera;
    [SerializeField, Min(3f)] private float pylonHeight = 8f;
    [SerializeField, Min(0.05f)] private float rayDistance = 5000f;
    [SerializeField] private LayerMask terrainMask = ~0;

    private readonly List<LiftPylonRuntime> pylons = new List<LiftPylonRuntime>();
    private Transform parent;
    private bool active;

    public float PylonHeight => pylonHeight;
    public IReadOnlyList<LiftPylonRuntime> Pylons => pylons;

    public void Begin(Transform pylonParent)
    {
        parent = pylonParent;
        pylons.Clear();
        active = true;
    }

    public void SetPylonHeight(float height)
    {
        pylonHeight = Mathf.Clamp(height, 3f, 40f);
    }

    public void AdjustPylonHeight(float delta)
    {
        SetPylonHeight(pylonHeight + delta);
    }

    public void Finish()
    {
        active = false;
    }

    public void Clear()
    {
        foreach (var p in pylons)
            if (p != null) Destroy(p.gameObject);
        pylons.Clear();
    }

    private void Update()
    {
        if (!active) return;
        if (constructionCamera == null) constructionCamera = Camera.main;

        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
            AdjustPylonHeight(1f);
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
            AdjustPylonHeight(-1f);

        if (Input.GetMouseButtonDown(0))
        {
            if (constructionCamera == null) return;
            Ray ray = constructionCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, rayDistance, terrainMask)) return;

            GameObject go = new GameObject("Pylon_Manual_" + (pylons.Count + 1).ToString("00"));
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = hit.point;

            var pylon = go.AddComponent<LiftPylonRuntime>();
            pylon.SetTechnicalData(pylonHeight, 0f, 1f);
            pylons.Add(pylon);
        }

        if (Input.GetMouseButtonDown(1))
            Finish();
    }
}
