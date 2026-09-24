using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// Lightweight runtime readiness check for the first real Unity playtest.
    /// It never blocks gameplay; it only reports the state so missing bootstrap pieces are obvious.
    /// </summary>
    public sealed class PlayableStartupGuard : MonoBehaviour
    {
        private float nextCheck;
        private string status = "Initialisation…";
        private bool ready;

        private void Start()
        {
            Invoke(nameof(CheckNow), 1.0f);
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextCheck)
            {
                nextCheck = Time.unscaledTime + 5f;
                CheckNow();
            }
        }

        private void CheckNow()
        {
            bool cameraOk = Camera.main != null;
            bool mountainOk = MountainRuntime.I != null && MountainRuntime.I.IsGenerated;
            bool stationOk = StationRuntime.I != null;
            bool clockOk = GameClock.I != null;
            bool financeOk = FinanceRuntime.I != null;
            bool demandOk = StationVisitorDemandRuntime.I != null;
            bool starterOk = GameObject.Find("SkiStation_StarterDomain") != null;

            ready = cameraOk && mountainOk && stationOk && clockOk && financeOk && demandOk && starterOk;
            status = ready
                ? "READY — domaine jouable"
                : BuildMissingStatus(cameraOk, mountainOk, stationOk, clockOk, financeOk, demandOk, starterOk);
        }

        private string BuildMissingStatus(bool cameraOk, bool mountainOk, bool stationOk, bool clockOk,
            bool financeOk, bool demandOk, bool starterOk)
        {
            string missing = "À vérifier : ";
            if (!cameraOk) missing += "Caméra, ";
            if (!mountainOk) missing += "Montagne, ";
            if (!stationOk) missing += "Station, ";
            if (!clockOk) missing += "Horloge, ";
            if (!financeOk) missing += "Finance, ";
            if (!demandOk) missing += "Visiteurs, ";
            if (!starterOk) missing += "Domaine de départ, ";
            return missing.TrimEnd(' ', ',');
        }

        private void OnGUI()
        {
            GUI.color = ready ? Color.green : Color.yellow;
            GUI.Label(new Rect(12f, 12f, 520f, 24f), "SkiStation 1.59 • " + status);
            GUI.color = Color.white;
        }
    }
}
