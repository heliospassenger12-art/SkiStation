// SkiStation — Gestion : massif 360° de 8 × 8 km, 1 640 → 4 200 m (portage exact du relief de la version web).
// Seul le RELIEF vient d'ici : roches, sapins et matériaux restent ceux du projet Unity existant.
using System;
using System.Collections.Generic;

namespace SkiStation.Gestion
{
    public sealed class Tree { public double x, z, y, s; public bool alive = true; }

    public sealed class Relief
    {
        public const double WORLD = 8000, HALF = WORLD / 2;
        public const int N = 257;
        public const double CELL = WORLD / (N - 1);
        public static readonly P2 HUB = new P2(0, 2480);          // front de neige
        public const double HUB_R = 320;
        public static readonly P2 VILLAGE = new P2(0, 2750); public const double VILLAGE_H = 1650;

        public readonly float[] H = new float[N * N];
        public float[] SnowMask = new float[N * N];
        public int Seed; public double MinH, MaxH;
        public readonly List<Tree> Trees = new List<Tree>();
        readonly Dictionary<int, List<int>> treeGrid = new Dictionary<int, List<int>>();
        public readonly HashSet<int> RemovedTrees = new HashSet<int>();
        Noise noise;
        /// <summary>Appelé quand une zone du relief change (terrassement) : xMin, zMin, xMax, zMax en indices de grille.</summary>
        public event Action<int, int, int, int> RegionChanged;

        static readonly double[][] DOMES = {
            new[]{0.06,-0.22,1.00,0.60}, new[]{0.56,-0.36,0.72,0.44}, new[]{-0.56,-0.30,0.76,0.46}, new[]{0.02,-0.80,0.66,0.42},
            new[]{0.74,0.02,0.42,0.36}, new[]{-0.74,0.06,0.46,0.38}, new[]{0.30,-0.52,0.55,0.30}, new[]{-0.30,-0.56,0.58,0.30}, new[]{0.66,-0.80,0.5,0.34}, new[]{-0.66,-0.82,0.52,0.36} };
        static readonly double[][] SPIKES = { new[]{0.10,-0.24,850,0.085}, new[]{0.58,-0.38,560,0.075}, new[]{-0.55,-0.32,640,0.075}, new[]{0.0,-0.80,520,0.075} };
        static readonly double[][] PLATEAUS = {
            new[]{-0.42,0.30,1980,0.17}, new[]{0.02,0.26,2030,0.16}, new[]{0.44,0.30,1990,0.17},
            new[]{-0.58,0.02,2420,0.15}, new[]{-0.18,-0.02,2480,0.15}, new[]{0.24,0.0,2450,0.15}, new[]{0.62,0.04,2400,0.15},
            new[]{-0.46,-0.30,2850,0.15}, new[]{0.02,-0.32,2900,0.14}, new[]{0.44,-0.30,2880,0.15},
            new[]{-0.40,-0.58,3150,0.15}, new[]{0.06,-0.60,3200,0.15}, new[]{0.52,-0.56,3120,0.15}, new[]{0.34,-0.40,3250,0.14}, new[]{-0.32,-0.46,3050,0.14}, new[]{0.0,-0.84,2950,0.15} };
        public static readonly double[] TALUS = { 0.40, 0.48, 0.58, 0.88, 1.1 };
        public const int ITER = 140; public const double BODY = 3300;

