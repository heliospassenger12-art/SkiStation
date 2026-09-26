using System;
using UnityEngine;

namespace SkiStation.MountainLab.Editor
{
    [Serializable]
    public sealed class MountainLabSettings317
    {
        public int seed = 317;
        public float heightScale = 1f;
        public float naturalVariation = .72f;
        public int resolution = 257;
        public bool trees = true;
        public bool slopeColours;
    }

    // 3.17: connected 1600-4200 m high-mountain study with passes, hanging valleys, glacier dome and 360-degree skiing.
    // Editor-only: deliberately isolated from the management terrain until the player validates the shape.
    public sealed class MountainLabHeight317
    {
        public const float Width = 6000f, Length = 8200f;
        public const float MinX = -3000f, MinZ = -2800f, FlatEnd = -1500f, BaseY = 1600f;
        readonly FastNoiseLite broad, detail, ridge, warp;
        readonly float scale, variation;

        public MountainLabHeight317(MountainLabSettings317 settings)
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
            float saddle = -120f * Gaussian(x, 120f, 520f) * Gaussian(z, 3170f, 650f);

            float noiseFade = Blend(z, -1300f, -450f) * (1f - .35f*Blend(z,3900f,5200f));
            float rolls = (68f*broad.GetNoise(wx,wz) + 13f*detail.GetNoise(x,z))*variation*noiseFade;

            // High alpine/glacial crown: wide snowfields separated by rocky crests rather than one needle peak.
            float summitMask = Blend(z, 2550f, 3450f) * (1f - .35f*Blend(z,4300f,5350f));
            float summit = summitMask * (150f + 120f*ridge.GetNoise(wx,wz));
            float crest = 90f * summitMask * Mathf.Abs(Mathf.Sin((x + .22f*z) * .0022f));
            float glacierBasin = -115f * Gaussian(x, -450f, 1250f) * Gaussian(z, 3850f, 900f);

            // 3.17 — glacier architecture: long flowing ice tongues, cirques and rocky ribs.
            // Keep the glacier broadly skiable; steep visual drama lives mainly on its edges and crests.
            float iceZone = Blend(z, 2850f, 3650f);
            float glacierTongueA = -125f * Gaussian(x + 520f + .11f*(z-3300f), 0f, 720f) * Gaussian(z, 3900f, 1250f) * iceZone;
            float glacierTongueB = -92f * Gaussian(x - 1280f - .06f*(z-3600f), 0f, 560f) * Gaussian(z, 4100f, 1100f) * iceZone;
            float highCirque = -155f * Gaussian(x, 520f, 1050f) * Gaussian(z, 4300f, 720f);
            float rockyRibWest = 185f * Gaussian(x, -2150f, 330f) * Gaussian(z, 3600f, 1050f) * iceZone;
            float rockyRibEast = 210f * Gaussian(x, 2250f, 300f) * Gaussian(z, 3850f, 980f) * iceZone;
            float summitNeedleA = 245f * Gaussian(x, -1500f, 420f) * Gaussian(z, 4200f, 520f);
            float summitNeedleB = 280f * Gaussian(x, 1550f, 390f) * Gaussian(z, 4450f, 500f);
            // Soft transverse waves suggest glacier flow without creating impassable steps.
            float glacierFlow = iceZone * 24f * Mathf.Sin((z + .18f*x) * .0042f) * (0.55f + .45f*Gaussian(x, 250f, 1900f));

            // 3.17 — natural altitude balconies: broad, gently undulating benches rather than artificial flat platforms.
            // They create believable buildable/activity areas for refuges, restaurants, sled dogs and paragliding.
            float balconyLow = -105f * Gaussian(z, 250f, 430f) * Gaussian(x, -1050f, 1450f);      // ~2000-2400 m wooded balcony
            float balconyMid = -145f * Gaussian(z, 1450f, 520f) * Gaussian(x, 850f, 1650f);       // ~2500-3000 m activity plateau
            float balconyPanorama = -175f * Gaussian(z, 2550f, 500f) * Gaussian(x, -650f, 1550f); // ~3000-3500 m panoramic terrace
            float glacierShelf = -135f * Gaussian(z, 3650f, 560f) * Gaussian(x, 700f, 1750f);     // high gentle glacier shelf

            // Soft local undulation keeps every balcony natural and prevents perfectly horizontal shelves.
            float balconyMask = Mathf.Clamp01(
                Gaussian(z, 250f, 650f) + Gaussian(z, 1450f, 720f) + Gaussian(z, 2550f, 680f) + Gaussian(z, 3650f, 720f));
            float balconyRolls = balconyMask * variation * (18f * broad.GetNoise(wx + 420f, wz - 310f) + 6f * detail.GetNoise(x, z));

            // Small raised lips around selected benches make the transitions read as natural shoulders/terraces.
            float terraceLipA = 42f * Gaussian(z, 760f, 260f) * Gaussian(x, -1250f, 1700f);
            float terraceLipB = 55f * Gaussian(z, 2020f, 300f) * Gaussian(x, 650f, 1750f);
            float terraceLipC = 48f * Gaussian(z, 3170f, 290f) * Gaussian(x, -450f, 1850f);

            // 3.17 — connected high-mountain geography: passes and hanging valleys make the crown traversable.
            // These are terrain forms, not pistes: the player remains free to choose the actual ski-domain layout.
            float glacierPassWest = -105f * Gaussian(x, -1050f, 430f) * Gaussian(z, 3500f, 520f) * iceZone;
            float glacierPassEast = -95f * Gaussian(x, 1180f, 420f) * Gaussian(z, 3820f, 500f) * iceZone;
            float hangingValleyWest = -115f * Gaussian(x, -1650f, 650f) * Gaussian(z, 2850f, 780f);
            float hangingValleyEast = -125f * Gaussian(x, 1550f, 700f) * Gaussian(z, 3000f, 820f);
            float panoramicSpur = 92f * Gaussian(x, -450f, 720f) * Gaussian(z, 2050f, 620f);
            float glacierDome = 135f * Gaussian(x, 250f, 980f) * Gaussian(z, 4680f, 700f) * iceZone;

