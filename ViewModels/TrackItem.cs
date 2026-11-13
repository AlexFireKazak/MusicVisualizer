namespace MusicVisualizer.ViewModels
{
    public class TrackItem
    {
        public string FilePath { get; set; } = "";
        public string Name { get; set; } = "";
        public override string ToString() => Name;
    }
}