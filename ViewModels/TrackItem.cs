using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MusicVisualizer.ViewModels
{
    public class TrackItem : INotifyPropertyChanged
    {
        private bool _isCurrent;
        private TimeSpan? _duration;

        public string FilePath { get; set; } = "";
        public string Name { get; set; } = "";

        public bool IsCurrent
        {
            get => _isCurrent;
            set
            {
                if (_isCurrent == value) return;
                _isCurrent = value;
                OnPropertyChanged();
            }
        }

        /// <summary>Длительность, известна только после первого воспроизведения трека.</summary>
        public TimeSpan? Duration
        {
            get => _duration;
            set
            {
                _duration = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DurationText));
            }
        }

        public string DurationText => _duration.HasValue
            ? (_duration.Value.TotalHours >= 1
                ? $"{(int)_duration.Value.TotalHours}:{_duration.Value.Minutes:D2}:{_duration.Value.Seconds:D2}"
                : $"{_duration.Value.Minutes:D2}:{_duration.Value.Seconds:D2}")
            : "";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public override string ToString() => Name;
    }
}