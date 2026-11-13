using MusicVisualizer.Visualizations;
using SkiaSharp;
using System;

public class SpectrumBarsMode : IVisualizationMode
{
    public string Name => "Спектральные столбцы";
    private const int BarCount = 64;
    private const float Smoothing = 0.25f;
    private readonly SKPaint _barPaint = new() { Style = SKPaintStyle.Fill, IsAntialias = true };
    private readonly SKPaint _bgPaint = new() { Color = new SKColor(20, 20, 20, 255), Style = SKPaintStyle.Fill };
    private readonly float[] _smoothed = new float[BarCount];

    public void Render(SKCanvas canvas, float width, float height, float[] audioData, float[] fftData)
    {
        canvas.DrawRect(0, 0, width, height, _bgPaint);
        if (fftData == null || fftData.Length == 0) return;

        float barWidth = width / BarCount;
        const float spacing = 2f;

        for (int i = 0; i < BarCount; i++)
        {
            // Квадратичная шкала: больше столбцов на низкие частоты
            float ratio = i / (float)(BarCount - 1);
            int index = Math.Min((int)(ratio * ratio * (fftData.Length - 1)), fftData.Length - 1);

            float target = Math.Min(fftData[index], 1f);
            _smoothed[i] = _smoothed[i] * (1 - Smoothing) + target * Smoothing;

            float normalizedHeight = _smoothed[i] * height * 0.9f;
            float x = i * barWidth + spacing / 2;
            float y = height - normalizedHeight;

            float r = normalizedHeight / height;
            _barPaint.Color = new SKColor(
                (byte)(100 + r * 155),
                (byte)(50 + r * 205),
                (byte)(150 - r * 100), 255);

            canvas.DrawRect(x, y, barWidth - spacing, normalizedHeight, _barPaint);
        }
    }

    public void Reset() => Array.Clear(_smoothed, 0, _smoothed.Length);
    public void Dispose() { _barPaint.Dispose(); _bgPaint.Dispose(); }
}