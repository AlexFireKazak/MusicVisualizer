using System;
using System.Collections.Generic;

namespace MusicVisualizer.ViewModels
{
    /// <summary>
    /// Чистая логика восстановления позиции: какой трек выбрать и с какого места.
    /// Вынесена из MainWindow для тестирования.
    /// </summary>
    public static class PlaylistRestorer
    {
        public static (int targetIndex, TimeSpan startPosition) Compute(
            IReadOnlyList<TrackItem> tracks, AppState state)
        {
            if (tracks.Count == 0) return (-1, TimeSpan.Zero);

            int targetIndex = -1;
            bool sameTrackFound = false;

            if (!string.IsNullOrEmpty(state.CurrentTrackPath))
            {
                for (int i = 0; i < tracks.Count; i++)
                {
                    if (string.Equals(tracks[i].FilePath, state.CurrentTrackPath,
                                      StringComparison.OrdinalIgnoreCase))
                    {
                        targetIndex = i;
                        sameTrackFound = true;
                        break;
                    }
                }
            }

            if (targetIndex < 0)
                targetIndex = Math.Clamp(state.CurrentIndex, 0, tracks.Count - 1);

            var startPos = sameTrackFound
                ? TimeSpan.FromSeconds(Math.Max(0, state.PositionSeconds))
                : TimeSpan.Zero;

            return (targetIndex, startPos);
        }
    }
}