using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using MusicVisualizer.Core;
using MusicVisualizer.ViewModels;
using MusicVisualizer.Visualizations;

namespace MusicVisualizer
{
    public partial class MainWindow : Window
    {
        private readonly PlaybackViewModel _vm = new();
        private readonly List<IVisualizationMode> _modes = new();
        private readonly AppStateService _stateService = new();

        private bool _suppressProgressChanged;
        private Point _dragStartPoint;
        private int _dragSourceIndex = -1;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _vm;

            _modes.Add(new SpectrumBarsMode());
            _modes.Add(new MirroredBarsMode());
            _modes.Add(new RadialBarsMode());
            _modes.Add(new CircularSpectrumMode());
            _modes.Add(new SpectrogramMode());
            _modes.Add(new ConcentricRingsMode());
            _modes.Add(new WaveformMode());
            _modes.Add(new VUMeterMode());
            _modes.Add(new EnergyRingMode());
            _modes.Add(new LissajousMode());
            _modes.Add(new FireMode());

            ModeSelector.ItemsSource = _modes;
            ModeSelector.DisplayMemberPath = "Name";

            _vm.TrackChanged += (_, _) =>
            {
                VisualizerControl.CurrentMode?.Reset();
                foreach (var m in _modes) m.Reset();
            };

            RestoreState();

            CompositionTarget.Rendering += OnRendering;
            Closed += OnClosed;
        }

        private void RestoreState()
        {
            var state = _stateService.Load();

            VolumeSlider.Value = Math.Clamp(state.Volume, 0, 1);

            if (state.PlaylistPaths.Count > 0)
            {
                _vm.Playlist.AddFiles(state.PlaylistPaths.Where(File.Exists));
            }

            var viz = _modes.FirstOrDefault(m => m.Name == state.VisualizationName) ?? _modes.FirstOrDefault();
            if (viz != null)
            {
                ModeSelector.SelectedItem = viz;
                VisualizerControl.CurrentMode = viz;
            }

            if (_vm.Playlist.Tracks.Count > 0)
            {
                int targetIndex = -1;
                bool sameTrackFound = false;

                if (!string.IsNullOrEmpty(state.CurrentTrackPath))
                {
                    for (int i = 0; i < _vm.Playlist.Tracks.Count; i++)
                    {
                        if (string.Equals(_vm.Playlist.Tracks[i].FilePath, state.CurrentTrackPath,
                                          StringComparison.OrdinalIgnoreCase))
                        {
                            targetIndex = i;
                            sameTrackFound = true;
                            break;
                        }
                    }
                }

                if (targetIndex < 0)
                {
                    // Сохранённый трек отсутствует (удалён) — играем по «следующему» индексу,
                    // но всегда с начала
                    targetIndex = Math.Clamp(state.CurrentIndex, 0, _vm.Playlist.Tracks.Count - 1);
                }

                var startPos = sameTrackFound
                    ? TimeSpan.FromSeconds(Math.Max(0, state.PositionSeconds))
                    : TimeSpan.Zero;

                _vm.PlayAt(targetIndex, autoPlay: false, startPosition: startPos);
                PlaylistList.SelectedIndex = targetIndex;

                _vm.UpdateFromRendering();
                _suppressProgressChanged = true;
                ProgressSlider.Value = _vm.Progress;
                _suppressProgressChanged = false;
                TimeLabel.Text = _vm.TimeText;
            }
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

            var spectrum = _vm.ComputeSpectrum(provider.GetLatestFFTBuffer());
            var (stereoL, stereoR) = provider.GetLatestStereoData();
            VisualizerControl.UpdateData(provider.GetLatestWaveformData(), spectrum, stereoL, stereoR);
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            CompositionTarget.Rendering -= OnRendering;

            var currentTrack = (_vm.Playlist.CurrentIndex >= 0 &&
                    _vm.Playlist.CurrentIndex < _vm.Playlist.Tracks.Count)
    ? _vm.Playlist.Tracks[_vm.Playlist.CurrentIndex].FilePath
    : null;

            _stateService.Save(new AppState
            {
                PlaylistPaths = _vm.Playlist.Tracks.Select(t => t.FilePath).ToList(),
                CurrentIndex = _vm.Playlist.CurrentIndex,
                CurrentTrackPath = currentTrack,
                VisualizationName = (ModeSelector.SelectedItem as IVisualizationMode)?.Name,
                Volume = VolumeSlider.Value,
                PositionSeconds = (currentTrack != null && _vm.Engine.TotalTime.TotalSeconds > 0)
                    ? _vm.Engine.Position.TotalSeconds
                    : 0
            });

            foreach (var mode in _modes) mode.Dispose();
            _vm.Engine.Dispose();
        }

