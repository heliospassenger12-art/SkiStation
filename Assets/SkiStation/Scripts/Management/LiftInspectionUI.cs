using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 0.70 - Simple specialist inspection panel.
/// Attach to a Canvas. It can inspect any selected lift GameObject and keeps
/// all actions read-only for this milestone.
/// </summary>
public class LiftInspectionUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Text titleText;
    [SerializeField] private Text metricsText;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        if (panel == null) panel = gameObject;
        if (refreshButton != null) refreshButton.onClick.AddListener(Refresh);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (LiftInspectionRuntime.Instance != null)
            LiftInspectionRuntime.Instance.Changed += OnChanged;
        OnChanged(LiftInspectionRuntime.Instance != null ? LiftInspectionRuntime.Instance.Snapshot : null);
    }

    private void OnDestroy()
    {
        if (LiftInspectionRuntime.Instance != null)
            LiftInspectionRuntime.Instance.Changed -= OnChanged;
    }

    public void Open(GameObject lift)
    {
        if (panel != null) panel.SetActive(true);
        if (LiftInspectionRuntime.Instance != null)
            LiftInspectionRuntime.Instance.SelectGameObject(lift);
    }

    public void Close()
    {
        if (LiftInspectionRuntime.Instance != null)
            LiftInspectionRuntime.Instance.ClearSelection();
        if (panel != null) panel.SetActive(false);
    }

    public void Refresh()
    {
        if (LiftInspectionRuntime.Instance != null)
            LiftInspectionRuntime.Instance.Refresh();
    }

    private void OnChanged(LiftInspectionRuntime.LiftInspectionSnapshot s)
    {
        if (titleText != null)
            titleText.text = s == null ? "Inspection remontée mécanique" : s.displayName;

        if (metricsText == null) return;
        if (s == null)
        {
            metricsText.text = "Aucune remontée sélectionnée.";
            return;
        }

        metricsText.text =
            $"Type : {s.type}\n" +
            $"État : {(s.faulted ? "PANNE" : (s.open ? "Ouverte" : "Fermée"))}\n" +
            $"Longueur : {s.length:0} m   Dénivelé : {s.verticalDrop:0} m\n" +
            $"Vitesse : {s.speed:0.0}\n" +
            $"Débit théorique : {s.theoreticalThroughput:0}/h\n" +
            $"Débit réel : {s.realThroughput:0}/h\n" +
            $"Passagers : {s.occupancy:0}   File : {s.queue:0}\n" +
            $"Attente estimée : {s.estimatedWaitMinutes:0.0} min\n" +
            $"Utilisation : {s.utilization * 100f:0}%   Pression : {s.pressure * 100f:0}%\n" +
            $"Pylônes : {s.pylonCount}   Véhicules : {s.vehicleCount}\n" +
            $"État technique : {s.condition01 * 100f:0}%\n" +
            $"Souterraine : {(s.underground ? "Oui" : "Non")}";
    }
}
