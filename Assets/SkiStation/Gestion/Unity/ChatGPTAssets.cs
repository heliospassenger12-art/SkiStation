// SkiStation — accès aux éléments de la version ChatGPT (rider, arcade, roche, sapins) SANS dépendance de compilation :
// les types sont retrouvés par leur nom à l'exécution. Le kit de gestion s'installe ainsi dans n'importe quelle version
// du projet (3.29, 3.94 / A2.64…) ; si un élément manque ou a changé, on bascule sur un équivalent simple au lieu d'échouer.
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SkiStation.GestionUnity
{
    public static class ChatGPTAssets
    {
        const BindingFlags ALL = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

        public static Type Find(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = null; try { t = asm.GetType(fullName, false); } catch (Exception) { }
                if (t != null) return t;
            }
            return null;
        }
        /// <summary>Affecte un champ ou une propriété s'il existe (ignoré sinon).</summary>
        public static void Set(object o, string name, object value)
        {
            if (o == null) return; var t = o.GetType();
            try
            {
                var f = t.GetField(name, ALL); if (f != null && (value == null || f.FieldType.IsInstanceOfType(value))) { f.SetValue(o, value); return; }
                var p = t.GetProperty(name, ALL); if (p != null && p.CanWrite && (value == null || p.PropertyType.IsInstanceOfType(value))) p.SetValue(o, value, null);
            }
            catch (Exception e) { Debug.LogWarning("SkiStation Gestion : " + t.Name + "." + name + " — " + e.Message); }
        }
        public static object Get(object o, string name)
        {
            if (o == null) return null; var t = o.GetType();
            try { var f = t.GetField(name, ALL); if (f != null) return f.GetValue(o); var p = t.GetProperty(name, ALL); if (p != null) return p.GetValue(o, null); } catch (Exception) { }
            return null;
        }
        /// <summary>Appelle une méthode dont les paramètres acceptent les arguments donnés ; renvoie false si introuvable.</summary>
        public static bool Call(object target, Type type, string name, params object[] args)
        {
            if (type == null) return false;
            foreach (var m in type.GetMethods(ALL).Where(x => x.Name == name))
            {
                var ps = m.GetParameters(); if (ps.Length < args.Length) continue;
                bool ok = true; var full = new object[ps.Length];
                for (int i = 0; i < ps.Length; i++)
                {
                    if (i < args.Length) { if (args[i] != null && !ps[i].ParameterType.IsInstanceOfType(args[i])) { ok = false; break; } full[i] = args[i]; }
                    else if (ps[i].IsOptional) full[i] = ps[i].DefaultValue; else { ok = false; break; }
                }
                if (!ok) continue;
                try { m.Invoke(m.IsStatic ? null : target, full); return true; }
                catch (Exception e) { Debug.LogWarning("SkiStation Gestion : " + type.Name + "." + name + " — " + (e.InnerException ?? e).Message); return false; }
            }
            return false;
        }
        public static Component Add(GameObject go, string typeName)
        {
            var t = Find(typeName); if (t == null || !typeof(Component).IsAssignableFrom(t)) return null;
            try { return go.AddComponent(t); } catch (Exception e) { Debug.LogWarning("SkiStation Gestion : " + typeName + " — " + e.Message); return null; }
        }

        // ---------- habillage de la montagne ----------
        public static Material RockMaterial()
        {
            var t = Find("SkiStation.AlpineRockMaterial"); if (t == null) return null;
            var p = t.GetProperty("Shared", ALL); try { return p != null ? p.GetValue(null, null) as Material : null; } catch (Exception) { return null; }
        }
        public static Mesh PineCrown()
        {
            var t = Find("SkiStation.AlpineForestVisual262"); if (t == null) return null;
            var m = t.GetMethod("BuildCrown", ALL); try { return m != null ? m.Invoke(null, null) as Mesh : null; } catch (Exception) { return null; }
        }
    }
}
