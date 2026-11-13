using NAudio.Wave;
using System;
using System.IO;

namespace MusicVisualizer.Core
{
    /// <summary>
    /// Быстрое чтение длительности из заголовка файла без загрузки полного ридера
    /// в AudioEngine. Тяжёлое сканирование Mp3FileReader для VBR-файлов без Xing
    /// выполняется вызывающей стороной в фоне.
    /// </summary>
    public static class TrackMetadataService
    {
        public static TimeSpan? TryGetDuration(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            try
            {
                string ext = Path.GetExtension(filePath).ToLowerInvariant();
                using WaveStream reader = ext switch
                {
                    ".wav" => new WaveFileReader(filePath),
                    ".mp3" => new Mp3FileReader(filePath),
                    ".aiff" or ".aif" => new AiffFileReader(filePath),
                    _ => new MediaFoundationReader(filePath)
                };
                return reader.TotalTime;
            }
            catch
            {
                return null;
            }
        }
    }
}