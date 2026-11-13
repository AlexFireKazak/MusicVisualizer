using SkiaSharp;
using System;

namespace MusicVisualizer.Visualizations
{
    public class SpectrogramMode : IVisualizationMode
    {
        public string Name => "Спектрограмма";

        private const int HistoryWidth = 256;
        private const int FreqHeight = 128;

        private readonly SKPaint _bgPaint = new() { Color = SKColors.Black, Style = SKPaintStyle.Fill };
        private SKBitmap? _bitmap;
        private int _writeCol;

        public void Render(SKCanvas canvas, float width, float height, float[] audioData, float[] fftData)
        {
            canvas.DrawRect(0, 0, width, height, _bgPaint);

            if (_bitmap == null)
            {
                _bitmap = new SKBitmap(HistoryWidth, FreqHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var init = new SKCanvas(_bitmap);
                init.Clear(SKColors.Black);
            }

            if (fftData != null && fftData.Length > 0)
            {
                for (int y = 0; y < FreqHeight; y++)
                {
                    float normY = y / (float)(FreqHeight - 1);
                    int bin = Math.Min((int)(normY * normY * (fftData.Length - 1)), fftData.Length - 1);
                    _bitmap.SetPixel(_writeCol, FreqHeight - 1 - y, Colormap(fftData[bin]));
                }
                _writeCol = (_writeCol + 1) % HistoryWidth;
            }

            canvas.DrawBitmap(_bitmap, new SKRect(0, 0, width, height));
        }

        private static SKColor Colormap(float v)
        {
            v = Math.Clamp(v, 0, 1);
            // Jet: синий → голубой → зелёный → жёлтый → красный
            float r = Math.Clamp(1.5f - MathF.Abs(4 * v - 3), 0, 1);
            float g = Math.Clamp(1.5f - MathF.Abs(4 * v - 2), 0, 1);
            float b = Math.Clamp(1.5f - MathF.Abs(4 * v - 1), 0, 1);
            return new SKColor((byte)(r * 255), (byte)(g * 255), (byte)(b * 255), 255);
        }

        public void Reset()
        {
            if (_bitmap != null)
            {
                using var c = new SKCanvas(_bitmap);
                c.Clear(SKColors.Black);
            }
            _writeCol = 0;
        }

        public void Dispose()
        {
            _bgPaint.Dispose();
            _bitmap?.Dispose();
        }
    }
}