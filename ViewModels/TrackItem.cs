using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MusicVisualizer.ViewModels
{
    public class TrackItem : INotifyPropertyChanged
    {
        private bool _isCurrent;

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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public override string ToString() => Name;
    }
}