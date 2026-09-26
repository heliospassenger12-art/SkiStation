using System;
using UnityEngine;

namespace SkiStation.MountainLab.Editor
{
    [Serializable]
    public sealed class MountainLabSettings308
    {
        public int seed = 308;
        public float heightScale = 1f;
        public float naturalVariation = .72f;
        public int resolution = 257;
        public bool trees = true;
        public bool slopeColours;
    }

    // Compact first-resort massif inspired by broad snowy alpine plateaus, forest shoulders and a marked summit ridge.
    // Editor-only: deliberately isolated from the management terrain until the player validates the shape.
    public sealed class MountainLabHeight308
    {
        public const float Width = 4400f, Length = 3800f;
        public const float MinX = -2200f, MinZ = -2200f, FlatEnd = -1120f, BaseY = 82f;
        readonly FastNoiseLite broad, detail, ridge, warp;
        readonly float scale, variation;

        public MountainLabHeight308(MountainLabSettings308 settings)
        {
            scale = Mathf.Clamp(settings.heightScale, .7f, 1.25f);
            variation = Mathf.Clamp01(settings.naturalVariation);
            broad = Noise(settings.seed, .00105f, 4, FastNoiseLite.FractalType.FBm);
            detail = Noise(unchecked(settings.seed + 11), .0042f, 2, FastNoiseLite.FractalType.FBm);
            ridge = Noise(unchecked(settings.seed + 29), .0024f, 3, FastNoiseLite.FractalType.Ridged);
            warp = new FastNoiseLite(unchecked(settings.seed + 47));
            warp.SetFrequency(.0009f); warp.SetDomainWarpAmp(135f * variation);
            warp.SetDomainWarpType(FastNoiseLite.DomainWarpType.OpenSimplex2);
        }

        static FastNoiseLite Noise(int seed, float frequency, int octaves, FastNoiseLite.FractalType type)
        {
            var n = new FastNoiseLite(seed); n.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            n.SetFrequency(frequency); n.SetFractalType(type); n.SetFractalOctaves(octaves); n.SetFractalGain(.46f); return n;
        }

        public float Height(float x, float z)
        {
            x = Mathf.Clamp(x, MinX, -MinX); z = Mathf.Clamp(z, MinZ, MinZ + Length);
            if (z <= FlatEnd) return BaseY; // compact village/front-de-neige apron
            float u = Mathf.Clamp01((z - FlatEnd) / 2900f);
            float wx=x,wz=z; warp.DomainWarp(ref wx, ref wz);

            // Main climb: deliberately skiable, with a broad intermediate plateau rather than a single uniform ramp.
            float climb = 690f * Mathf.Pow(u, 1.14f);
            float plateau = -115f * Gaussian(z, 170f, 560f); // gentle mid-mountain bowl/plateau
            float upper = 135f * Blend(z, 720f, 1450f);

            // Asymmetric relief families visible in the references: rolling left shoulder, central valley, steeper right shoulder.
            float leftShoulder = 105f * Gaussian(x, -1320f, 680f) * Blend(z, -820f, 820f);
            float centralValley = -125f * Gaussian(x, -180f, 600f) * Blend(z, -650f, 1100f);
            float rightShoulder = 175f * Gaussian(x, 1220f, 620f) * Blend(z, -520f, 1400f);

            // Long gentle corridors: broad piste-like natural lines, not artificial trenches.
            float corridorA = -48f * Gaussian(x + 1050f + 0.18f*(z+300f), 0f, 210f) * Blend(z,-800f,1150f);
            float corridorB = -42f * Gaussian(x - 620f - 0.10f*(z+200f), 0f, 250f) * Blend(z,-700f,1300f);

            // Extra relief inspired by the references: rolling beginner plateau + open high alpine bowl.
            float rollingPlateau = 34f * Mathf.Sin((x + 0.35f*z) * .0025f) * Blend(z,-900f,500f) * (1f-Blend(z,700f,1150f));
            float alpineBowl = -72f * Gaussian(x, 520f, 780f) * Gaussian(z, 1050f, 620f);
            float sideRidge = 88f * Gaussian(x, -1750f, 330f) * Blend(z,-350f,1200f);

            float noiseFade = Blend(u, .05f, .22f) * (1f - .35f*Blend(u,.88f,1f));
            float rolls = (42f*broad.GetNoise(wx,wz) + 8f*detail.GetNoise(x,z))*variation*noiseFade;

            // Rocky summit only at the back/top: keeps most of the first resort readable and buildable.
            float summitMask = Blend(z, 1120f, 1570f);
            float summit = summitMask * (120f + 95f*ridge.GetNoise(wx,wz));
            float crest = 65f * summitMask * Mathf.Abs(Mathf.Sin((x + 0.28f*z) * .0031f));

            return BaseY + scale * Mathf.Max(0f, climb + plateau + upper + leftShoulder + centralValley + rightShoulder + corridorA + corridorB + rollingPlateau + alpineBowl + sideRidge + rolls + summit + crest);
        }

        public float SlopeDegrees(float x,float z)
        {
            const float d=7f; float xa=Mathf.Max(MinX,x-d), xb=Mathf.Min(-MinX,x+d);
            float za=Mathf.Max(MinZ,z-d), zb=Mathf.Min(MinZ+Length,z+d);
            float dx=(Height(xb,z)-Height(xa,z))/Mathf.Max(.01f,xb-xa), dz=(Height(x,zb)-Height(x,za))/Mathf.Max(.01f,zb-za);
            return Mathf.Atan(Mathf.Sqrt(dx*dx+dz*dz))*Mathf.Rad2Deg;
        }
        public static int SlopeBand(float d) { return d<10f?0:d<18f?1:d<28f?2:3; }
        static float Blend(float x,float a,float b){float t=Mathf.Clamp01((x-a)/(b-a));return t*t*(3f-2f*t);}
        static float Gaussian(float x,float centre,float sigma){float q=(x-centre)/Mathf.Max(1f,sigma);return Mathf.Exp(-.5f*q*q);}
    }
}
