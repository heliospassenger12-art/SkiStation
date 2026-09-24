#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Read-only audit of prefab assets, complementary to the active-scene phase 38 check.</summary>
public static class SkiStationPrefabAudit39
{
    [MenuItem("SkiStation/Diagnostic/Verifier les prefabs (phase 39)")]
    public static void CheckPrefabs()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[SkiStation P39] Attendre la fin de l'import et arreter Play avant de verifier les prefabs.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        int checkedPrefabs = 0;
        int missingComponents = 0;
        int unreadable = 0;
        var examples = new List<string>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                unreadable++;
                if (examples.Count < 12) examples.Add("Prefab inaccessible : " + path);
                continue;
            }
            checkedPrefabs++;
            int localMissing = 0;
            foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
                localMissing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
            if (localMissing > 0)
            {
                missingComponents += localMissing;
                if (examples.Count < 12) examples.Add(path + " : " + localMissing + " script(s) manquant(s)");
            }
        }

        string report = "[SkiStation P39] Prefabs examines : " + checkedPrefabs +
            " ; composants manquants : " + missingComponents +
            " ; prefabs inaccessibles : " + unreadable + "." +
            (examples.Count > 0 ? "\nExemples (12 maximum) :\n" + string.Join("\n", examples.ToArray()) : "\nAucun script manquant detecte dans les prefabs examines.") +
            "\nAudit en lecture seule : ne verifie pas les objets crees dynamiquement ni les trajets des visiteurs. Tester en Play avec F12.";
        if (missingComponents > 0 || unreadable > 0) Debug.LogWarning(report);
        else Debug.Log(report);
    }
}
#endif
