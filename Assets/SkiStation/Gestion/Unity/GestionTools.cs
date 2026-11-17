// SkiStation — outils de construction (remontées, pistes à la main ou en tracé assisté, bâtiments, routes) avec aperçu
// et DEVIS AVANT VALIDATION, puis sélection des objets. Aucune règle ici : tout passe par Game.Analyze* / Game.Commit*.
using System.Collections.Generic;
using UnityEngine;
using SkiStation.Gestion;

namespace SkiStation.GestionUnity
{
    public sealed class GestionTools : MonoBehaviour
    {
        public enum Mode { None, Lift, Piste, PisteAuto, Building, Road }
        public Mode mode; public string type;              // modèle de remontée ou de bâtiment
        public int autoEase = 1; public double pisteWidth = 30; public double rot;
        public readonly List<P2> pts = new List<P2>();
        public LiftAnalysis liftAn; public PisteAnalysis pisteAn; public BuildingAnalysis bldAn; public RoadAnalysis roadAn; public string autoErr;
        public P2 cursor; public bool cursorOk;
        public object selected; public string message;
        LineRenderer line; GameObject ghost; Camera cam;
        public System.Action<object> Selected;

        void Awake()
        {
            cam = GetComponent<Camera>() ?? Camera.main;
            var go = new GameObject("Apercu_Construction"); line = go.AddComponent<LineRenderer>(); line.widthMultiplier = 4; line.positionCount = 0;
            line.sharedMaterial = W.Unlit("Apercu", new Color(.2f, .85f, .35f));
            ghost = GameObject.CreatePrimitive(PrimitiveType.Cylinder); ghost.name = "Apercu_Batiment"; Destroy(ghost.GetComponent<Collider>()); ghost.SetActive(false);
        }
        public void Begin(Mode m, string t = null) { Cancel(); mode = m; type = t; message = null; }
        public void Cancel()
        {
            mode = Mode.None; type = null; pts.Clear(); liftAn = null; pisteAn = null; bldAn = null; roadAn = null; autoErr = null;
            if (line != null) line.positionCount = 0; if (ghost != null) ghost.SetActive(false);
        }
        public bool HasQuote { get { return liftAn != null && pts.Count == 2 || pisteAn != null && (mode == Mode.Piste || mode == Mode.PisteAuto) && pts.Count >= 2 || roadAn != null && pts.Count >= 2; } }

