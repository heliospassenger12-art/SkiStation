using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class SkiStationProjectValidator
{
    private const string ScriptsRoot = "Assets/SkiStation/Scripts";

    [MenuItem("SkiStation/Validate Project")]
    public static void ValidateProject()
    {
        int errors = 0;
        int warnings = 0;

        string[] required =
        {
            "Assets",
            "Assets/SkiStation",
            ScriptsRoot,
            "Assets/Scenes",
            "Packages",
            "ProjectSettings"
        };

        foreach (string path in required)
        {
            if (!Directory.Exists(path) && !File.Exists(path))
            {
                Debug.LogError("[SkiStation] Missing: " + path);
                errors++;
            }
        }

        string manifest = "Packages/manifest.json";
        if (!File.Exists(manifest))
        {
            Debug.LogError("[SkiStation] Missing Packages/manifest.json");
            errors++;
        }

        string scene = "Assets/Scenes/SkiStation_Bootstrap.unity";
        if (!File.Exists(scene))
        {
            Debug.LogError("[SkiStation] Missing starter scene: " + scene);
            errors++;
        }

        var classes = new Dictionary<string, List<string>>();
        int scriptCount = 0;

        if (Directory.Exists(ScriptsRoot))
        {
            foreach (string file in Directory.GetFiles(ScriptsRoot, "*.cs", SearchOption.AllDirectories))
            {
                scriptCount++;
                string text = File.ReadAllText(file);
                MatchCollection matches = Regex.Matches(
                    text,
                    @"\b(?:public|internal|private|protected)?\s*(?:abstract\s+|sealed\s+|static\s+)?class\s+([A-Za-z_][A-Za-z0-9_]*)\b"
                );

                foreach (Match match in matches)
                {
                    string typeName = match.Groups[1].Value;
                    if (!classes.TryGetValue(typeName, out List<string> files))
                    {
                        files = new List<string>();
                        classes.Add(typeName, files);
                    }
                    files.Add(file);
                }
            }
        }

        foreach (KeyValuePair<string, List<string>> pair in classes)
        {
            if (pair.Value.Count > 1)
            {
                Debug.LogError("[SkiStation] Duplicate class detected: " + pair.Key + "\n" + string.Join("\n", pair.Value));
                errors++;
            }
        }

        string projectVersion = "ProjectSettings/ProjectVersion.txt";
        if (File.Exists(projectVersion))
        {
            string versionText = File.ReadAllText(projectVersion);
            if (!versionText.Contains("6000.3.3f1"))
            {
                Debug.LogWarning("[SkiStation] ProjectVersion.txt does not target 6000.3.3f1.");
                warnings++;
            }
        }

        Debug.Log($"[SkiStation] Validation complete. Scripts: {scriptCount}. Errors: {errors}. Warnings: {warnings}.");
    }
}
