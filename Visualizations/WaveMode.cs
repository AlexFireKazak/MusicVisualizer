using SkiaSharp;
using System;

namespace MusicVisualizer.Visualizations
{
    public class WaveMode : IVisualizationMode
    {
        public string Name => "Волна";

        private readonly SKPaint _bgPaint = new() { Color = new SKColor(8, 12, 22), Style = SKPaintStyle.Fill };
        private readonly SKPaint _wave1 = new() { Style = SKPaintStyle.Stroke, StrokeWidth = 3, Color = new SKColor(100, 180, 255, 255), IsAntialias = true, StrokeCap = SKStrokeCap.Round };
        private readonly SKPaint _wave2 = new() { Style = SKPaintStyle.Stroke, StrokeWidth = 2, Color = new SKColor(140, 120, 255, 170), IsAntialias = true, StrokeCap = SKStrokeCap.Round };
        private readonly SKPaint _wave3 = new() { Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f, Color = new SKColor(200, 140, 255, 90), IsAntialias = true, StrokeCap = SKStrokeCap.Round };

        private float _amplitude;
        private float _freq = 2f;
        private double _phase;
        private const float AmpSmoothing = 0.2f;
        private const float FreqSmoothing = 0.1f;

        public void Render(SKCanvas canvas, float width, float height, float[] audioData, float[] fftData)
        {
            canvas.DrawRect(0, 0, width, height, _bgPaint);

            // RMS по waveform
            float rms = 0;
            if (audioData != null && audioData.Length > 0)
            {
                for (int i = 0; i < audioData.Length; i++)
                    rms += audioData[i] * audioData[i];
                rms = MathF.Sqrt(rms*6 / audioData.Length);
            }

            // Доминирующая частота — ищем максимум по первым 64 бинам
            int dominantBin = 0;
            float maxMag = 0;
            if (fftData != null && fftData.Length > 2)
            {
                int limit = Math.Min(fftData.Length, 64);
                for (int i = 2; i < limit; i++)
                    if (fftData[i] > maxMag) { maxMag = fftData[i]; dominantBin = i; }
            }

            float targetAmp = Math.Clamp(rms * 6, 0, 1) * height * 0.35f;
            float targetFreq = 2f + (dominantBin / 64f) * 8f; // 2..8 периодов

            _amplitude = _amplitude * (1 - AmpSmoothing) + targetAmp * AmpSmoothing;
            _freq = _freq * (1 - FreqSmoothing) + targetFreq * FreqSmoothing;
            _phase += 0.04;

            float cy = height / 2;
            const int points = 320;
            float step = width / (points - 1);

            DrawWave(canvas, cy, points, step, 1.0f, 1.0f, _wave1);
            DrawWave(canvas, cy, points, step, 0.55f, 0.7f, _wave2);
            DrawWave(canvas, cy, points, step, 0.25f, 0.4f, _wave3);
        }

        private void DrawWave(SKCanvas canvas, float cy, int points, float step, float ampScale, float phaseScale, SKPaint paint)
        {
            using var path = new SKPath();
            for (int i = 0; i < points; i++)
            {
                float x = i * step;
                float t = i / (float)(points - 1);
                float y = cy + MathF.Sin((float)(t * _freq * MathF.PI * 2 + _phase * phaseScale)) * _amplitude * ampScale;
                if (i == 0) path.MoveTo(x, y); else path.LineTo(x, y);
            }
            canvas.DrawPath(path, paint);
        }

        public void Reset() { _amplitude = 0; _freq = 2f; _phase = 0; }
        public void Dispose() { _bgPaint.Dispose(); _wave1.Dispose(); _wave2.Dispose(); _wave3.Dispose(); }
    }
}