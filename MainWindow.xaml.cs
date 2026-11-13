using Microsoft.Win32;
using MusicVisualizer.Core;
using MusicVisualizer.ViewModels;
using MusicVisualizer.Visualizations;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MusicVisualizer
{
    public partial class MainWindow : Window
    {
        private readonly PlaybackViewModel _vm = new();
        private readonly List<IVisualizationMode> _modes = new();
        private bool _isDraggingProgress;
        private bool _suppressProgressChanged;
        public MainWindow()
        {
            InitializeComponent();
            DataContext = _vm;

            _modes.Add(new SpectrumBarsMode());
            _modes.Add(new MirroredBarsMode());
            _modes.Add(new RadialBarsMode());
            _modes.Add(new CircularSpectrumMode());
            _modes.Add(new WaveformMode());
            _modes.Add(new FireMode());
            _modes.Add(new LissajousMode());
            _modes.Add(new SpectrogramMode());
            _modes.Add(new ConcentricRingsMode());
            _modes.Add(new VUMeterMode());
            _modes.Add(new EnergyRingMode());

            

            ModeSelector.ItemsSource = _modes;
            ModeSelector.DisplayMemberPath = "Name";
            if (_modes.Count > 0)
            {
                ModeSelector.SelectedIndex = 0;
                VisualizerControl.CurrentMode = _modes[0];
            }

            CompositionTarget.Rendering += OnRendering;
            Closed += OnClosed;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            _vm.UpdateFromRendering();

            _suppressProgressChanged = true;
            ProgressSlider.Value = _vm.Progress;
            _suppressProgressChanged = false;
            TimeLabel.Text = _vm.TimeText;

            if (!_vm.Engine.IsPlaying) return;
            var provider = _vm.Engine.VisualizerProvider;
            if (provider == null) return;
            var spectrum = _vm.Analyzer.ComputeSpectrum(provider.GetLatestFFTBuffer());
            var (stereoL, stereoR) = provider.GetLatestStereoData();
            VisualizerControl.UpdateData(provider.GetLatestWaveformData(), spectrum, stereoL, stereoR);
        }

        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suppressProgressChanged) return;
            if (_vm.Engine.TotalTime.TotalSeconds <= 0) return;
            _vm.Seek(e.NewValue);
            TimeLabel.Text = _vm.TimeText;
        }

        

        //private void ProgressSlider_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        //{
        //    var slider = (Slider)sender;
        //    double ratio = Math.Clamp(e.GetPosition(slider).X / slider.ActualWidth, 0, 1);

        //    _isDraggingProgress = true;
        //    _vm.Seek(ratio);
        //    slider.Value = ratio;
        //}

        //private void ProgressSlider_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        //{
        //    _vm.Seek(ProgressSlider.Value);
        //    _isDraggingProgress = false;
        //}

        private void OnClosed(object? sender, EventArgs e)
        {
            CompositionTarget.Rendering -= OnRendering;
            foreach (var mode in _modes) mode.Dispose();
            _vm.Engine.Dispose();
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Audio Files|*.mp3;*.wav;*.flac;*.ogg;*.m4a;*.aac",
                Title = "Выберите аудиофайл"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                _vm.LoadFile(dialog.FileName);
                Title = _vm.Title;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки файла: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e) => _vm.Engine.Play();
        private void Pause_Click(object sender, RoutedEventArgs e) => _vm.Engine.Pause();
        private void Stop_Click(object sender, RoutedEventArgs e) => _vm.Engine.Stop();

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) =>
            _vm.Engine.SetVolume((float)e.NewValue);
                
        private void ModeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModeSelector.SelectedItem is IVisualizationMode mode)
                VisualizerControl.CurrentMode = mode;
        }
    }
}