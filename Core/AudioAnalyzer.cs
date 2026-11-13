using NAudio.Dsp;
using System;

namespace MusicVisualizer.Core
{
    public class AudioAnalyzer
    {
        private const int FftSize = 1024;
        private const int DisplayBins = 256;        // 0..~11 кГц при 44.1 кГц
        private const float MaxDecay = 0.95f;
        private const float MinMaxLevel = 0.010f;
        private const float TiltExponent = 0.15f;   // компенсация спада 1/f

        private readonly Complex[] _fftComplex = new Complex[FftSize];
        private readonly float[] _fftMagnitudes = new float[DisplayBins];
        private float _smoothedMax;

        public float[] ComputeSpectrum(float[] rawBuffer)
        {
            if (rawBuffer == null || rawBuffer.Length < FftSize)
                return Array.Empty<float>();

            for (int i = 0; i < FftSize; i++)
            {
                _fftComplex[i].X = (float)(rawBuffer[i] * FastFourierTransform.HannWindow(i, FftSize));
                _fftComplex[i].Y = 0;
            }
            FastFourierTransform.FFT(true, 10, _fftComplex);

            // Только первые DisplayBins
            float frameMax = 0f;
            for (int i = 0; i < DisplayBins; i++)
            {
                float re = _fftComplex[i].X;
                float im = _fftComplex[i].Y;
                float mag = (float)Math.Sqrt(re * re + im * im);
                _fftMagnitudes[i] = mag;
                if (mag > frameMax) frameMax = mag;
            }

            _smoothedMax = Math.Max(frameMax, _smoothedMax * MaxDecay);
            if (_smoothedMax < MinMaxLevel) _smoothedMax = MinMaxLevel;

            for (int i = 0; i < DisplayBins; i++)
            {
                float normalized = _fftMagnitudes[i] / _smoothedMax;
                float tilt = MathF.Pow(i + 1f, TiltExponent);   // 1.0 → 2.4 на верхах
                _fftMagnitudes[i] = MathF.Min(MathF.Sqrt(normalized * tilt), 1f);
            }

            return _fftMagnitudes;
        }

        public void Reset() => _smoothedMax = 0f;
    }
}