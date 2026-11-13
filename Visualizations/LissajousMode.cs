using SkiaSharp;
using System;

namespace MusicVisualizer.Visualizations
{
    public class LissajousMode : IStereoVisualizationMode
    {
        public string Name => "Фигуры Лиссажу";

        private readonly SKPaint _bgPaint = new() { Color = new SKColor(5, 5, 15), Style = SKPaintStyle.Fill };
        private readonly SKPaint _linePaint = new()
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.5f,
            Color = new SKColor(100, 200, 255, 220),
            IsAntialias = true
        };

        public void Render(SKCanvas canvas, float width, float height, float[] audioData, float[] fftData)
            => RenderStereo(canvas, width, height, audioData, audioData, fftData);

        public void RenderStereo(SKCanvas canvas, float width, float height, float[] left, float[] right, float[] fftData)
        {
            canvas.DrawRect(0, 0, width, height, _bgPaint);
            if (left == null || right == null || left.Length < 2) return;

            float cx = width / 2, cy = height / 2;
            float scale = Math.Min(width, height) * 0.42f;

            using var path = new SKPath();
            path.MoveTo(cx + left[0] * scale, cy + right[0] * scale);
            for (int i = 1; i < left.Length; i++)
                path.LineTo(cx + left[i] * scale, cy + right[i] * scale);
            canvas.DrawPath(path, _linePaint);
        }

        public void Reset() { }
        public void Dispose() { _bgPaint.Dispose(); _linePaint.Dispose(); }
    }
}