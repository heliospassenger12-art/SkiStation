// SkiStation — pont vers l'ARCADE de la version ChatGPT : le rider (ArcadeSkierRig), son contrôleur de glisse, ses figures,
// HUD et sa caméra 3e personne sont repris tels quels et posés au départ d'une piste construite en gestion.
// Aucune dépendance de compilation : les composants sont retrouvés par leur nom (ChatGPTAssets), pour fonctionner
// avec toutes les versions de l'arcade (A2.3x, A2.63, A2.64…).
using UnityEngine;
using SkiStation.Gestion;

namespace SkiStation.GestionUnity
{
    public sealed class GestionArcade : MonoBehaviour
    {
        const string NS = "SkiStation.Arcade.";
        public Camera managementCamera; public MonoBehaviour managementControls;
        public bool Playing { get; private set; }
        public Piste piste; public string lastError;
        GameObject avatar; Component skier, follow; Camera skiCam; bool wasPaused, built;

        bool Build()
        {
            built = true;
            if (ChatGPTAssets.Find(NS + "ArcadeSkiController") == null) { lastError = "Arcade introuvable dans ce projet (SkiStation.Arcade.ArcadeSkiController)."; return false; }
            avatar = GameObject.CreatePrimitive(PrimitiveType.Capsule); avatar.name = "JOUEUR - Rider arcade"; avatar.transform.SetParent(transform);
            avatar.GetComponent<Collider>().isTrigger = true;
            skier = ChatGPTAssets.Add(avatar, NS + "ArcadeSkiController");
            foreach (var hud in new[] { "ArcadeBigAirLanding", "ArcadeComboHUD", "ArcadeTrickNameHUD" }) ChatGPTAssets.Set(ChatGPTAssets.Add(avatar, NS + hud), "skier", skier);
            ChatGPTAssets.Add(avatar, NS + "ArcadeLandingCelebrationHUD");
            var jacket = W.Mat("Rider_veste", new Color(.95f, .28f, .26f), .3f); var dark = W.Mat("Rider_sombre", new Color(.12f, .12f, .14f), .3f); var snow = W.Mat("Rider_neige", new Color(.93f, .96f, 1f), .3f);
            bool rig = ChatGPTAssets.Call(null, ChatGPTAssets.Find(NS + "ArcadeSkierRig"), "Build", avatar.transform, skier, jacket, dark, snow);
            var body = avatar.transform.Find("Arcade body visual");
            if (rig && body != null)
            {
                var trick = ChatGPTAssets.Add(avatar, NS + "ArcadeSkiTrickVisual");
                ChatGPTAssets.Set(trick, "controller", skier); ChatGPTAssets.Set(trick, "bodyVisual", body);
                ChatGPTAssets.Set(trick, "leftSki", body.Find("Left ski")); ChatGPTAssets.Set(trick, "rightSki", body.Find("Right ski"));
                var carving = ChatGPTAssets.Add(avatar, NS + "ArcadeCarvingVisual");
                ChatGPTAssets.Set(carving, "controller", skier); ChatGPTAssets.Set(carving, "body", body);
                ChatGPTAssets.Set(carving, "leftSki", body.Find("Left ski")); ChatGPTAssets.Set(carving, "rightSki", body.Find("Right ski"));
                var sh = Shader.Find("Sprites/Default"); var trackMat = sh != null ? new Material(sh) : null;
                ChatGPTAssets.Set(carving, "leftTrack", Track(body.Find("Left ski"), "Trace gauche", trackMat)); ChatGPTAssets.Set(carving, "rightTrack", Track(body.Find("Right ski"), "Trace droite", trackMat));
                avatar.GetComponent<Renderer>().enabled = false;
            }
            else avatar.GetComponent<Renderer>().sharedMaterial = jacket;          // rider indisponible : capsule colorée
            var camGo = new GameObject("Camera arcade 3e personne"); camGo.transform.SetParent(transform);
            skiCam = camGo.AddComponent<Camera>(); skiCam.fieldOfView = 72; skiCam.nearClipPlane = .08f; skiCam.farClipPlane = 20000; skiCam.enabled = false;
            follow = ChatGPTAssets.Add(camGo, NS + "ArcadeThirdPersonCamera");
            ChatGPTAssets.Set(follow, "target", avatar.transform); ChatGPTAssets.Set(follow, "controller", skier);
            var fx = ChatGPTAssets.Add(camGo, NS + "ArcadePeripheralSpeedEffect"); ChatGPTAssets.Set(fx, "controller", skier); ChatGPTAssets.Set(fx, "showBlurWhenSupported", false);
            ChatGPTAssets.Call(skier, skier.GetType(), "StopPlaying");
            avatar.SetActive(false);
            return true;
        }
        static TrailRenderer Track(Transform ski, string name, Material mat)
        {
            if (ski == null) return null;
            var go = new GameObject(name); go.transform.SetParent(ski, false); go.transform.localPosition = new Vector3(0, -.07f, -.45f);
            var t = go.AddComponent<TrailRenderer>(); t.time = 5; t.startWidth = .11f; t.endWidth = .025f; t.minVertexDistance = .12f;
            t.startColor = new Color(.57f, .73f, .83f, .8f); t.endColor = new Color(.57f, .73f, .83f, 0); t.sharedMaterial = mat; t.enabled = mat != null; t.emitting = false; return t;
        }

        /// <summary>Lance une descente arcade depuis le haut de la piste (la gestion est en pause pendant la descente).</summary>
        public bool Launch(Piste p)
        {
            if (p == null || p.pts == null || p.pts.Count < 3) return false;
            if (!built && !Build()) return false; if (skier == null) return false;
            piste = p; var a = p.pts[1]; var b = p.pts[Mathf.Min(p.pts.Count - 1, 4)];
            var pos = W.U(a.x, Game.T.GetH(a.x, a.z) + .15, a.z); var face = Quaternion.Euler(0, W.Yaw(b.x - a.x, b.z - a.z), 0);
            wasPaused = Game.G.paused; Game.G.paused = true;
            avatar.SetActive(true);
            if (!ChatGPTAssets.Call(skier, skier.GetType(), "BeginAt", pos, face)) { avatar.transform.SetPositionAndRotation(pos, face); }
            if (managementCamera != null) managementCamera.enabled = false; if (managementControls != null) managementControls.enabled = false;
            skiCam.enabled = true; if (follow != null) ChatGPTAssets.Call(follow, follow.GetType(), "SnapBehind");
            Playing = true; return true;
        }
        public void Stop()
        {
            if (!Playing) return; Playing = false;
            if (skier != null) ChatGPTAssets.Call(skier, skier.GetType(), "StopPlaying");
            if (avatar != null) avatar.SetActive(false); if (skiCam != null) skiCam.enabled = false;
            if (managementCamera != null) managementCamera.enabled = true; if (managementControls != null) managementControls.enabled = true;
            if (Game.G != null) Game.G.paused = wasPaused; piste = null;
        }
        void Update() { if (Playing && Input.GetKeyDown(KeyCode.Escape)) Stop(); }
        void OnGUI()
        {
            if (!Playing || piste == null) return;
            GUI.Label(new Rect(16, Screen.height - 60, 900, 24), "<b>Arcade</b> — " + piste.name + " (" + Catalog.DIFF_NAMES[piste.diff] + ") · Échap : retour à la gestion", GestionUI.Rich);
        }
    }
}
