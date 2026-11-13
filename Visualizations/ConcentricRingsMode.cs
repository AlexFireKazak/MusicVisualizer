using SkiaSharp;
using System;

namespace MusicVisualizer.Visualizations
{
    public class ConcentricRingsMode : IVisualizationMode
    {
        public string Name => "Частотные кольца";

        private static readonly int[] BandEnds = { 8, 24, 64, 128, 192, 256 };
        private const int BandCount = 6;
        private const float Smoothing = 0.25f;
        private const float PeakDecay = 0.008f;

        private readonly SKPaint _bgPaint = new() { Color = new SKColor(10, 10, 20), Style = SKPaintStyle.Fill };
        private readonly SKPaint _ringPaint = new() { Style = SKPaintStyle.Stroke, IsAntialias = true };
        private readonly float[] _levels = new float[BandCount];
        private readonly float[] _peaks = new float[BandCount];

        public void Render(SKCanvas canvas, float width, float height, float[] audioData, float[] fftData)
        {
            canvas.DrawRect(0, 0, width, height, _bgPaint);
            if (fftData == null || fftData.Length == 0) return;

            var center = new SKPoint(width / 2, height / 2);
            float minDim = Math.Min(width, height);
            float baseR = minDim * 0.06f;
            float gap = minDim * 0.065f;
            float ampScale = gap * 1.0f;

            for (int b = 0; b < BandCount; b++)
            {
                int start = b == 0 ? 0 : BandEnds[b - 1];
                int end = Math.Min(BandEnds[b], fftData.Length);
                if (end <= start) continue;

                // Пик по диапазону, а не среднее — даёт полный отклик
                float peakInBand = 0f;
                for (int i = start; i < end; i++)
                    if (fftData[i] > peakInBand) peakInBand = fftData[i];
                peakInBand = Math.Min(peakInBand, 1f);

                _levels[b] = _levels[b] * (1 - Smoothing) + peakInBand * Smoothing;
                _peaks[b] = Math.Max(_levels[b], _peaks[b] - PeakDecay);

                float radius = baseR + b * gap + _levels[b] * ampScale;
                _ringPaint.StrokeWidth = 2 + _levels[b] * 18;
                _ringPaint.Color = SKColor.FromHsl((b * 55) % 360, 80, 55);
                canvas.DrawCircle(center, radius, _ringPaint);

                if (_peaks[b] > 0.05f)
                {
                    float peakR = baseR + b * gap + _peaks[b] * ampScale + 10;
                    _ringPaint.StrokeWidth = 1.5f;
                    _ringPaint.Color = new SKColor(255, 255, 255, 140);
                    canvas.DrawCircle(center, peakR, _ringPaint);
                }
            }
        }

        public void Reset()
        {
            Array.Clear(_levels, 0, BandCount);
            Array.Clear(_peaks, 0, BandCount);
        }

        public void Dispose() { _bgPaint.Dispose(); _ringPaint.Dispose(); }
    }
}