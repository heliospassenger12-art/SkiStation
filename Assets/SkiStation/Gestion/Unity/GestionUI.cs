// SkiStation — interface de la gestion (IMGUI, sans dépendance) : menu de départ, barre du haut (horloge centrale, vitesses,
// trésorerie, satisfaction, PRESTIGE en étoiles, NOTORIÉTÉ en %), construction avec devis avant validation, fiches,
// Finances, Recherche, Exploitation, Événements, Carrière, Journal, bilans quotidiens, événements du matin.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SkiStation.Gestion;

namespace SkiStation.GestionUnity
{
    public sealed class GestionUI : MonoBehaviour
    {
        public static bool PointerOverUI, TextFocus; public static GUIStyle Rich;
        public GestionBootstrap boot; public GestionTools tools; public GestionCamera camCtl; public GestionArcade arcade; public GestionRender render; public GestionWorld world;
        GameState G { get { return Game.G; } }
        string panel; string tab = ""; Vector2 scroll, scroll2; readonly List<Rect> rects = new List<Rect>();
        readonly List<KeyValuePair<string, float>> toasts = new List<KeyValuePair<string, float>>();
        DayReport report; bool showReport; float scale = 1;
        // menu de départ
        string mMode = "career", mScn = "first", importCode = ""; int mSeed = 1337, mLevel = 1; bool mUnlimited, mAll, mCasual;
        GUIStyle box, title, small, btnOn, wrap;

        void Awake()
        {
            Game.Bus += (ev, a, b) =>
            {
                if (ev == "toast") Toast((string)a);
                else if (ev == "day:end") { report = (DayReport)a; showReport = report.notes.Count > 0 || report.victory || report.failed; }
            };
        }
        public void Toast(string m) { toasts.Add(new KeyValuePair<string, float>(m, Time.unscaledTime + 6)); if (toasts.Count > 5) toasts.RemoveAt(0); }

        void Styles()
        {
            scale = Mathf.Clamp(Screen.height / 1000f, .8f, 2f);
            GUI.skin.label.fontSize = GUI.skin.button.fontSize = GUI.skin.toggle.fontSize = GUI.skin.textField.fontSize = (int)(14 * scale);
            GUI.skin.label.richText = true; GUI.skin.button.richText = true;
            if (Rich == null) { Rich = new GUIStyle(GUI.skin.label) { richText = true }; }
            Rich.fontSize = (int)(14 * scale);
            box = new GUIStyle(GUI.skin.box) { padding = new RectOffset(10, 10, 8, 8) };
            title = new GUIStyle(GUI.skin.label) { fontSize = (int)(18 * scale), fontStyle = FontStyle.Bold, richText = true };
            small = new GUIStyle(GUI.skin.label) { fontSize = (int)(12 * scale), richText = true, wordWrap = true };
            wrap = new GUIStyle(GUI.skin.label) { wordWrap = true, richText = true, fontSize = (int)(14 * scale) };
            btnOn = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold }; btnOn.normal.textColor = new Color(1f, .85f, .3f);
        }
        float S(float v) { return v * scale; }
        Rect Box(Rect r) { rects.Add(r); GUI.Box(r, GUIContent.none, box); GUI.Box(r, GUIContent.none, box); return r; }

        void Update()
        {
            var mp = Input.mousePosition; var p = new Vector2(mp.x, Screen.height - mp.y);
            PointerOverUI = rects.Any(r => r.Contains(p));
            if (G != null && !TextFocus && !(arcade != null && arcade.Playing))
            {
                if (Input.GetKeyDown(KeyCode.Space)) G.paused = !G.paused;
                if (Input.GetKeyDown(KeyCode.Alpha1)) Actions.Speed(1); if (Input.GetKeyDown(KeyCode.Alpha2)) Actions.Speed(3); if (Input.GetKeyDown(KeyCode.Alpha3)) Actions.Speed(8);
            }
        }

        void OnGUI()
        {
            Styles(); rects.Clear(); TextFocus = GUIUtility.keyboardControl != 0;
            if (arcade != null && arcade.Playing) return;
            if (G == null) { MainMenu(); return; }
            TopBar(); LeftBar(); Toasts();
            if (tools.mode != GestionTools.Mode.None) BuildHelp();
            else if (tools.selected != null) Inspector();
            if (panel != null) Panel();
            if (MorningEvents.Current != null) Morning();
            else if (showReport && report != null) Report();
        }