        float[] DomeField(int seed)
        {
            var r = new Rng((long)seed * 31 + 7); var f = new float[N * N];
            var dj = new double[DOMES.Length][];
            for (int k = 0; k < DOMES.Length; k++) { var d = DOMES[k]; double a = d[0] + (r.Next() - 0.5) * 0.14, b = d[1] + (r.Next() - 0.5) * 0.12, c = d[2] * (0.85 + r.Next() * 0.3), w = d[3] * 1.18 * (0.9 + r.Next() * 0.2); dj[k] = new[] { a, b, c, w }; }
            for (int iz = 0; iz < N; iz++) for (int ix = 0; ix < N; ix++)
            {
                double nx = ((double)ix / (N - 1)) * 2 - 1, nz = ((double)iz / (N - 1)) * 2 - 1;
                double wx = nx + 0.16 * noise.Fbm(nx * 1.7 + 11, nz * 1.7 + 3, 3), wz = nz + 0.16 * noise.Fbm(nx * 1.7 - 7, nz * 1.7 + 19, 3);
                double e = 0; foreach (var d in dj) { double q = (wx - d[0]) * (wx - d[0]) + (wz - d[1]) * (wz - d[1]); e += d[2] * Math.Exp(-q / (d[3] * d[3])); }
                double apron = GMath.Smooth(0.66, 0.02, nz); e *= apron;
                double rid = noise.Ridged(nx * 2.3 + 40, nz * 2.3 + 8, 4) - 0.5;
                double rid2 = noise.Ridged((nx + 0.15 * noise.Fbm(nx * 3 + 5, nz * 3, 2)) * 4.8 + 13, nz * 4.8 + 57, 4) - 0.45;
                double calm = GMath.Smooth(0.62, 0.28, nz);
                f[iz * N + ix] = (float)(e * 2250 + (rid * 560 * (0.25 + Math.Min(1, e)) + rid2 * 470 * Math.Min(1, 0.25 + e * 1.1) + noise.Fbm(nx * 8 + 3, nz * 8 + 77, 3) * 26 * (0.3 + Math.Min(1, e)) + noise.Fbm(nx * 3.4 + 90, nz * 3.4 + 12, 3) * 140) * apron * calm);
            }
            return f;
        }
        static double Talus(double a) { double t = (a - 1640) / 500; int i = (int)GMath.Clamp(Math.Floor(t), 0, 3); double u = GMath.Clamp(t - i, 0, 1); return TALUS[i] + (TALUS[i + 1] - TALUS[i]) * u; }
        void Erode(float[] h)
        {
            var d = new float[N * N]; int[] ox = { 1, -1, 0, 0, 1, -1, 1, -1 }, oz = { 0, 0, 1, -1, 1, 1, -1, -1 }; double[] w = { 1, 1, 1, 1, 1.414, 1.414, 1.414, 1.414 };
            for (int it = 0; it < ITER; it++)
            {
                Array.Clear(d, 0, d.Length);
                for (int z = 1; z < N - 1; z++) for (int x = 1; x < N - 1; x++)
                {
                    int i = z * N + x; double hi = h[i], lim = Talus(hi);
                    for (int k = 0; k < 8; k++)
                    {
                        int j = i + oz[k] * N + ox[k]; double ex = hi - h[j] - lim * CELL * w[k];
                        if (ex > 0) { double m = ex * 0.2; d[i] = (float)(d[i] - m); d[j] = (float)(d[j] + m); }
                    }
                }
                for (int i = 0; i < h.Length; i++) h[i] = (float)(h[i] + d[i]);
            }
        }
        static void SmoothOnce(float[] h, double k)
        {
            var o = (float[])h.Clone();
            for (int z = 1; z < N - 1; z++) for (int x = 1; x < N - 1; x++) { int i = z * N + x; o[i] = (float)(h[i] * (1 - k) + k * 0.25 * ((double)h[i - 1] + h[i + 1] + h[i - N] + h[i + N])); }
            Array.Copy(o, h, h.Length);
        }

