using System.Collections.Generic;
using UnityEngine;
using SkiStation;

/// <summary>
/// 2.05 - robust two-click lift construction flow with visible placement feedback.
/// Click 1 = bottom station, click 2 = top station, then recap before any payment/build.
/// </summary>
public class LiftConstructionPlanner : MonoBehaviour
{
    [SerializeField] private Camera constructionCamera;
    [SerializeField] private GameObject liftPrefab;
    [SerializeField] private LiftAutomaticPylonGenerator automaticGenerator;
    [SerializeField] private LiftManualPylonPlacementController manualController;
    [SerializeField] private SkiStation.LiftDefinition definition;
    [SerializeField] private LiftPylonPlacementMode placementMode = LiftPylonPlacementMode.QuickAuto;
    [SerializeField] private Transform constructionRoot;

    private Vector3 bottom;
    private Vector3 top;
    private bool hasBottom;
    private bool hasTop;
    private bool placingPylons;
    private bool awaitingConfirmation;
    private GameObject bottomMarker;
    private GameObject topMarker;
    private GameObject previewBeam;
    private Vector3 hoverPoint;
    private bool hasHover;
    private bool choosingModel;
    private string placementMessage;

    public LiftPylonPlacementMode PlacementMode => placementMode;
    public bool HasStations => hasBottom && hasTop;
    public bool IsPlacingPylons => placingPylons;
    public bool IsActive { get; private set; }
    public bool AwaitingConfirmation => awaitingConfirmation;
    public float PendingLength => HasStations ? Vector3.Distance(bottom, top) : 0f;
    public float PendingVerticalRise => HasStations ? Mathf.Abs(top.y - bottom.y) : 0f;
    public float PendingAverageSlope => PendingLength > .01f ? Mathf.Asin(Mathf.Clamp01(PendingVerticalRise / PendingLength)) * Mathf.Rad2Deg : 0f;
    public float PendingCost => definition != null ? definition.buildCost : 0f;
    public float PendingSpeedKmh => definition != null ? definition.speedKmh : 0f;
    public float PendingCapacityPerHour => definition != null ? (3600f / Mathf.Max(.5f, definition.spacingSeconds)) * Mathf.Max(1, definition.seatsPerVehicle) : 0f;
    public string PendingDisplayName => definition != null ? definition.displayName : "Remontee";
    public int PendingPylonCount { get {
        if (placementMode == LiftPylonPlacementMode.Manual && manualController != null) return manualController.Pylons.Count;
        return Mathf.Max(0, Mathf.RoundToInt(PendingLength / 120f) - 1);
    }}

    public void SetActive(bool value)
    {
        IsActive = value;
        if (!value) { choosingModel = false; ResetPlacement(true); }
    }

    public void SetPlacementMode(LiftPylonPlacementMode mode)
    {
        if (placingPylons) return;
        placementMode = mode;
    }

    public void SetDefinition(SkiStation.LiftDefinition value)
    {
        if (value != null && !SkiStation.LiftCareerCatalog249.IsUnlocked(value.id))
        {
            Debug.LogWarning("Remontée non débloquée au laboratoire : " + value.displayName);
            return;
        }
        definition = value;
        choosingModel = false;
        placementMessage = null;
    }

    private float EffectiveLengthLimit => definition == null ? 0f : SkiStation.LiftCareerCatalog249.EffectiveMaxLength(definition.id, definition.maxLineLengthMeters);

    public string PendingRestriction => definition == null ? "Aucun modèle sélectionné"
        : !SkiStation.LiftCareerCatalog249.IsUnlocked(definition.id) ? "Modèle à débloquer au laboratoire"
        : PendingLength > EffectiveLengthLimit ? "Longueur maximale : " + EffectiveLengthLimit.ToString("N0") + " m"
        : string.Empty;

    public void Begin()
    {
        ResetPlacement(true);
        IsActive = true;
        // Always present the model selector for a new construction, even if a default model exists.
        choosingModel = true;
        Debug.Log("SkiStation 2.05: Lift placement ACTIVE - click departure then arrival.");
    }

    public void SetStations(Vector3 bottomStation, Vector3 topStation)
    {
        bottom = bottomStation; top = topStation; hasBottom = true; hasTop = true;
        RefreshMarkers();
    }