        // =========================================================== MENU DE DÉPART
        void MainMenu()
        {
            var r = Box(new Rect(Screen.width / 2 - S(380), S(40), S(760), Screen.height - S(80)));
            GUILayout.BeginArea(new Rect(r.x + 14, r.y + 10, r.width - 28, r.height - 20));
            GUILayout.Label("<size=" + (int)S(30) + "><b>SkiStation</b></size>  <color=#9cc>— gestion de station de ski</color>", Rich);
            GUILayout.Label("Développez votre domaine sur un massif à 360° (1 640 → 4 200 m) : remontées, pistes, services, hébergements, sécurité, et visez les X Games puis les Jeux Olympiques. La descente arcade se fait avec le rider sur n'importe quelle piste construite.", wrap);
            if (boot.HasSave() && GUILayout.Button("▶  Continuer la partie sauvegardée", GUILayout.Height(S(36)))) boot.Continue();
            GUILayout.Space(S(6));
            GUILayout.BeginHorizontal(); if (GUILayout.Button("Carrière (scénarios)", mMode == "career" ? btnOn : GUI.skin.button)) mMode = "career"; if (GUILayout.Button("Bac à sable", mMode == "sandbox" ? btnOn : GUI.skin.button)) mMode = "sandbox"; GUILayout.EndHorizontal();
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(r.height * .5f));
            if (mMode == "career")
                foreach (var sc in Catalog.SCENARIOS)
                {
                    var rec = Scenarios.LoadRecords("skistation_scn_" + sc.id);
                    if (GUILayout.Button("<b>" + sc.name + "</b>  <color=#aaa>" + sc.tag + (sc.deadline > 0 ? " · " + sc.deadline + " jours" : "") + (rec != null ? " · record " + new string('★', int.Parse(rec.Split('|')[0])) : "") + "</color>", mScn == sc.id ? btnOn : GUI.skin.button)) mScn = sc.id;
                    if (mScn == sc.id) { GUILayout.Label(sc.brief, wrap); GUILayout.Label("<color=#9cc>Étapes : " + string.Join(" → ", sc.stages.Select(s => s.title)) + "</color>", small); }
                }
            else
            {
                mUnlimited = GUILayout.Toggle(mUnlimited, " Argent illimité"); mAll = GUILayout.Toggle(mAll, " Tout le catalogue débloqué"); mCasual = GUILayout.Toggle(mCasual, " Neige simplifiée (pas de verglas ni de fonte)");
            }
            GUILayout.EndScrollView();
            var scd = mMode == "career" ? Catalog.Scenario(mScn) : null; bool seedLock = scd != null && scd.seedLock;
            GUILayout.BeginHorizontal(); GUILayout.Label("Montagne", GUILayout.Width(S(110)));
            foreach (var m in new[] { Tuple.Create("Classique", 1337), Tuple.Create("Dents Blanches", 4242), Tuple.Create("Val Perdu", 9001), Tuple.Create("Aléatoire", 0) })
            { GUI.enabled = !seedLock; if (GUILayout.Button(m.Item1, mSeed == m.Item2 ? btnOn : GUI.skin.button)) mSeed = m.Item2; GUI.enabled = true; }
            GUILayout.EndHorizontal();
            if (seedLock) GUILayout.Label("<color=#aaa>Ce scénario se joue sur une montagne imposée.</color>", small);
            GUILayout.BeginHorizontal(); GUILayout.Label("Difficulté", GUILayout.Width(S(110)));
            string[] lv = { "Facile", "Normale", "Difficile" }; for (int i = 0; i < 3; i++) if (GUILayout.Button(lv[i], mLevel == i ? btnOn : GUI.skin.button)) mLevel = i;
            GUILayout.EndHorizontal();
            if (GUILayout.Button("<b>Nouvelle partie</b>", GUILayout.Height(S(40))))
            {
                int seed = seedLock ? scd.start.seed : mSeed == 0 ? UnityEngine.Random.Range(1, 99999) : mSeed;
                boot.NewGame(new NewGameOptions { mode = mMode, scn = mMode == "career" ? mScn : null, seed = seed, level = mLevel, unlimited = mMode == "sandbox" && mUnlimited, allUnlocked = mMode == "sandbox" && mAll, casual = mCasual });
            }
            GUILayout.Space(S(8)); GUILayout.Label("<color=#aaa>Code de sauvegarde (version web ou autre appareil) :</color>", small);
            GUILayout.BeginHorizontal(); importCode = GUILayout.TextField(importCode, GUILayout.Width(r.width - S(170))); if (GUILayout.Button("Importer")) { if (!boot.Import(importCode)) Toast("Code de sauvegarde invalide."); } GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        // =========================================================== BARRE DU HAUT
        void TopBar()
        {
            Box(new Rect(0, 0, Screen.width, S(46)));
            GUILayout.BeginArea(new Rect(8, S(6), Screen.width - 16, S(40))); GUILayout.BeginHorizontal();
            var w = G.weather; var wd = Weather.Def;
            GUILayout.Label("<b>" + G.name + "</b>  " + Game.DateStr(G.day) + (G.summer ? " ☀ été" : "") + " · An " + G.year + "  <b>" + GMath.HHMM(G.time) + "</b>  " + wd.icon + " " + w.temp + " °C · vent " + Game.R(Weather.WindNow) + " km/h", Rich, GUILayout.Width(S(520)));
            if (GUILayout.Button(G.paused ? "▶" : "⏸", GUILayout.Width(S(40)))) G.paused = !G.paused;
            foreach (var sp in new[] { 1, 3, 8 }) if (GUILayout.Button("x" + sp, !G.paused && G.speed == sp ? btnOn : GUI.skin.button, GUILayout.Width(S(42)))) Actions.Speed(sp);
            if (GUILayout.Button("⏭", GUILayout.Width(S(40)))) { if (!Sim.SkipToEnd()) Toast("Attendez que la station soit vide (fin d'après-midi) pour passer au lendemain."); }
            GUILayout.Space(S(10));
            string money = G.unlimited ? "∞" : GMath.Eur(G.money);
            GUILayout.Label("<b><color=" + (G.money < 0 ? "#f66" : "#8f8") + ">" + money + "</color></b>  👥 " + Sim.Present + " / " + G.visitorsToday + "  😊 " + G.sat + " %  " + Stars(G.prestige) + " <color=#fc6>Prestige " + G.prestige + "</color>  📣 Notoriété " + Game.R(G.fame) + " %", Rich);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal(); GUILayout.EndArea();
        }
        public static string Stars(double prestige) { double s = Game.PrestigeStars(prestige); int full = (int)Math.Floor(s); return "<color=#fc3>" + new string('★', full) + (s - full >= .5 ? "½" : "") + "</color><color=#555>" + new string('☆', 5 - full - (s - full >= .5 ? 1 : 0)) + "</color>"; }

        // =========================================================== MENU DE GAUCHE
        static readonly string[][] MENU = { new[] { "build", "🏗 Construire" }, new[] { "finance", "💶 Finances" }, new[] { "research", "🔬 Recherche" }, new[] { "ops", "⛑ Exploitation" }, new[] { "events", "🏆 Événements" }, new[] { "career", "🎖 Carrière" }, new[] { "journal", "📰 Journal" }, new[] { "options", "⚙ Options" } };
        void LeftBar()
        {
            var r = Box(new Rect(0, S(50), S(150), S(36) * MENU.Length + 12));
            for (int i = 0; i < MENU.Length; i++) if (GUI.Button(new Rect(6, r.y + 6 + i * S(36), r.width - 12, S(32)), MENU[i][1], panel == MENU[i][0] ? btnOn : GUI.skin.button)) { panel = panel == MENU[i][0] ? null : MENU[i][0]; scroll = Vector2.zero; tab = ""; }
            var hint = Scenarios.Hint(); var dl = Scenarios.DaysLeft();
            if (hint != null) { var hr = Box(new Rect(0, r.yMax + 6, S(300), S(110))); GUI.Label(new Rect(8, hr.y + 4, hr.width - 16, hr.height - 8), hint + (dl.HasValue ? "  <color=#fc6>(" + dl + " jours restants)</color>" : ""), small); }
        }
        void Toasts()
        {
            toasts.RemoveAll(t => t.Value < Time.unscaledTime);
            float y = Screen.height - S(40);
            for (int i = toasts.Count - 1; i >= 0; i--) { var r = Box(new Rect(Screen.width / 2 - S(300), y - S(30), S(600), S(28))); GUI.Label(new Rect(r.x + 8, r.y + 3, r.width - 16, r.height), toasts[i].Key, Rich); y -= S(32); }
            if (tools.message != null) { Toast(tools.message); tools.message = null; }
        }

