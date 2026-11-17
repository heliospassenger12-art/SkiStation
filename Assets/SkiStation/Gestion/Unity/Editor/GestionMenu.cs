// SkiStation — menu éditeur : ouvrir / lancer la scène de gestion fusionnée.
using UnityEditor;
using UnityEditor.SceneManagement;

namespace SkiStation.GestionUnity.EditorTools
{
    public static class GestionMenu
    {
        const string SCENE = "Assets/Scenes/SkiStation_Arcade_Test_Gestion.unity";
        [MenuItem("SkiStation/Gestion fusionnée/Ouvrir la scène de gestion", false, 1)]
        static void Open() { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(SCENE); }
        [MenuItem("SkiStation/Gestion fusionnée/Jouer (gestion + arcade)", false, 2)]
        static void Play() { Open(); EditorApplication.isPlaying = true; }
    }
}
