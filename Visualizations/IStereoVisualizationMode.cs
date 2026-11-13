using SkiaSharp;

namespace MusicVisualizer.Visualizations
{
    public interface IStereoVisualizationMode : IVisualizationMode
    {
        void RenderStereo(SKCanvas canvas, float width, float height,
                          float[] left, float[] right, float[] fftData);
    }
}