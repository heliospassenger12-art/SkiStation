using System;
using UnityEngine;

namespace SkiStation.MountainLab.Editor
{
    [Serializable]
    public sealed class MountainLabSettings306
    {
        public int seed = 306;
        public float heightScale = 1f;
        public float naturalVariation = .65f;
        public int resolution = 257;
        public bool trees = true;
        public bool slopeColours;
    }

    // Editor-only height source. No reference to the management height function or scene.
    public sealed class MountainLabHeight306
    {
        public const float Width = 8000f, Length = 5500f;
        public const float MinX = -4000f, MinZ = -3400f, FlatEnd = -1400f, BaseY = 82f;
        readonly FastNoiseLite broad, detail, ridge, warp;
        readonly float scale, variation;

        public MountainLabHeight306(MountainLabSettings306 settings)
        {
            scale = Mathf.Clamp(settings.heightScale, .6f, 1.4f);
            variation = Mathf.Clamp01(settings.naturalVariation);
            broad = Noise(settings.seed, .00085f, 3, FastNoiseLite.FractalType.FBm);
            detail = Noise(unchecked(settings.seed + 11), .005f, 2, FastNoiseLite.FractalType.FBm);
            ridge = Noise(unchecked(settings.seed + 29), .0022f, 3, FastNoiseLite.FractalType.Ridged);
            warp = new FastNoiseLite(unchecked(settings.seed + 47));
            warp.SetFrequency(.0007f);
            warp.SetDomainWarpAmp(180f * variation);
            warp.SetDomainWarpType(FastNoiseLite.DomainWarpType.OpenSimplex2);
        }

        static FastNoiseLite Noise(int seed, float frequency, int octaves, FastNoiseLite.FractalType type)
        {
            var noise = new FastNoiseLite(seed);
            noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            noise.SetFrequency(frequency);
            noise.SetFractalType(type);
            noise.SetFractalOctaves(octaves);
            noise.SetFractalGain(.45f);
            return noise;
        }

        public float Height(float x, float z)
        {
            x = Mathf.Clamp(x, MinX, -MinX);
            z = Mathf.Clamp(z, MinZ, MinZ + Length);
            // Preserve the entire 8 km x 2 km resort floor exactly, for every seed.
            if (z <= FlatEnd) return BaseY;
            float u = Mathf.Clamp01((z - FlatEnd) / 3000f);
            float wx = x, wz = z;
            warp.DomainWarp(ref wx, ref wz);
            float rise = 280f + 280f * Blend(wx, -2733f, -1533f)
                + 340f * Blend(wx, -600f, 600f) + 500f * Blend(wx, 1533f, 2733f);
            float baseSlope = rise * Mathf.Pow(u, 1.25f);
            float fade = Blend(u, 0f, .20f) * (1f - Blend(u, .88f, 1f));
            float rolls = (32f * broad.GetNoise(wx, wz) + 5f * detail.GetNoise(x, z)) * variation * fade;
            // Higher-frequency rock detail is confined to the summit crown.
            float crownMask = Blend(z, 1550f, 1920f);
            float crown = crownMask * (190f + 100f * ridge.GetNoise(wx, wz));
            float crestZ = 1810f + broad.GetNoise(x, 0f) * 90f;
            float scarp = Blend(z, crestZ - 35f, crestZ + 35f)
                * (85f + 55f * ridge.GetNoise(x, z * .3f));
            return BaseY + scale * (baseSlope + rolls + crown + scarp);
        }

        public float SlopeDegrees(float x, float z)
        {
            const float d = 8f;
            float xa = Mathf.Max(MinX, x - d), xb = Mathf.Min(-MinX, x + d);
            float za = Mathf.Max(MinZ, z - d), zb = Mathf.Min(MinZ + Length, z + d);
            float dx = (Height(xb, z) - Height(xa, z)) / Mathf.Max(.01f, xb - xa);
            float dz = (Height(x, zb) - Height(x, za)) / Mathf.Max(.01f, zb - za);
            return Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz)) * Mathf.Rad2Deg;
        }

        // Terrain inspection bands only: piste difficulty also depends on width and conditions.
        public static int SlopeBand(float degrees)
        { return degrees < 10f ? 0 : degrees < 18f ? 1 : degrees < 28f ? 2 : 3; }

        static float Blend(float x, float a, float b)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }
    }
}