        public void Init(int seed)
        {
            if (seed == 0) seed = 1337; noise = new Noise(seed); Seed = seed;
            SnowMask = new float[N * N]; RemovedTrees.Clear();
            var f = DomeField(seed); double mx = 0;
            for (int i = 0; i < f.Length; i++) { f[i] = Math.Max(0f, f[i]); if (f[i] > mx) mx = f[i]; }
            for (int i = 0; i < H.Length; i++) H[i] = (float)(f[i] * (BODY - 1640) / mx);
            var pr = new Rng((long)seed * 13 + 5);
            foreach (var pl in PLATEAUS)
            {
                double cx = (pl[0] + (pr.Next() - 0.5) * 0.12 + 1) / 2 * (N - 1), cz = (pl[1] + (pr.Next() - 0.5) * 0.12 + 1) / 2 * (N - 1), rad = pl[3] * (N - 1) / 2, tgt = pl[2] - 1640;
                for (int iz = 0; iz < N; iz++) for (int ix = 0; ix < N; ix++)
                {
                    double q = GMath.Hypot(ix - cx, iz - cz) / rad; if (q > 2.2) continue; double wgt = 1 - GMath.Smooth(0.55, 2.2, q);
                    H[iz * N + ix] = (float)GMath.Lerp(H[iz * N + ix], tgt + noise.Fbm(ix * 0.06, iz * 0.06, 2) * 40, wgt);
                }
            }
            Erode(H); SmoothOnce(H, 0.3); SmoothOnce(H, 0.2);
            for (int iz = 0; iz < N; iz++) for (int ix = 0; ix < N; ix++)
            {
                int i = iz * N + ix; double nx = ((double)ix / (N - 1)) * 2 - 1, nz = ((double)iz / (N - 1)) * 2 - 1, zz = -HALF + iz * CELL;
                double h = 1640 + H[i] + GMath.Clamp((2760 - zz) * 0.07, 0, 130) * (1 - GMath.Smooth(1700, 2400, H[i] + 1640));
                foreach (var sp in SPIKES) { double q = (nx - sp[0]) * (nx - sp[0]) + (nz - sp[1]) * (nz - sp[1]); double rug = 0.55 + 0.9 * (noise.Ridged(nx * 26 + sp[0] * 9, nz * 26, 3) - 0.35); h += sp[2] * Math.Exp(-q / (sp[3] * sp[3])) * GMath.Clamp(rug, 0.25, 1.2); }
                h += noise.Fbm(nx * 18 + 5, nz * 18 + 9, 3) * 6 * (0.4 + GMath.Smooth(1700, 2800, h));
                H[i] = (float)Math.Max(1640, h);
            }
            double top = 0; for (int i = 0; i < H.Length; i++) if (H[i] > top) top = H[i];
            for (int i = 0; i < H.Length; i++) if (H[i] > 3350) H[i] = (float)(3350 + (H[i] - 3350) * (4200 - 3350) / Math.Max(1, top - 3350));
            for (int iz = 0; iz < N; iz++) for (int ix = 0; ix < N; ix++)
            {
                double x = -HALF + ix * CELL, z = -HALF + iz * CELL, d = GMath.Hypot((x - VILLAGE.x) * 0.8, z - 2760), wgt = 1 - GMath.Smooth(330, 900, d);
                if (wgt > 0) H[iz * N + ix] = (float)GMath.Lerp(H[iz * N + ix], VILLAGE_H + GMath.Clamp((VILLAGE.z - z) * 0.07, -8, 60), wgt);
            }
            MinH = double.PositiveInfinity; MaxH = double.NegativeInfinity;
            for (int i = 0; i < H.Length; i++) { if (H[i] < MinH) MinH = H[i]; if (H[i] > MaxH) MaxH = H[i]; }
        }

        /// <summary>Hauteur exacte (même découpe en triangles que le maillage).</summary>
        public double GetH(double x, double z)
        {
            double gx = GMath.Clamp((x + HALF) / CELL, 0, N - 1.001), gz = GMath.Clamp((z + HALF) / CELL, 0, N - 1.001);
            int ix = (int)gx, iz = (int)gz; double fx = gx - ix, fz = gz - iz; int i = iz * N + ix;
            double a = H[i], d = H[i + 1], b = H[i + N], c = H[i + N + 1];
            return fx + fz <= 1 ? a + (d - a) * fx + (b - a) * fz : c + (b - c) * (1 - fx) + (d - c) * (1 - fz);
        }
        public void Grad(double x, double z, out double gx, out double gz)
        {
            double e = CELL * 0.6;
            gx = (GetH(x + e, z) - GetH(x - e, z)) / (2 * e);
            gz = (GetH(x, z + e) - GetH(x, z - e)) / (2 * e);
        }
        public double Slope(double x, double z) { double gx, gz; Grad(x, z, out gx, out gz); return GMath.Hypot(gx, gz); }
        public static bool Inside(double x, double z, double m = 60) { return Math.Abs(x) < HALF - m && Math.Abs(z) < HALF - m; }

