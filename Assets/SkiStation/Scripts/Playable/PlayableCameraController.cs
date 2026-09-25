using UnityEngine;

namespace SkiStation
{
    /// <summary>Gestion uniquement : zoom sur le point vise, deplacement horizontal et recentrage.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayableCameraController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float panSpeed = 190f;
        [SerializeField] private float edgePanSpeed = 150f;
        [SerializeField] private float edgePanSize = 24f;
        [SerializeField] private float rotationSpeed = 65f;
        [SerializeField] private float zoomFractionPerNotch = .18f;
        [SerializeField] private float minimumDistance = 18f;
        [SerializeField] private float maximumDistance = 18000f;
        private Vector3 focus;
        private bool initialized;
        private int visitorIndex;
        private int slopeIndex;

        private void Start() { Initialize(); }

        private void Initialize()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) return;
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) { enabled = false; return; }
            focus = Vector3.zero;
            if (MountainRuntime.I != null && MountainRuntime.I.IsGenerated)
            {
                Vector3 center = Vector3.zero;
                if (MountainRuntime.I.TryGetSurface(center, out var surface)) focus = surface;
            }
            targetCamera.orthographic = false;
            initialized = true;
            FocusEntrance();
        }

        public void FocusEntrance()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) return;
            // The visible village uses -2600 as its fallback anchor, not ManagementSurface.Entrance (-1670).
            Vector3 villageAnchor = StationGatewayRuntime.I != null && StationGatewayRuntime.I.GatewayArrivalPoint != null
                ? StationGatewayRuntime.I.GatewayArrivalPoint.position
                : new Vector3(0f, 0f, -2600f);
            Vector3 target = villageAnchor + new Vector3(0f, 0f, 115f);
            target.y = SkiStationGuaranteedMountainVisual.Height(target.x, target.z);
            focus = target;
            targetCamera.transform.position = focus + new Vector3(0f, 170f, -230f);
            targetCamera.transform.LookAt(focus);
            KeepAboveGround();
        }

        public void ZoomBy(float steps)
        {
            if (targetCamera == null) return;
            Vector3 offset = targetCamera.transform.position - focus;
            if (offset.sqrMagnitude < .01f) return;
            float distance = Mathf.Clamp(offset.magnitude * Mathf.Pow(1f - zoomFractionPerNotch, steps), minimumDistance, maximumDistance);
            targetCamera.transform.position = focus + offset.normalized * distance;
            KeepAboveGround();
        }

        public void FocusArrivals()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            var hub = ArrivalHub250.I;
            if (hub == null || !hub.Ready) { PlayableFeedbackRuntime.I?.Show("Accueil en cours de creation.", 3f); return; }
            FocusClose(hub.Point(40,-5), new Vector3(45f,95f,-130f));
        }

        public void FocusVisitor()
        {
            var visitors = FindObjectsByType<SkierRuntime>(FindObjectsSortMode.InstanceID);
            if (visitors.Length == 0) { PlayableFeedbackRuntime.I?.Show("Aucun visiteur present pour le moment.", 3f); return; }
            FocusClose(visitors[visitorIndex++ % visitors.Length].transform.position, new Vector3(0f, 10f, -16f));
        }

        public void FocusRocks()
        {
            var rock=GameObject.Find("RockButtress_36");
            var renderer=rock != null ? rock.GetComponent<Renderer>() : null;
            if(renderer == null) { PlayableFeedbackRuntime.I?.Show("Les rochers sont en cours de creation.",3f);return; }
            FocusClose(renderer.bounds.center,new Vector3(15f,45f,-160f));
        }

        public void FocusSlope()
        {
            var slopes = FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.InstanceID);
            var built = new System.Collections.Generic.List<SlopeRuntime>();
            foreach (var slope in slopes)
                if (slope.name.StartsWith("Piste_") && slope.Points.Count > 1) built.Add(slope);
            if (built.Count == 0) { PlayableFeedbackRuntime.I?.Show("Construisez puis validez une piste pour la voir ici.", 3f); return; }
            var selected = built[slopeIndex++ % built.Count];
            FocusClose(selected.Points[selected.Points.Count / 2], new Vector3(0f, 45f, -65f));
        }

        private void FocusClose(Vector3 target, Vector3 offset)
        {
            if (targetCamera == null) return;
            focus = target;
            targetCamera.transform.position = target + offset;
            KeepAboveGround();
            targetCamera.transform.LookAt(focus);
        }

        private void KeepAboveGround()
        {
            if (targetCamera == null) return;
            var cam = targetCamera.transform;
            if (ManagementSurface.TryGet(cam.position, out var ground) && cam.position.y < ground.y + 4f)
                cam.position = new Vector3(cam.position.x, ground.y + 4f, cam.position.z);
        }

        public void Overview()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) return;
            // A readable management overview, rather than framing the entire enormous collider bounds.
            Vector3 target = new Vector3(0f, 0f, -850f);
            target.y = SkiStationGuaranteedMountainVisual.Height(target.x, target.z);
            focus = target;
            targetCamera.farClipPlane = Mathf.Max(targetCamera.farClipPlane, 10000f);
            targetCamera.transform.position = focus + new Vector3(0f, 1850f, -2400f);
            targetCamera.transform.LookAt(focus);
            KeepAboveGround();
        }

        private void Update()
        {
            if (!initialized) { Initialize(); if (!initialized) return; }
            if (targetCamera == null) return;
            if (Input.GetKeyDown(KeyCode.Home)) { Overview(); return; }
            if (Input.GetKeyDown(KeyCode.End)) { FocusEntrance(); return; }
            if (Input.GetKeyDown(KeyCode.PageUp)) ZoomBy(1f);
            if (Input.GetKeyDown(KeyCode.PageDown)) ZoomBy(-1f);
            Transform cam = targetCamera.transform;
            float dt = Time.unscaledDeltaTime;
            Vector3 forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            // Fleches/WASD : deplacement volontairement plus vif pour parcourir un grand domaine.
            Vector3 keyboardMovement = forward * Input.GetAxisRaw("Vertical") + right * Input.GetAxisRaw("Horizontal");
            if (keyboardMovement.sqrMagnitude > 1f) keyboardMovement.Normalize();

            // Edge scrolling type jeu de gestion : haut=avance, bas=recule, gauche/droite=translation.
            // Desactive lorsque le pointeur survole les controles camera du HUD.
            Vector3 edgeMovement = Vector3.zero;
            if (!PlayableGameHUD.PointerOverCameraControls && Application.isFocused)
            {
                Vector3 mouse = Input.mousePosition;
                if (mouse.x >= 0f && mouse.y >= 0f && mouse.x <= Screen.width && mouse.y <= Screen.height)
                {
                    if (mouse.y >= Screen.height - edgePanSize) edgeMovement += forward;
                    else if (mouse.y <= edgePanSize) edgeMovement -= forward;
                    if (mouse.x >= Screen.width - edgePanSize) edgeMovement += right;
                    else if (mouse.x <= edgePanSize) edgeMovement -= right;
                }
            }
            if (edgeMovement.sqrMagnitude > 1f) edgeMovement.Normalize();

            float distance = Vector3.Distance(cam.position, focus);
            float distanceFactor = Mathf.Clamp(distance / 350f, .25f, 5f);
            float boost = Input.GetKey(KeyCode.LeftShift) ? 2.5f : 1f;
            Vector3 delta = (keyboardMovement * panSpeed + edgeMovement * edgePanSpeed) * distanceFactor * boost * dt;
            cam.position += delta;
            focus += delta;
            if (Input.GetKey(KeyCode.Q)) cam.RotateAround(focus, Vector3.up, -rotationSpeed * dt);
            if (Input.GetKey(KeyCode.E)) cam.RotateAround(focus, Vector3.up, rotationSpeed * dt);
            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > .01f && !PlayableGameHUD.PointerOverCameraControls)
            {
                // Un cran produit le meme rapport de zoom, independamment des FPS.
                Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
                Vector3 target = focus;
                if (Physics.Raycast(ray, out RaycastHit hit, 10000f)) target = hit.point;
                else if (new Plane(Vector3.up, focus).Raycast(ray, out float t)) target = ray.GetPoint(t);
                Vector3 fromTarget = cam.position - target;
                float oldDistance = fromTarget.magnitude;
                if (oldDistance > .01f)
                {
                    float desired = Mathf.Clamp(oldDistance * Mathf.Pow(1f - zoomFractionPerNotch, wheel), minimumDistance, maximumDistance);
                    cam.position = target + fromTarget * (desired / oldDistance);
                    focus = target;
                }
            }
            KeepAboveGround();
        }
    }
}
