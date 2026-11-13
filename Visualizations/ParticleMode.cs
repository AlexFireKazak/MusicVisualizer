using SkiaSharp;
using System;
using System.Collections.Generic;

namespace MusicVisualizer.Visualizations
{
    public class ParticleMode : IVisualizationMode
    {
        public string Name => "Частицы";
        private readonly SKPaint _particlePaint = new() { Style = SKPaintStyle.Fill, IsAntialias = true };
        private readonly SKPaint _bgPaint = new() { Color = new SKColor(0, 0, 0, 30), Style = SKPaintStyle.Fill };
        private readonly List<SKPoint> _particles = new();
        private readonly Random _rand = new();
        private float _width = 1000, _height = 1000;

        public ParticleMode()
        {
            for (int i = 0; i < 200; i++)
                _particles.Add(new SKPoint(_rand.Next(1000), _rand.Next(1000)));
        }

        public void Render(SKCanvas canvas, float width, float height, float[] audioData, float[] fftData)
        {
            _width = width; _height = height;
            canvas.DrawRect(0, 0, width, height, _bgPaint);
            if (fftData == null) return;

            float bass = 0;
            for (int i = 0; i < 10; i++) bass += fftData[i];
            bass /= 10;

            for (int i = 0; i < _particles.Count; i++)
            {
                var p = _particles[i];
                p.X += (_rand.NextSingle() - 0.5f) * (bass * 10 + 1);
                p.Y += (_rand.NextSingle() - 0.5f) * (bass * 10 + 1);
                if (p.X < 0) p.X = width; if (p.X > width) p.X = 0;
                if (p.Y < 0) p.Y = height; if (p.Y > height) p.Y = 0;
                _particles[i] = p;

                _particlePaint.Color = new SKColor(255, (byte)(bass * 255), 0, 200);
                canvas.DrawCircle(p.X, p.Y, 2 + bass * 5, _particlePaint);
            }
        }

        public void Reset()
        {
            for (int i = 0; i < _particles.Count; i++)
                _particles[i] = new SKPoint(_rand.Next((int)_width), _rand.Next((int)_height));
        }

        public void Dispose()
        {
            _particlePaint.Dispose();
            _bgPaint.Dispose();
        }
    }
}