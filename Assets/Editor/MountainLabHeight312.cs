using System;
using UnityEngine;

namespace SkiStation.MountainLab.Editor
{
    [Serializable]
    public sealed class MountainLabSettings312
    {
        public int seed = 312;
        public float heightScale = 1f;
        public float naturalVariation = .72f;
        public int resolution = 257;
        public bool trees = true;
        public bool slopeColours;
    }

    // 3.12: vertical resort study from 1600 m to a high glacial sector near 4200 m, with a 360-degree rear face.
    // Editor-only: deliberately isolated from the management terrain until the player validates the shape.
    public sealed class MountainLabHeight312
    {
        public const float Width = 6000f, Length = 8200f;
        public const float MinX = -3000f, MinZ = -2800f, FlatEnd = -1500f, BaseY = 1600f;
        readonly FastNoiseLite broad, detail, ridge, warp;
        readonly float scale, variation;

        public MountainLabHeight312(MountainLabSettings312 settings)
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
            if (z <= FlatEnd) return BaseY;
            float wx=x,wz=z; warp.DomainWarp(ref wx, ref wz);

            // 1600 -> ~4200 m: long progressive climb so the vertical range does not become one giant wall.
            float frontU = Mathf.Clamp01((z - FlatEnd) / 5000f);
            float climb = 1850f * Mathf.Pow(frontU, 1.12f);
            float high = Blend(z, 1750f, 3350f);
            float glacier = 200f * high;

            // Large readable terrain families: low resort, family shoulder, central valley and alpine side.
            float lowPlateau = -150f * Gaussian(z, -450f, 760f);
            float westShoulder = 150f * Gaussian(x, -1750f, 900f) * Blend(z, -1100f, 1550f);
            float centralValley = -185f * Gaussian(x, -180f, 820f) * Blend(z, -900f, 2250f);
            float eastShoulder = 230f * Gaussian(x, 1700f, 820f) * Blend(z, -650f, 2650f);
            float familySector = -75f * Gaussian(x, -1350f, 760f) * Gaussian(z, 350f, 1150f);
            float alpineBowl = -155f * Gaussian(x, 620f, 1050f) * Gaussian(z, 2350f, 950f);

            // Natural ski corridors and a broad saddle through the upper massif.
            float corridorA = -72f * Gaussian(x + 1450f + .16f*(z+250f), 0f, 300f) * Blend(z,-1150f,2200f);
            float corridorB = -65f * Gaussian(x - 850f - .08f*(z+100f), 0f, 340f) * Blend(z,-1000f,2600f);
            float saddle = -120f * Gaussian(x, 120f, 520f) * Gaussian(z, 3150f, 650f);

            float noiseFade = Blend(z, -1300f, -450f) * (1f - .35f*Blend(z,3900f,5200f));
            float rolls = (68f*broad.GetNoise(wx,wz) + 13f*detail.GetNoise(x,z))*variation*noiseFade;

            // High alpine/glacial crown: wide snowfields separated by rocky crests rather than one needle peak.
            float summitMask = Blend(z, 2550f, 3450f) * (1f - .35f*Blend(z,4300f,5350f));
            float summit = summitMask * (150f + 120f*ridge.GetNoise(wx,wz));
            float crest = 90f * summitMask * Mathf.Abs(Mathf.Sin((x + .22f*z) * .0022f));
            float glacierBasin = -115f * Gaussian(x, -450f, 1250f) * Gaussian(z, 3850f, 900f);

            // Rear face remains usable: the main crown drops toward a second high valley behind the watershed.
            float rear = Blend(z, 4050f, 5400f);
            float rearDrop = -rear * 820f;
            float rearBowl = -150f * Gaussian(x, -650f, 1050f) * Gaussian(z, 4850f, 800f) * rear;
            float rearFamily = -70f * Gaussian(x, -1900f, 720f) * Gaussian(z, 4700f, 900f) * rear;
            float rearSpur = 150f * Gaussian(x, 1650f, 620f) * Gaussian(z, 4550f, 800f) * rear;
            float rearRolls = rear * variation * (42f*broad.GetNoise(wx+900f,wz-1200f) + 10f*detail.GetNoise(x,z));

            return BaseY + scale * Mathf.Max(0f, climb + glacier + lowPlateau + westShoulder + centralValley + eastShoulder + familySector + alpineBowl + corridorA + corridorB + saddle + rolls + summit + crest + glacierBasin + rearDrop + rearBowl + rearFamily + rearSpur + rearRolls);
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
