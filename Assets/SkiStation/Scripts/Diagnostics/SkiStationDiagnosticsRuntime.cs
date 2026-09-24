using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum SkiStationDiagnosticSeverity { Info, Warning, Error }

    [Serializable]
    public struct SkiStationDiagnosticEntry
    {
        public SkiStationDiagnosticSeverity severity;
        public string code;
        public string message;

        public SkiStationDiagnosticEntry(SkiStationDiagnosticSeverity severity, string code, string message)
        {
            this.severity = severity;
            this.code = code;
            this.message = message;
        }
    }

    public sealed class SkiStationDiagnosticsRuntime : MonoBehaviour
    {
        public static SkiStationDiagnosticsRuntime I { get; private set; }
        public IReadOnlyList<SkiStationDiagnosticEntry> Entries => entries;
        public bool HasErrors { get; private set; }
        public bool HasWarnings { get; private set; }

        private readonly List<SkiStationDiagnosticEntry> entries = new List<SkiStationDiagnosticEntry>();

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }
            I = this;
            DontDestroyOnLoad(gameObject);
            RunStartupDiagnostics();
        }

        public void Clear()
        {
            entries.Clear();
            HasErrors = false;
            HasWarnings = false;
        }

        public void Add(SkiStationDiagnosticSeverity severity, string code, string message)
        {
            entries.Add(new SkiStationDiagnosticEntry(severity, code, message));
            if (severity == SkiStationDiagnosticSeverity.Error) HasErrors = true;
            if (severity == SkiStationDiagnosticSeverity.Warning) HasWarnings = true;
        }

        public void RunStartupDiagnostics()
        {
            Clear();
            Add(SkiStationDiagnosticSeverity.Info, "CORE-001", "SkiStation runtime initialisé — version " + SkiStationVersion.Current + ".");
            Add(SkiStationDiagnosticSeverity.Info, "CORE-002", "Cible Unity : " + SkiStationVersion.ProjectTarget + ".");

            if (SkiStationRuntimeRegistry.I == null)
                Add(SkiStationDiagnosticSeverity.Error, "CORE-003", "Le registre runtime n'est pas disponible.");
            else
                Add(SkiStationDiagnosticSeverity.Info, "CORE-004", "Registre runtime disponible.");

            if (Camera.main == null)
                Add(SkiStationDiagnosticSeverity.Warning, "SCENE-001", "Aucune caméra MainCamera n'est présente dans la scène active.");
            else
                Add(SkiStationDiagnosticSeverity.Info, "SCENE-002", "Caméra principale détectée.");

            if (FindObjectsByType<Light>(FindObjectsSortMode.None).Length == 0)
                Add(SkiStationDiagnosticSeverity.Warning, "SCENE-003", "Aucune lumière détectée dans la scène active.");
            else
                Add(SkiStationDiagnosticSeverity.Info, "SCENE-004", "Éclairage de scène détecté.");
        }
    }
}