    // The visible massif is a heightfield. Resolve the mouse ray against that
    // heightfield, not against the legacy prototype or unrelated scene colliders.
    // A bracketed ray/height intersection also works if a MeshCollider has not
    // finished cooking, and preserves the actual clicked X/Z coordinates.
    private bool TryGetTerrainPoint(out Vector3 point)
    {
        point = default;
        if (constructionCamera == null || !constructionCamera.isActiveAndEnabled)
            constructionCamera = Camera.main;
        if (constructionCamera == null) return false;
        Vector3 mouse = Input.mousePosition;
        if (!constructionCamera.pixelRect.Contains(new Vector2(mouse.x, mouse.y))) return false;
        Ray ray = constructionCamera.ScreenPointToRay(mouse);
        var visible = GameObject.Find("SkiStation_MASSIF_01");
        if (visible != null && visible.activeInHierarchy)
        {
            // First use the actual visible mesh. This is a collider-specific raycast,
            // so buildings, vegetation and the old mountain cannot intercept it.
            var collider = visible.GetComponent<MeshCollider>();
            if (collider != null && collider.enabled && collider.sharedMesh != null &&
                collider.Raycast(ray, out var hit, 30000f))
            {
                point = hit.point;
                return true;
            }
            // Ray-heightfield intersection: a reliable fallback for the same massif.
            // Never clamp X/Z to the edge: a click outside the massif must fail.
            var mesh = visible.GetComponent<MeshFilter>();
            if (mesh != null && mesh.sharedMesh != null)
            {
                Bounds bounds = mesh.sharedMesh.bounds;
                Vector3 min = visible.transform.TransformPoint(bounds.min);
                Vector3 max = visible.transform.TransformPoint(bounds.max);
                float xMin = Mathf.Min(min.x, max.x), xMax = Mathf.Max(min.x, max.x);
                float zMin = Mathf.Min(min.z, max.z), zMax = Mathf.Max(min.z, max.z);
                const float step = 8f;
                float previousT = 0f;
                Vector3 previous = ray.GetPoint(previousT);
                float previousDelta = previous.y - SkiStationGuaranteedMountainVisual.Height(previous.x, previous.z);
                for (float t = step; t <= 30000f; t += step)
                {
                    Vector3 current = ray.GetPoint(t);
                    if (current.x < xMin || current.x > xMax || current.z < zMin || current.z > zMax)
                    {
                        previousT = t;
                        previousDelta = current.y - SkiStationGuaranteedMountainVisual.Height(current.x, current.z);
                        continue;
                    }
                    float delta = current.y - SkiStationGuaranteedMountainVisual.Height(current.x, current.z);
                    if (previousDelta >= 0f && delta <= 0f)
                    {
                        float low = previousT, high = t;
                        for (int i = 0; i < 15; i++)
                        {
                            float mid = (low + high) * .5f;
                            Vector3 sample = ray.GetPoint(mid);
                            if (sample.y > SkiStationGuaranteedMountainVisual.Height(sample.x, sample.z)) low = mid;
                            else high = mid;
                        }
                        Vector3 candidate = ray.GetPoint((low + high) * .5f);
                        if (candidate.x >= xMin && candidate.x <= xMax && candidate.z >= zMin && candidate.z <= zMax)
                        {
                            point = new Vector3(candidate.x, SkiStationGuaranteedMountainVisual.Height(candidate.x, candidate.z), candidate.z);
                            return true;
                        }
                    }
                    previousT = t;
                    previousDelta = delta;
                }
            }
            // Do not silently fall back to the different, invisible prototype.
            return false;
        }
        var mountain = SkiStation.MountainRuntime.I;
        if (mountain != null && mountain.IsGenerated)
        {
            var collider = mountain.GetComponent<MeshCollider>();
            if (collider != null && collider.enabled && collider.Raycast(ray, out var hit, 30000f))
            { point = hit.point; return true; }
        }
        return false;
    }

    private bool WithinLengthLimit(Vector3 candidate)
    {
        return !hasBottom || definition == null || Vector3.Distance(bottom, candidate) <= EffectiveLengthLimit + .01f;
    }

