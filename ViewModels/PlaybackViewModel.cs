using MusicVisualizer.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace MusicVisualizer.ViewModels
{
    public enum LoopMode { Off, All, One }

    public class PlaybackViewModel : INotifyPropertyChanged
    {
        private readonly AudioEngine _engine = new();
        private readonly AudioAnalyzer _analyzer = new();
        private readonly Random _rand = new();
        private readonly HashSet<int> _shufflePlayed = new();

        private string _title = "Music Visualizer";
        private string _timeText = "00:00 / 00:00";
        private double _progress;
        private LoopMode _loopMode = LoopMode.Off;
        private bool _shuffle;
        private bool _isLoading;

        public PlaylistViewModel Playlist { get; } = new();
        public AudioEngine Engine => _engine;

        public event EventHandler? TrackChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public string Title { get => _title; private set => Set(ref _title, value); }
        public string TimeText { get => _timeText; private set => Set(ref _timeText, value); }
        public double Progress { get => _progress; private set => Set(ref _progress, value); }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (_isLoading == value) return;
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
                OnPropertyChanged(nameof(CanPlay));
            }
        }

        /// <summary>Инверсия IsLoading для привязки к IsEnabled кнопок Play/Pause/Stop.</summary>
        public bool CanPlay => !_isLoading;

        public LoopMode LoopMode
        {
            get => _loopMode;
            set { if (_loopMode != value) { _loopMode = value; OnPropertyChanged(); } }
        }

        public bool Shuffle
        {
            get => _shuffle;
            set
            {
                if (_shuffle == value) return;
                _shuffle = value;
                _shufflePlayed.Clear();
                OnPropertyChanged();
            }
        }

        public PlaybackViewModel()
        {
            _engine.TrackEnded += OnEngineTrackEnded;
            _engine.PlaybackError += OnEnginePlaybackError;
            Playlist.Tracks.CollectionChanged += (_, _) => _shufflePlayed.Clear();
        }

        private void OnEngineTrackEnded(object? sender, EventArgs e)
        {
            Application.Current.Dispatcher.InvokeAsync(async () => await PlayNext());
        }

        private void OnEnginePlaybackError(object? sender, PlaybackErrorEventArgs args)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show($"Ошибка воспроизведения: {args.Exception.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _engine.Stop();
            });
        }

        public async Task PlayNext()
        {
            if (Playlist.Tracks.Count == 0) return;

            if (LoopMode == LoopMode.One)
            {
                await PlayAtInternal(Playlist.CurrentIndex, autoPlay: true,
                               startPosition: null, resetShuffleCycle: false);
                return;
            }

            int count = Playlist.Tracks.Count;

            if (Shuffle)
            {
                await PlayShuffleNextAsync(count);
                return;
            }

            int next = Playlist.CurrentIndex + 1;
            if (next >= count)
            {
                if (LoopMode == LoopMode.All) next = 0;
                else { _engine.Stop(); return; }
            }
            await PlayAtInternal(next, autoPlay: true, startPosition: null, resetShuffleCycle: false);
        }

        private async Task PlayShuffleNextAsync(int count)
        {
            var candidates = Enumerable.Range(0, count)
                .Where(i => !_shufflePlayed.Contains(i))
                .ToList();

            if (candidates.Count == 0)
            {
                if (LoopMode == LoopMode.All)
                {
                    _shufflePlayed.Clear();
                    if (count > 1) _shufflePlayed.Add(Playlist.CurrentIndex);
                    candidates = Enumerable.Range(0, count)
                        .Where(i => !_shufflePlayed.Contains(i))
                        .ToList();
                    if (candidates.Count == 0) candidates.Add(Playlist.CurrentIndex);
                }
                else
                {
                    _engine.Stop();
                    return;
                }
            }

            int nextIdx = candidates[_rand.Next(candidates.Count)];
            _shufflePlayed.Add(nextIdx);
            await PlayAtInternal(nextIdx, autoPlay: true, startPosition: null, resetShuffleCycle: false);
        }

        public Task PlayAt(int index, bool autoPlay = true, TimeSpan? startPosition = null) =>
            PlayAtInternal(index, autoPlay, startPosition, resetShuffleCycle: true);


private async Task PlayAtInternal(int index, bool autoPlay, TimeSpan? startPosition, bool resetShuffleCycle)
    {
        // Пропускаем отсутствующие файлы, не вызывая LoadFileAsync
        var missing = new List<string>();
        while (index >= 0 && index < Playlist.Tracks.Count)
        {
            if (File.Exists(Playlist.Tracks[index].FilePath)) break;

            missing.Add(Playlist.Tracks[index].Name);
            Playlist.RemoveAt(index);
            if (index >= Playlist.Tracks.Count) index = Playlist.Tracks.Count - 1;
        }

        if (missing.Count > 0)
        {
            MessageBox.Show(
                "Следующие файлы не найдены и удалены из очереди:\n\n" +
                string.Join("\n", missing),
                "Файлы не найдены", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        if (index < 0 || index >= Playlist.Tracks.Count)
        {
            // Ничего не осталось — чистим состояние, но очередь уже пуста
            _engine.Unload();
            _analyzer.Reset();
            Progress = 0;
            TimeText = "00:00 / 00:00";
            Title = "Music Visualizer";
            TrackChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        Playlist.CurrentIndex = index;

        if (resetShuffleCycle && Shuffle)
        {
            _shufflePlayed.Clear();
            _shufflePlayed.Add(index);
        }

        var track = Playlist.Tracks[index];
        IsLoading = true;
        Title = "Загрузка…";
        try
        {
            _analyzer.Reset();
            await _engine.LoadFileAsync(track.FilePath);

            if (startPosition.HasValue && startPosition.Value > TimeSpan.Zero)
                _engine.Seek(startPosition.Value);

            if (autoPlay) _engine.Play();
            Title = $"Music Visualizer - {track.Name}";
            TrackChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Title = "Music Visualizer";
            MessageBox.Show($"Ошибка загрузки файла: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void ClearPlaylist()
        {
            _engine.Unload();
            _analyzer.Reset();
            Playlist.Clear();
            _shufflePlayed.Clear();
            Progress = 0;
            TimeText = "00:00 / 00:00";
            Title = "Music Visualizer";
            TrackChanged?.Invoke(this, EventArgs.Empty);
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

        public void SeekRelative(TimeSpan delta)
        {
            var target = _engine.Position + delta;
            if (target < TimeSpan.Zero) target = TimeSpan.Zero;
            if (target > _engine.TotalTime) target = _engine.TotalTime;
            _engine.Seek(target);
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
            OnPropertyChanged(name);
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}