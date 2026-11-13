using SkiaSharp;
using System;

namespace MusicVisualizer.Visualizations
{
    public class CircularSpectrumMode : IVisualizationMode
    {
        public string Name => "Круговой спектр";
        private const int Bars = 128;
        private const float Smoothing = 0.25f;

        private readonly SKPaint _linePaint = new() { Style = SKPaintStyle.Stroke, StrokeWidth = 2, IsAntialias = true };
        private readonly SKPaint _bgPaint = new() { Color = new SKColor(20, 20, 20, 255), Style = SKPaintStyle.Fill };
        private readonly float[] _smoothed = new float[Bars];

        public void Render(SKCanvas canvas, float width, float height, float[] audioData, float[] fftData)
        {
            canvas.DrawRect(0, 0, width, height, _bgPaint);
            if (fftData == null || fftData.Length == 0) return;

            var center = new SKPoint(width / 2, height / 2);
            float radius = Math.Min(width, height) / 4;

            for (int i = 0; i < Bars; i++)
            {
                float angle = (float)(i * 2 * Math.PI / Bars - Math.PI / 2);

                // Квадратичная индексация — та же, что у остальных спектральных режимов
                float ratio = i / (float)(Bars - 1);
                int index = Math.Min((int)(ratio * ratio * (fftData.Length - 1)), fftData.Length - 1);

                float target = Math.Min(fftData[index], 1f);
                _smoothed[i] = _smoothed[i] * (1 - Smoothing) + target * Smoothing;

                float amplitude = _smoothed[i] * radius;

                var start = new SKPoint(
                    center.X + (float)Math.Cos(angle) * radius,
                    center.Y + (float)Math.Sin(angle) * radius);
                var end = new SKPoint(
                    center.X + (float)Math.Cos(angle) * (radius + amplitude),
                    center.Y + (float)Math.Sin(angle) * (radius + amplitude));

                byte g = (byte)Math.Clamp(255 - _smoothed[i] * 255, 0, 255);
                _linePaint.Color = new SKColor(0, g, 255, 255);
                canvas.DrawLine(start, end, _linePaint);
            }
        }

        public void Reset() => Array.Clear(_smoothed, 0, _smoothed.Length);
        public void Dispose() { _linePaint.Dispose(); _bgPaint.Dispose(); }
    }
}