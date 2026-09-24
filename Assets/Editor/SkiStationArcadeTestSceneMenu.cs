#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkiStation.Arcade.Editor
{
    public static class SkiStationArcadeTestSceneMenu
    {
        [MenuItem("SkiStation/Arcade/Creer et ouvrir la scene de glisse 2.42")]
        public static void CreateTestScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Quittez Play avant de creer la scene de test arcade."); return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("SkiStation Arcade Test - isolated from management");
            root.AddComponent<ArcadeTestSlope>();
            string path = "Assets/Scenes/SkiStation_Arcade_Test_2_42.unity";
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(scene, path);
            EditorSceneManager.OpenScene(path);
            Selection.activeGameObject = root;
            Debug.Log("Scene arcade creee : " + path + " | Appuyez sur Play pour skier.");
        }
    }
}
#endif