        /// <summary>Terrassement local : replat sous l'emprise, raccord naturel autour.</summary>
        public void Flatten(double x, double z, double r, double h, double blend = 0)
        {
            if (blend <= 0) blend = r * 0.9; double R = r + blend;
            int x0 = (int)Math.Floor((x - R + HALF) / CELL), x1 = (int)Math.Ceiling((x + R + HALF) / CELL);
            int z0 = (int)Math.Floor((z - R + HALF) / CELL), z1 = (int)Math.Ceiling((z + R + HALF) / CELL);
            for (int iz = Math.Max(0, z0); iz <= Math.Min(N - 1, z1); iz++) for (int ix = Math.Max(0, x0); ix <= Math.Min(N - 1, x1); ix++)
            {
                double d = GMath.Hypot(-HALF + ix * CELL - x, -HALF + iz * CELL - z), w = 1 - GMath.Smooth(r, R, d);
                if (w > 0) H[iz * N + ix] = (float)GMath.Lerp(H[iz * N + ix], h, w);
            }
            RegionChanged?.Invoke(x0 - 1, z0 - 1, x1 + 1, z1 + 1);
        }
        /// <summary>Marque une bande enneigée (les pistes recouvrent la roche).</summary>
        public void SnowAlong(IList<P3> pts, double halfW)
        {
            foreach (var p in pts)
            {
                int x0 = (int)Math.Floor((p.x - halfW + HALF) / CELL), x1 = (int)Math.Ceiling((p.x + halfW + HALF) / CELL);
                int z0 = (int)Math.Floor((p.z - halfW + HALF) / CELL), z1 = (int)Math.Ceiling((p.z + halfW + HALF) / CELL);
                for (int iz = Math.Max(0, z0); iz <= Math.Min(N - 1, z1); iz++) for (int ix = Math.Max(0, x0); ix <= Math.Min(N - 1, x1); ix++) SnowMask[iz * N + ix] = 1;
            }
        }