    // The same point is used for the purple preview AND for the arrival click.
    // A distant click selects the visible end of the line instead of silently
    // rejecting it. Reproject to the massif so the station stays on the snow.
    private Vector3 CappedPreviewPoint(Vector3 candidate)
    {
        if (!hasBottom || definition == null || WithinLengthLimit(candidate)) return candidate;
        float limit = EffectiveLengthLimit;
        if (limit <= 0f) return bottom;
        float low = 0f, high = 1f;
        Vector3 accepted = bottom;
        for (int i = 0; i < 22; i++)
        {
            float fraction = (low + high) * .5f;
            Vector3 sample = Vector3.Lerp(bottom, candidate, fraction);
            sample.y = SkiStationGuaranteedMountainVisual.Height(sample.x, sample.z);
            if (Vector3.Distance(bottom, sample) <= limit - .1f)
            { low = fraction; accepted = sample; }
            else high = fraction;
        }
        return accepted;
    }

    public bool PlaceStationFromMouse()
    {
        if (!TryGetTerrainPoint(out var p)) { placementMessage = "Aucun terrain sous le curseur : rapprochez la camera ou visez le massif."; return false; }
        if (!hasBottom)
        {
            bottom = p; hasBottom = true; placementMessage = null; CreateOrMoveMarker(ref bottomMarker, "Lift_Depart_Preview", bottom, 10f);
            Debug.Log("SkiStation Phase 60: Gare depart = " + bottom.ToString("F1") + " / souris = " + Input.mousePosition);
            return true;
        }
        if (!hasTop)
        {
            bool wasBeyondRange = !WithinLengthLimit(p);
            // Always retain the exact terrain click. The purple beam may be capped
            // during hover, but it must never silently move the chosen station.
            // The recap explicitly reports an overlength route and blocks payment.
            top = p; hasTop = true;
            placementMessage = wasBeyondRange ? "Arrivée sélectionnée au clic, mais hors portée : choisissez Modifier et rapprochez-la." : null;
            CreateOrMoveMarker(ref topMarker, "Lift_Arrival_Preview", top, 10f);
            RefreshPreviewBeam(bottom, top);
            Debug.Log("SkiStation Phase 60: Gare arrivee = " + top.ToString("F1") + " / depart = " + bottom.ToString("F1") + " / souris = " + Input.mousePosition);
            return true;
        }
        return false;
    }

    public void StartPylonPhase()
    {
        if (!HasStations) return;
        placingPylons = true;
        Transform root = constructionRoot != null ? constructionRoot : transform;
        if (placementMode == LiftPylonPlacementMode.Manual)
        {
            if (manualController == null) manualController = gameObject.AddComponent<LiftManualPylonPlacementController>();
            manualController.Begin(root);
        }
        else
        {
            if (automaticGenerator == null) automaticGenerator = gameObject.AddComponent<LiftAutomaticPylonGenerator>();
        }
    }

    public void RequestConfirmation()
    {
        if (!HasStations) return;
        // Show the recap even for an invalid length: the player must be able to modify/cancel.
        if (!string.IsNullOrEmpty(PendingRestriction)) placementMessage = PendingRestriction;
        placingPylons = false; awaitingConfirmation = true;
        LiftConstructionConfirmationRuntime.Ensure().Open(this);
    }

    public void ConfirmPendingLift()
    {
        if (!awaitingConfirmation) return;
        awaitingConfirmation = false; LiftConstructionConfirmationRuntime.I?.Close();
        GameObject built = FinalizeLift();
        if (built != null) { IsActive = false; ClearPreview(); Debug.Log("SkiStation 2.05: Lift construction CONFIRMED and built."); }
        else { awaitingConfirmation = true; LiftConstructionConfirmationRuntime.Ensure().Open(this); }
    }

    public void ResumePendingLift()
    {
        if (!awaitingConfirmation) return;
        LiftConstructionConfirmationRuntime.I?.Close();
        // Modify means choose the two stations again. Nothing is charged.
        ResetPlacement(false); IsActive = true;
        Debug.Log("SkiStation 2.05: Lift placement modified - choose departure again.");
    }

    public void CancelPendingLift()
    {
        LiftConstructionConfirmationRuntime.I?.Close();
        SetActive(false);
        Debug.Log("SkiStation 2.05: Lift construction cancelled - no charge.");
    }

