// SkiStation — caméra de gestion : vue orbitale autour d'un point du relief (360°), déplacement, rotation, zoom, suivi.
using UnityEngine;
using SkiStation.Gestion;

namespace SkiStation.GestionUnity
{
    [RequireComponent(typeof(Camera))]
    public sealed class GestionCamera : MonoBehaviour
    {
        public double tx = 0, tz = 2350;                    // cible au sol (coordonnées du noyau)
        public float dist = 2200, yaw = 0, pitch = 42;
        public Visitor follow;
        public bool inputEnabled = true;
        Vector3 lastMouse; Camera cam;

        void Awake() { cam = GetComponent<Camera>(); cam.nearClipPlane = 1f; cam.farClipPlane = 40000; cam.fieldOfView = 50; }
        public void Focus(double x, double z, float d = -1) { tx = x; tz = z; if (d > 0) dist = d; follow = null; }

        void LateUpdate()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
            if (inputEnabled && !GestionUI.PointerOverUI)
            {
                float sp = dist * 0.9f * dt * (Input.GetKey(KeyCode.LeftShift) ? 2.5f : 1);
                var fw = Quaternion.Euler(0, yaw, 0) * Vector3.forward; var rt = Quaternion.Euler(0, yaw, 0) * Vector3.right;
                Vector3 mv = Vector3.zero;
                if (!GestionUI.TextFocus)
                {
                    if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.Z)) mv += fw;
                    if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) mv -= fw;
                    if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) mv += rt;
                    if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) mv -= rt;
                    if (Input.GetKey(KeyCode.Q)) yaw -= 70 * dt; if (Input.GetKey(KeyCode.E)) yaw += 70 * dt;
                }
                if (mv != Vector3.zero) { follow = null; tx += mv.x * sp; tz -= mv.z * sp; }
                float wheel = Input.mouseScrollDelta.y; if (wheel != 0) dist = Mathf.Clamp(dist * Mathf.Pow(0.88f, wheel), 40, 9000);
                var m = Input.mousePosition; var d = m - lastMouse;
                if (Input.GetMouseButton(1)) { yaw += d.x * .25f; pitch = Mathf.Clamp(pitch - d.y * .2f, 8, 88); }
                if (Input.GetMouseButton(2) || (Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftShift)))
                {
                    follow = null; float k = dist / Screen.height * 1.6f;
                    var pan = (-rt * d.x - fw * d.y) * k; tx += pan.x; tz -= pan.z;
                }
            }
            lastMouse = Input.mousePosition;
            if (follow != null) { if (follow.state == "gone") follow = null; else { tx = GMath.Lerp(tx, follow.x, .15); tz = GMath.Lerp(tz, follow.z, .15); } }
            tx = GMath.Clamp(tx, -Relief.HALF + 100, Relief.HALF - 100); tz = GMath.Clamp(tz, -Relief.HALF + 100, Relief.HALF - 100);
            var target = W.Ground(tx, tz);
            var off = Quaternion.Euler(pitch, yaw, 0) * new Vector3(0, 0, -dist);
            var pos = target + off; float ground = (float)Game.T.GetH(pos.x, -pos.z) + 25; if (pos.y < ground) pos.y = ground;
            transform.position = pos; transform.LookAt(target);
        }
    }
}
