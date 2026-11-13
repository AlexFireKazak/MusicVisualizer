using SkiaSharp;
using System;

namespace MusicVisualizer.Visualizations
{
    public interface IVisualizationMode : IDisposable
    {
        void Render(SKCanvas canvas, float width, float height, float[] audioData, float[] fftData);
        string Name { get; }
        void Reset();
    }
}