using MusicVisualizer.Visualizations;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System;
using System.Windows;

namespace MusicVisualizer.UI.Controls
{
    public class SkiaVisualizerControl : SKElement
    {
        private IVisualizationMode? _currentMode;
        private float[] _audioData = new float[512];
        private float[] _fftData = new float[512];
        private float[] _stereoLeft = Array.Empty<float>();
        private float[] _stereoRight = Array.Empty<float>();

        public static readonly DependencyProperty CurrentModeProperty =
            DependencyProperty.Register(nameof(CurrentMode), typeof(IVisualizationMode),
                typeof(SkiaVisualizerControl), new PropertyMetadata(null, OnModeChanged));

        public IVisualizationMode? CurrentMode
        {
            get => (IVisualizationMode?)GetValue(CurrentModeProperty);
            set => SetValue(CurrentModeProperty, value);
        }

        private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SkiaVisualizerControl control)
            {
                // Сбрасываем состояние старого режима, но НЕ Dispose — режим может вернуться
                control._currentMode?.Reset();
                control._currentMode = (IVisualizationMode?)e.NewValue;
                control._currentMode?.Reset();
                control.InvalidateVisual();
            }
        }

        public void UpdateData(float[] audioData, float[] fftData, float[] stereoLeft, float[] stereoRight)
        {
            _audioData = audioData;
            _fftData = fftData;
            _stereoLeft = stereoLeft;
            _stereoRight = stereoRight;
            InvalidateVisual();
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);
            var canvas = e.Surface.Canvas;
            float w = (float)ActualWidth, h = (float)ActualHeight;
            if (_currentMode == null || w <= 0 || h <= 0) return;

            if (_currentMode is IStereoVisualizationMode stereo && _stereoLeft.Length > 0)
                stereo.RenderStereo(canvas, w, h, _stereoLeft, _stereoRight, _fftData);
            else
                _currentMode.Render(canvas, w, h, _audioData, _fftData);
        }
    }
}