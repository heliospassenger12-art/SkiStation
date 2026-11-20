using System.Collections.Generic;
using UnityEngine;
using SkiStation;

/// <summary>
/// 2.05 - robust two-click lift construction flow with visible placement feedback.
/// Click 1 = bottom station, click 2 = top station, then recap before any payment/build.
/// </summary>
public partial class LiftConstructionPlanner : MonoBehaviour
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
    private Material markerMaterial, beamMaterial;
    private List<Vector3> pendingRoute;

    public LiftPylonPlacementMode PlacementMode => placementMode;
    public bool HasStations => hasBottom && hasTop;
    public bool IsPlacingPylons => placingPylons;
    public bool IsActive { get; private set; }
    public bool AwaitingConfirmation => awaitingConfirmation;
    public float PendingLength => HasStations ? Vector3.Distance(bottom, top) : 0f;
    public float PendingVerticalRise => HasStations ? Mathf.Abs(top.y - bottom.y) : 0f;
    public float PendingAverageSlope => PendingLength > .01f ? Mathf.Asin(Mathf.Clamp01(PendingVerticalRise / PendingLength)) * Mathf.Rad2Deg : 0f;
    public float PendingCost => LiftConstructionQuote391.Cost(definition,pendingRoute,PendingLength);
    public float PendingSpeedKmh => definition != null ? definition.speedKmh : 0f;
    public float PendingCapacityPerHour => definition != null ? (3600f / Mathf.Max(.5f, definition.spacingSeconds)) * Mathf.Max(1, definition.seatsPerVehicle) : 0f;
    public string PendingDisplayName => definition != null ? definition.displayName : "Remontee";
    public int PendingPylonCount { get {
        if (placementMode == LiftPylonPlacementMode.Manual && manualController != null) return manualController.Pylons.Count;
        return Mathf.Max(0, Mathf.RoundToInt(PendingLength / 120f) - 1);
    }}

    public void SetActive(bool value)
    {
        if(!value&&RecommendationDraft389!=null)ReleaseRecommendationContext389();
        if (value && !IsActive) ConstructionInput349.Activate(ConstructionTool349.Lift);
        IsActive = value;
        if (!value) { choosingModel = false; ResetPlacement(true); }
    }

    public void SetPlacementMode(LiftPylonPlacementMode mode)
    {
        if (placingPylons || awaitingConfirmation) return;
        placementMode = mode;
    }

    public void SetDefinition(SkiStation.LiftDefinition value)
    {
        if(RecommendationDraft389!=null){SetRecommendationDefinition389(value);return;}
        if (value != null && !SkiStation.LiftCareerCatalog249.IsUnlocked(value.id))
        {
            Debug.LogWarning("Remontée non débloquée au laboratoire : " + value.displayName);
            return;
        }
        if (awaitingConfirmation || hasBottom) ResetPlacement(true);
        definition = value;
        choosingModel = false;
        placementMessage = null;
    }

    private float EffectiveLengthLimit => definition == null ? 0f : SkiStation.LiftCareerCatalog249.EffectiveMaxLength(definition.id, definition.maxLineLengthMeters);

    public string PendingRestriction
    {
        get
        {
            if (definition == null) return "Aucun modele selectionne";
            if (!LiftCareerCatalog249.IsUnlocked(definition.id)) return "Modele a debloquer au laboratoire";
            if (!HasStations) return "Placez les deux gares";
            ConstructionRules349.LiftStations(bottom, top, EffectiveLengthLimit, out string reason);
            return reason;
        }
    }

    public bool CanConfirm349(out string reason)
        => ValidatePending349(true,out reason);

    bool ValidatePending349(bool checkBudget,out string reason)
    {
        reason = PendingRestriction;
        if (!IsActive || (!awaitingConfirmation && RecommendationDraft389==null) || reason.Length > 0) return false;
        if (!ConstructionPlacement349.ValidateSite(bottom, new Vector2(13f, 13f), out reason, true) ||
            !ConstructionPlacement349.ValidateSite(top, new Vector2(13f, 13f), out reason, true)) return false;
        bool surfaceLift = definition.type == LiftType.Platter || definition.type == LiftType.TBar || definition.type == LiftType.Fork || definition.type == LiftType.MagicCarpet;
        if (SkiStation.TerrainGeneration.ValAlpinTestScene.IsActive && !surfaceLift && placementMode == LiftPylonPlacementMode.QuickAuto &&
            automaticGenerator != null && !string.IsNullOrEmpty(automaticGenerator.LastGeometryReason391))
        { reason = "Tracé aérien refusé : " + automaticGenerator.LastGeometryReason391; return false; }
        if (pendingRoute == null || pendingRoute.Count < 2) { reason = "Trace incomplet"; return false; }
        if(!SkiStation.Altitude.AltitudeConstructionPolicy.ValidateRoute(pendingRoute,5,out reason,this,definition.type!=LiftType.Funicular))return false;
        for (int i = 0; i < pendingRoute.Count; i++)
        {
            Vector3 p = pendingRoute[i];
            if (!ConstructionRules349.Finite(p) || !ManagementSurface.TryGet(p, out var ground))
            { reason = "La ligne sort du terrain visible"; return false; }
            if (surfaceLift && Mathf.Abs(p.y - ground.y) > .5f) { reason = "Le terrain a change : modifiez la ligne"; return false; }
        }
        if (surfaceLift)
        {
            if (!SkiStation.TerrainGeneration.RealTerrainWater.AllowsRoute(pendingRoute,10f,out reason,false)) return false;
            if (!ConstructionPlacement349.RouteClearOfBuildings(pendingRoute, 5f, out reason)) return false;
            foreach (var slope in Object.FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None))
                if (ConstructionRules349.SurfaceLiftCrosses(slope.Points, slope.WidestWidth * .5f, pendingRoute))
                { reason = "Le teleski traverse une piste"; return false; }
        }
        reason = checkBudget?ConstructionPlacement349.BudgetReason(PendingCost):"";
        return reason.Length == 0;
    }

    public void Begin()
    {
        if(RecommendationDraft389!=null)ReleaseRecommendationContext389();
        ConstructionInput349.Activate(ConstructionTool349.Lift);
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

    // Use the same visible-ground picker as buildings and pistes.
    private bool TryGetTerrainPoint(out Vector3 point)
    {
        if (constructionCamera == null || !constructionCamera.isActiveAndEnabled) constructionCamera = Camera.main;
        return ConstructionInput349.TryGround(constructionCamera, out point);
    }

    private bool WithinLengthLimit(Vector3 candidate)
    {
        return !hasBottom || definition == null || Vector3.Distance(bottom, candidate) <= EffectiveLengthLimit + .01f;
    }

    // The capped beam shows the model's range. The station retains the exact click;
    // an out-of-range station is refused in the recap instead of being moved.
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
            if (!ManagementSurface.TryGet(sample, out sample)) { high = fraction; continue; }
            if (Vector3.Distance(bottom, sample) <= limit - .1f)
            { low = fraction; accepted = sample; }
            else high = fraction;
        }
        return accepted;
    }

    public bool PlaceStationFromMouse()
    {
        if (!IsActive || choosingModel || awaitingConfirmation || ConstructionInput349.PointerBlocked) return false;
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
        if(RecommendationDraft389!=null)return; // never enter the legacy real-pylon placement path for a draft
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
        if(RecommendationDraft389!=null){RecommendationConstructionRuntime389.I?.RequestReview();return;}
        if (!HasStations) return;
        // Show the recap even for an invalid length: the player must be able to modify/cancel.
        if (!string.IsNullOrEmpty(PendingRestriction)) placementMessage = PendingRestriction;
        if (!IsActive) return;
        if (manualController != null) manualController.Finish();
        pendingRoute = BuildPendingRoute349();
        placingPylons = false; awaitingConfirmation = true;
        if (SkiStation.TerrainGeneration.ValAlpinTestScene.IsActive && !CanConfirm349(out string refusal))
            Debug.LogWarning("[ValAlpin remontée] " + PendingDisplayName + " ; aval=" + bottom.ToString("F2") + " ; amont=" + top.ToString("F2") + " ; refus=" + refusal, this);
        ConstructionInput349.ConsumeClick();
        LiftConstructionConfirmationRuntime.Ensure().Open(this);
    }

    public void ConfirmPendingLift()
    {
        if(RecommendationDraft389!=null){RecommendationConstructionRuntime389.I?.Confirm(this);return;}
        if (!awaitingConfirmation) return;
        GameObject built = FinalizeLift();
        if (built == null) return;
        IsActive = false; ClearPreview();
        LiftConstructionConfirmationRuntime.I?.Close(); ConstructionInput349.ConsumeClick();
        ConstructionSelectionRuntime.I?.Select349(built.GetComponent<LiftRuntime>());
        PlayableLoopRuntime.I?.SetMessage("Remontee construite.");
    }

    public void ResumePendingLift()
    {
        if(RecommendationDraft389!=null){awaitingConfirmation=false;LiftConstructionConfirmationRuntime.I?.Close();RecommendationConstructionRuntime389.I?.LeaveReview();return;}
        if (!awaitingConfirmation) return;
        LiftConstructionConfirmationRuntime.I?.Close();
        // Modify means choose the two stations again. Nothing is charged.
        ResetPlacement(false); IsActive = true;
        Debug.Log("SkiStation 2.05: Lift placement modified - choose departure again.");
    }

    public void CancelPendingLift()
    {
        if(RecommendationDraft389!=null){RecommendationConstructionRuntime389.I?.CancelDraft();return;}
        LiftConstructionConfirmationRuntime.I?.Close();
        SetActive(false);
        Debug.Log("SkiStation 2.05: Lift construction cancelled - no charge.");
    }

    private List<Vector3> BuildPendingRoute349()
    {
        if (!HasStations || definition == null) return null;
        if (SkiStation.TerrainGeneration.ValAlpinTestScene.IsActive && automaticGenerator != null)
            automaticGenerator.ClearPlanningError391();
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
            bool surfaceLift = definition.type == LiftType.Platter || definition.type == LiftType.TBar || definition.type == LiftType.Fork || definition.type == LiftType.MagicCarpet;
            if (surfaceLift)
            {
                route.Clear(); int count = Mathf.Max(2, Mathf.CeilToInt(PendingLength / 12f));
                for (int i = 0; i <= count; i++)
                {
                    Vector3 p = Vector3.Lerp(bottom, top, i / (float)count);
                    route.Add(ManagementSurface.TryGet(p, out var ground) ? ground : new Vector3(float.NaN, 0, 0));
                }
            }
            else route = automaticGenerator.BuildCableRoute(bottom, top,definition);
        }

        return route;
    }

    public GameObject FinalizeLift()
    {
        if(RecommendationDraft389!=null)return null; // Imported drafts must pass the explicit 3.89 confirmation gate.
        if (!CanConfirm349(out string reason)) { placementMessage = reason; PlayableFeedbackRuntime.I?.Show(reason, 4f); return null; }
        if(!LiftGeometryIntegration391.Validate(definition,pendingRoute,this,out reason))
        {placementMessage=reason;PlayableFeedbackRuntime.I?.Show(reason,4f);return null;}
        if(SkiStation.ValAlpin.ValAlpinChairTerrainPreview.Applies(definition) &&
            !SkiStation.ValAlpin.ValAlpinChairTerrainPreview.ValidatePads(bottom,top,this,out reason))
        {placementMessage=reason;PlayableFeedbackRuntime.I?.Show(reason,4f);return null;}
        var route = pendingRoute;
        float confirmedCost391=PendingCost;
        SkiStation.ValAlpin.ValAlpinChairTerrainPreview.ConfirmedPads stationPads;
        try {stationPads=SkiStation.ValAlpin.ValAlpinChairTerrainPreview.BeginConfirmedPads(definition,route,this);}
        catch(System.Exception e)
        {placementMessage="Terrassement des gares refusé avant paiement : "+e.Message;PlayableFeedbackRuntime.I?.Show(placementMessage,4f);return null;}
        using(stationPads)
        {
            GameObject go;
            try {go=BuildInfrastructure389(definition,route,true,null,constructionRoot,true);}
            catch(System.Exception e){placementMessage="Construction refusée avant paiement : "+e.Message;PlayableFeedbackRuntime.I?.Show(placementMessage,4f);return null;}
            if (!StationRuntime.I.Spend(confirmedCost391, FinanceCategory.Construction, "lift_route", "Construction remontee"))
            {var stage=go.transform.parent;if(stage!=null)Destroy(stage.gameObject);else Destroy(go);return null;}
            awaitingConfirmation = false; pendingRoute = null;

            var lift = go.GetComponent<LiftRuntime>();
            var site376 = ConstructionSiteRuntime376.BeginLift(lift, confirmedCost391);
            stationPads?.Complete();
            PublishInfrastructure391(go,definition,route,constructionRoot,stationPads!=null);
            if (manualController != null) manualController.Commit349(go.transform);
            if (site376 != null)
                PlayableFeedbackRuntime.I?.Show("Chantier remontée lancé • " + Mathf.RoundToInt(site376.DurationGameMinutes) + " min de jeu", 4f);
            return go;
        }
    }

    private void Update()
    {
        if (!IsActive) return;
        if(RecommendationDraft389!=null){StepRecommendation389();return;}
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)) { SetActive(false); return; }
        if (awaitingConfirmation || choosingModel || ConstructionInput349.PointerBlocked) return;
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
        if(RecommendationDraft389!=null){RecommendationConstructionRuntime389.I?.DrawDraft();return;}
        if (choosingModel)
        {
            float w = Mathf.Min(470f, Screen.width - 32f);
            Rect panel = new Rect((Screen.width - w) * .5f, (Screen.height - 295f) * .5f, w, 295f);
            ConstructionInput349.PanelArea(this, "lift-models", panel);
            GUI.Box(panel, "CHOISIR UNE REMONTÉE");
            GUILayout.BeginArea(new Rect(panel.x + 16f, panel.y + 30f, panel.width - 32f, panel.height - 42f));
            GUILayout.Label("Choisis un modèle avant de placer les deux gares.");
            if (GUILayout.Button("Téléski classique — 1 place · 1 000 m max · 65 000 €", GUILayout.Height(39f)))
                SelectStartingModel("tow_classic");
            if (GUILayout.Button("Télésiège fixe 2 places — 1 500 m max · 125 000 €", GUILayout.Height(39f)))
                SelectStartingModel("chair_fixed_2");
            bool fixed4 = SkiStation.LiftCareerCatalog249.IsUnlocked("chair_fixed_4");
            GUI.enabled = fixed4;
            if (GUILayout.Button(fixed4 ? "Télésiège fixe 4 places — 1 100 m max · 240 000 €"
                                        : "Télésiège fixe 4 places — à débloquer (Recherche, palier 1)", GUILayout.Height(39f)))
                SelectStartingModel("chair_fixed_4");
            GUI.enabled = true;
            GUILayout.Label("Autres modèles : catalogue F8 (selon le mode et la recherche).");
            if (GUILayout.Button("ANNULER", GUILayout.Height(28f))) SetActive(false);
            GUILayout.EndArea();
            return;
        }
        string text = !hasBottom ? "REMONTÉE : cliquez sur la GARE DE DÉPART"
                    : !hasTop ? "REMONTÉE : départ placé ✓  — cliquez sur la GARE D’ARRIVÉE"
                    : "REMONTÉE : préparation du récapitulatif…";
        Rect r = new Rect((Screen.width - 560f) * .5f, 52f, 560f, 42f);
        ConstructionInput349.PanelArea(this, "lift-tool", new Rect(r.x, r.y, r.width, string.IsNullOrEmpty(placementMessage) ? r.height : 95f));
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
        if (markerMaterial == null) markerMaterial = RuntimeMaterialFactory.Create(new Color(1f,.48f,.04f,1f));
        var mr = marker.GetComponent<Renderer>(); if (mr != null) mr.sharedMaterial = markerMaterial;
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
        if (beamMaterial == null) beamMaterial = RuntimeMaterialFactory.Create(new Color(1f,.08f,.65f,1f));
        if (pr != null) pr.sharedMaterial = beamMaterial;
    }

    private void ResetPlacement(bool closeConfirmation)
    {
        hasBottom = false; hasTop = false; placingPylons = false; awaitingConfirmation = false; hasHover = false;
        if (closeConfirmation) LiftConstructionConfirmationRuntime.I?.Close();
        pendingRoute = null;
        if (manualController != null) { manualController.Finish(); manualController.Clear(); }
        ConstructionInput349.ConsumeClick();
        ClearPreview();
    }

    private void OnDestroy() { if (manualController != null) manualController.Clear(); ClearPreview(); }

    private void ClearPreview()
    {
        if (bottomMarker != null) Destroy(bottomMarker); bottomMarker = null;
        if (topMarker != null) Destroy(topMarker); topMarker = null;
        if (previewBeam != null) Destroy(previewBeam); previewBeam = null;
        if (markerMaterial != null) Destroy(markerMaterial); markerMaterial = null;
        if (beamMaterial != null) Destroy(beamMaterial); beamMaterial = null;
    }
}
