using System.Text.RegularExpressions;
using OpusConverter.Config;

namespace OpusConverter.Core
{
    public class CueSheetProcessor
    {
        private readonly AppConfig _config;

        public CueSheetProcessor(AppConfig config)
        {
            _config = config;
        }

        public class CueTrack
        {
            public string Title { get; set; }
            public string Artist { get; set; }
            public string Performer { get; set; }
            public int TrackNumber { get; set; }
            public TimeSpan StartTime { get; set; } = TimeSpan.Zero;
            public TimeSpan EndTime { get; set; } // Не инициализируем - будет TimeSpan.Zero для последнего трека
            public string SourceFile { get; set; }
            public string FileFormat { get; set; }
        }

        public class CueSheet
        {
            public string Title { get; set; }
            public string Artist { get; set; }
            public string Performer { get; set; }
            public List<CueTrack> Tracks { get; set; } = new();
            public string SourceCueFile { get; set; }
            public string AudioFile { get; set; }
        }

        public List<CueSheet> FindAndParseCueSheets(string inputDirectory)
        {
            var cueSheets = new List<CueSheet>();

            if (!Directory.Exists(inputDirectory))
                return cueSheets;

            // Ищем CUE файлы
            var cueFiles = Directory.GetFiles(inputDirectory, "*.cue", SearchOption.AllDirectories)
                                  .Concat(Directory.GetFiles(inputDirectory, "*.CUE", SearchOption.AllDirectories))
                                  .Distinct()
                                  .ToList();

            foreach (var cueFile in cueFiles)
            {
                try
                {
                    var cueSheet = ParseCueFile(cueFile);
                    if (cueSheet != null && cueSheet.Tracks.Any())
                    {
                        cueSheets.Add(cueSheet);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    Ошибка чтения CUE файла {Path.GetFileName(cueFile)}: {ex.Message}");
                }
            }

            return cueSheets;
        }

        public CueSheet ParseCueFile(string cueFilePath)
        {
            if (!File.Exists(cueFilePath))
                return null;

            var cueSheet = new CueSheet
            {
                SourceCueFile = cueFilePath
            };

            string[] lines = File.ReadAllLines(cueFilePath);
            CueTrack currentTrack = null;
            string currentFile = null;
            string currentFormat = null;
            TimeSpan currentIndexTime = TimeSpan.Zero; // Только для INDEX 01

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine))
                    continue;

                // Обработка строки FILE
                var fileMatch = Regex.Match(trimmedLine, @"^FILE\s+""([^""]+)""\s+(\w+)$", RegexOptions.IgnoreCase);
                if (fileMatch.Success)
                {
                    currentFile = fileMatch.Groups[1].Value;
                    currentFormat = fileMatch.Groups[2].Value.ToUpper();

                    // Полный путь к аудиофайлу
                    var audioFilePath = Path.Combine(Path.GetDirectoryName(cueFilePath), currentFile);
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
                    // Сохраняем предыдущий трек с временем начала
                    if (currentTrack != null)
                    {
                        currentTrack.StartTime = currentIndexTime;
                        cueSheet.Tracks.Add(currentTrack);
                        currentIndexTime = TimeSpan.Zero; // Сбрасываем для следующего трека
                    }

                    currentTrack = new CueTrack
                    {
                        TrackNumber = int.Parse(trackMatch.Groups[1].Value),
                        SourceFile = currentFile,
                        FileFormat = currentFormat
                    };
                    continue;
                }

                // Обработка TITLE (глобальный или трековый)
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

                // Обработка PERFORMER (глобальный или трековый)
                var performerMatch = Regex.Match(trimmedLine, @"^PERFORMER\s+""([^""]+)""$", RegexOptions.IgnoreCase);
                if (performerMatch.Success)
                {
                    if (currentTrack != null)
                    {
                        currentTrack.Performer = performerMatch.Groups[1].Value;
                    }
                    else
                    {
                        cueSheet.Performer = performerMatch.Groups[1].Value;
                        cueSheet.Artist = performerMatch.Groups[1].Value;
                    }
                    continue;
                }

