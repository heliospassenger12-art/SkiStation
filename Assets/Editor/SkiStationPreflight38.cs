#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Manual, read-only smoke check before testing the management scene.</summary>
public static class SkiStationPreflight38
{
    [MenuItem("SkiStation/Diagnostic/Verifier le projet (phase 38)")]
    public static void CheckProject()
    {
        int errors = 0;
        int warnings = 0;
        var notes = new List<string>();
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("[SkiStation P38] Unity importe ou compile encore : relancer le diagnostic apres la fin de l'import.");
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[SkiStation P38] Arreter Play avant le diagnostic pour eviter un resultat ambigu.");
            return;
        }

        string[] scenePaths = AssetDatabase.FindAssets("t:Scene");
        if (scenePaths.Length == 0)
        {
            errors++;
            notes.Add("Aucune scene Unity detectee dans Assets.");
        }
        else
        {
            notes.Add("Scenes disponibles : " + scenePaths.Length + ".");
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            warnings++;
            notes.Add("Aucune scene chargee : ouvrir la scene de gestion avant le test.");
        }
        else
        {
            int objectsChecked = 0;
            int missingScripts = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform tr in root.GetComponentsInChildren<Transform>(true))
                {
                    objectsChecked++;
                    missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject);
                }
            }
            notes.Add("Scene active : " + scene.name + " ; objets inspectes : " + objectsChecked + ".");
            if (missingScripts > 0)
            {
                errors += missingScripts;
                notes.Add("Scripts manquants dans la scene : " + missingScripts + ".");
            }
            else notes.Add("Aucun script manquant detecte dans la scene active.");
        }

        if (EditorBuildSettings.scenes.Length == 0)
        {
            warnings++;
            notes.Add("Aucune scene dans Build Settings (verifier avant une version distribuee).");
        }
        string summary = "[SkiStation P38] Verification terminee : " + errors + " erreur(s), " + warnings + " avertissement(s).\n" + string.Join("\n", notes.ToArray()) + "\nCe controle ne valide ni les trajets des visiteurs ni le comportement des remontees : tester en Play et utiliser F12.";
        if (errors > 0) Debug.LogError(summary);
        else if (warnings > 0) Debug.LogWarning(summary);
        else Debug.Log(summary);
    }
}
#endif
