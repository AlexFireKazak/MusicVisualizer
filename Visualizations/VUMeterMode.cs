using SkiaSharp;
using System;

namespace MusicVisualizer.Visualizations
{
    public class VUMeterMode : IStereoVisualizationMode
    {
        public string Name => "VU-метр";

        private readonly SKPaint _bgPaint = new() { Color = new SKColor(20, 20, 20), Style = SKPaintStyle.Fill };
        private readonly SKPaint _barPaint = new() { Style = SKPaintStyle.Fill, IsAntialias = true };
        private readonly SKPaint _peakPaint = new() { Color = new SKColor(255, 255, 255, 220), Style = SKPaintStyle.Fill };
        private readonly SKPaint _labelPaint = new() { Color = SKColors.LightGray, TextSize = 20, IsAntialias = true };

        private float _lvlL, _lvlR, _pkL, _pkR;
        private const float Smoothing = 0.3f;
        private const float PeakDecay = 0.008f;

        public void Render(SKCanvas canvas, float width, float height, float[] audioData, float[] fftData)
            => RenderStereo(canvas, width, height, audioData, audioData, fftData);

        public void RenderStereo(SKCanvas canvas, float width, float height, float[] left, float[] right, float[] fftData)
        {
            canvas.DrawRect(0, 0, width, height, _bgPaint);
            if (left == null || right == null || left.Length == 0) return;

            _lvlL = _lvlL * (1 - Smoothing) + Rms(left) * Smoothing;
            _lvlR = _lvlR * (1 - Smoothing) + Rms(right) * Smoothing;
            _pkL = Math.Max(_lvlL, _pkL - PeakDecay);
            _pkR = Math.Max(_lvlR, _pkR - PeakDecay);

            float margin = width * 0.1f;
            float barW = width - margin * 2;
            float barH = height * 0.15f;
            float gap = height * 0.08f;
            float cy = height / 2;

            DrawBar(canvas, margin, cy - barH - gap / 2, barW, barH, _lvlL, _pkL, "L");
            DrawBar(canvas, margin, cy + gap / 2, barW, barH, _lvlR, _pkR, "R");
        }

        private void DrawBar(SKCanvas canvas, float x, float y, float w, float h, float lvl, float peak, string label)
        {
            float norm = ToNorm(lvl);
            float pk = ToNorm(peak);

            _barPaint.Color = new SKColor(35, 35, 35);
            canvas.DrawRect(x, y, w, h, _barPaint);

            using var shader = SKShader.CreateLinearGradient(
                new SKPoint(x, 0), new SKPoint(x + w, 0),
                new[] { new SKColor(0, 200, 0), new SKColor(200, 200, 0), new SKColor(220, 0, 0) },
                new[] { 0f, 0.75f, 1f }, SKShaderTileMode.Clamp);
            _barPaint.Shader = shader;
            canvas.DrawRect(x, y, w * norm, h, _barPaint);
            _barPaint.Shader = null;

            canvas.DrawRect(x + w * pk - 2, y, 4, h, _peakPaint);
            canvas.DrawText(label, x - 25, y + h / 2 + 7, _labelPaint);
        }

        private static float ToNorm(float v)
        {
            float db = 20 * MathF.Log10(Math.Max(v, 1e-6f));
            return Math.Clamp((db + 60) / 60, 0, 1);
        }

        private static float Rms(float[] d)
        {
            float s = 0;
            for (int i = 0; i < d.Length; i++) s += d[i] * d[i];
            return MathF.Sqrt(s / d.Length);
        }

        public void Reset() { _lvlL = _lvlR = _pkL = _pkR = 0; }
        public void Dispose() { _bgPaint.Dispose(); _barPaint.Dispose(); _peakPaint.Dispose(); _labelPaint.Dispose(); }
    }
}