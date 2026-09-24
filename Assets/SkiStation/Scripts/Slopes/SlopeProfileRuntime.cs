using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum SlopeProfileMode { Natural, Graded, Bumps, Moguls, Compression, Wall }

    /// <summary>
    /// Shapes the skiable snow surface without destructively changing the mountain.
    /// The profile is stored as route geometry, so skiers also feel the resulting grade.
    /// </summary>
    public sealed class SlopeProfileRuntime : MonoBehaviour
    {
        [SerializeField] SlopeProfileMode mode = SlopeProfileMode.Natural;
        [SerializeField, Min(0.1f)] float amplitude = 0.55f;
        [SerializeField, Min(0.5f)] float wavelength = 7f;
        [SerializeField, Min(0.1f)] float blend = 1f;
        [SerializeField] bool applyOnStart = true;
        public SlopeProfileMode Mode => mode;
        public float Amplitude => amplitude;
        public float Wavelength => wavelength;
        public bool Applied { get; private set; }

        void Start() { if (applyOnStart) Apply(); }

        public void SetMode(SlopeProfileMode value) { mode = value; Applied = false; }
        public void SetAmplitude(float value) { amplitude = Mathf.Max(0.1f, value); Applied = false; }
        public void SetWavelength(float value) { wavelength = Mathf.Max(0.5f, value); Applied = false; }

        public bool Apply()
        {
            var slope = GetComponent<SlopeRuntime>();
            if (slope == null || slope.Points.Count < 2) return false;
            var source = new List<Vector3>(slope.Points);
            var widths = new List<float>(slope.Widths);
            if (mode == SlopeProfileMode.Natural) { Applied = true; return true; }

            float total = Mathf.Max(0.01f, slope.Length);
            float distance = 0f;
            for (int i = 0; i < source.Count; i++)
            {
                if (i > 0) distance += Vector3.Distance(source[i - 1], source[i]);
                float t = Mathf.Clamp01(distance / total);
                float d = ProfileDelta(distance, t);
                source[i] += Vector3.up * (d * Mathf.Clamp01(blend));
            }
            slope.Build(source, widths);
            Applied = true;
            var mesh = GetComponent<SlopeMeshGenerator>();
            if (mesh != null) mesh.Generate(slope);
            return true;
        }

        float ProfileDelta(float distance, float t)
        {
            switch (mode)
            {
                case SlopeProfileMode.Graded:
                    // Smoothly reduce small grade variations; endpoints stay fixed.
                    return Mathf.Sin(t * Mathf.PI) * -0.15f * amplitude;
                case SlopeProfileMode.Bumps:
                    return Mathf.Sin(distance / wavelength * Mathf.PI * 2f) * amplitude * 0.55f;
                case SlopeProfileMode.Moguls:
                    return Mathf.Sin(distance / Mathf.Max(2f, wavelength * 0.72f) * Mathf.PI * 2f)
                        * Mathf.Sin(t * Mathf.PI) * amplitude * 0.9f;
                case SlopeProfileMode.Compression:
                    return Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * amplitude * 1.4f;
                case SlopeProfileMode.Wall:
                    float wall = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.68f, 0.94f, t));
                    return wall * amplitude * 2.2f;
                default: return 0f;
            }
        }
    }
}
