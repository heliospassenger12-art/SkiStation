using UnityEngine;
namespace SkiStation.Arcade {
public sealed class ArcadePlaytestReadiness : MonoBehaviour {
 ArcadeSkiController c; ArcadeThirdPersonCamera cam; ArcadePlaytestDiagnostics d; bool show=false;
 void Start(){c=FindFirstObjectByType<ArcadeSkiController>();cam=FindFirstObjectByType<ArcadeThirdPersonCamera>();d=FindFirstObjectByType<ArcadePlaytestDiagnostics>();Debug.Log("[Arcade 2.42] Controller="+(c?"OK":"MISSING")+" Camera3P="+(cam?"OK":"MISSING")+" Diagnostics="+(d?"OK":"MISSING"));}
 void Update(){if(Input.GetKeyDown(KeyCode.F2))show=!show;}
 void OnGUI(){if(!show)return;GUILayout.BeginArea(new Rect(Mathf.Max(18,Screen.width-408),210,390,220),GUI.skin.box);GUILayout.Label("SkiStation Arcade 2.42 - PRET POUR TEST");GUILayout.Label((c?"OK  ":"MANQUANT  ")+"Controleur arcade");GUILayout.Label((cam?"OK  ":"MANQUANTE  ")+"Camera 3e personne");GUILayout.Label((d?"OK  ":"MANQUANT  ")+"Diagnostic F1");GUILayout.Space(6);GUILayout.Label("ZQSD glisse | Espace saut | F/G rotation | H grab");GUILayout.Label("Maj boost | R recommencer | F1 diagnostic | F2 panneau");if(c)GUILayout.Label("Vitesse "+c.Speed.ToString("0.0")+" | Boost "+Mathf.RoundToInt(c.BoostFraction*100f)+"%");GUILayout.EndArea();}
}}