                // Обработка только INDEX 01 (начало трека)
                var indexMatch = Regex.Match(trimmedLine, @"^INDEX\s+01\s+(\d+):(\d+):(\d+)$", RegexOptions.IgnoreCase);
                if (indexMatch.Success && currentTrack != null)
                {
                    int minutes = int.Parse(indexMatch.Groups[1].Value);
                    int seconds = int.Parse(indexMatch.Groups[2].Value);
                    int frames = int.Parse(indexMatch.Groups[3].Value);

                    // Преобразуем фреймы в секунды (75 фреймов = 1 секунда)
                    double totalSeconds = minutes * 60 + seconds + (frames / 75.0);
                    currentIndexTime = TimeSpan.FromSeconds(totalSeconds);
                }
                // Игнорируем INDEX 00 и другие INDEX
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

                // Для всех треков кроме последнего
                if (i < cueSheet.Tracks.Count - 1)
                {
                    current.EndTime = cueSheet.Tracks[i + 1].StartTime;
                }
                // Для последнего трека EndTime остается TimeSpan.Zero
                // что означает "до конца файла"

                // Заполняем недостающие метаданные из глобальных
                if (string.IsNullOrEmpty(current.Artist))
                    current.Artist = cueSheet.Artist;
                if (string.IsNullOrEmpty(current.Performer))
                    current.Performer = cueSheet.Performer;
                if (string.IsNullOrEmpty(current.Title))
                    current.Title = $"Track {current.TrackNumber:00}";
            }

            return cueSheet;
        }

        public List<CueConversionTask> CreateConversionTasks(CueSheet cueSheet)
        {
            var tasks = new List<CueConversionTask>();

            if (string.IsNullOrEmpty(cueSheet.AudioFile) || !File.Exists(cueSheet.AudioFile))
            {
                Console.WriteLine($"    Аудиофайл для CUE не найден: {cueSheet.AudioFile}");
                return tasks;
            }

            foreach (var track in cueSheet.Tracks)
            {
                tasks.Add(new CueConversionTask
                {
                    CueSheet = cueSheet,
                    Track = track,
                    SourceAudioFile = cueSheet.AudioFile,
                    OutputFileName = GenerateTrackOutputFileName(cueSheet, track)
                });
            }

            return tasks;
        }

        private string GenerateTrackOutputFileName(CueSheet cueSheet, CueTrack track)
        {
            string pattern = _config.OutputFilenamePattern;

            // Используем метаданные из трека
            pattern = pattern.Replace("{artist}",
                !string.IsNullOrEmpty(track.Artist) ? track.Artist :
                !string.IsNullOrEmpty(cueSheet.Artist) ? cueSheet.Artist :
                "Unknown Artist");

            pattern = pattern.Replace("{title}", track.Title);
            pattern = pattern.Replace("{album}",
                !string.IsNullOrEmpty(cueSheet.Title) ? cueSheet.Title :
                "Unknown Album");

            pattern = pattern.Replace("{year}", ""); // CUE обычно не содержит года
            pattern = pattern.Replace("{track}", track.TrackNumber.ToString("D2"));

            // Жанр и другие теги отсутствуют в CUE
            pattern = pattern.Replace("{genre}", "");
            pattern = pattern.Replace("{performer}",
                !string.IsNullOrEmpty(track.Performer) ? track.Performer :
                !string.IsNullOrEmpty(track.Artist) ? track.Artist :
                "");

            pattern = pattern.Replace("{composer}", "");
            pattern = pattern.Replace("{directory}",
                Path.GetRelativePath(_config.InputDirectory, Path.GetDirectoryName(cueSheet.SourceCueFile))
                     .Replace(Path.DirectorySeparatorChar, '-'));

            // Убираем пустые скобки и лишние разделители
            pattern = Regex.Replace(pattern, @"\{\w+\}", "");
            pattern = Regex.Replace(pattern, @"\s*-\s*-", "-");
            pattern = Regex.Replace(pattern, @"^-+|-+$", "");
            pattern = pattern.Trim(' ', '-', '_', '.');

            // Очищаем от недопустимых символов
            string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            foreach (char c in invalidChars)
            {
                pattern = pattern.Replace(c.ToString(), "_");
            }

            pattern = Regex.Replace(pattern, @"_{2,}", "_");
            pattern = pattern.Trim();

            if (string.IsNullOrWhiteSpace(pattern))
                pattern = $"Track_{track.TrackNumber:00}";

            return pattern + ".opus";
        }
    }

    public class CueConversionTask
    {
        public CueSheetProcessor.CueSheet CueSheet { get; set; }
        public CueSheetProcessor.CueTrack Track { get; set; }
        public string SourceAudioFile { get; set; }
        public string OutputFileName { get; set; }
    }
}