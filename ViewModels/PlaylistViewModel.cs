using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace MusicVisualizer.ViewModels
{
    public class PlaylistViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<TrackItem> Tracks { get; } = new();

        private int _currentIndex = -1;
        public int CurrentIndex
        {
            get => _currentIndex;
            set
            {
                if (_currentIndex == value) return;
                int old = _currentIndex;
                _currentIndex = value;

                if (old >= 0 && old < Tracks.Count) Tracks[old].IsCurrent = false;
                if (value >= 0 && value < Tracks.Count) Tracks[value].IsCurrent = true;

                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void AddFiles(IEnumerable<string> paths)
        {
            foreach (var p in paths)
            {
                if (!File.Exists(p)) continue;
                Tracks.Add(new TrackItem { FilePath = p, Name = Path.GetFileName(p) });
            }
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Tracks.Count) return;

            Tracks[index].IsCurrent = false;
            bool wasCurrent = index == CurrentIndex;
            Tracks.RemoveAt(index);

            if (Tracks.Count == 0)
            {
                _currentIndex = -1;
                OnPropertyChanged(nameof(CurrentIndex));
                return;
            }

            if (wasCurrent)
            {
                int newIdx = System.Math.Min(index, Tracks.Count - 1);
                _currentIndex = newIdx;
                Tracks[newIdx].IsCurrent = true;
                OnPropertyChanged(nameof(CurrentIndex));
            }
            else if (index < CurrentIndex)
            {
                _currentIndex--;
                OnPropertyChanged(nameof(CurrentIndex));
            }
        }

        public void Clear()
        {
            foreach (var t in Tracks) t.IsCurrent = false;
            Tracks.Clear();
            _currentIndex = -1;
            OnPropertyChanged(nameof(CurrentIndex));
        }

        public void Move(int from, int to)
        {
            if (from < 0 || from >= Tracks.Count) return;
            if (to < 0 || to >= Tracks.Count) return;
            if (from == to) return;

            int newCurrent = CurrentIndex;
            if (CurrentIndex == from) newCurrent = to;
            else if (from < CurrentIndex && to >= CurrentIndex) newCurrent--;
            else if (from > CurrentIndex && to <= CurrentIndex) newCurrent++;

            Tracks.Move(from, to);

            if (newCurrent != CurrentIndex)
            {
                _currentIndex = newCurrent;
                OnPropertyChanged(nameof(CurrentIndex));
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}