    public GameObject FinalizeLift()
    {
        if (!HasStations || definition == null || !string.IsNullOrEmpty(PendingRestriction)) return null;
        var route = new List<Vector3> { bottom, top };
        if (placementMode == LiftPylonPlacementMode.Manual && manualController != null && manualController.Pylons.Count > 0)
        {
            route.Clear(); route.Add(bottom);
            foreach (var p in manualController.Pylons) if (p != null) route.Add(p.transform.position + Vector3.up * p.TowerHeight);
            route.Add(top);
        }
        else if (placementMode == LiftPylonPlacementMode.QuickAuto)
        {
            if (automaticGenerator == null) automaticGenerator = gameObject.AddComponent<LiftAutomaticPylonGenerator>();
            route = automaticGenerator.BuildCableRoute(bottom, top);
        }

        float cost = definition.buildCost;
        if (StationRuntime.I != null && !StationRuntime.I.Spend(cost, FinanceCategory.Construction, "lift_route", "Lift construction"))
            return null;

        GameObject go = liftPrefab != null ? Instantiate(liftPrefab, Vector3.zero, Quaternion.identity) : new GameObject(definition.displayName);
        var lift = go.GetComponent<SkiStation.LiftRuntime>() ?? go.AddComponent<SkiStation.LiftRuntime>();
        lift.Build(definition, route, false);
        // Small independent station pads; do not constrain the mountain between them.
        ConstructionTerrainAdaptation251.Flatten(bottom, 13f, 13f, 4f);
        ConstructionTerrainAdaptation251.Flatten(top, 13f, 13f, 4f);
        ConstructionClearance249.ClearTrees(new Vector3[] { bottom }, 12f);
        ConstructionClearance249.ClearTrees(new Vector3[] { top }, 12f);
        ConstructionClearance249.ClearTrees(route, definition.type == LiftType.Platter || definition.type == LiftType.TBar || definition.type == LiftType.Fork ? 5f : 3f);
        (go.GetComponent<LiftPlayableVisibilityRuntime>() ?? go.AddComponent<LiftPlayableVisibilityRuntime>()).Build(route);
        if (manualController != null) manualController.Finish();
        return go;
    }

    private void Update()
    {
        if (!IsActive || awaitingConfirmation || choosingModel) return;
        hasHover = TryGetTerrainPoint(out hoverPoint);
        if (hasBottom && !hasTop && hasHover)
        {
            RefreshPreviewBeam(bottom, CappedPreviewPoint(hoverPoint));
            placementMessage = WithinLengthLimit(hoverPoint) ? null
                : "HORS PORTÉE : " + Vector3.Distance(bottom, hoverPoint).ToString("N0")
                  + " m / maximum " + EffectiveLengthLimit.ToString("N0")
                  + " m. Le trait violet montre la limite, pas votre clic.";
        }

        if (Input.GetKeyDown(KeyCode.Escape)) { SetActive(false); return; }
        Vector2 mouseGui = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        if (Input.GetMouseButtonDown(0) && !PlayableGameHUD.PointerOverCameraControls
            // Phase 61: the model-selection panel is not visible during placement.
            // The former unconditional 500x310 centre-screen exclusion silently
            // discarded clicks on most of the visible mountain (both stations).
            && !new Rect(0, 0, Screen.width, 112f).Contains(mouseGui)
            && !new Rect(0, Screen.height - 105f, Screen.width, 105f).Contains(mouseGui))
        {
            if (PlaceStationFromMouse() && HasStations)
                RequestConfirmation();
        }
    }

