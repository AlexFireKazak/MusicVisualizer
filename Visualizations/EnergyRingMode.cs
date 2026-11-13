using SkiaSharp;
using System;

namespace MusicVisualizer.Visualizations
{
    public class EnergyRingMode : IVisualizationMode
    {
        public string Name => "Энергетическое кольцо";

        private readonly SKPaint _bgPaint = new() { Color = new SKColor(15, 15, 25), Style = SKPaintStyle.Fill };
        private readonly SKPaint _trackPaint = new() { Style = SKPaintStyle.Stroke, StrokeWidth = 20, IsAntialias = true };
        private readonly SKPaint _arcPaint = new() { Style = SKPaintStyle.Stroke, StrokeWidth = 20, StrokeCap = SKStrokeCap.Round, IsAntialias = true };
        private readonly SKPaint _dotPaint = new() { Style = SKPaintStyle.Fill, IsAntialias = true };

        private float _level, _peak;
        private const float Smoothing = 0.25f;
        private const float PeakDecay = 0.006f;

        public void Render(SKCanvas canvas, float width, float height, float[] audioData, float[] fftData)
        {
            canvas.DrawRect(0, 0, width, height, _bgPaint);
            if (audioData == null || audioData.Length == 0) return;

            float rms = 0;
            for (int i = 0; i < audioData.Length; i++) rms += audioData[i] * audioData[i];
            rms = MathF.Sqrt(rms / audioData.Length);

            _level = _level * (1 - Smoothing) + rms * Smoothing;
            _peak = Math.Max(_level, _peak - PeakDecay);

            var center = new SKPoint(width / 2, height / 2);
            float radius = Math.Min(width, height) * 0.35f;
            var rect = new SKRect(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius);

            _trackPaint.Color = new SKColor(40, 40, 60);
            canvas.DrawCircle(center, radius, _trackPaint);

            float sweep = Math.Clamp(_level * 4, 0, 1) * 360;
            _arcPaint.Color = SKColor.FromHsl(200 - sweep * 0.4f, 80, 55);
            using (var path = new SKPath())
            {
                path.AddArc(rect, -90, sweep);
                canvas.DrawPath(path, _arcPaint);
            }

            float peakAngle = (-90 + Math.Clamp(_peak * 4, 0, 1) * 360) * MathF.PI / 180;
            _dotPaint.Color = SKColors.White;
            canvas.DrawCircle(center.X + MathF.Cos(peakAngle) * radius,
                              center.Y + MathF.Sin(peakAngle) * radius, 8, _dotPaint);
        }

        public void Reset() { _level = _peak = 0; }
        public void Dispose() { _bgPaint.Dispose(); _trackPaint.Dispose(); _arcPaint.Dispose(); _dotPaint.Dispose(); }
    }
}