        void Update()
        {
            if (Game.G == null || cam == null) return;
            Vector3 hp; cursorOk = !GestionUI.PointerOverUI && GestionWorld.Pick(cam.ScreenPointToRay(Input.mousePosition), out cursor, out hp);
            if (Input.GetKeyDown(KeyCode.Escape) && !GestionUI.TextFocus) { if (mode != Mode.None) Cancel(); else Select(null); }
            if (mode == Mode.Building) UpdateBuilding();
            else if (mode == Mode.Lift) UpdateLift();
            else if (mode == Mode.Piste || mode == Mode.Road) UpdatePath();
            else if (mode == Mode.PisteAuto) UpdateAuto();
            else if (cursorOk && Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftShift)) Select(PickObject(cursor));
            if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && !GestionUI.TextFocus && HasQuote) Validate();
        }

        // ---------- remontée : gare de départ puis gare d'arrivée ----------
        void UpdateLift()
        {
            if (pts.Count == 2) { DrawLine(new[] { pts[0], pts[1] }, liftAn.ok); return; }
            if (pts.Count == 1 && cursorOk) { liftAn = Game.AnalyzeLift(type, pts[0], cursor); DrawLine(new[] { pts[0], cursor }, liftAn.ok); }
            if (cursorOk && Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftShift))
            {
                pts.Add(Snap(cursor));
                if (pts.Count == 2) liftAn = Game.AnalyzeLift(type, pts[0], pts[1]);
            }
        }
        /// <summary>Accroche aux gares et extrémités existantes (rayon 40 m) pour relier facilement le domaine.</summary>
        P2 Snap(P2 p)
        {
            double best = 40; P2 r = p;
            foreach (var n in Game.Net.nodes) { if (n.hub) continue; double d = GMath.Hypot(n.x - p.x, n.z - p.z); if (d < best) { best = d; r = new P2(n.x, n.z); } }
            return r;
        }
        // ---------- piste (à la main) ou route : points successifs ----------
        void UpdatePath()
        {
            if (cursorOk && Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftShift)) { pts.Add(pts.Count == 0 && mode == Mode.Piste ? Snap(cursor) : cursor); Requote(); }
            if (Input.GetKeyDown(KeyCode.Backspace) && pts.Count > 0 && !GestionUI.TextFocus) { pts.RemoveAt(pts.Count - 1); Requote(); }
            var show = new List<P2>(pts); if (cursorOk && pts.Count > 0 && !HasQuote) show.Add(cursor);
            if (mode == Mode.Piste && pisteAn != null && pisteAn.pts != null) DrawLine(pisteAn.pts.ConvertAll(q => q.Xz), pisteAn.ok); else DrawLine(show, roadAn == null || roadAn.ok);
        }
        public void Requote()
        {
            pisteAn = null; roadAn = null;
            if (pts.Count < 2) return;
            if (mode == Mode.Piste || mode == Mode.PisteAuto) pisteAn = Game.AnalyzePiste(pts, pisteWidth);
            else if (mode == Mode.Road) roadAn = Game.AnalyzeRoad(pts);
        }
        // ---------- tracé assisté : un clic sur le départ (souvent l'arrivée d'une remontée) ----------
        void UpdateAuto()
        {
            if (pisteAn != null && pisteAn.pts != null) DrawLine(pisteAn.pts.ConvertAll(q => q.Xz), pisteAn.ok);
            if (cursorOk && Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftShift))
            {
                var start = Snap(cursor); var r = Game.AutoPiste(start, null, pisteWidth, autoEase);
                pts.Clear(); autoErr = r.err; pisteAn = null;
                if (r.err == null) { pts.AddRange(r.ctrl); pisteAn = r.an; }
            }
        }
        // ---------- bâtiment ----------
        void UpdateBuilding()
        {
            var def = Catalog.Building(type);
            if (Input.GetKeyDown(KeyCode.R) && !GestionUI.TextFocus) rot += Mathf.PI / 8;
            if (!cursorOk) { ghost.SetActive(false); return; }
            bldAn = Game.AnalyzeBuilding(type, cursor.x, cursor.z);
            ghost.SetActive(true); ghost.transform.position = W.Ground(cursor.x, cursor.z, 3); ghost.transform.localScale = new Vector3((float)def.r * 2, 3, (float)def.r * 2);
            ghost.GetComponent<Renderer>().sharedMaterial = W.Mat(bldAn.ok ? "Apercu_ok" : "Apercu_err", bldAn.ok ? new Color(.2f, .85f, .35f) : new Color(.9f, .2f, .2f));
            if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftShift) && bldAn.ok)
            {
                var b = Game.CommitBuilding(bldAn, rot); message = b != null ? def.name + " construit pour " + GMath.Eur(bldAn.cost) + "." : "Fonds insuffisants.";
            }
        }

        public void Validate()
        {
            if (mode == Mode.Lift && liftAn != null && liftAn.ok) { var l = Game.CommitLift(liftAn); message = l != null ? l.name + " construit." : "Fonds insuffisants."; Cancel(); if (l != null) Select(l); return; }
            if ((mode == Mode.Piste || mode == Mode.PisteAuto) && pisteAn != null && pisteAn.ok) { var p = Game.CommitPiste(pts, pisteAn); message = p != null ? "Piste " + p.name + " ouverte." : "Fonds insuffisants."; var m = mode; Cancel(); if (p != null) Select(p); if (m == Mode.PisteAuto) mode = Mode.None; return; }
            if (mode == Mode.Road && roadAn != null && roadAn.ok) { var r = Game.CommitRoad(roadAn.pts ?? pts, roadAn); message = r != null ? "Route construite." : "Fonds insuffisants."; Cancel(); FindObjectOfType<GestionRender>().SyncRoads(); }
        }

        void DrawLine(IList<P2> p, bool ok)
        {
            line.positionCount = p.Count; for (int i = 0; i < p.Count; i++) line.SetPosition(i, W.Ground(p[i].x, p[i].z, 3));
            line.sharedMaterial = W.Unlit(ok ? "Apercu" : "Apercu_rouge", ok ? new Color(.2f, .85f, .35f) : new Color(.95f, .25f, .2f));
        }

        // ---------- sélection ----------
        public void Select(object o) { selected = o; var r = FindObjectOfType<GestionRender>(); if (r != null) r.highlight = o; if (Selected != null) Selected(o); }
        public static object PickObject(P2 c)
        {
            var G = Game.G; object best = null; double bd = 1e9;
            foreach (var v in Sim.Visitors) { if (v.state == "gone" || v.state == "inS" || v.state == "pending") continue; double d = GMath.Hypot(v.x - c.x, v.z - c.z); if (d < 9 && d < bd) { bd = d; best = v; } }
            if (best != null) return best;
            foreach (var b in G.buildings) { double d = GMath.Hypot(b.x - c.x, b.z - c.z); if (d < b.Def.r + 4 && d < bd) { bd = d; best = b; } }
            if (best != null) return best;
            foreach (var l in G.lifts) { double t; double d = GMath.SegDist(c.x, c.z, l.a.x, l.a.z, l.b.x, l.b.z, out t); if (d < 18 && d < bd) { bd = d; best = l; } }
            if (best != null) return best;
            foreach (var p in G.pistes) { double s; double d = Game.ClosestOnPiste(p, c.x, c.z, out s); if (d < p.width / 2 + 4 && d < bd) { bd = d; best = p; } }
            return best;
        }
    }
}
