// SkiStation — couche Unity : rendu des pistes, remontées (gares, pylônes, câbles, véhicules physiques), bâtiments, routes,
// village, visiteurs, dameuses et secours. Tout l'état vient du noyau (SkiStation.Gestion) ; ce script ne fait que l'afficher.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using SkiStation.Gestion;

namespace SkiStation.GestionUnity
{
    public sealed class GestionRender : MonoBehaviour
    {
        GameState G { get { return Game.G; } }
        Transform root, pisteRoot, liftRoot, bldRoot, roadRoot, decorRoot;
        readonly Dictionary<Piste, GameObject> pistes = new Dictionary<Piste, GameObject>();
        readonly Dictionary<Lift, GameObject> lifts = new Dictionary<Lift, GameObject>();
        readonly Dictionary<Building, GameObject> blds = new Dictionary<Building, GameObject>();
        readonly Dictionary<Road, GameObject> roads = new Dictionary<Road, GameObject>();
        Mesh box, capsule, cylinder, roof, sphere;
        public object highlight;                            // objet sélectionné (surligné)
        public string overlay;                               // null, "crowd" ou "snow"
        Light sun;

        public void Build(Light sunLight)
        {
            sun = sunLight;
            root = new GameObject("SkiStation_Gestion_Rendu").transform; root.SetParent(transform, false);
            pisteRoot = Child("Pistes"); liftRoot = Child("Remontees"); bldRoot = Child("Batiments"); roadRoot = Child("Routes"); decorRoot = Child("Village");
            box = PrimMesh(PrimitiveType.Cube); capsule = PrimMesh(PrimitiveType.Capsule); cylinder = PrimMesh(PrimitiveType.Cylinder); sphere = PrimMesh(PrimitiveType.Sphere); roof = RoofMesh();
            Game.Bus += OnBus;
        }
        void OnDestroy() { Game.Bus -= OnBus; }
        Transform Child(string n) { var t = new GameObject(n).transform; t.SetParent(root, false); return t; }
        static Mesh PrimMesh(PrimitiveType t) { var g = GameObject.CreatePrimitive(t); var m = g.GetComponent<MeshFilter>().sharedMesh; Destroy(g); return m; }
        static Mesh RoofMesh()
        {
            var m = new Mesh { name = "Toit" };
            m.vertices = new[] { new Vector3(-.5f, 0, -.5f), new Vector3(.5f, 0, -.5f), new Vector3(.5f, 0, .5f), new Vector3(-.5f, 0, .5f), new Vector3(-.5f, .5f, 0), new Vector3(.5f, .5f, 0) };
            m.triangles = new[] { 0, 4, 5, 0, 5, 1, 2, 5, 4, 2, 4, 3, 0, 3, 4, 1, 5, 2 };
            m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        void OnBus(string ev, object a, object b)
        {
            switch (ev)
            {
                case "piste:add": case "piste:refresh": MakePiste((Piste)a); break;
                case "lift:add": case "lift:refresh": MakeLift((Lift)a); break;
                case "building:add": MakeBuilding((Building)a); break;
                case "removed": Remove(a); break;
            }
        }
        /// <summary>Reconstruit tout l'affichage (nouvelle partie, chargement, changement de saison).</summary>
        public void ClearAll()
        {
            foreach (var g in pistes.Values) Destroy(g); foreach (var g in lifts.Values) Destroy(g); foreach (var g in blds.Values) Destroy(g); foreach (var g in roads.Values) Destroy(g);
            pistes.Clear(); lifts.Clear(); blds.Clear(); roads.Clear();
            foreach (Transform t in decorRoot) Destroy(t.gameObject);
        }
        public void RebuildAll()
        {
            ClearAll();
            foreach (var d in Const.DECOR) MakeDecor(d);
            foreach (var p in G.pistes) MakePiste(p);
            foreach (var l in G.lifts) MakeLift(l);
            foreach (var bd in G.buildings) MakeBuilding(bd);
            SyncRoads();
        }
        void Remove(object o)
        {
            GameObject g;
            var p = o as Piste; if (p != null && pistes.TryGetValue(p, out g)) { Destroy(g); pistes.Remove(p); }
            var l = o as Lift; if (l != null && lifts.TryGetValue(l, out g)) { Destroy(g); lifts.Remove(l); }
            var b = o as Building; if (b != null && blds.TryGetValue(b, out g)) { Destroy(g); blds.Remove(b); }
            var r = o as Road; if (r != null && roads.TryGetValue(r, out g)) { Destroy(g); roads.Remove(r); }
        }
        public void SyncRoads()
        {
            foreach (var r in G.roads) if (!roads.ContainsKey(r)) roads[r] = Ribbon("Route " + r.id, r.pts, 8, 0.25f, W.Mat("Route", new Color(.25f, .25f, .27f), .1f), null, 0, roadRoot);
        }

        // ---------- objets ----------
        GameObject Obj(string name, Transform parent, Mesh mesh, Material mat, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.SetPositionAndRotation(pos, rot); g.transform.localScale = scale;
            g.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = ShadowCastingMode.On;
            return g;
        }
        /// <summary>Ruban posé sur le relief (pistes, routes). borderMat : bandes latérales de couleur (difficulté).</summary>
        GameObject Ribbon(string name, IList<P2> pts2, double width, float lift, Material mat, Material borderMat, double border, Transform parent)
        {
            var pts = new List<P2>(); for (int i = 0; i < pts2.Count - 1; i++) { var a = pts2[i]; var b = pts2[i + 1]; int n = Mathf.Max(1, Mathf.CeilToInt((float)(GMath.Hypot(b.x - a.x, b.z - a.z) / 10))); for (int k = 0; k < n; k++) pts.Add(new P2(GMath.Lerp(a.x, b.x, (double)k / n), GMath.Lerp(a.z, b.z, (double)k / n))); }
            pts.Add(pts2[pts2.Count - 1]);
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var m = new Mesh { name = name }; var v = new List<Vector3>(); var t0 = new List<int>(); var t1 = new List<int>();
            double hw = width / 2; double[] offs = borderMat != null ? new[] { -hw, -hw + border, hw - border, hw } : new[] { -hw, hw };
            for (int i = 0; i < pts.Count; i++)
            {
                var p = pts[i]; var q = pts[Mathf.Min(pts.Count - 1, i + 1)]; var o = pts[Mathf.Max(0, i - 1)];
                double dx = q.x - o.x, dz = q.z - o.z, d = GMath.Hypot(dx, dz); if (d == 0) d = 1; double nx = -dz / d, nz = dx / d;
                foreach (var off in offs) { double x = p.x + nx * off, z = p.z + nz * off; v.Add(W.U(x, Game.T.GetH(x, z) + lift, z)); }
            }
            int w = offs.Length;
            for (int i = 0; i < pts.Count - 1; i++) for (int k = 0; k < w - 1; k++)
                {
                    int a = i * w + k, b = a + 1, c = a + w, d = c + 1; var tl = (borderMat != null && k != 1) ? t1 : t0;
                    tl.Add(a); tl.Add(c); tl.Add(b); tl.Add(b); tl.Add(c); tl.Add(d);
                }
            m.SetVertices(v); m.subMeshCount = borderMat != null ? 2 : 1; m.SetTriangles(t0, 0); if (borderMat != null) m.SetTriangles(t1, 1);
            m.RecalculateNormals(); m.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = m; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterials = borderMat != null ? new[] { mat, borderMat } : new[] { mat }; mr.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }
        public static Material PisteMat(Piste p)
        {
            if (!p.open || p.snowClosed) return W.Mat("Piste_Fermee", new Color(.62f, .64f, .66f), .1f);
            return W.Mat("Piste_Damee", new Color(.97f, .985f, 1f), .45f);
        }
        void MakePiste(Piste p)
        {
            GameObject old; if (pistes.TryGetValue(p, out old)) Destroy(old);
            var col = W.Hex(Catalog.DIFF_COLORS[p.diff]);
            var g = Ribbon("Piste " + p.name, p.pts.ConvertAll(q => q.Xz), p.width, 0.35f, PisteMat(p), W.Mat("Balisage_" + p.diff, col, .2f), 2.2, pisteRoot);
            if (p.lit) for (double s = 40; s < p.len; s += 90) { var at = Game.PisteAt(p, s); double d = GMath.Hypot(at.dx, at.dz); if (d == 0) d = 1; double x = at.x - at.dz / d * (p.width / 2 + 3), z = at.z + at.dx / d * (p.width / 2 + 3); var post = Obj("Lampadaire", g.transform, cylinder, W.Mat("Mat_lampadaire", new Color(.3f, .3f, .32f)), W.Ground(x, z, 5), Quaternion.identity, new Vector3(.25f, 5, .25f)); Obj("Lampe", post.transform, sphere, W.Mat("Lampe", new Color(1f, .93f, .7f)), W.Ground(x, z, 10.2), Quaternion.identity, Vector3.one * 1.2f); }
            if (p.cannons) for (double s = 60; s < p.len; s += 140) { var at = Game.PisteAt(p, s); double d = GMath.Hypot(at.dx, at.dz); if (d == 0) d = 1; double x = at.x + at.dz / d * (p.width / 2 + 3), z = at.z - at.dx / d * (p.width / 2 + 3); Obj("Enneigeur", g.transform, cylinder, W.Mat("Enneigeur", new Color(.85f, .2f, .15f)), W.Ground(x, z, 2), Quaternion.Euler(30, W.Yaw(at.dx, at.dz), 0), new Vector3(.6f, 2, .6f)); }
            pistes[p] = g;
        }
        public void RefreshPisteColors() { foreach (var kv in pistes) { var mr = kv.Value.GetComponent<MeshRenderer>(); var mats = mr.sharedMaterials; mats[0] = PisteMat(kv.Key); mr.sharedMaterials = mats; } }

        void MakeLift(Lift l)
        {
            GameObject old; if (lifts.TryGetValue(l, out old)) Destroy(old);
            var def = Catalog.Lift(l.type); var g = new GameObject("Remontee " + l.name); g.transform.SetParent(liftRoot, false);
            float yaw = W.Yaw(l.b.x - l.a.x, l.b.z - l.a.z); var rot = Quaternion.Euler(0, yaw, 0);
            var steel = W.Mat("Acier", new Color(.55f, .57f, .6f), .5f); var house = W.Mat("Gare", new Color(.72f, .23f, .18f), .2f);
            float big = def.kind == "cablecar" || def.kind == "funi" ? 1.6f : def.kind == "gondola" ? 1.25f : def.kind == "tow" || def.kind == "carpet" ? .55f : 1f;
            foreach (var st in new[] { l.a, l.b })
            {
                var c = W.U(st); Obj("Gare", g.transform, box, house, c + Vector3.up * 3 * big, rot, new Vector3(12, 6, 16) * big);
                Obj("Toit", g.transform, roof, W.Mat("Toit_gare", new Color(.25f, .25f, .28f)), c + Vector3.up * 6 * big, rot * Quaternion.Euler(0, 90, 0), new Vector3(18, 6, 14) * big);
                if (def.kind != "carpet" && def.kind != "funi") Obj("Poulie", g.transform, cylinder, steel, c + Vector3.up * (float)(def.kind == "tow" ? 5 : 7.5), rot, new Vector3(8, .4f, 8) * big);
            }
            foreach (var cp in l.path) if (cp.pylon) { var bot = W.U(cp.x, cp.g, cp.z); float h = (float)(cp.y - cp.g); Obj("Pylone", g.transform, cylinder, steel, bot + Vector3.up * h / 2, Quaternion.identity, new Vector3(1.2f * big, h / 2, 1.2f * big)); Obj("Traverse", g.transform, box, steel, bot + Vector3.up * h, rot, new Vector3(8 * big, .5f, .8f)); }
            if (def.kind == "funi") { }                                    // funiculaire souterrain : rien de visible entre les gares
            else if (def.kind == "carpet")
            {
                var rail = Ribbon("Tapis", new[] { l.a.Xz, l.b.Xz }, 2, .4f, W.Mat("Tapis", new Color(.1f, .35f, .55f)), null, 0, g.transform);
                rail.name = "Tapis";
            }
            else
            {
                var side = Quaternion.Euler(0, yaw, 0) * Vector3.right * (def.kind == "tow" ? 1.2f : 3.2f) * big;
                for (int s = -1; s <= 1; s += 2)
                {
                    var lrGo = new GameObject("Cable"); lrGo.transform.SetParent(g.transform, false); var lr = lrGo.AddComponent<LineRenderer>();
                    lr.positionCount = l.path.Count; for (int i = 0; i < l.path.Count; i++) lr.SetPosition(i, W.U(l.path[i].x, l.path[i].y, l.path[i].z) + side * s);
                    lr.widthMultiplier = .25f * big; lr.sharedMaterial = W.Unlit("Cable", new Color(.12f, .12f, .14f)); lr.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
            lifts[l] = g;
        }

        void MakeBuilding(Building b)
        {
            GameObject old; if (blds.TryGetValue(b, out old)) Destroy(old);
            var def = b.Def; var g = new GameObject(def.name + " " + b.id); g.transform.SetParent(bldRoot, false);
            var c = W.U(b.x, b.y, b.z); var rot = Quaternion.Euler(0, (float)(-b.rot * Mathf.Rad2Deg), 0); float r = (float)def.r;
            Color body = def.type == "lodging" ? (def.seg == 2 ? new Color(.45f, .32f, .22f) : def.seg == 1 ? new Color(.58f, .42f, .28f) : new Color(.70f, .58f, .42f))
                : def.type == "food" || def.type == "drink" ? new Color(.62f, .38f, .22f) : def.type == "rescue" || def.type == "heli" ? new Color(.85f, .2f, .18f)
                : def.type == "ticket" ? new Color(.2f, .45f, .75f) : def.type == "garage" ? new Color(.85f, .55f, .1f) : def.type == "school" || def.type == "kids" ? new Color(.95f, .55f, .15f) : new Color(.62f, .55f, .48f);
            string look = def.look ?? "";
            if (look == "parking" || look == "bus" || look == "snowpark" || look == "rink" || look == "luge" || look == "garden" || look == "stadium" || look == "olympic" || look == "heli")
            {
                Color plate = look == "parking" || look == "bus" ? new Color(.3f, .3f, .32f) : look == "rink" ? new Color(.7f, .85f, .95f) : look == "heli" ? new Color(.35f, .35f, .38f) : new Color(.92f, .95f, 1f);
                Obj("Emprise", g.transform, cylinder, W.Mat("Sol_" + look, plate, .3f), c + Vector3.up * .2f, rot, new Vector3(r * 1.8f, .2f, r * 1.8f));
                if (look == "snowpark") for (int i = 0; i < 4; i++) Obj("Module", g.transform, box, W.Mat("Module", new Color(.85f, .9f, .97f)), c + rot * new Vector3(0, 1.5f, (i - 1.5f) * r * .4f), rot * Quaternion.Euler(-12, 0, 0), new Vector3(8, 3, 6));
                if (look == "stadium" || look == "olympic") Obj("Tribune", g.transform, box, W.Mat("Tribune", new Color(.3f, .35f, .55f)), c + rot * new Vector3(r * .7f, 4, 0), rot, new Vector3(6, 8, r * 1.4f));
                if (look == "heli") Obj("H", g.transform, box, W.Mat("Marquage", Color.white), c + Vector3.up * .35f, rot, new Vector3(r * .6f, .1f, r * .15f));
                if (look == "parking") for (int i = 0; i < 6; i++) Obj("Voiture", g.transform, box, W.Mat("Voiture" + i % 3, i % 3 == 0 ? new Color(.7f, .1f, .1f) : i % 3 == 1 ? new Color(.15f, .25f, .6f) : new Color(.85f, .85f, .85f)), c + rot * new Vector3((i % 3 - 1) * 6, .8f, (i / 3 - .5f) * 9), rot, new Vector3(2, 1.5f, 4.2f));
            }
            else
            {
                float h = Mathf.Clamp(r * (def.type == "lodging" ? .9f : .55f), 4, def.id == "palace" ? 30 : 22), w = r * 1.25f;
                Obj("Corps", g.transform, box, W.Mat("Bat_" + def.type + def.seg, body, .15f), c + Vector3.up * h / 2, rot, new Vector3(w, h, w * .8f));
                Obj("Toit", g.transform, roof, W.Mat("Toit_bois", new Color(.23f, .18f, .15f), .1f), c + Vector3.up * h, rot, new Vector3(w * 1.15f, w * .7f, w * .95f));
                Obj("Neige_toit", g.transform, roof, W.Mat("Neige_toit", new Color(.95f, .97f, 1f), .3f), c + Vector3.up * (h + .25f), rot, new Vector3(w * 1.1f, w * .66f, w * .9f));
            }
            blds[b] = g;
        }
        void MakeDecor(Decor d)
        {
            var c = W.Ground(d.x, d.z); var g = new GameObject(d.k == "church" ? "Eglise" : "Chalet du village"); g.transform.SetParent(decorRoot, false);
            float r = (float)d.r;
            Obj("Murs", g.transform, box, W.Mat("Village_murs", new Color(.86f, .8f, .7f)), c + Vector3.up * r * .4f, Quaternion.identity, new Vector3(r * 1.2f, r * .8f, r));
            Obj("Toit", g.transform, roof, W.Mat("Toit_bois", new Color(.23f, .18f, .15f), .1f), c + Vector3.up * r * .8f, Quaternion.identity, new Vector3(r * 1.35f, r * .8f, r * 1.15f));
            if (d.k == "church") Obj("Clocher", g.transform, box, W.Mat("Village_murs", new Color(.86f, .8f, .7f)), c + new Vector3(r * .5f, r * 1.3f, 0), Quaternion.identity, new Vector3(4, r * 2.6f, 4));
        }

        // ---------- éléments mobiles (dessinés par instanciation) ----------
        readonly Dictionary<int, List<Matrix4x4>> people = new Dictionary<int, List<Matrix4x4>>();
        readonly List<Matrix4x4> chairs = new List<Matrix4x4>(), cabins = new List<Matrix4x4>(), bigCabins = new List<Matrix4x4>(), tbars = new List<Matrix4x4>(), machines = new List<Matrix4x4>(), redCross = new List<Matrix4x4>();
        public bool bigPeople = true;
        void LateUpdate()
        {
            if (G == null) return;
            foreach (var l in people.Values) l.Clear(); chairs.Clear(); cabins.Clear(); bigCabins.Clear(); tbars.Clear(); machines.Clear(); redCross.Clear();
            float ps = bigPeople ? 2f : 1f;
            foreach (var l in G.lifts) LiftVehicles(l);
            foreach (var v in Sim.Visitors)
            {
                if (v.state == "gone" || v.state == "pending" || v.state == "inS" || v.state == "ride") continue;
                List<Matrix4x4> list; if (!people.TryGetValue(v.color, out list)) people[v.color] = list = new List<Matrix4x4>();
                bool down = v.state == "down" || v.state == "hurt";
                list.Add(Matrix4x4.TRS(W.U(v.x, v.y + (down ? .5 : 1.0) * ps, v.z), Quaternion.Euler(down ? 80 : 0, W.Yaw(v.hx, v.hz), 0), new Vector3(.7f, v.child ? .55f : .85f, .7f) * ps));
            }
            foreach (var g in Sim.Groomers) if (g.state != "parked" && g.state != "idle") machines.Add(Matrix4x4.TRS(W.U(g.x, g.y + 1.6, g.z), Quaternion.Euler(0, W.Yaw(g.hx, g.hz), 0), new Vector3(4.2f, 3.2f, 6.5f)));
            foreach (var r in Sim.Rescues) redCross.Add(Matrix4x4.TRS(W.U(r.x, r.y + 1.2, r.z), Quaternion.identity, r.kind == "heli" ? new Vector3(4, 3, 10) : new Vector3(1.6f, 1.2f, 2.6f)));
            foreach (var kv in people) Draw(capsule, W.Mat("Skieur_" + kv.Key, W.Hex(kv.Key), .3f, true), kv.Value);
            Draw(box, W.Mat("Siege", new Color(.15f, .15f, .18f), .3f, true), chairs);
            Draw(box, W.Mat("Cabine", new Color(.85f, .2f, .18f), .5f, true), cabins);
            Draw(box, W.Mat("Benne", new Color(.92f, .92f, .95f), .6f, true), bigCabins);
            Draw(box, W.Mat("Perche", new Color(.4f, .4f, .42f), .3f, true), tbars);
            Draw(box, W.Mat("Dameuse", new Color(.95f, .45f, .05f), .3f, true), machines);
            Draw(box, W.Mat("Secours", new Color(.9f, .1f, .1f), .3f, true), redCross);
        }
        readonly Matrix4x4[] buf = new Matrix4x4[1023];
        void Draw(Mesh mesh, Material mat, List<Matrix4x4> list)
        {
            for (int i = 0; i < list.Count; i += 1023) { int n = Mathf.Min(1023, list.Count - i); list.CopyTo(i, buf, 0, n); Graphics.DrawMeshInstanced(mesh, 0, mat, buf, n); }
        }
        void LiftVehicles(Lift l)
        {
            var def = Catalog.Lift(l.type); if (l.path == null || l.L <= 0) return;
            double L2 = 2 * l.L; float yaw = W.Yaw(l.b.x - l.a.x, l.b.z - l.a.z);
            var right = Quaternion.Euler(0, yaw, 0) * Vector3.right; float big = def.kind == "cablecar" || def.kind == "funi" ? 1.6f : def.kind == "gondola" ? 1.25f : 1f;
            float ps = bigPeople ? 2f : 1f;
            for (int i = 0; i < l.n; i++)
            {
                double s = (l.phase + i * l.D) % L2; bool up = s < l.L; var p = Game.LiftPos(l, up ? s : L2 - s);
                var pos = W.U(p) + right * (up ? 1 : -1) * (def.kind == "tow" ? 1.2f : 3.2f) * big; var rot = Quaternion.Euler(0, up ? yaw : yaw + 180, 0);
                Vector3 seat;
                if (def.kind == "tow") { tbars.Add(Matrix4x4.TRS(pos - Vector3.up * 2.5f, rot, new Vector3(.15f, 5, .15f))); seat = pos - Vector3.up * 4.2f; }
                else if (def.kind == "chair") { chairs.Add(Matrix4x4.TRS(pos - Vector3.up * 2.6f, rot, new Vector3(def.seats * .6f, .5f, .8f))); seat = pos - Vector3.up * 1.8f; }
                else if (def.kind == "gondola") { cabins.Add(Matrix4x4.TRS(pos - Vector3.up * 3.2f, rot, new Vector3(2.4f, 2.6f, 2.4f) * big)); seat = pos - Vector3.up * 3f; }
                else if (def.kind == "cablecar") { bigCabins.Add(Matrix4x4.TRS(pos - Vector3.up * 5f, rot, new Vector3(6, 4.2f, 9))); seat = pos - Vector3.up * 5f; }
                else if (def.kind == "funi") continue;                         // les rames circulent dans le tunnel
                else { seat = W.Ground(p.x, p.z, .6); }
                if (l.veh != null && i < l.veh.Count) { int k = 0; foreach (var v in l.veh[i]) { List<Matrix4x4> list; if (!people.TryGetValue(v.color, out list)) people[v.color] = list = new List<Matrix4x4>(); list.Add(Matrix4x4.TRS(seat + right * (k++ - l.veh[i].Count / 2f) * .7f + Vector3.up * .5f * ps, rot, new Vector3(.7f, .85f, .7f) * ps)); } }
            }
        }

        // ---------- jour et nuit ----------
        public void UpdateSky(Camera cam)
        {
            if (G == null || sun == null) return;
            double h = G.time / 3600.0; float day = Mathf.Clamp01((float)((h - 6.5) / 13.0));
            float elev = Mathf.Sin(day * Mathf.PI) * 58 - 4;
            sun.transform.rotation = Quaternion.Euler(Mathf.Max(-10, elev), 140 + day * 120, 0);
            float k = Mathf.Clamp01((elev + 6) / 20f);
            var wd = Weather.Def; float overcast = wd.sky == 2 ? .55f : wd.sky == 1 ? .8f : 1f;
            sun.intensity = Mathf.Lerp(.05f, 1.25f, k) * overcast; sun.color = Color.Lerp(new Color(1f, .62f, .4f), new Color(1f, .97f, .92f), k);
            var skyDay = G.summer ? new Color(.45f, .68f, .92f) : new Color(.55f, .74f, .93f); var skyNight = new Color(.03f, .05f, .12f);
            var sky = Color.Lerp(skyNight, Color.Lerp(new Color(.7f, .74f, .78f), skyDay, overcast), k);
            if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = sky; }
            RenderSettings.ambientLight = Color.Lerp(new Color(.08f, .1f, .18f), new Color(.62f, .66f, .72f), k);
            RenderSettings.fog = true; RenderSettings.fogColor = sky; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 2500; RenderSettings.fogEndDistance = wd.sky == 2 ? 9000 : 16000;
        }
    }
}