    private void OnGUI()
    {
        if (!IsActive || awaitingConfirmation) return;
        if (choosingModel)
        {
            float w = Mathf.Min(470f, Screen.width - 32f);
            Rect panel = new Rect((Screen.width - w) * .5f, (Screen.height - 250f) * .5f, w, 250f);
            GUI.Box(panel, "CHOISIR UNE REMONTÉE");
            GUILayout.BeginArea(new Rect(panel.x + 16f, panel.y + 30f, panel.width - 32f, panel.height - 42f));
            GUILayout.Label("Choisis un modèle avant de placer les deux gares.");
            if (GUILayout.Button("Téléski classique — 1 place · 1 000 m max · 65 000 €", GUILayout.Height(39f)))
                SelectStartingModel("tow_classic");
            if (GUILayout.Button("Télésiège fixe 2 places — 1 500 m max · 125 000 €", GUILayout.Height(39f)))
                SelectStartingModel("chair_fixed_2");
            GUILayout.Label("Autres modèles : catalogue F8 (selon le mode et la recherche).");
            if (GUILayout.Button("ANNULER", GUILayout.Height(28f))) SetActive(false);
            GUILayout.EndArea();
            return;
        }
        string text = !hasBottom ? "REMONTÉE : cliquez sur la GARE DE DÉPART"
                    : !hasTop ? "REMONTÉE : départ placé ✓  — cliquez sur la GARE D’ARRIVÉE"
                    : "REMONTÉE : préparation du récapitulatif…";
        Rect r = new Rect((Screen.width - 560f) * .5f, 52f, 560f, 42f);
        GUI.Box(r, text + "   |   Échap : annuler");
        if (!string.IsNullOrEmpty(placementMessage)) GUI.Box(new Rect(r.x, r.y + 45f, r.width, 48f), placementMessage + " — Échap pour annuler, ou recommencer le tracé (L).");
    }

    private void SelectStartingModel(string id)
    {
        if (!SkiStation.LiftCareerCatalog249.IsUnlocked(id)) return;
        var selected = ScriptableObject.CreateInstance<SkiStation.LiftDefinition>();
        SkiStation.LiftCareerCatalog249.Configure(selected, id);
        SetDefinition(selected);
    }

    private void RefreshMarkers()
    {
        if (hasBottom) CreateOrMoveMarker(ref bottomMarker, "Lift_Depart_Preview", bottom, 10f);
        if (hasTop) CreateOrMoveMarker(ref topMarker, "Lift_Arrival_Preview", top, 10f);
        if (HasStations) RefreshPreviewBeam(bottom, top);
    }

    private void CreateOrMoveMarker(ref GameObject marker, string markerName, Vector3 p, float size)
    {
        if (marker == null)
        {
            marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = markerName; marker.transform.SetParent(transform, true);
            var col = marker.GetComponent<Collider>(); if (col != null) col.enabled = false;
        }
        marker.transform.position = p + Vector3.up * (size * .55f);
        marker.transform.localScale = Vector3.one * (size * 2.4f);
        var mr = marker.GetComponent<Renderer>(); if (mr != null) mr.sharedMaterial = RuntimeMaterialFactory.Create(new Color(1f,.48f,.04f,1f));
    }

    private void RefreshPreviewBeam(Vector3 a, Vector3 b)
    {
        if (previewBeam == null)
        {
            previewBeam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            previewBeam.name = "Lift_Line_Preview"; previewBeam.transform.SetParent(transform, true);
            var col = previewBeam.GetComponent<Collider>(); if (col != null) col.enabled = false;
        }
        Vector3 aa = a + Vector3.up * 8f, bb = b + Vector3.up * 8f;
        Vector3 d = bb - aa; float len = d.magnitude;
        if (len < .01f) { previewBeam.SetActive(false); return; }
        previewBeam.SetActive(true); previewBeam.transform.position = (aa + bb) * .5f;
        previewBeam.transform.up = d.normalized; previewBeam.transform.localScale = new Vector3(5.5f, len * .5f, 5.5f);
        var pr = previewBeam.GetComponent<Renderer>();
        if (pr != null)
        {
            var sh = Shader.Find("Unlit/Color");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            if (sh != null) { var mat = new Material(sh); if (mat.HasProperty("_Color")) mat.color = new Color(1f,.08f,.65f,1f); pr.sharedMaterial = mat; }
            else pr.sharedMaterial = RuntimeMaterialFactory.Create(new Color(1f,.08f,.65f,1f));
        }
    }

    private void ResetPlacement(bool closeConfirmation)
    {
        hasBottom = false; hasTop = false; placingPylons = false; awaitingConfirmation = false; hasHover = false;
        if (closeConfirmation) LiftConstructionConfirmationRuntime.I?.Close();
        if (manualController != null) manualController.Finish();
        ClearPreview();
    }

    private void ClearPreview()
    {
        if (bottomMarker != null) Destroy(bottomMarker); bottomMarker = null;
        if (topMarker != null) Destroy(topMarker); topMarker = null;
        if (previewBeam != null) Destroy(previewBeam); previewBeam = null;
    }
}
