using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MusicVisualizer.Core
{
    /// <summary>
    /// Чтение и запись плейлистов в формате M3U (Extended M3U).
    /// Относительные пути сохраняются относительно файла плейлиста,
    /// при чтении разрешаются в абсолютные.
    /// </summary>
    public static class M3UService
    {
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        public static void Save(string m3uPath, IEnumerable<string> trackPaths)
        {
            string baseDir = Path.GetDirectoryName(Path.GetFullPath(m3uPath)) ?? "";

            var sb = new StringBuilder();
            sb.AppendLine("#EXTM3U");

            foreach (var path in trackPaths)
            {
                string title = Path.GetFileNameWithoutExtension(path);
                // Длительность в M3U — секунды. Не знаем точное значение — пишем -1,
                // плееры трактуют это как «неизвестно» и считают сами.
                sb.AppendLine($"#EXTINF:-1,{title}");
                sb.AppendLine(ToRelativeIfPossible(path, baseDir));
            }

            File.WriteAllText(m3uPath, sb.ToString(), Utf8NoBom);
        }

        public static List<string> Load(string m3uPath)
        {
            var result = new List<string>();
            if (!File.Exists(m3uPath)) return result;

            string baseDir = Path.GetDirectoryName(Path.GetFullPath(m3uPath)) ?? "";
            string[] lines = File.ReadAllLines(m3uPath, Utf8NoBom);

            foreach (var raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith("#")) continue; // комментарии и #EXTINF

                string resolved = Path.IsPathRooted(line)
                    ? line
                    : Path.GetFullPath(Path.Combine(baseDir, line));

                if (File.Exists(resolved))
                    result.Add(resolved);
            }

            return result;
        }

        private static string ToRelativeIfPossible(string absolutePath, string baseDir)
        {
            try
            {
                if (string.IsNullOrEmpty(baseDir)) return absolutePath;

                string full = Path.GetFullPath(absolutePath);
                string baseFull = Path.GetFullPath(baseDir);

                // Только если общий корень — иначе смысла в относительном пути нет
                string baseWithSep = baseFull.EndsWith(Path.DirectorySeparatorChar.ToString())
                    ? baseFull
                    : baseFull + Path.DirectorySeparatorChar;

                if (!full.StartsWith(baseWithSep, StringComparison.OrdinalIgnoreCase))
                    return absolutePath;

                return Path.GetRelativePath(baseFull, full);
            }
            catch
            {
                return absolutePath;
            }
        }
    }
}