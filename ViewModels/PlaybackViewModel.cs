using MusicVisualizer.Core;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace MusicVisualizer.ViewModels
{
    public class PlaybackViewModel : INotifyPropertyChanged
    {
        private readonly AudioEngine _engine = new();
        private readonly AudioAnalyzer _analyzer = new();

        private string _title = "Music Visualizer";
        private string _timeText = "00:00 / 00:00";
        private double _progress;

        public PlaylistViewModel Playlist { get; } = new();
        public AudioEngine Engine => _engine;

        /// <summary>Срабатывает при загрузке нового трека — сброс визуализаций.</summary>
        public event EventHandler? TrackChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public string Title { get => _title; private set => Set(ref _title, value); }
        public string TimeText { get => _timeText; private set => Set(ref _timeText, value); }
        public double Progress { get => _progress; private set => Set(ref _progress, value); }

        public PlaybackViewModel()
        {
            _engine.TrackEnded += OnEngineTrackEnded;
        }

        private void OnEngineTrackEnded(object? sender, EventArgs e)
        {
            // Приходит с аудио-потока — пробрасываем на UI
            Application.Current.Dispatcher.Invoke(PlayNext);
        }

        public void PlayNext()
        {
            int next = Playlist.CurrentIndex + 1;
            if (next >= Playlist.Tracks.Count)
            {
                _engine.Stop();
                return;
            }
            PlayAt(next);
        }

        public void PlayAt(int index, bool autoPlay = true, TimeSpan? startPosition = null)
        {
            if (index < 0 || index >= Playlist.Tracks.Count) return;
            Playlist.CurrentIndex = index;
            var track = Playlist.Tracks[index];
            try
            {
                _analyzer.Reset();
                _engine.LoadFile(track.FilePath);

                if (startPosition.HasValue && startPosition.Value > TimeSpan.Zero)
                    _engine.Seek(startPosition.Value);

                if (autoPlay) _engine.Play();
                Title = $"Music Visualizer - {track.Name}";
                TrackChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки файла: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Play() => _engine.Play();
        public void Pause() => _engine.Pause();
        public void Stop() => _engine.Stop();
        public void SetVolume(float v) => _engine.SetVolume(v);

        public void Seek(double ratio)
        {
            if (_engine.TotalTime.TotalSeconds <= 0) return;
            _engine.Seek(TimeSpan.FromSeconds(ratio * _engine.TotalTime.TotalSeconds));
            UpdateFromRendering();
        }

        public void UpdateFromRendering()
        {
            var total = _engine.TotalTime.TotalSeconds;
            if (total <= 0) return;
            Progress = _engine.Position.TotalSeconds / total;

            bool withHours = _engine.TotalTime.TotalHours >= 1;
            TimeText = $"{FormatTime(_engine.Position, withHours)} / {FormatTime(_engine.TotalTime, withHours)}";
        }

        public float[] ComputeSpectrum(float[] rawFft) => _analyzer.ComputeSpectrum(rawFft);

        private static string FormatTime(TimeSpan t, bool withHours) =>
            withHours
                ? $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}"
                : $"{t.Minutes:D2}:{t.Seconds:D2}";

        private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void ClearPlaylist()
        {
            _engine.Unload();
            _analyzer.Reset();
            Playlist.Clear();
            Progress = 0;
            TimeText = "00:00 / 00:00";
            Title = "Music Visualizer";
            TrackChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}