        // --- forêts (positions seulement : le modèle de sapin est celui du projet Unity) ---
        public void GenTrees()
        {
            var r = new Rng(Seed + 99); const double step = 21; Trees.Clear(); treeGrid.Clear();
            for (double z = -HALF + 30; z < HALF - 30; z += step) for (double x = -HALF + 30; x < HALF - 30; x += step)
            {
                double px = x + (r.Next() - 0.5) * step, pz = z + (r.Next() - 0.5) * step, rr = r.Next(), rs = r.Next();
                double h = GetH(px, pz), line = 2260 + noise.Fbm(px * 0.0012, pz * 0.0012, 2) * 260;
                if (h > line) continue;
                double dens = noise.Fbm(px * 0.0021 + 50, pz * 0.0021 + 9, 3) * 0.5 + 0.5, fade = GMath.Smooth(line, line - 260, h);
                if (rr > (dens - 0.34) * 1.9 * fade) continue;
                if (Slope(px, pz) > 0.8) continue;
                if (GMath.Hypot(px - VILLAGE.x, pz - 2720) < 560) continue;
                if (Math.Abs(px - 40) < 45 && pz > 2700) continue;
                Trees.Add(new Tree { x = px, z = pz, y = h, s = 0.75 + rs * 0.7 });
            }
            for (int i = 0; i < Trees.Count; i++)
            {
                var t = Trees[i]; int key = (int)((t.x + HALF) / 100) + (int)((t.z + HALF) / 100) * 1000;
                List<int> a; if (!treeGrid.TryGetValue(key, out a)) treeGrid[key] = a = new List<int>(); a.Add(i);
            }
            foreach (var i in RemovedTrees) if (i < Trees.Count) Trees[i].alive = false;
        }
        public event Action<int> TreeRemoved;
        public int TreesIn(Func<double, double, bool> test, double bx0, double bz0, double bx1, double bz1, bool remove)
        {
            int n = 0, gx0 = (int)((bx0 + HALF) / 100), gx1 = (int)((bx1 + HALF) / 100), gz0 = (int)((bz0 + HALF) / 100), gz1 = (int)((bz1 + HALF) / 100);
            for (int gz = gz0; gz <= gz1; gz++) for (int gx = gx0; gx <= gx1; gx++)
            {
                List<int> a; if (!treeGrid.TryGetValue(gx + gz * 1000, out a)) continue;
                foreach (int i in a) { var t = Trees[i]; if (t.alive && test(t.x, t.z)) { n++; if (remove) { t.alive = false; RemovedTrees.Add(i); TreeRemoved?.Invoke(i); } } }
            }
            return n;
        }
        public int TreesNearPath(IList<P2> pts, double halfW, bool remove)
        {
            double x0 = 1e9, z0 = 1e9, x1 = -1e9, z1 = -1e9;
            foreach (var p in pts) { x0 = Math.Min(x0, p.x); x1 = Math.Max(x1, p.x); z0 = Math.Min(z0, p.z); z1 = Math.Max(z1, p.z); }
            return TreesIn((x, z) => {
                for (int i = 0; i < pts.Count - 1; i++) if (GMath.SegDist(x, z, pts[i].x, pts[i].z, pts[i + 1].x, pts[i + 1].z) < halfW) return true;
                return pts.Count == 1 && GMath.Hypot(x - pts[0].x, z - pts[0].z) < halfW;
            }, x0 - halfW, z0 - halfW, x1 + halfW, z1 + halfW, remove);
        }

        /// <summary>Zones d'où une piste peut redescendre jusqu'au front de neige (descente continue, pente ≤ lim).</summary>
        public byte[] ReachMask(double lim = 0.42)
        {
            var seen = new byte[N * N]; var q = new List<int>();
            int hx = (int)Math.Round((HUB.x + HALF) / CELL), hz = (int)Math.Round((HUB.z + HALF) / CELL), R = (int)Math.Ceiling(HUB_R / CELL);
            for (int dz = -R; dz <= R; dz++) for (int dx = -R; dx <= R; dx++) if (GMath.Hypot(dx, dz) * CELL < HUB_R) { int i = (hz + dz) * N + hx + dx; seen[i] = 1; q.Add(i); }
            int[] ox = { 1, -1, 0, 0, 1, -1, 1, -1 }, oz = { 0, 0, 1, -1, 1, 1, -1, -1 }; double[] w = { 1, 1, 1, 1, 1.414, 1.414, 1.414, 1.414 };
            for (int k = 0; k < q.Count; k++)
            {
                int i = q[k], x = i % N, z = i / N;
                for (int m = 0; m < 8; m++)
                {
                    int nx = x + ox[m], nz = z + oz[m]; if (nx < 2 || nz < 2 || nx > N - 3 || nz > N - 3) continue;
                    int j = nz * N + nx; if (seen[j] != 0) continue; double s = (H[j] - H[i]) / (CELL * w[m]);
                    if (s > -0.02 && s <= lim) { seen[j] = 1; q.Add(j); }
                }
            }
            return seen;
        }
        public bool InReach(byte[] mask, double x, double z) { int gi = (int)Math.Round((z + HALF) / CELL) * N + (int)Math.Round((x + HALF) / CELL); return gi >= 0 && gi < mask.Length && mask[gi] != 0; }
    }

    /// <summary>Point 3D (x, y, z) avec abscisse curviligne s.</summary>
    public struct P3 { public double x, y, z, s; public P3(double x, double y, double z) { this.x = x; this.y = y; this.z = z; s = 0; } public P2 Xz { get { return new P2(x, z); } } }
}
