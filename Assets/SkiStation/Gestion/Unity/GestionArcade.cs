// SkiStation — pont vers l'ARCADE de la version ChatGPT : le rider (ArcadeSkierRig), son contrôleur de glisse, ses figures,
// HUD et sa caméra 3e personne sont repris tels quels ; on les pose simplement au départ d'une piste construite en gestion.
using UnityEngine;
using SkiStation.Arcade;
using SkiStation.Gestion;

namespace SkiStation.GestionUnity
{
    public sealed class GestionArcade : MonoBehaviour
    {
        ArcadeSkiSession session; ArcadeSkiController skier; Camera skiCam; Material trackMat;
        public Camera managementCamera; public MonoBehaviour managementControls;
        public bool Playing { get { return session != null && session.IsPlaying; } }
        public Piste piste; bool wasPaused;

        void Build()
        {
            var avatar = GameObject.CreatePrimitive(PrimitiveType.Capsule); avatar.name = "JOUEUR - Rider arcade"; avatar.transform.SetParent(transform);
            avatar.GetComponent<Collider>().isTrigger = true;
            skier = avatar.AddComponent<ArcadeSkiController>();
            avatar.AddComponent<ArcadeBigAirLanding>().skier = skier;
            avatar.AddComponent<ArcadeComboHUD>().skier = skier;
            avatar.AddComponent<ArcadeTrickNameHUD>().skier = skier;
            avatar.AddComponent<ArcadeLandingCelebrationHUD>();
            var jacket = W.Mat("Rider_veste", new Color(.95f, .28f, .26f), .3f); var dark = W.Mat("Rider_sombre", new Color(.12f, .12f, .14f), .3f); var snow = W.Mat("Rider_neige", new Color(.93f, .96f, 1f), .3f);
            ArcadeSkierRig.Build(avatar.transform, skier, jacket, dark, snow);
            var trick = avatar.AddComponent<ArcadeSkiTrickVisual>(); trick.controller = skier;
            trick.bodyVisual = avatar.transform.Find("Arcade body visual"); trick.leftSki = trick.bodyVisual.Find("Left ski"); trick.rightSki = trick.bodyVisual.Find("Right ski");
            var carving = avatar.AddComponent<ArcadeCarvingVisual>(); carving.controller = skier; carving.body = trick.bodyVisual; carving.leftSki = trick.leftSki; carving.rightSki = trick.rightSki;
            var sh = Shader.Find("Sprites/Default"); trackMat = sh != null ? new Material(sh) : null;
            carving.leftTrack = Track(trick.leftSki, "Trace gauche"); carving.rightTrack = Track(trick.rightSki, "Trace droite");
            avatar.GetComponent<Renderer>().enabled = false;
            var camGo = new GameObject("Camera arcade 3e personne"); camGo.transform.SetParent(transform);
            skiCam = camGo.AddComponent<Camera>(); skiCam.fieldOfView = 72; skiCam.nearClipPlane = .08f; skiCam.farClipPlane = 20000; skiCam.enabled = false;
            var follow = camGo.AddComponent<ArcadeThirdPersonCamera>(); follow.target = avatar.transform; follow.controller = skier;
            var fx = camGo.AddComponent<ArcadePeripheralSpeedEffect>(); fx.controller = skier; fx.showBlurWhenSupported = false;
            session = gameObject.AddComponent<ArcadeSkiSession>(); session.player = skier; session.playerCamera = skiCam; session.managementCamera = managementCamera; session.managementCameraControls = managementControls;
            avatar.SetActive(false);
        }
        TrailRenderer Track(Transform ski, string name)
        {
            var go = new GameObject(name); go.transform.SetParent(ski, false); go.transform.localPosition = new Vector3(0, -.07f, -.45f);
            var t = go.AddComponent<TrailRenderer>(); t.time = 5; t.startWidth = .11f; t.endWidth = .025f; t.minVertexDistance = .12f;
            t.startColor = new Color(.57f, .73f, .83f, .8f); t.endColor = new Color(.57f, .73f, .83f, 0); t.sharedMaterial = trackMat; t.enabled = trackMat != null; t.emitting = false; return t;
        }

        /// <summary>Lance une descente arcade depuis le haut de la piste (la gestion est mise en pause pendant la descente).</summary>
        public bool Launch(Piste p)
        {
            if (p == null || p.pts == null || p.pts.Count < 3) return false;
            if (session == null) Build();
            piste = p; var a = p.pts[1]; var b = p.pts[Mathf.Min(p.pts.Count - 1, 4)];
            var pos = W.U(a.x, Game.T.GetH(a.x, a.z) + .15, a.z); var face = Quaternion.Euler(0, W.Yaw(b.x - a.x, b.z - a.z), 0);
            wasPaused = Game.G.paused; Game.G.paused = true;
            if (!session.EnterWorld(pos, face)) return false;
            var cam = skiCam.GetComponent<ArcadeThirdPersonCamera>(); if (cam != null) cam.SnapBehind();
            return true;
        }
        public void Stop() { if (session != null) session.ExitWorld(); }
        void Update()
        {
            if (session != null && !session.IsPlaying && piste != null) { piste = null; if (Game.G != null) Game.G.paused = wasPaused; }
        }
        void OnGUI()
        {
            if (!Playing) return;
            GUI.Label(new Rect(16, Screen.height - 60, 900, 24), "<b>Arcade</b> — " + piste.name + " (" + Catalog.DIFF_NAMES[piste.diff] + ") · Échap : retour à la gestion", GestionUI.Rich);
        }
    }
}
