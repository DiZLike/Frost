using System.Text.RegularExpressions;
using CueSplitter.Models;

namespace CueSplitter.Services
{
    public class CueParser
    {
        public CueSheet ParseCueFile(string cueFilePath)
        {
            if (!File.Exists(cueFilePath))
            {
                throw new FileNotFoundException($"CUE файл не найден: {cueFilePath}");
            }

            var cueSheet = new CueSheet
            {
                SourceCueFile = cueFilePath
            };

            string[] lines = File.ReadAllLines(cueFilePath);
            CueTrack? currentTrack = null;
            string? currentFile = null;
            string? currentFormat = null;
            TimeSpan currentIndexTime = TimeSpan.Zero;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine))
                    continue;

                // Обработка REM команд
                ParseRemCommand(trimmedLine, cueSheet);

                // Обработка FILE
                var fileMatch = Regex.Match(trimmedLine, @"^FILE\s+""([^""]+)""\s+(\w+)$", RegexOptions.IgnoreCase);
                if (fileMatch.Success)
                {
                    currentFile = fileMatch.Groups[1].Value;
                    currentFormat = fileMatch.Groups[2].Value.ToUpper();

                    var audioFilePath = Path.Combine(Path.GetDirectoryName(cueFilePath)!, currentFile);
                    if (File.Exists(audioFilePath))
                    {
                        cueSheet.AudioFile = audioFilePath;
                    }
                    continue;
                }

                // Обработка TRACK
                var trackMatch = Regex.Match(trimmedLine, @"^TRACK\s+(\d+)\s+(\w+)$", RegexOptions.IgnoreCase);
                if (trackMatch.Success)
                {
                    if (currentTrack != null)
                    {
                        currentTrack.StartTime = currentIndexTime;
                        cueSheet.Tracks.Add(currentTrack);
                        currentIndexTime = TimeSpan.Zero;
                    }

                    currentTrack = new CueTrack
                    {
                        TrackNumber = int.Parse(trackMatch.Groups[1].Value),
                        FileFormat = currentFormat ?? "WAVE"
                    };
                    continue;
                }

                // Обработка TITLE
                var titleMatch = Regex.Match(trimmedLine, @"^TITLE\s+""([^""]+)""$", RegexOptions.IgnoreCase);
                if (titleMatch.Success)
                {
                    if (currentTrack != null)
                    {
                        currentTrack.Title = titleMatch.Groups[1].Value;
                    }
                    else
                    {
                        cueSheet.Title = titleMatch.Groups[1].Value;
                    }
                    continue;
                }

                // Обработка PERFORMER
                var performerMatch = Regex.Match(trimmedLine, @"^PERFORMER\s+""([^""]+)""$", RegexOptions.IgnoreCase);
                if (performerMatch.Success)
                {
                    if (currentTrack != null)
                    {
                        currentTrack.Performer = performerMatch.Groups[1].Value;
                        currentTrack.Artist = performerMatch.Groups[1].Value;
                    }
                    else
                    {
                        cueSheet.Performer = performerMatch.Groups[1].Value;
                        cueSheet.Artist = performerMatch.Groups[1].Value;
                    }
                    continue;
                }

                // Обработка INDEX 01
                var indexMatch = Regex.Match(trimmedLine, @"^INDEX\s+01\s+(\d+):(\d+):(\d+)$", RegexOptions.IgnoreCase);
                if (indexMatch.Success && currentTrack != null)
                {
                    int minutes = int.Parse(indexMatch.Groups[1].Value);
                    int seconds = int.Parse(indexMatch.Groups[2].Value);
                    int frames = int.Parse(indexMatch.Groups[3].Value);

                    double totalSeconds = minutes * 60 + seconds + (frames / 75.0);
                    currentIndexTime = TimeSpan.FromSeconds(totalSeconds);
                }
            }

            // Добавляем последний трек
            if (currentTrack != null)
            {
                currentTrack.StartTime = currentIndexTime;
                cueSheet.Tracks.Add(currentTrack);
            }

            // Устанавливаем время окончания для треков
            for (int i = 0; i < cueSheet.Tracks.Count; i++)
            {
                var current = cueSheet.Tracks[i];

                // Заполняем недостающие метаданные из глобальных
                if (string.IsNullOrEmpty(current.Artist))
                    current.Artist = cueSheet.Artist;
                if (string.IsNullOrEmpty(current.Performer))
                    current.Performer = cueSheet.Performer;
                if (string.IsNullOrEmpty(current.Title))
                    current.Title = $"Track {current.TrackNumber:00}";

                // Устанавливаем EndTime
                if (i < cueSheet.Tracks.Count - 1)
                {
                    current.EndTime = cueSheet.Tracks[i + 1].StartTime;
                }
            }

            return cueSheet;
        }

        private void ParseRemCommand(string line, CueSheet cueSheet)
        {
            if (line.StartsWith("REM ", StringComparison.OrdinalIgnoreCase))
            {
                var remLine = line.Substring(4).Trim();

                // GENRE
                var genreMatch = Regex.Match(remLine, @"^GENRE\s+""([^""]+)""$", RegexOptions.IgnoreCase);
                if (genreMatch.Success)
                {
                    cueSheet.Genre = genreMatch.Groups[1].Value;
                    return;
                }

                // DATE
                var dateMatch = Regex.Match(remLine, @"^DATE\s+(\d{4})$", RegexOptions.IgnoreCase);
                if (dateMatch.Success)
                {
                    if (int.TryParse(dateMatch.Groups[1].Value, out int year))
                    {
                        cueSheet.Year = year;
                    }
                    return;
                }

                // DISCID
                var discIdMatch = Regex.Match(remLine, @"^DISCID\s+([\w]+)$", RegexOptions.IgnoreCase);
                if (discIdMatch.Success)
                {
                    // Можно сохранить если нужно
                    return;
                }

                // COMMENT
                var commentMatch = Regex.Match(remLine, @"^COMMENT\s+""([^""]+)""$", RegexOptions.IgnoreCase);
                if (commentMatch.Success)
                {
                    // Можно сохранить если нужно
                    return;
                }
            }
        }
    }
}