using MusicVisualizer.Core;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MusicVisualizer.ViewModels
{
    public class PlaybackViewModel : INotifyPropertyChanged
    {
        private readonly AudioEngine _engine = new();
        private readonly AudioAnalyzer _analyzer = new();
        private string _title = "Music Visualizer";
        private string _timeText = "00:00 / 00:00";
        private double _progress;

        public event PropertyChangedEventHandler? PropertyChanged;

        public AudioEngine Engine => _engine;
        public AudioAnalyzer Analyzer => _analyzer;

        public string Title { get => _title; private set => Set(ref _title, value); }
        public string TimeText { get => _timeText; private set => Set(ref _timeText, value); }
        public double Progress { get => _progress; private set => Set(ref _progress, value); }

        public void LoadFile(string path)
        {
            _engine.LoadFile(path);
            _engine.Play(); // автозапуск
            Title = $"Music Visualizer - {System.IO.Path.GetFileName(path)}";
        }

        public void UpdateFromRendering()
        {
            var total = _engine.TotalTime.TotalSeconds;
            if (total <= 0) return;
            Progress = _engine.Position.TotalSeconds / total;

            bool withHours = _engine.TotalTime.TotalHours >= 1;
            TimeText = $"{FormatTime(_engine.Position, withHours)} / {FormatTime(_engine.TotalTime, withHours)}";
        }

        private static string FormatTime(TimeSpan t, bool withHours) =>
            withHours
                ? $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}"
                : $"{t.Minutes:D2}:{t.Seconds:D2}";
        public void Seek(double ratio)
        {
            _engine.Seek(TimeSpan.FromSeconds(ratio * _engine.TotalTime.TotalSeconds));
            UpdateFromRendering();
        }

        private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}