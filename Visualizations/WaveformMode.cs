using SkiaSharp;
using System;

namespace MusicVisualizer.Visualizations
{
    public class WaveformMode : IVisualizationMode
    {
        public string Name => "Осциллограф";
        private const int Columns = 512;
        private const float TemporalSmoothing = 0.3f;

        private readonly SKPaint _fillPaint = new()
        {
            Style = SKPaintStyle.Fill,
            Color = new SKColor(0, 255, 0, 200),
            IsAntialias = true
        };
        private readonly SKPaint _bgPaint = new()
        {
            Color = new SKColor(10, 10, 10, 255),
            Style = SKPaintStyle.Fill
        };
        private readonly float[] _envMin = new float[Columns];
        private readonly float[] _envMax = new float[Columns];

        public void Render(SKCanvas canvas, float width, float height, float[] audioData, float[] fftData)
        {
            canvas.DrawRect(0, 0, width, height, _bgPaint);
            if (audioData == null || audioData.Length == 0) return;

            // DC removal — центрируем волну
            float mean = 0f;
            for (int i = 0; i < audioData.Length; i++) mean += audioData[i];
            mean /= audioData.Length;

            int samplesPerCol = Math.Max(1, audioData.Length / Columns);

            // Min/max по столбцам + временное сглаживание
            for (int col = 0; col < Columns; col++)
            {
                int start = col * samplesPerCol;
                int end = Math.Min(start + samplesPerCol, audioData.Length);

                float min = 0f, max = 0f;
                for (int i = start; i < end; i++)
                {
                    float v = audioData[i] - mean;
                    if (v < min) min = v;
                    if (v > max) max = v;
                }

                _envMin[col] = _envMin[col] * (1 - TemporalSmoothing) + min * TemporalSmoothing;
                _envMax[col] = _envMax[col] * (1 - TemporalSmoothing) + max * TemporalSmoothing;
            }

            // Заливка между min и max
            float step = width / (Columns - 1);
            var path = new SKPath();
            path.MoveTo(0, height / 2 - _envMax[0] * height / 2);
            for (int col = 1; col < Columns; col++)
                path.LineTo(col * step, height / 2 - _envMax[col] * height / 2);
            for (int col = Columns - 1; col >= 0; col--)
                path.LineTo(col * step, height / 2 - _envMin[col] * height / 2);
            path.Close();

            canvas.DrawPath(path, _fillPaint);
            path.Dispose();
        }

        public void Reset()
        {
            Array.Clear(_envMin, 0, _envMin.Length);
            Array.Clear(_envMax, 0, _envMax.Length);
        }

        public void Dispose()
        {
            _fillPaint.Dispose();
            _bgPaint.Dispose();
        }
    }
}