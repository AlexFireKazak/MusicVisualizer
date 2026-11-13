using SkiaSharp;
using System;

namespace MusicVisualizer.Visualizations
{
    public class MirroredBarsMode : IVisualizationMode
    {
        public string Name => "Зеркальные столбцы";

        private const int BarCount = 64;
        private const float Smoothing = 0.25f;
        private const float HeightRatio = 0.45f; // от центра максимум 45% высоты

        private readonly SKPaint _barPaint = new() { Style = SKPaintStyle.Fill, IsAntialias = true };
        private readonly SKPaint _bgPaint = new() { Color = new SKColor(20, 20, 20), Style = SKPaintStyle.Fill };
        private readonly SKPaint _midlinePaint = new() { Style = SKPaintStyle.Stroke, StrokeWidth = 1, Color = new SKColor(80, 80, 80, 150) };
        private readonly float[] _smoothed = new float[BarCount];

        public void Render(SKCanvas canvas, float width, float height, float[] audioData, float[] fftData)
        {
            canvas.DrawRect(0, 0, width, height, _bgPaint);
            if (fftData == null || fftData.Length == 0) return;

            float cx = height / 2;
            canvas.DrawLine(0, cx, width, cx, _midlinePaint);

            float barWidth = width / BarCount;
            const float spacing = 2f;

            for (int i = 0; i < BarCount; i++)
            {
                float ratio = i / (float)(BarCount - 1);
                int index = Math.Min((int)(ratio * ratio * (fftData.Length - 1)), fftData.Length - 1);

                float target = Math.Min(fftData[index], 1f);
                _smoothed[i] = _smoothed[i] * (1 - Smoothing) + target * Smoothing;

                float halfH = _smoothed[i] * height * HeightRatio;
                float x = i * barWidth + spacing / 2;

                float r = _smoothed[i];
                _barPaint.Color = new SKColor(
                    (byte)(100 + r * 155),
                    (byte)(50 + r * 205),
                    (byte)(150 - r * 100), 255);

                // Верхняя половина
                canvas.DrawRect(x, cx - halfH, barWidth - spacing, halfH, _barPaint);
                // Нижняя половина — симметрично
                canvas.DrawRect(x, cx, barWidth - spacing, halfH, _barPaint);
            }
        }

        public void Reset() => Array.Clear(_smoothed, 0, _smoothed.Length);
        public void Dispose() { _barPaint.Dispose(); _bgPaint.Dispose(); _midlinePaint.Dispose(); }
    }
}