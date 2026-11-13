using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MusicVisualizer.ViewModels
{
    public class AppState
    {
        public List<string> PlaylistPaths { get; set; } = new();
        public int CurrentIndex { get; set; } = -1;
        public string? VisualizationName { get; set; }
        public double Volume { get; set; } = 0.5;
        public double PositionSeconds { get; set; }
        public string? CurrentTrackPath { get; set; }
    }

    public class AppStateService
    {
        private static readonly string Dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MusicVisualizer");
        private static readonly string StatePath = Path.Combine(Dir, "state.json");
        private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

        public AppState Load()
        {
            try
            {
                if (!File.Exists(StatePath)) return new AppState();
                var json = File.ReadAllText(StatePath);
                return JsonSerializer.Deserialize<AppState>(json) ?? new AppState();
            }
            catch { return new AppState(); }
        }

        public void Save(AppState state)
        {
            try
            {
                Directory.CreateDirectory(Dir);
                File.WriteAllText(StatePath, JsonSerializer.Serialize(state, JsonOpts));
            }
            catch { /* best-effort */ }
        }

        public void SavePlaylist(string path, IEnumerable<string> trackPaths)
        {
            File.WriteAllText(path, JsonSerializer.Serialize(trackPaths.ToList(), JsonOpts));
        }

        public List<string> LoadPlaylist(string path)
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<string>>(json) ?? new();
        }
    }
}