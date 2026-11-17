// SkiStation — Gestion (portage C# du noyau web). Code pur, sans dépendance à Unity :
// il se compile et se teste hors de l'éditeur et pourra servir au mode compétition Alpine Empires.
using System;
using System.Globalization;

namespace SkiStation.Gestion
{
    /// <summary>Petit vecteur 2D (x, z) utilisé par toute la logique de gestion.</summary>
    public struct P2
    {
        public double x, z;
        public P2(double x, double z) { this.x = x; this.z = z; }
        public override string ToString() { return "(" + x.ToString("0.#", CultureInfo.InvariantCulture) + ", " + z.ToString("0.#", CultureInfo.InvariantCulture) + ")"; }
    }

    public static class GMath
    {
        public static double Clamp(double v, double a, double b) { return v < a ? a : v > b ? b : v; }
        public static int Clamp(int v, int a, int b) { return v < a ? a : v > b ? b : v; }
        public static double Lerp(double a, double b, double t) { return a + (b - a) * t; }
        /// <summary>Lissage d'Hermite identique à SS.smooth (accepte a &gt; b).</summary>
        public static double Smooth(double a, double b, double v) { double t = Clamp((v - a) / (b - a), 0, 1); return t * t * (3 - 2 * t); }
        public static double Hypot(double x, double y) { return Math.Sqrt(x * x + y * y); }
        public static double Hypot(double x, double y, double z) { return Math.Sqrt(x * x + y * y + z * z); }
        public static double Dist(P2 a, P2 b) { return Hypot(a.x - b.x, a.z - b.z); }

        /// <summary>Distance d'un point à un segment, et position t (0..1) du point le plus proche.</summary>
        public static double SegDist(double px, double pz, double ax, double az, double bx, double bz, out double t)
        {
            double dx = bx - ax, dz = bz - az, l2 = dx * dx + dz * dz;
            t = l2 > 0 ? ((px - ax) * dx + (pz - az) * dz) / l2 : 0;
            t = Clamp(t, 0, 1);
            return Hypot(px - ax - dx * t, pz - az - dz * t);
        }
        public static double SegDist(double px, double pz, double ax, double az, double bx, double bz) { double t; return SegDist(px, pz, ax, az, bx, bz, out t); }

        /// <summary>Intersection stricte de deux segments 2D.</summary>
        public static bool SegCross(P2 a, P2 b, P2 c, P2 d)
        {
            double o1 = Orient(a, b, c), o2 = Orient(a, b, d), o3 = Orient(c, d, a), o4 = Orient(c, d, b);
            return (o1 > 0) != (o2 > 0) && (o3 > 0) != (o4 > 0);
        }
        static double Orient(P2 p, P2 q, P2 r) { return (q.x - p.x) * (r.z - p.z) - (q.z - p.z) * (r.x - p.x); }

        // --- formatage français ---
        static readonly NumberFormatInfo FR = MakeFr();
        static NumberFormatInfo MakeFr() { var f = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone(); f.NumberGroupSeparator = " "; f.NumberDecimalSeparator = ","; return f; }
        public static string Num(double v) { return Math.Round(v).ToString("#,0", FR); }
        public static string Eur(double v) { return Num(v) + " €"; }
        public static string HHMM(double t) { int h = (int)Math.Floor(t / 3600), m = (int)Math.Floor(t % 3600 / 60); return h.ToString("00") + ":" + m.ToString("00"); }
    }

    /// <summary>Générateur pseudo-aléatoire identique à SS.rng (mulberry32) : mêmes graines, mêmes tirages que la version web.</summary>
    public sealed class Rng
    {
        int a;
        public Rng(long seed) { a = unchecked((int)(uint)(seed & 0xFFFFFFFF)); }
        static int Imul(int x, int y) { return unchecked(x * y); }
        public double Next()
        {
            unchecked
            {
                a = a + 0x6D2B79F5;
                int t = Imul(a ^ (int)((uint)a >> 15), 1 | a);
                t = (t + Imul(t ^ (int)((uint)t >> 7), 61 | t)) ^ t;
                return (uint)(t ^ (int)((uint)t >> 14)) / 4294967296.0;
            }
        }
    }

    /// <summary>Aléatoire « de jeu » (non reproductible), équivalent de Math.random.</summary>
    public static class Rand
    {
        static System.Random r = new System.Random();
        public static void Seed(int s) { r = new System.Random(s); }
        public static double Next() { return r.NextDouble(); }
        public static double Range(double a, double b) { return a + r.NextDouble() * (b - a); }
        public static T Pick<T>(T[] arr) { return arr[(int)Math.Floor(r.NextDouble() * arr.Length)]; }
    }

    /// <summary>Bruit de valeur 2D déterministe, identique à SS.makeNoise.</summary>
    public sealed class Noise
    {
        readonly ushort[] P = new ushort[512];
        readonly float[] G = new float[256];
        public Noise(long seed)
        {
            var r = new Rng(seed); var p = new int[256];
            for (int i = 0; i < 256; i++) { p[i] = i; G[i] = (float)(r.Next() * 2 - 1); }
            for (int i = 255; i > 0; i--) { int j = (int)Math.Floor(r.Next() * (i + 1)); int t = p[i]; p[i] = p[j]; p[j] = t; }
            for (int i = 0; i < 512; i++) P[i] = (ushort)p[i & 255];
        }
        double Gv(int x, int y) { return G[P[(x & 255) + P[y & 255]]]; }
        public double Value(double x, double y)
        {
            int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y); double xf = x - xi, yf = y - yi;
            double u = xf * xf * (3 - 2 * xf), v = yf * yf * (3 - 2 * yf);
            double a = Gv(xi, yi), b = Gv(xi + 1, yi), c = Gv(xi, yi + 1), d = Gv(xi + 1, yi + 1);
            return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
        }
        public double Fbm(double x, double y, int oct)
        {
            double s = 0, a = 0.5, f = 1, n = 0;
            for (int i = 0; i < oct; i++) { s += a * Value(x * f, y * f); n += a; a *= 0.5; f *= 2.03; }
            return s / n;
        }
        public double Ridged(double x, double y, int oct)
        {
            double s = 0, a = 0.5, f = 1, n = 0;
            for (int i = 0; i < oct; i++) { double v = 1 - Math.Abs(Value(x * f, y * f)); s += a * v * v; n += a; a *= 0.5; f *= 2.1; }
            return s / n;
        }
    }
}