        // ─── Верхняя панель ──────────────────────────────────────────

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Audio Files|*.mp3;*.wav;*.flac;*.ogg;*.m4a;*.aac;*.aiff",
                Multiselect = true,
                Title = "Выберите аудиофайлы"
            };
            if (dialog.ShowDialog() != true) return;
            AddFilesToPlaylist(dialog.FileNames);
        }

        private void AddFilesToPlaylist(IEnumerable<string> paths)
        {
            bool wasEmpty = _vm.Playlist.Tracks.Count == 0;
            _vm.Playlist.AddFiles(paths);
            if (wasEmpty && _vm.Playlist.Tracks.Count > 0)
                _vm.PlayAt(0);
        }

        private void Play_Click(object sender, RoutedEventArgs e) => _vm.Play();
        private void Pause_Click(object sender, RoutedEventArgs e) => _vm.Pause();
        private void Stop_Click(object sender, RoutedEventArgs e) => _vm.Stop();

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) =>
            _vm.SetVolume((float)e.NewValue);

        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suppressProgressChanged) return;
            _vm.Seek(e.NewValue);
        }

        private void ModeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModeSelector.SelectedItem is IVisualizationMode mode)
                VisualizerControl.CurrentMode = mode;
        }

        // ─── Очередь ────────────────────────────────────────────────

        private void PlaylistList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PlaylistList.SelectedIndex >= 0)
                _vm.PlayAt(PlaylistList.SelectedIndex);
        }

        private void RemoveTrack_Click(object sender, RoutedEventArgs e) =>
    DeleteTrackAt(PlaylistList.SelectedIndex);

        private void PlaylistList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                DeleteTrackAt(PlaylistList.SelectedIndex);
                e.Handled = true;
            }
        }

        private void DeleteTrackAt(int index)
        {
            if (index < 0 || index >= _vm.Playlist.Tracks.Count) return;

            bool wasCurrent = index == _vm.Playlist.CurrentIndex;
            bool wasPlaying = _vm.Engine.IsPlaying;

            _vm.Playlist.RemoveAt(index);

            if (_vm.Playlist.Tracks.Count == 0)
            {
                ClearPlaylist_Click(this, new RoutedEventArgs());
                return;
            }

            if (wasCurrent)
            {
                // Перезагружаем новый текущий с 0 — плеер больше не держит удалённый стрим
                int newIdx = Math.Clamp(_vm.Playlist.CurrentIndex, 0, _vm.Playlist.Tracks.Count - 1);
                _vm.PlayAt(newIdx, autoPlay: wasPlaying);
                PlaylistList.SelectedIndex = newIdx;
            }
        }

        private void ClearPlaylist_Click(object sender, RoutedEventArgs e)
        {
            _vm.ClearPlaylist();

            _suppressProgressChanged = true;
            ProgressSlider.Value = 0;
            _suppressProgressChanged = false;
            TimeLabel.Text = "00:00 / 00:00";

            VisualizerControl.ClearData();
        }

        private void SavePlaylist_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Music Visualizer Playlist|*.mvpl|JSON|*.json",
                FileName = "playlist.mvpl"
            };
            if (dialog.ShowDialog() != true) return;
            try
            {
                _stateService.SavePlaylist(dialog.FileName,
                    _vm.Playlist.Tracks.Select(t => t.FilePath));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось сохранить плейлист: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPlaylist_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Music Visualizer Playlist|*.mvpl|JSON|*.json",
                Title = "Загрузить плейлист"
            };
            if (dialog.ShowDialog() != true) return;
            try
            {
                var paths = _stateService.LoadPlaylist(dialog.FileName).Where(File.Exists);
                _vm.Stop();
                _vm.Playlist.Clear();
                _vm.Playlist.AddFiles(paths);
                if (_vm.Playlist.Tracks.Count > 0)
                    _vm.PlayAt(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить плейлист: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─── Drag-and-drop: из проводника на окно ───────────────────

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_DragOver(object sender, DragEventArgs e) => Window_DragEnter(sender, e);

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = ((string[])e.Data.GetData(DataFormats.FileDrop))
                .Where(IsAudioFile)
                .ToArray();
            if (files.Length > 0) AddFilesToPlaylist(files);
        }

        private static bool IsAudioFile(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext is ".mp3" or ".wav" or ".flac" or ".ogg" or ".m4a" or ".aac" or ".aiff" or ".aif";
        }

        // ─── Drag-and-drop: перестановка внутри ListBox ─────────────

        private void PlaylistList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
            _dragSourceIndex = GetIndexUnderMouse(e.GetPosition(PlaylistList));
        }

        private void PlaylistList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _dragSourceIndex < 0) return;
            var diff = _dragStartPoint - e.GetPosition(null);
            if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance) return;

            var item = _vm.Playlist.Tracks[_dragSourceIndex];
            DragDrop.DoDragDrop(PlaylistList, new DataObject(typeof(TrackItem), item),
                DragDropEffects.Move);
        }

        private void PlaylistList_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(TrackItem)))
                e.Effects = DragDropEffects.Move;
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void PlaylistList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(TrackItem)) && _dragSourceIndex >= 0)
            {
                int targetIndex = GetIndexUnderMouse(e.GetPosition(PlaylistList));
                if (targetIndex >= 0 && targetIndex != _dragSourceIndex)
                    _vm.Playlist.Move(_dragSourceIndex, targetIndex);
            }
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = ((string[])e.Data.GetData(DataFormats.FileDrop))
                    .Where(IsAudioFile).ToArray();
                if (files.Length > 0) AddFilesToPlaylist(files);
            }

            _dragSourceIndex = -1;
            e.Handled = true;
        }

        private int GetIndexUnderMouse(Point pos)
        {
            var element = PlaylistList.InputHitTest(pos) as DependencyObject;
            while (element != null && element is not ListBoxItem)
                element = VisualTreeHelper.GetParent(element);
            return element is ListBoxItem item
                ? PlaylistList.ItemContainerGenerator.IndexFromContainer(item)
                : -1;
        }
    }
}