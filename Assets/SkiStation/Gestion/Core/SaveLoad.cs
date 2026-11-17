// SkiStation — Gestion : sauvegarde. JSON lisible (champs publics non marqués [NonSerialized]), code de sauvegarde
// portable « SKI1: » compatible avec la version web, restauration (relief, terrassements, arbres coupés, tracés recalculés).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SkiStation.Gestion
{
    public static class SaveLoad
    {
        public static string Serialize(GameState g)
        {
            g.removedTrees = Game.T.RemovedTrees.ToList();
            var sb = new StringBuilder(); Json.Write(sb, g); return sb.ToString();
        }
        public static string ExportCode(GameState g) { return "SKI1:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(Serialize(g))); }
        public static GameState ImportCode(string txt)
        {
            try { txt = (txt ?? "").Trim(); if (!txt.StartsWith("SKI1:")) return null; return Deserialize(Encoding.UTF8.GetString(Convert.FromBase64String(txt.Substring(5)))); }
            catch (Exception) { return null; }
        }
        public static GameState Deserialize(string json)
        {
            var o = Json.Parse(json) as Dictionary<string, object>; if (o == null) return null;
            // la version web stocke les terrassements en tableaux [x, z, r, h, blend]
            object fl; if (o.TryGetValue("flats", out fl) && fl is List<object>)
            {
                var l = (List<object>)fl;
                for (int i = 0; i < l.Count; i++) { var a = l[i] as List<object>; if (a != null) { var d = new Dictionary<string, object>(); string[] k = { "x", "z", "r", "h", "blend" }; for (int j = 0; j < a.Count && j < 5; j++) d[k[j]] = a[j]; l[i] = d; } }
            }
            return (GameState)Json.To(typeof(GameState), o);
        }
        /// <summary>Recharge une partie : relief de la graine, terrassements, arbres, tracés recalculés, réseau, journée relancée au matin.</summary>
        public static GameState Restore(GameState data)
        {
            var T = Game.T; T.Init(data.seed); T.GenTrees();
            Game.G = data; data.paused = true; Mgmt.Migrate(data);
            foreach (var f in data.flats) T.Flatten(f.x, f.z, f.r, f.h, f.blend);
            foreach (var i in data.removedTrees ?? new List<int>()) T.RemovedTrees.Add(i);
            T.GenTrees();
            var pistes = data.pistes; var lifts = data.lifts; data.pistes = new List<Piste>(); data.lifts = new List<Lift>();
            foreach (var l in lifts) { var an = Game.AnalyzeLift(l.type, l.a.Xz, l.b.Xz, true); if (an.path != null) { Game.CommitLift(an, l); data.lifts.Add(l); } }
            foreach (var p in pistes) { var an = Game.AnalyzePiste(p.ctrl, p.width); if (an.pts != null) { Game.CommitPiste(p.ctrl, an, p); data.pistes.Add(p); } }
            foreach (var b in data.buildings) { b.queue = new List<Visitor>(); b.occ = 0; }
            if (data.time > 9 * 3600) data.time = 8 * 3600;
            Game.Net.Rebuild(); Sim.StartDay(); Game.Emit("world");
            return data;
        }
    }

    /// <summary>JSON minimal sans dépendance (Unity : JsonUtility ne gère ni dictionnaires ni listes imbriquées).</summary>
    public static class Json
    {
        static readonly CultureInfo C = CultureInfo.InvariantCulture;
        public static void Write(StringBuilder sb, object v)
        {
            if (v == null) { sb.Append("null"); return; }
            var t = v.GetType();
            if (v is string) { Str(sb, (string)v); return; }
            if (v is bool) { sb.Append((bool)v ? "true" : "false"); return; }
            if (v is double || v is float) { double d = Convert.ToDouble(v); sb.Append(double.IsNaN(d) || double.IsInfinity(d) ? "null" : d.ToString("R", C)); return; }
            if (t.IsPrimitive || v is decimal) { sb.Append(Convert.ToString(v, C)); return; }
            if (v is IDictionary)
            {
                sb.Append('{'); bool first = true;
                foreach (DictionaryEntry e in (IDictionary)v) { if (!first) sb.Append(','); first = false; Str(sb, Convert.ToString(e.Key, C)); sb.Append(':'); Write(sb, e.Value); }
                sb.Append('}'); return;
            }
            if (v is IEnumerable)
            {
                sb.Append('['); bool first = true;
                foreach (var e in (IEnumerable)v) { if (!first) sb.Append(','); first = false; Write(sb, e); }
                sb.Append(']'); return;
            }
            sb.Append('{'); bool f = true;
            foreach (var fi in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (fi.IsNotSerialized || fi.IsInitOnly) continue;
                var ft = fi.FieldType; if (typeof(Delegate).IsAssignableFrom(ft) || ft == typeof(Node) || ft == typeof(Edge) || ft == typeof(LiftDef) || ft == typeof(BuildingDef)) continue;
                var val = fi.GetValue(v); if (val == null) continue;
                if (!f) sb.Append(','); f = false; Str(sb, fi.Name); sb.Append(':'); Write(sb, val);
            }
            sb.Append('}');
        }
        static void Str(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break; case '\\': sb.Append("\\\\"); break; case '\n': sb.Append("\\n"); break; case '\r': sb.Append("\\r"); break; case '\t': sb.Append("\\t"); break;
                    default: if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4")); else sb.Append(c); break;
                }
            }
            sb.Append('"');
        }

        // ---------- lecture ----------
        public static object Parse(string s) { int i = 0; var v = Val(s, ref i); return v; }
        static void Ws(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }
        static object Val(string s, ref int i)
        {
            Ws(s, ref i); char c = s[i];
            if (c == '{')
            {
                var d = new Dictionary<string, object>(); i++; Ws(s, ref i);
                if (s[i] == '}') { i++; return d; }
                for (; ; ) { Ws(s, ref i); string k = (string)Val(s, ref i); Ws(s, ref i); i++; d[k] = Val(s, ref i); Ws(s, ref i); if (s[i] == ',') { i++; continue; } i++; return d; }
            }
            if (c == '[')
            {
                var l = new List<object>(); i++; Ws(s, ref i);
                if (s[i] == ']') { i++; return l; }
                for (; ; ) { l.Add(Val(s, ref i)); Ws(s, ref i); if (s[i] == ',') { i++; continue; } i++; return l; }
            }
            if (c == '"')
            {
                var sb = new StringBuilder(); i++;
                while (s[i] != '"')
                {
                    if (s[i] == '\\')
                    {
                        i++; char e = s[i];
                        if (e == 'n') sb.Append('\n'); else if (e == 't') sb.Append('\t'); else if (e == 'r') sb.Append('\r'); else if (e == 'b') sb.Append('\b'); else if (e == 'f') sb.Append('\f');
                        else if (e == 'u') { sb.Append((char)Convert.ToInt32(s.Substring(i + 1, 4), 16)); i += 4; } else sb.Append(e);
                        i++;
                    }
                    else sb.Append(s[i++]);
                }
                i++; return sb.ToString();
            }
            if (s.Substring(i).StartsWith("true")) { i += 4; return true; }
            if (s.Substring(i, Math.Min(5, s.Length - i)).StartsWith("false")) { i += 5; return false; }
            if (s.Substring(i, Math.Min(4, s.Length - i)).StartsWith("null")) { i += 4; return null; }
            int st = i; while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            return double.Parse(s.Substring(st, i - st), C);
        }

        /// <summary>Convertit l'arbre JSON vers un type (classes à champs publics, listes, dictionnaires à clé chaîne ou nombre).</summary>
        public static object To(Type t, object v)
        {
            if (v == null) return t.IsValueType ? Activator.CreateInstance(t) : null;
            if (t == typeof(object)) return v;
            if (t == typeof(string)) return v is string ? v : Convert.ToString(v, C);
            if (t == typeof(bool)) return v is bool ? v : Convert.ToDouble(v, C) != 0;
            if (t.IsPrimitive) return v is bool ? Convert.ChangeType((bool)v ? 1 : 0, t) : Convert.ChangeType(v, t, C);
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
            {
                var et = t.GetGenericArguments()[0]; var l = (IList)Activator.CreateInstance(t);
                var src = v as List<object>; if (src != null) foreach (var e in src) l.Add(To(et, e));
                return l;
            }
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var a = t.GetGenericArguments(); var d = (IDictionary)Activator.CreateInstance(t);
                var src = v as Dictionary<string, object>; if (src != null) foreach (var kv in src) d[To(a[0], kv.Key)] = To(a[1], kv.Value);
                return d;
            }
            var o = Activator.CreateInstance(t); var dict = v as Dictionary<string, object>; if (dict == null) return o;
            foreach (var fi in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (fi.IsNotSerialized || fi.IsInitOnly) continue; object x;
                if (!dict.TryGetValue(fi.Name, out x)) continue;
                try { fi.SetValue(o, To(fi.FieldType, x)); } catch (Exception) { }
            }
            return o;
        }
    }
}
