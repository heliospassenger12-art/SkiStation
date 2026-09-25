using System;
using UnityEditor;
using UnityEngine;

namespace SkiStation.MountainLab.Editor
{
    public static class MountainLabChecks306
    {
        [MenuItem("SkiStation/Montagne/Verifier le generateur 3.06")]
        public static void CheckMenu()
        {
            try { Debug.Log(Run()); }
            catch (Exception ex) { Debug.LogException(ex); }
        }

        public static string Run()
        {
            int samples = 0;
            foreach (int seed in new[] { 0, 306, -127, int.MaxValue })
                foreach (float scale in new[] { .6f, 1f, 1.4f })
                {
                    var settings = new MountainLabSettings306 { seed = seed, heightScale = scale, naturalVariation = 1f };
                    var a = new MountainLabHeight306(settings);
                    var b = new MountainLabHeight306(settings);
                    // Every corner, grid sample and apron boundary must stay exactly flat.
                    for (int iz = 0; iz <= 10; iz++)
                        for (int ix = 0; ix <= 32; ix++)
                        {
                            float x = -4000f + ix * 250f, z = -3400f + iz * 200f;
                            Require(a.Height(x, z) == MountainLabHeight306.BaseY, "Front de neige modifié"); samples++;
                        }
                    for (int iz = 0; iz <= 55; iz++)
                        for (int ix = 0; ix <= 80; ix++)
                        {
                            float x = -4000f + ix * 100f, z = -3400f + iz * 100f;
                            float y = a.Height(x, z), slope = a.SlopeDegrees(x, z);
                            Require(!float.IsNaN(y) && !float.IsInfinity(y) && y >= 81f && y < 3500f, "Altitude invalide");
                            Require(slope >= 0f && slope < 90f, "Inclinaison invalide");
                            Require(y == b.Height(x, z), "Génération non reproductible"); samples++;
                        }
                    for (int ix = 0; ix <= 32; ix++)
                        Require(Mathf.Abs(a.Height(-4000f + ix * 250f, -1399.9f) - 82f) < .1f, "Rupture au front de neige");
                }
            var normal = new MountainLabHeight306(new MountainLabSettings306());
            var other = new MountainLabHeight306(new MountainLabSettings306 { seed = 307 });
            bool changed = false;
            var bands = new bool[4];
            for (int z = -1000; z <= 1900; z += 100)
                for (int x = -3500; x <= 3500; x += 100)
                {
                    changed |= Mathf.Abs(normal.Height(x, z) - other.Height(x, z)) > .01f;
                    bands[MountainLabHeight306.SlopeBand(normal.SlopeDegrees(x, z))] = true;
                }
            Require(changed, "La graine ne change pas le relief");
            foreach (bool present in bands) Require(present, "Une bande d’inclinaison manque dans le massif par défaut");
            return "PASS 3.06 : " + samples + " échantillons contrôlés ; front plat, altitudes finies, déterminisme, transition et quatre bandes d’inclinaison. Ce contrôle ne valide pas le rendu ni le GPS.";
        }

        static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
    }
}
