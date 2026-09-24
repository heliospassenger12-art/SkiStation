using UnityEngine;

namespace SkiStation
{
    // 2.45: four connected slope sectors, flat southern base, rocky northern crown.
    // Public entry point retained so the mesh and gameplay use one height source.
    public static class AlpineMassifGeneratorV2
    {
        public const float HalfSize = 2100f;
        public const float HalfWidth = 4000f;
        public const float MinZ = -3400f;
        public const float BaseHeight = 82f;
        public const float FlatEndZ = -1400f;
        public const float SlopeTopZ = 1600f;
        public const float RockStartZ = 1650f;
        public const float RockEndZ = 1950f;

        public static float Height(float x, float z)
        {
            x = Mathf.Clamp(x,-HalfWidth,HalfWidth);
            z = Mathf.Clamp(z,MinZ,HalfSize);
            float u = Mathf.Clamp01((z-FlatEndZ)/(SlopeTopZ-FlatEndZ));
            // Monotonic, laterally level piste-building surface; gentle at the base.
            float slope = SectorRise(x) * Mathf.Pow(u,1.25f);
            // Broad, low undulations, fading completely at the flat resort floor.
            float fade = Blend(u,0f,.18f) * (1f-Blend(u,.92f,1f));
            float rolls = 5f*Mathf.Sin(x*.006f+Mathf.Sin(z*.002f))*Mathf.Sin(z*.007f)
                        + 2f*Mathf.Sin(x*.013f+z*.011f);
            slope += rolls*fade;
            float rock = Mathf.Clamp01((z-RockStartZ)/(RockEndZ-RockStartZ));
            rock = rock*rock*(3f-2f*rock);
            float crown = 215f + 28f*Mathf.Sin(x*.008f) + 15f*Mathf.Cos(x*.019f);
            return BaseHeight + slope + rock*crown;
        }
        public static float SectorRise(float x)
        {
            x *= .75f; // Widen the four sectors with the enlarged mountain.
            return 280f + 280f*Blend(x,-2050f,-1150f)
                + 340f*Blend(x,-450f,450f) + 500f*Blend(x,1150f,2050f);
        }
        private static float Blend(float x,float a,float b)
        {
            float t=Mathf.Clamp01((x-a)/(b-a)); return t*t*(3f-2f*t);
        }
    }
}