            // 3.17 — summit diversity: a serrated mineral crown, rounded glacial domes and broad skiable cols.
            // The contrast avoids a repeated ridge silhouette while keeping several natural crossings.
            float mineralCrownWest = 250f * Gaussian(x, -2350f, 360f) * Gaussian(z, 4350f, 620f) * (0.72f + 0.28f*Mathf.Abs(Mathf.Sin((x + .31f*z)*.0062f)));
            float mineralCrownEast = 285f * Gaussian(x, 2380f, 340f) * Gaussian(z, 4520f, 650f) * (0.70f + 0.30f*Mathf.Abs(Mathf.Sin((x - .27f*z)*.0068f)));
            float centralRockTower = 205f * Gaussian(x, 720f, 360f) * Gaussian(z, 4920f, 430f);
            float roundedGlacialDomeWest = 105f * Gaussian(x, -780f, 900f) * Gaussian(z, 4750f, 760f);
            float roundedGlacialDomeEast = 90f * Gaussian(x, 1450f, 820f) * Gaussian(z, 4250f, 720f);
            float broadColWest = -125f * Gaussian(x, -1550f, 520f) * Gaussian(z, 4380f, 470f);
            float broadColCentral = -145f * Gaussian(x, 120f, 600f) * Gaussian(z, 4550f, 500f);
            float broadColEast = -118f * Gaussian(x, 1620f, 500f) * Gaussian(z, 4460f, 480f);
            float summitSerrationMask = Blend(z, 3600f, 4300f) * (1f - Blend(z, 5050f, 5350f));
            float summitSerration = summitSerrationMask * 58f * Mathf.Abs(Mathf.Sin((x + .42f*z) * .0049f)) * (0.45f + 0.55f*Gaussian(x, 0f, 2700f));

            // 3.17 — grands vallons intermédiaires : relient visuellement et topographiquement le village aux étages alpins.
            // Ils ne tracent aucune piste : ce sont de larges formes naturelles qui laissent plusieurs choix au joueur.
            float lowerWestValley = -92f * Gaussian(x, -1550f, 620f) * Gaussian(z, 650f, 1050f);
            float lowerEastValley = -78f * Gaussian(x, 1450f, 700f) * Gaussian(z, 900f, 1150f);
            float middleWestValley = -128f * Gaussian(x, -900f, 720f) * Gaussian(z, 1850f, 1050f);
            float middleEastValley = -112f * Gaussian(x, 1250f, 760f) * Gaussian(z, 2100f, 1100f);
            float alpineConnector = -118f * Gaussian(x, 180f, 850f) * Gaussian(z, 2850f, 980f);
            // Épaules entre vallons : elles évitent une succession de cuvettes molles et créent des changements de paysage.
            float valleyShoulderWest = 72f * Gaussian(x, -2350f, 520f) * Gaussian(z, 1450f, 1450f);
            float valleyShoulderEast = 86f * Gaussian(x, 2350f, 500f) * Gaussian(z, 1700f, 1500f);
            float middleSpine = 58f * Gaussian(x, 380f, 520f) * Gaussian(z, 1500f, 1150f);
            // Une ondulation très large casse les longues pentes uniformes sans créer de marches artificielles.
            float valleyWaveMask = Blend(z, -850f, 450f) * (1f - Blend(z, 3100f, 3700f));
            float valleyWaves = valleyWaveMask * variation * (30f * Mathf.Sin((x + .20f*z) * .00155f) + 14f * broad.GetNoise(wx-760f,wz+540f));

            // Rear face remains usable: the main crown drops toward a second high valley behind the watershed.
            float rear = Blend(z, 4050f, 5400f);
            float rearDrop = -rear * 820f;
            float rearBowl = -150f * Gaussian(x, -650f, 1050f) * Gaussian(z, 4850f, 800f) * rear;
            float rearFamily = -70f * Gaussian(x, -1900f, 720f) * Gaussian(z, 4700f, 900f) * rear;
            float rearSpur = 150f * Gaussian(x, 1650f, 620f) * Gaussian(z, 4550f, 800f) * rear;
            float rearRolls = rear * variation * (42f*broad.GetNoise(wx+900f,wz-1200f) + 10f*detail.GetNoise(x,z));

            return BaseY + scale * Mathf.Max(0f, climb + glacier + lowPlateau + westShoulder + centralValley + eastShoulder + familySector + alpineBowl + corridorA + corridorB + saddle + rolls + summit + crest + glacierBasin + glacierTongueA + glacierTongueB + highCirque + rockyRibWest + rockyRibEast + summitNeedleA + summitNeedleB + glacierFlow + balconyLow + balconyMid + balconyPanorama + glacierShelf + balconyRolls + terraceLipA + terraceLipB + terraceLipC + glacierPassWest + glacierPassEast + hangingValleyWest + hangingValleyEast + panoramicSpur + glacierDome + mineralCrownWest + mineralCrownEast + centralRockTower + roundedGlacialDomeWest + roundedGlacialDomeEast + broadColWest + broadColCentral + broadColEast + summitSerration + lowerWestValley + lowerEastValley + middleWestValley + middleEastValley + alpineConnector + valleyShoulderWest + valleyShoulderEast + middleSpine + valleyWaves + rearDrop + rearBowl + rearFamily + rearSpur + rearRolls);
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