        // =========================================================== PANNEAUX
        void Panel()
        {
            var r = Box(new Rect(S(156), S(50), S(560), Screen.height - S(110)));
            GUILayout.BeginArea(new Rect(r.x + 10, r.y + 8, r.width - 20, r.height - 16));
            GUILayout.BeginHorizontal(); GUILayout.Label(MENU.First(m => m[0] == panel)[1], title); GUILayout.FlexibleSpace(); if (GUILayout.Button("✕", GUILayout.Width(S(32)))) panel = null; GUILayout.EndHorizontal();
            scroll = GUILayout.BeginScrollView(scroll);
            switch (panel)
            {
                case "build": BuildPanel(); break; case "finance": FinancePanel(); break; case "research": ResearchPanel(); break; case "ops": OpsPanel(); break;
                case "events": EventsPanel(); break; case "career": CareerPanel(); break; case "journal": JournalPanel(); break; case "options": OptionsPanel(); break;
            }
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
        bool Tabs(params string[] names) { if (tab == "" || !names.Contains(tab)) tab = names[0]; GUILayout.BeginHorizontal(); foreach (var n in names) if (GUILayout.Button(n, tab == n ? btnOn : GUI.skin.button)) { tab = n; scroll = Vector2.zero; } GUILayout.EndHorizontal(); return true; }
        void H(string t) { GUILayout.Space(S(6)); GUILayout.Label("<b>" + t + "</b>", Rich); }
        void L(string t) { GUILayout.Label(t, wrap); }
        void Row(string k, string v) { GUILayout.BeginHorizontal(); GUILayout.Label(k, Rich); GUILayout.FlexibleSpace(); GUILayout.Label(v, Rich); GUILayout.EndHorizontal(); }

        // ---------- construction ----------
        void BuildPanel()
        {
            Tabs("Remontées", "Pistes", "Bâtiments", "Routes");
            if (tab == "Remontées")
                foreach (var d in Catalog.LIFTS)
                {
                    bool ok = Game.LiftUnlocked(d);
                    GUI.enabled = ok;
                    if (GUILayout.Button("<b>" + d.name + "</b>  <color=#aaa>" + d.seats + " pl. · " + d.speed + " m/s · portée " + Game.MaxLen(d) + " m · dès " + GMath.Eur(d.cost * .45) + (ok ? "" : " · palier " + d.tier + " de recherche requis") + "</color>")) { tools.Begin(GestionTools.Mode.Lift, d.id); panel = null; }
                    GUI.enabled = true;
                }
            else if (tab == "Pistes")
            {
                L("Une piste se descend : du départ en haut vers l'arrivée en bas. La difficulté est calculée sur le relief réellement traversé.");
                GUILayout.BeginHorizontal(); GUILayout.Label("Largeur " + tools.pisteWidth + " m", GUILayout.Width(S(130))); tools.pisteWidth = Math.Round(GUILayout.HorizontalSlider((float)tools.pisteWidth, 15, 60) / 5) * 5; GUILayout.EndHorizontal();
                if (GUILayout.Button("✏ Tracer à la main (clic par point, Entrée pour le devis)")) { tools.Begin(GestionTools.Mode.Piste); panel = null; }
                GUILayout.Label("<b>Tracé assisté</b> : cliquez sur le départ (arrivée d'une remontée), le jeu trouve la descente jusqu'au front de neige.", wrap);
                GUILayout.BeginHorizontal(); for (int i = 0; i < 4; i++) if (GUILayout.Button((i == 3 ? "Libre" : Catalog.DIFF_NAMES[i]), tools.autoEase == i ? btnOn : GUI.skin.button)) tools.autoEase = i; GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal(); GUILayout.Label("Arrivée", GUILayout.Width(S(70)));
                foreach (var d in new[] { Tuple.Create("any", "Autre remontée ou front de neige"), Tuple.Create("self", "Retour à sa remontée"), Tuple.Create("hub", "Front de neige") })
                    if (GUILayout.Button(d.Item2, Game.AutoDest == d.Item1 ? btnOn : GUI.skin.button)) Game.AutoDest = d.Item1;
                GUILayout.EndHorizontal();
                if (GUILayout.Button("🧭 Tracé assisté")) { tools.Begin(GestionTools.Mode.PisteAuto); panel = null; }
            }
            else if (tab == "Bâtiments")
                foreach (var grp in Catalog.BUILDINGS.Where(b => !b.id.StartsWith("_")).GroupBy(b => b.cat))
                {
                    H(grp.Key);
                    foreach (var d in grp)
                    {
                        bool ok = Game.BuildingUnlocked(d); GUI.enabled = ok;
                        if (GUILayout.Button("<b>" + d.name + "</b>  <color=#aaa>" + GMath.Eur(d.cost) + (d.cap > 0 ? " · cap. " + d.cap : "") + (ok ? "" : " · recherche requise") + "</color>")) { tools.Begin(GestionTools.Mode.Building, d.id); panel = null; }
                        GUI.enabled = true;
                    }
                }
            else
            {
                L("Une route part du village ou d'une route existante, pente 16 % maximum. Elle permet de construire hors du village les bâtiments qui doivent être desservis.");
                if (GUILayout.Button("🛣 Tracer une route")) { tools.Begin(GestionTools.Mode.Road); panel = null; }
            }
        }
        void BuildHelp()
        {
            var r = Box(new Rect(Screen.width - S(400), S(50), S(392), S(330)));
            GUILayout.BeginArea(new Rect(r.x + 10, r.y + 8, r.width - 20, r.height - 16));
            var m = tools.mode; Analysis an = tools.liftAn as Analysis ?? (Analysis)tools.pisteAn ?? (Analysis)tools.bldAn ?? tools.roadAn;
            string what = m == GestionTools.Mode.Lift ? Catalog.Lift(tools.type).name : m == GestionTools.Mode.Building ? Catalog.Building(tools.type).name : m == GestionTools.Mode.Road ? "Route" : "Piste";
            GUILayout.Label("<b>" + what + "</b>", title);
            if (m == GestionTools.Mode.Lift) L(tools.pts.Count == 0 ? "Cliquez la gare de départ (en bas), près du front de neige ou d'une arrivée de piste." : tools.pts.Count == 1 ? "Cliquez la gare d'arrivée (en haut)." : "Devis prêt : vérifiez puis validez.");
            if (m == GestionTools.Mode.Piste) L("Clic : ajouter un point · Retour arrière : retirer · Entrée : valider.");
            if (m == GestionTools.Mode.Road) L("Clic : ajouter un point · Retour arrière : retirer · Entrée : valider.");
            if (m == GestionTools.Mode.PisteAuto) L(tools.autoErr ?? "Cliquez le point de départ de la descente (" + (tools.autoEase == 3 ? "difficulté libre" : "visée : " + Catalog.DIFF_NAMES[tools.autoEase].ToLowerInvariant()) + ").");
            if (m == GestionTools.Mode.Building) L("Clic : construire · R : tourner · Échap : terminer.");
            if (an != null)
            {
                H("Devis avant validation");
                Row("Coût", "<b>" + GMath.Eur(an.cost) + "</b>");
                var la = an as LiftAnalysis; var pa = an as PisteAnalysis; var ba = an as BuildingAnalysis; var ra = an as RoadAnalysis;
                if (la != null) { Row("Longueur / dénivelé", GMath.Num(la.len) + " m / " + GMath.Num(la.vert) + " m"); Row("Portée maximale", GMath.Num(la.maxLen) + " m"); Row("Débit", GMath.Num(la.capacity) + " pers./h · " + la.vehicles + " véhicules"); Row("Trajet", la.rideMin.ToString("0.0") + " min"); Row("Entretien", GMath.Eur(la.maint) + " / jour"); }
                if (pa != null && pa.pts != null) { Row("Départ", pa.startLift != null ? "⬆ arrivée du " + pa.startLift : "—"); Row("Arrivée", pa.joins != null ? "↘ jonction avec " + pa.joins : pa.endLift != null ? "⬇ départ du " + pa.endLift : "—"); }
                if (pa != null && pa.pts != null) { Row("Longueur / dénivelé", GMath.Num(pa.len) + " m / " + GMath.Num(pa.drop) + " m"); Row("Pente moyenne / forte", Game.R(pa.avg * 100) + " % / " + Game.R(pa.p90 * 100) + " %"); Row("Difficulté", "<color=" + Catalog.DIFF_COLORS[pa.diff] + "><b>" + Catalog.DIFF_NAMES[pa.diff] + "</b></color>"); }
                if (ba != null) { Row("Pente", Game.R(ba.slope * 100) + " %"); Row("Terrassement", GMath.Eur(ba.earth)); }
                if (ra != null) { Row("Longueur", GMath.Num(ra.len) + " m"); Row("Pente max.", Game.R(ra.maxSlope * 100) + " %"); }
                if (an.trees > 0) Row("Arbres abattus", an.trees.ToString());
                if (an.err != null) GUILayout.Label("<color=#f77>✘ " + an.err + "</color>", wrap);
                else if (an.warn != null) GUILayout.Label("<color=#fc6>⚠ " + an.warn + "</color>", wrap);
            }
            GUILayout.FlexibleSpace(); GUILayout.BeginHorizontal();
            GUI.enabled = tools.HasQuote && an != null && an.ok; if (m != GestionTools.Mode.Building && GUILayout.Button("✔ Valider")) tools.Validate(); GUI.enabled = true;
            if (GUILayout.Button(m == GestionTools.Mode.Building ? "Terminer" : "Annuler")) tools.Cancel();
            GUILayout.EndHorizontal(); GUILayout.EndArea();
        }

        // ---------- fiche de l'objet sélectionné ----------
        string rename = ""; object renameFor;
        void Inspector()
        {
            var o = tools.selected; var p = o as Piste; var l = o as Lift; var b = o as Building; var v = o as Visitor;
            if (p != null && !G.pistes.Contains(p) || l != null && !G.lifts.Contains(l) || b != null && !G.buildings.Contains(b)) { tools.Select(null); return; }
            var r = Box(new Rect(Screen.width - S(420), S(50), S(412), Screen.height - S(110)));
            GUILayout.BeginArea(new Rect(r.x + 10, r.y + 8, r.width - 20, r.height - 16));
            GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace(); if (GUILayout.Button("✕", GUILayout.Width(S(32)))) tools.Select(null); GUILayout.EndHorizontal();
            scroll2 = GUILayout.BeginScrollView(scroll2);
            if (p != null) PisteSheet(p); else if (l != null) LiftSheet(l); else if (b != null) BuildingSheet(b); else if (v != null) VisitorSheet(v);
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
        void Rename(object o, string current)
        {
            if (renameFor != o) { renameFor = o; rename = current; }
            GUILayout.BeginHorizontal(); rename = GUILayout.TextField(rename, 28); if (GUILayout.Button("Renommer", GUILayout.Width(S(100)))) Actions.Rename(o, rename); GUILayout.EndHorizontal();
        }
        bool sure; object sureFor;
        void Demolish(object o)
        {
            if (sureFor != o) { sure = false; sureFor = o; }
            if (GUILayout.Button(sure ? "<color=#f66>Confirmer la démolition</color>" : "Démolir")) { if (!sure) sure = true; else { var e = Game.Demolish(o); if (e != null) Toast(e); else { Toast("Démolition effectuée."); tools.Select(null); } sure = false; } }
        }
        void PisteSheet(Piste p)
        {
            GUILayout.Label("<b>" + p.name + "</b>  <color=" + Catalog.DIFF_COLORS[p.diff] + ">● " + Catalog.DIFF_NAMES[p.diff] + "</color>", title);
            Rename(p, p.name);
            Row("Longueur / dénivelé", GMath.Num(p.len) + " m / " + GMath.Num(p.drop) + " m");
            Row("Sommet", GMath.Num(p.topAlt) + " m"); Row("État", Weather.State(p) + (Game.PisteOpen(p) ? "" : " · <color=#f77>fermée</color>"));
            Row("Neige", Game.R(p.depth) + " cm · damage " + Game.R(p.groomed * 100) + " %"); Row("Skieurs", p.on + " · passages " + p.passes);
            Row("Sécurisée", p.secured ? "oui" : "<color=#fc6>non</color>");
            if (GUILayout.Button(p.open ? "Fermer la piste" : "Ouvrir la piste")) Actions.ToggleOpen(p);
            if (!p.cannons && GUILayout.Button("Installer des enneigeurs — " + GMath.Eur(Game.R(p.len * 55)))) Msg(Actions.Cannons(p), "Enneigeurs installés.");
            if (!p.lit && Game.Researched("night") && GUILayout.Button("Éclairage nocturne — " + GMath.Eur(Game.R(p.len * 45)))) Msg(Actions.Light(p), "Éclairage installé.");
            GUILayout.BeginHorizontal(); GUILayout.Label("Damage"); string[] pr = { "Jamais", "Normal", "Prioritaire" }; for (int i = 0; i < 3; i++) if (GUILayout.Button(pr[i], p.prio == i ? btnOn : GUI.skin.button)) Actions.GroomPrio(p, i); GUILayout.EndHorizontal();
            if (GUILayout.Button("<b>⛷ Descendre en arcade (rider)</b>", GUILayout.Height(S(36)))) { if (!arcade.Launch(p)) Toast(arcade.lastError ?? "Descente impossible."); }
            Demolish(p);
        }
        void LiftSheet(Lift l)
        {
            var d = Catalog.Lift(l.type);
            GUILayout.Label("<b>" + l.name + "</b>", title); GUILayout.Label(d.name, Rich); Rename(l, l.name);
            Row("Longueur / dénivelé", GMath.Num(l.len) + " m / " + GMath.Num(l.vert) + " m"); Row("Débit", GMath.Num(l.capacity) + " pers./h");
            Row("File d'attente", l.queue.Count + " (≈ " + Game.R(Sim.LiftWait(l)) + " min)"); Row("Passagers / montées", l.riders + " / " + l.rides);
            Row("État", Game.R(l.cond * 100) + " %" + (l.windStop ? " · <color=#fc6>arrêt vent</color>" : "") + (G.time < l.brokenUntil ? " · <color=#f77>en panne</color>" : ""));
            if (GUILayout.Button(l.open ? "Fermer" : "Ouvrir")) Actions.ToggleOpen(l);
            if (Game.Researched("night") && GUILayout.Button(l.night ? "☾ Ouverte en nocturne (retirer)" : "☾ Ouvrir en nocturne")) Actions.NightLift(l);
            H("Moderniser sur la même ligne");
            foreach (var nd in Catalog.LIFTS.Where(x => x.id != l.type && x.kind != "carpet" && Game.LiftUnlocked(x) && x.tier >= d.tier))
            {
                double cost; LiftAnalysis an; var e = Game.Modernize(l, nd.id, true, out cost, out an);
                GUI.enabled = e == null;
                if (GUILayout.Button(nd.name + (e == null ? "  <color=#aaa>" + GMath.Eur(cost) + "</color>" : "  <color=#a66>" + e + "</color>"))) { var e2 = Game.Modernize(l, nd.id, false, out cost, out an); Msg(e2, "Remontée modernisée pour " + GMath.Eur(cost) + "."); }
                GUI.enabled = true;
            }
            Demolish(l);
        }
        void BuildingSheet(Building b)
        {
            var d = b.Def; var t = d.type;
            GUILayout.Label("<b>" + d.name + "</b>", title); if (!string.IsNullOrEmpty(d.desc)) GUILayout.Label(d.desc, small);
            Row("Niveau / capacité", b.level + " / " + Game.Bcap(b)); Row("Note des clients", b.rating.ToString("0.0") + " / 5");
            if (Mgmt.IsService(b) || t == "lodging") Row("Qualité", Game.R(Mgmt.Quality(b) * 100) + " %");
            Row("Aujourd'hui", b.visits + " clients · " + GMath.Eur(b.revenue)); Row("Saison", GMath.Eur(b.sRev) + " recettes · " + GMath.Eur(b.sCost) + " coûts");
            var c = Mgmt.DailyCost(b); Row("Charges du jour", GMath.Eur(c[0] + c[1]));
            if (t == "lodging") Row("Résidents", b.guests + " / " + Game.Bcap(b) + " (" + Catalog.SEG[Math.Max(0, d.seg)] + ")");
            if (d.hasPrice && t != "ticket" && t != "parking")
            {
                double rp = Mgmt.RefPrice(b);
                GUILayout.BeginHorizontal(); GUILayout.Label("Prix " + GMath.Eur(b.price), GUILayout.Width(S(120))); Actions.BuildingPrice(b, Math.Round(GUILayout.HorizontalSlider((float)b.price, (float)Math.Max(1, rp * .4), (float)(rp * 2.2)))); GUILayout.EndHorizontal();
                GUILayout.Label("<color=#aaa>Prix de référence " + GMath.Eur(rp) + "</color>", small);
            }
            if (Mgmt.IsService(b))
            {
                GUILayout.BeginHorizontal(); GUILayout.Label("Gamme", GUILayout.Width(S(80))); for (int i = 0; i < 3; i++) if (GUILayout.Button(Catalog.RANGE[i], b.range == i ? btnOn : GUI.skin.button)) Actions.SetRange(b, i); GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal(); GUILayout.Label("Personnel", GUILayout.Width(S(80))); for (int i = 0; i < 3; i++) if (GUILayout.Button(Catalog.STAFF[i], b.staffLvl == i ? btnOn : GUI.skin.button)) Actions.StaffLevel(b, i); GUILayout.EndHorizontal();
                if (t == "food" || t == "drink") { GUILayout.BeginHorizontal(); GUILayout.Label("Horaires", GUILayout.Width(S(80))); if (GUILayout.Button("Midi", b.hours != "eve" ? btnOn : GUI.skin.button)) Actions.Hours(b, "day"); if (GUILayout.Button("Midi et soir", b.hours == "eve" ? btnOn : GUI.skin.button)) Actions.Hours(b, "eve"); GUILayout.EndHorizontal(); }
            }
            if (t == "lodging")
            {
                if (GUILayout.Button((b.opts.hb ? "☑" : "☐") + " Demi-pension (+" + GMath.Eur(Mgmt.DinnerPrice(b)) + " par résident)")) Actions.HotelOption(b, "hb");
                if (GUILayout.Button(b.opts.spa ? "☑ Spa installé" : "☐ Installer un spa — " + GMath.Eur(Mgmt.SpaCost(b)))) Msg(Actions.HotelOption(b, "spa"), null);
                if (GUILayout.Button((b.opts.shuttle ? "☑" : "☐") + " Navette vers le front de neige")) Actions.HotelOption(b, "shuttle");
            }
            if (t == "school") { GUILayout.BeginHorizontal(); GUILayout.Label("Moniteurs : " + b.staff); if (GUILayout.Button("−")) Actions.Staff(b, -1); if (GUILayout.Button("+")) Actions.Staff(b, 1); GUILayout.EndHorizontal(); }
            if (t == "garage") { GUILayout.BeginHorizontal(); GUILayout.Label("Dameuses : " + b.machines); if (GUILayout.Button("−")) Msg(Actions.Machines(b, -1), null); if (GUILayout.Button("+ " + GMath.Eur(Mgmt.MachineCost))) Msg(Actions.Machines(b, 1), "Dameuse livrée."); GUILayout.EndHorizontal(); }
            if (t == "rescue") { GUILayout.BeginHorizontal(); GUILayout.Label("Pisteurs : " + b.patrol); if (GUILayout.Button("−")) Actions.Patrol(b, -1); if (GUILayout.Button("+")) Actions.Patrol(b, 1); GUILayout.EndHorizontal(); }
            if (GUILayout.Button(b.open ? "Fermer" : "Ouvrir")) Actions.ToggleOpen(b);
            if (d.cap > 0 && b.level < 3 && t != "garage" && t != "rescue" && GUILayout.Button("Agrandir — " + GMath.Eur(Game.UpgradeCost(b)))) Msg(Actions.Upgrade(b), "Agrandissement terminé.");
            if (!(b.starter && t == "ticket")) Demolish(b);
        }
        void VisitorSheet(Visitor v)
        {
            GUILayout.Label("<b>" + v.name + "</b>", title);
            GUILayout.Label(v.prof.name + " · " + Catalog.SKILL[v.skill] + (v.child ? " · enfant" : "") + (v.lodger ? " · en séjour" : " · à la journée") + (v.grp != null ? " · " + v.grp.name : "") + (v.mode != null ? " · " + (v.mode == "vtt" ? "VTT" : "randonnée") : ""), small);
            Row("Décision", "<b>" + v.decision + "</b>"); if (v.reason != "") GUILayout.Label("<color=#aaa>" + v.reason + "</color>", small);
            Row("Satisfaction", Game.R(v.sat * 100) + " %"); Row("Faim / énergie", Game.R(v.hunger * 100) + " % / " + Game.R(v.energy * 100) + " %");
            Row("Descentes", v.runs + " · " + v.km.ToString("0.0") + " km"); Row("Dépenses", GMath.Eur(v.spent)); Row("Départ prévu", GMath.HHMM(v.leaveT));
            if (v.target != null) Row("Piste visée", v.target.name);
            H("Pensées"); foreach (var t in v.thoughts) GUILayout.Label("« " + t + " »", small);
            if (GUILayout.Button("🎥 Suivre ce visiteur")) { camCtl.follow = v; camCtl.dist = Mathf.Min(camCtl.dist, 400); }
        }
        void Msg(string err, string ok) { if (err != null) Toast(err); else if (ok != null) Toast(ok); }

        // ---------- finances ----------
        void FinancePanel()
        {
            Tabs("Résumé", "Tarifs", "Banque", "Marketing");
            if (tab == "Résumé")
            {
                H("Aujourd'hui"); foreach (var kv in G.ledger.inc) Row(kv.Key, "<color=#8f8>+" + GMath.Eur(kv.Value) + "</color>"); foreach (var kv in G.ledger.exp) Row(kv.Key, "<color=#f88>−" + GMath.Eur(kv.Value) + "</color>");
                H("Saison (" + G.season.days + " jours, " + GMath.Num(G.season.visitors) + " visiteurs)");
                Row("Recettes", GMath.Eur(Mgmt.Sum(G.season.inc))); Row("Charges d'exploitation", GMath.Eur(Mgmt.Opex(G.season.exp))); Row("Investissements", GMath.Eur(Mgmt.Sum(G.season.exp) - Mgmt.Opex(G.season.exp)));
                double fixedC = G.buildings.Sum(b => { var c = Mgmt.DailyCost(b); return c[0] + c[1]; }) + G.lifts.Sum(l => l.maint * new[] { .55, 1, 1.6 }[G.maint]);
                Row("Charges fixes estimées", GMath.Eur(fixedC) + " / jour"); Row("Seuil de rentabilité", "≈ " + GMath.Num(Math.Ceiling(fixedC / Math.Max(1, Game.PassNow() * 1.4))) + " visiteurs / jour");
                H("Rentabilité par bâtiment"); foreach (var b in G.buildings.OrderByDescending(x => x.sRev - x.sCost).Take(12)) Row(b.Def.name, GMath.Eur(b.sRev - b.sCost));
            }
            else if (tab == "Tarifs")
            {
                Slider("Forfait journée", G.passPrice, 8, 80, v => Actions.Price("pass", v));
                if (Game.Researched("night")) Slider("Forfait nocturne", G.nightPrice, 4, 40, v => Actions.Price("night", v));
                if (Game.Researched("summer")) Slider("Forfait été", G.summerPass, 3, 40, v => Actions.Price("summer", v));
                if (Sim.Today != null) GUILayout.Label("<color=#aaa>Juste prix estimé du forfait pour ce domaine : " + GMath.Eur(Sim.Today.fair) + "</color>", small);
                H("Salaires"); GUILayout.BeginHorizontal(); string[] wg = { "Serrés", "Standard", "Généreux" }; for (int i = 0; i < 3; i++) if (GUILayout.Button(wg[i], G.wage == i ? btnOn : GUI.skin.button)) Actions.Wage(i); GUILayout.EndHorizontal();
                H("Entretien des remontées"); GUILayout.BeginHorizontal(); string[] mt = { "Minimal", "Normal", "Renforcé" }; for (int i = 0; i < 3; i++) if (GUILayout.Button(mt[i], G.maint == i ? btnOn : GUI.skin.button)) Actions.Maint(i); GUILayout.EndHorizontal();
            }
            else if (tab == "Banque")
            {
                Row("Dette totale", GMath.Eur(G.loan)); Row("Plafond d'endettement", GMath.Eur(Mgmt.CreditLimit()));
                foreach (var l in G.loans.ToList()) { GUILayout.BeginHorizontal(); GUILayout.Label(l.name + " : " + GMath.Eur(l.left) + " · " + GMath.Eur(l.pay) + "/j · " + l.daysLeft + " j", small); if (GUILayout.Button("Solder", GUILayout.Width(S(80)))) Msg(Mgmt.RepayLoan(l.id), "Prêt soldé."); GUILayout.EndHorizontal(); }
                H("Emprunter"); foreach (var p in Catalog.LOANS) if (GUILayout.Button(p.name + " — " + GMath.Eur(p.amount) + " sur " + p.days + " jours (" + (p.rate * 100).ToString("0.00") + " %/j)")) Msg(Mgmt.TakeLoan(p.id), "🏦 Prêt accordé.");
            }
            else
            {
                if (GUILayout.Button("Campagne réseaux sociaux — 8 000 € (5 jours)" + (G.marketing.social > 0 ? " · en cours" : ""))) Msg(Actions.Marketing("social"), "Campagne lancée : effet dès demain.");
                if (GUILayout.Button("Campagne télévisée — 45 000 € (7 jours)" + (G.marketing.tv > 0 ? " · en cours" : ""))) Msg(Actions.Marketing("tv"), "Campagne lancée : effet dès demain.");
            }
        }
        void Slider(string name, double val, float min, float max, Action<double> set)
        {
            GUILayout.BeginHorizontal(); GUILayout.Label(name + " : <b>" + GMath.Eur(val) + "</b>", Rich, GUILayout.Width(S(220))); var nv = Math.Round(GUILayout.HorizontalSlider((float)val, min, max)); if (nv != val) set(nv); GUILayout.EndHorizontal();
        }

        // ---------- recherche ----------
        void ResearchPanel()
        {
            var R = G.research;
            Row("Laboratoire", "niveau " + G.lab + " · " + G.lab + " projet(s) en parallèle");
            if (G.lab < 3 && GUILayout.Button("Agrandir le laboratoire — " + GMath.Eur(Mgmt.LabCost()))) Msg(Actions.LabUpgrade(), "🔬 Laboratoire amélioré.");
            foreach (var a in R.active) { var d = Catalog.Research(a.id); Row("⏳ " + d.name, (a.total - a.left) + " / " + a.total + " j"); }
            if (R.queue.Count > 0) GUILayout.Label("<color=#aaa>File : " + string.Join(", ", R.queue.Select(q => Catalog.Research(q).name)) + "</color>", small);
            foreach (var cat in Catalog.RCATS)
            {
                H(cat);
                foreach (var r in Catalog.RESEARCH.Where(x => x.cat == cat))
                {
                    var st = Mgmt.RState(r);
                    string tag = st == "done" ? "<color=#8f8>✔ acquise</color>" : st == "active" ? "<color=#fc6>en cours</color>" : st == "queued" ? "<color=#9cf>en file</color>" : st == "locked" ? "<color=#888>prérequis : " + Catalog.Research(r.req).name + "</color>" : GMath.Eur(r.cost) + " · " + Mgmt.RDays(r) + " j";
                    GUI.enabled = st == "open" || st == "queued";
                    if (GUILayout.Button("<b>" + r.name + "</b>  " + tag + "\n<size=" + (int)S(11) + "><color=#bbb>" + r.desc + "</color></size>"))
                    { if (st == "queued") Mgmt.CancelQueued(r.id); else { var e = Mgmt.StartResearch(r.id); Toast(e == "queued" ? "Ajouté à la file." : e ?? "🔬 Recherche lancée."); } }
                    GUI.enabled = true;
                }
            }
        }

        // ---------- exploitation ----------
        void OpsPanel()
        {
            Tabs("Pisteurs", "Dameuses", "Hébergements", "Services");
            if (tab == "Pisteurs")
            {
                var pi = Mgmt.PatrolInfo ?? Mgmt.SecurePistes();
                Row("Pisteurs / postes", pi.patrol + " / " + pi.posts); Row("Pistes à sécuriser", pi.needKm.ToString("0.0") + " km pour " + pi.capKm.ToString("0.0") + " km de capacité");
                Row("Pistes non sécurisées", pi.unsecured.ToString()); Row("Accidents (saison)", G.safety.incidents + " dont " + G.safety.severe + " graves");
                if (G.safety.respN > 0) Row("Délai moyen d'intervention", Game.R(G.safety.respSum / G.safety.respN / 60) + " min");
                foreach (var b in G.buildings.Where(x => x.type == "rescue")) { GUILayout.BeginHorizontal(); GUILayout.Label("Poste à " + GMath.Num(b.y) + " m : " + b.patrol + " pisteurs", small); if (GUILayout.Button("−", GUILayout.Width(S(30)))) Actions.Patrol(b, -1); if (GUILayout.Button("+", GUILayout.Width(S(30)))) Actions.Patrol(b, 1); GUILayout.EndHorizontal(); }
            }
            else if (tab == "Dameuses")
            {
                Row("Parc", Mgmt.GroomerCount() + " dameuses"); Row("Capacité de la nuit", Mgmt.GroomCapacityKm().ToString("0.0") + " km");
                Row("Besoin (pistes à damer)", (Mgmt.GroomPlan().Sum(p => p.len) / 1000).ToString("0.0") + " km");
                if (GUILayout.Button((G.groomNight ? "☑" : "☐") + " Équipe de nuit renforcée (+50 % de vitesse, salaires de nuit)")) Actions.GroomNight();
                if (G.groomReport != null) GUILayout.Label("Dernière nuit : " + G.groomReport.done + " piste(s) damée(s), " + G.groomReport.km.ToString("0.0") + " km" + (G.groomReport.missed.Count > 0 ? " · non faites : " + string.Join(", ", G.groomReport.missed) : ""), small);
                foreach (var p in G.pistes) { GUILayout.BeginHorizontal(); GUILayout.Label(p.name + " <color=#aaa>" + Weather.State(p) + "</color>", small); string[] pr = { "Jamais", "Normal", "Prioritaire" }; for (int i = 0; i < 3; i++) if (GUILayout.Button(pr[i], p.prio == i ? btnOn : GUI.skin.button, GUILayout.Width(S(90)))) Actions.GroomPrio(p, i); GUILayout.EndHorizontal(); }
            }
            else if (tab == "Hébergements")
            {
                var li = Mgmt.LodgingInfo;
                if (li != null) for (int s = 0; s < 3; s++) Row(Catalog.SEG[s], "demande " + GMath.Num(li.pools[s]) + " · logés " + GMath.Num(li.served[s]) + " · lits " + GMath.Num(li.beds[s]));
                foreach (var b in G.buildings.Where(x => x.Def.type == "lodging")) if (GUILayout.Button(b.Def.name + " — " + b.guests + "/" + Game.Bcap(b) + " · " + GMath.Eur(b.price) + " · ★ " + b.rating.ToString("0.0"))) { tools.Select(b); camCtl.Focus(b.x, b.z, 600); }
            }
            else foreach (var b in G.buildings.Where(Mgmt.IsService)) if (GUILayout.Button(b.Def.name + " — " + b.visits + " clients · " + GMath.Eur(b.revenue) + " · ★ " + b.rating.ToString("0.0"))) { tools.Select(b); camCtl.Focus(b.x, b.z, 600); }
        }

        // ---------- événements, X Games, Jeux Olympiques ----------
        void EventsPanel()
        {
            Tabs("Événements", "X Games", "Jeux Olympiques");
            if (tab == "Événements")
            {
                if (G.@event != null) { var ev = Catalog.Event(G.@event.id); L("<b>Programmé :</b> " + ev.name + (G.@event.day > G.day ? " dans " + (G.@event.day - G.day) + " jour(s)" : " — en cours (" + G.@event.left + " j)")); }
                var s = Game.Stats();
                foreach (var e in Catalog.EVENTS)
                {
                    int h; G.eventsHosted.TryGetValue(e.id, out h); bool ok = e.need(s) && !e.bid;
                    GUI.enabled = ok && G.@event == null;
                    if (GUILayout.Button("<b>" + e.name + "</b>  " + GMath.Eur(e.cost) + " · " + e.days + " j" + (h > 0 ? " · organisé " + h + "×" : "") + "\n<size=" + (int)S(11) + "><color=" + (ok ? "#9c9" : "#c99") + ">" + e.needTxt + "</color></size>")) Msg(Actions.ScheduleEvent(e.id), e.name + " programmé(e) dans 3 jours.");
                    GUI.enabled = true;
                }
            }
            else if (tab == "X Games") { L("Les X Games sont facultatifs mais leurs critères sont un passage obligé vers les Jeux Olympiques."); Crits("xgames"); }
            else
            {
                L("Les critères olympiques reprennent ceux des X Games avec des seuils plus élevés. <b>Le jour 1 des Jeux, la partie est gagnée.</b>");
                Crits("olympics");
                var b = G.bid;
                H("Candidature");
                if (b != null)
                {
                    string ph = b.phase == "eval" ? "Évaluation par la commission (vote dans " + (b.due - G.elapsed) + " j)" : b.phase == "prep" ? "Préparation : inspection finale dans " + (b.due - G.elapsed) + " j" + (b.late ? " (reportée à l'hiver)" : "") : b.phase == "games" ? "Les Jeux ont lieu !" : b.phase == "done" ? "Jeux organisés" : "Refusée — nouvelle tentative possible dans " + Math.Max(0, b.retry - G.elapsed) + " j";
                    L(ph); foreach (var rv in b.rivals) Row(rv.name, Game.R(rv.score) + " / 100");
                    if (b.score >= 0) Row("Votre dossier", b.score + " / 100");
                }
                Row("Score actuel de votre dossier", Game.R(Scenarios.DossierScore()) + " / 100");
                var c = Scenarios.BidCan(); foreach (var w in c.why) GUILayout.Label("<color=#f99>• " + w + "</color>", small);
                GUI.enabled = c.ok; if (GUILayout.Button("📁 Déposer la candidature — " + GMath.Eur(Scenarios.BID_COST))) Msg(Scenarios.BidStart(), "Dossier déposé."); GUI.enabled = true;
            }
        }
        void Crits(string id) { foreach (var c in Scenarios.CritStateOf(id)) GUILayout.Label((c.ok ? "<color=#8f8>✔</color> " : "<color=#f77>✘</color> ") + c.text + "  <color=#aaa>" + c.val + "</color>", wrap); }

        // ---------- carrière, prestige, notoriété, succès ----------
        void CareerPanel()
        {
            var sc = Scenarios.Cur;
            if (sc != null)
            {
                GUILayout.Label("<b>" + sc.name + "</b>" + (G.scn.done ? "  <color=#fc3>" + new string('★', G.scn.stars) + " réussi</color>" : G.scn.failed ? "  <color=#f77>échec</color>" : ""), title);
                var dl = Scenarios.DaysLeft(); if (dl.HasValue) Row("Délai", dl + " jours restants");
                for (int i = 0; i < sc.stages.Length; i++)
                {
                    var st = sc.stages[i]; bool cur = i == G.scn.stage && !G.scn.done, done = i < G.scn.stage || G.scn.done;
                    H((done ? "✔ " : cur ? "▶ " : "· ") + st.title + "  <color=#aaa>prime " + GMath.Eur(st.reward) + "</color>");
                    if (cur) foreach (var g in Scenarios.GoalState(st)) GUILayout.Label((g.ok ? "<color=#8f8>✔</color> " : "<color=#f77>✘</color> ") + g.text + "  <color=#aaa>" + g.val + "</color>", wrap);
                }
                if (GUILayout.Button("Continuer en mode libre (abandonner le scénario)")) Scenarios.Free();
            }
            H("Prestige " + Stars(G.prestige) + " (" + G.prestige + " / 100) — le standing");
            foreach (var kv in G.prestigeParts) Row(kv.Key, kv.Value.ToString("0.0"));
            foreach (var c in G.prestigeCaps) GUILayout.Label("<color=#fc6>Plafonné : " + c + "</color>", small);
            H("Notoriété " + Game.R(G.fame) + " % — la renommée (humeur, rapport qualité/prix)");
            foreach (var kv in Scenarios.FameParts()) Row(kv.Key, kv.Value.ToString("0.0"));
            Row("Objectif actuel", Game.R(Scenarios.FameTarget()) + " %");
            H("Succès (" + G.ach.Count + " / " + Catalog.ACH.Length + ")");
            foreach (var a in Catalog.ACH) GUILayout.Label((G.ach.ContainsKey(a.id) ? "🏅 <b>" + a.name + "</b>" : "<color=#888>" + a.name + "</color>") + " <color=#aaa>— " + a.desc + "</color>", small);
        }
        void JournalPanel()
        {
            H("Dix derniers jours");
            foreach (var h in G.history.Skip(Math.Max(0, G.history.Count - 10)).Reverse()) Row(Game.DateStr(h.day, h.su), h.v + " visiteurs · " + h.sat + " % · " + (h.inc - h.exp >= 0 ? "<color=#8f8>+" : "<color=#f88>") + GMath.Eur(h.inc - h.exp) + "</color>");
            H("Faits marquants"); foreach (var l in G.log.Take(30)) GUILayout.Label("<color=#aaa>" + Game.DateStr(l.d) + " " + GMath.HHMM(l.t) + "</color> " + l.msg, small);
        }
        void OptionsPanel()
        {
            GUILayout.BeginHorizontal(); GUILayout.Label("Nom de la station", GUILayout.Width(S(150))); stationName = GUILayout.TextField(stationName ?? G.name, 28); if (GUILayout.Button("OK", GUILayout.Width(S(50)))) Actions.Rename(null, stationName); GUILayout.EndHorizontal();
            world.showTrees = GUILayout.Toggle(world.showTrees, " Forêts"); render.bigPeople = GUILayout.Toggle(render.bigPeople, " Visiteurs agrandis (lisibilité)");
            if (G.summer && GUILayout.Button("Abréger l'été")) { if (!Game.SkipSummer()) Toast("Attendez que la station soit vide (le soir)."); }
            if (GUILayout.Button("💾 Sauvegarder")) Toast(boot.Save() ? "💾 Partie sauvegardée." : "Sauvegarde impossible.");
            if (GUILayout.Button("📋 Copier le code de sauvegarde")) { GUIUtility.systemCopyBuffer = SaveLoad.ExportCode(G); Toast("Code copié dans le presse-papiers."); }
            if (GUILayout.Button("Quitter vers le menu")) { boot.Save(); boot.ToMenu(); panel = null; }
        }
        string stationName;

        // =========================================================== FENÊTRES
        void Morning()
        {
            var e = MorningEvents.Current; G.paused = true;
            var r = Box(new Rect(Screen.width / 2 - S(280), Screen.height / 2 - S(130), S(560), S(260)));
            GUILayout.BeginArea(new Rect(r.x + 14, r.y + 10, r.width - 28, r.height - 20));
            GUILayout.Label("<b>" + e.title + "</b>", title); L(e.text); GUILayout.FlexibleSpace();
            for (int i = 0; i < e.choices.Length; i++) if (GUILayout.Button(e.choices[i].label, GUILayout.Height(S(34)))) { MorningEvents.Choose(i); G.paused = false; }
            GUILayout.EndArea();
        }
        void Report()
        {
            var r = Box(new Rect(Screen.width / 2 - S(300), S(90), S(600), Screen.height - S(200)));
            GUILayout.BeginArea(new Rect(r.x + 14, r.y + 10, r.width - 28, r.height - 20));
            var rp = report;
            if (rp.victory) GUILayout.Label("<color=#fc3><b>🏆 Victoire ! " + new string('★', Math.Max(1, rp.stars)) + "</b></color>", title);
            else if (rp.failed) GUILayout.Label("<color=#f77><b>" + rp.failTitle + "</b></color>", title);
            else GUILayout.Label("<b>Bilan du " + rp.date + "</b>", title);
            if (rp.victory) L(rp.victoryText); if (rp.failed) L(rp.failText);
            Row("Visiteurs", rp.visitors + (rp.lost > 0 ? " <color=#fc6>(" + rp.lost + " refoulés : parkings saturés)</color>" : "")); Row("Satisfaction", rp.sat + " %");
            Row("Résultat d'exploitation", GMath.Eur(rp.inc - rp.opex)); if (rp.why != null) GUILayout.Label("<color=#fc6>" + rp.why + "</color>", wrap);
            if (rp.fb.Count > 0) { H("Ce qui a déplu"); foreach (var kv in rp.fb.OrderByDescending(x => x.Value).Take(5)) Row(kv.Key, kv.Value.ToString()); }
            if (rp.notes.Count > 0) { H("Faits marquants"); scroll2 = GUILayout.BeginScrollView(scroll2); foreach (var n in rp.notes) L("• " + n); GUILayout.EndScrollView(); }
            GUILayout.FlexibleSpace(); if (GUILayout.Button("Continuer", GUILayout.Height(S(34)))) showReport = false;
            GUILayout.EndArea();
        }
    }
}
