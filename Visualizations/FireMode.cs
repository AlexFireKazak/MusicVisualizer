using SkiaSharp;
using System;

namespace MusicVisualizer.Visualizations
{
    public class FireMode : IVisualizationMode
    {
        public string Name => "Огонь";

        private const int W = 600;
        private const int H = 120;
        private const float Decay = 0.94f;
        private const float BaseHeat = 0.32f;
        private const float FftContribution = 0.88f;

        private readonly SKPaint _bgPaint = new() { Color = SKColors.Black, Style = SKPaintStyle.Fill };
        private readonly SKPaint _drawPaint = new() { FilterQuality = SKFilterQuality.Medium, IsAntialias = false };
        private readonly float[] _fire = new float[W * H];
        private readonly Random _rand = new();
        private SKBitmap? _bitmap;

        public void Render(SKCanvas canvas, float width, float height, float[] audioData, float[] fftData)
        {
            canvas.DrawRect(0, 0, width, height, _bgPaint);

            _bitmap ??= new SKBitmap(W, H, SKColorType.Rgba8888, SKAlphaType.Premul);

            UpdateFire(fftData);
            WriteBitmap();

            canvas.DrawBitmap(_bitmap, new SKRect(0, 0, width, height), _drawPaint);
        }

        private void UpdateFire(float[] fftData)
        {
            // Нижний ряд — источник тепла
            for (int x = 0; x < W; x++)
            {
                float heat = BaseHeat;
                if (fftData != null && fftData.Length > 0)
                {
                    // Квадратичная индексация: низы дают больше тепла
                    float ratio = x / (float)(W - 1);
                    int idx = Math.Min((int)(ratio * ratio * (fftData.Length - 1)), fftData.Length - 1);
                    heat += Math.Min(fftData[idx], 1f) * FftContribution;
                }
                _fire[(H - 1) * W + x] = Math.Min(heat + _rand.NextSingle() * 0.08f, 1f);
            }

            // Распространение вверх с затуханием и случайным сдвигом
            for (int y = H - 2; y >= 0; y--)
            {
                int rowBase = y * W;
                int belowBase = (y + 1) * W;
                for (int x = 0; x < W; x++)
                {
                    int srcX = x + _rand.Next(-1, 2);
                    if (srcX < 0) srcX = 0;
                    else if (srcX >= W) srcX = W - 1;

                    float v = _fire[belowBase + srcX] * Decay;
                    _fire[rowBase + x] = v < 0.01f ? 0f : v;
                }
            }
        }

        private void WriteBitmap()
        {
            for (int y = 0; y < H; y++)
            {
                int baseIdx = y * W;
                for (int x = 0; x < W; x++)
                    _bitmap!.SetPixel(x, y, FireColor(_fire[baseIdx + x]));
            }
        }

        private static SKColor FireColor(float v)
        {
            if (v <= 0f) return SKColors.Black;
            if (v >= 1f) return SKColors.White;

            if (v < 0.33f)
            {
                float t = v / 0.33f;
                return new SKColor((byte)(t * 255), 0, 0, 255);
            }
            if (v < 0.66f)
            {
                float t = (v - 0.33f) / 0.33f;
                return new SKColor(255, (byte)(t * 255), 0, 255);
            }
            float t2 = (v - 0.66f) / 0.34f;
            return new SKColor(255, 255, (byte)(t2 * 255), 255);
        }

        public void Reset() => Array.Clear(_fire, 0, _fire.Length);

        public void Dispose()
        {
            _bgPaint.Dispose();
            _drawPaint.Dispose();
            _bitmap?.Dispose();
        }
    }
}