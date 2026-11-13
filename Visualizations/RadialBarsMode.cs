using SkiaSharp;
using System;

namespace MusicVisualizer.Visualizations
{
    public class RadialBarsMode : IVisualizationMode
    {
        public string Name => "Радиальные полосы";
        private const int Bars = 64;
        private const float Smoothing = 0.25f;
        private const float InnerRatio = 0.20f;
        private const float MaxRatio = 0.30f;

        private readonly SKPaint _barPaint = new() { Style = SKPaintStyle.Fill, IsAntialias = true };
        private readonly SKPaint _bgPaint = new() { Color = new SKColor(20, 20, 20, 255), Style = SKPaintStyle.Fill };
        private readonly float[] _smoothed = new float[Bars];

        public void Render(SKCanvas canvas, float width, float height, float[] audioData, float[] fftData)
        {
            canvas.DrawRect(0, 0, width, height, _bgPaint);
            if (fftData == null || fftData.Length == 0) return;

            var center = new SKPoint(width / 2, height / 2);
            float minDim = Math.Min(width, height);
            float innerRadius = minDim * InnerRatio;
            float maxBarLength = minDim * MaxRatio;

            for (int i = 0; i < Bars; i++)
            {
                // Та же квадратичная шкала, что в SpectrumBarsMode
                float ratio = i / (float)(Bars - 1);
                int index = Math.Min((int)(ratio * ratio * (fftData.Length - 1)), fftData.Length - 1);

                float target = Math.Min(fftData[index], 1f);
                _smoothed[i] = _smoothed[i] * (1 - Smoothing) + target * Smoothing;

                float amplitude = _smoothed[i] * maxBarLength;
                if (amplitude <= 0.5f) continue;

                float angle = (float)(i * 2 * Math.PI / Bars);
                _barPaint.Color = new SKColor(100, 50, 200, 255);

                canvas.Save();
                canvas.Translate(center.X, center.Y);
                canvas.RotateDegrees((float)(angle * 180 / Math.PI));
                canvas.DrawRect(-5, -innerRadius - amplitude, 10, amplitude, _barPaint);
                canvas.Restore();
            }
        }

        public void Reset() => Array.Clear(_smoothed, 0, _smoothed.Length);
        public void Dispose() { _barPaint.Dispose(); _bgPaint.Dispose(); }
    }
}