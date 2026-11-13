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
            set { if (_currentIndex != value) { _currentIndex = value; OnPropertyChanged(); } }
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
            Tracks.RemoveAt(index);
            if (CurrentIndex >= Tracks.Count) CurrentIndex = Tracks.Count - 1;
        }

        public void Clear()
        {
            Tracks.Clear();
            CurrentIndex = -1;
        }

        public void Move(int from, int to)
        {
            if (from < 0 || from >= Tracks.Count) return;
            if (to < 0 || to >= Tracks.Count) return;
            if (from == to) return;

            Tracks.Move(from, to);

            // Пересчёт CurrentIndex при перестановке
            if (CurrentIndex == from) CurrentIndex = to;
            else if (from < CurrentIndex && to >= CurrentIndex) CurrentIndex--;
            else if (from > CurrentIndex && to <= CurrentIndex) CurrentIndex++;
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}