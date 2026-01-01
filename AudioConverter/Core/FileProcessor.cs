using OpusConverter.Config;
using System.Text.RegularExpressions;

namespace OpusConverter.Core
{
    public class FileProcessor
    {
        private readonly AppConfig _config;
        private int _totalFiles;
        private int _processedFiles;
        private int _successfulFiles;
        private int _failedFiles;
        private DateTime _startTime;

        public FileProcessor(AppConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void InitializeProgress(int totalFiles)
        {
            _totalFiles = totalFiles;
            _processedFiles = 0;
            _successfulFiles = 0;
            _failedFiles = 0;
            _startTime = DateTime.Now;
        }

        public List<string> FindAudioFiles()
        {
            if (!Directory.Exists(_config.InputDirectory))
            {
                Directory.CreateDirectory(_config.InputDirectory);
                return new List<string>();
            }

            var files = new List<string>();
            var searchPatterns = _config.SupportedExtensions
                .Select(ext => $"*{ext}")
                .Concat(_config.SupportedExtensions.Select(ext => $"*{ext.ToUpper()}"))
                .Distinct();

            foreach (var pattern in searchPatterns)
            {
                try
                {
                    files.AddRange(Directory.GetFiles(_config.InputDirectory, pattern, SearchOption.AllDirectories));
                }
                catch { }
            }

            return files.Distinct().OrderBy(f => f).ToList();
        }

        public string GenerateOutputFileName(string inputFile, TagLib.Tag tag)
        {
            if (string.IsNullOrWhiteSpace(_config.OutputFilenamePattern))
                return Path.GetFileNameWithoutExtension(inputFile);

            string pattern = _config.OutputFilenamePattern;

            // Определяем часть паттерна для имени файла (после последнего слеша)
            int lastSlash = Math.Max(
                pattern.LastIndexOf('/'),
                pattern.LastIndexOf('\\')
            );

            string filePattern;
            if (lastSlash >= 0)
            {
                // Берем только часть для имени файла (после последнего слеша)
                filePattern = pattern.Substring(lastSlash + 1);
            }
            else
            {
                // Если нет слешей - весь паттерн для имени файла
                filePattern = pattern;
            }

            // Обрабатываем ТОЛЬКО имя файла
            return ProcessFileNamePattern(inputFile, tag, filePattern) + ".opus";
        }

        public string GenerateOutputFilePath(string inputFile, TagLib.Tag tag)
        {
            if (string.IsNullOrWhiteSpace(_config.OutputFilenamePattern))
                return Path.Combine(_config.OutputDirectory, Path.GetFileNameWithoutExtension(inputFile) + ".opus");

            string pattern = _config.OutputFilenamePattern;

            // Определяем часть паттерна для пути (до последнего слеша)
            int lastSlash = Math.Max(
                pattern.LastIndexOf('/'),
                pattern.LastIndexOf('\\')
            );

            string folderPattern;
            if (lastSlash >= 0)
            {
                // Берем часть для пути (до последнего слеша)
                folderPattern = pattern.Substring(0, lastSlash);
            }
            else
            {
                // Если нет слешей - только корневая папка
                return Path.Combine(_config.OutputDirectory, GenerateOutputFileName(inputFile, tag));
            }

            // Обрабатываем путь
            string processedPath = ProcessPathPattern(inputFile, tag, folderPattern);

            // Создаем папки
            string fullPath = Path.Combine(_config.OutputDirectory, processedPath);
            Directory.CreateDirectory(fullPath);

            // Возвращаем полный путь с именем файла
            return Path.Combine(fullPath, GenerateOutputFileName(inputFile, tag));
        }

        private string ProcessFileNamePattern(string inputFile, TagLib.Tag tag, string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return Path.GetFileNameWithoutExtension(inputFile);

            // Обрабатываем теги для имени файла
            pattern = ReplaceTagsInPattern(inputFile, tag, pattern);

            // Очищаем от недопустимых символов в именах файлов
            pattern = CleanFileName(pattern);

            return pattern.Trim();
        }

        private string ProcessPathPattern(string inputFile, TagLib.Tag tag, string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return "";

            // Обрабатываем теги для пути
            pattern = ReplaceTagsInPattern(inputFile, tag, pattern);

            // Заменяем все слеши на правильные разделители
            pattern = pattern.Replace('/', Path.DirectorySeparatorChar)
                            .Replace('\\', Path.DirectorySeparatorChar);

            // Очищаем от недопустимых символов для путей
            pattern = CleanPath(pattern);

            return pattern.Trim();
        }

        private string ReplaceTagsInPattern(string inputFile, TagLib.Tag tag, string pattern)
        {
            // Основные теги
            pattern = pattern.Replace("{artist}",
                tag.AlbumArtists?.FirstOrDefault() ??
                tag.Performers?.FirstOrDefault() ??
                tag.Composers?.FirstOrDefault() ??
                "Unknown Artist");

            pattern = pattern.Replace("{title}",
                !string.IsNullOrWhiteSpace(tag.Title) ?
                tag.Title :
                Path.GetFileNameWithoutExtension(inputFile));

            pattern = pattern.Replace("{album}",
                !string.IsNullOrWhiteSpace(tag.Album) ?
                tag.Album :
                "Unknown Album");

            pattern = pattern.Replace("{year}",
                tag.Year > 0 ?
                tag.Year.ToString() :
                "");

            pattern = pattern.Replace("{track}",
                tag.Track > 0 ?
                tag.Track.ToString("D2") :
                "");

            pattern = pattern.Replace("{trackcount}",
                tag.TrackCount > 0 ?
                tag.TrackCount.ToString("D2") :
                "");

            pattern = pattern.Replace("{disc}",
                tag.Disc > 0 ?
                tag.Disc.ToString("D2") :
                "");

            pattern = pattern.Replace("{disccount}",
                tag.DiscCount > 0 ?
                tag.DiscCount.ToString("D2") :
                "");

            // Жанр
            pattern = pattern.Replace("{genre}",
                tag.Genres?.FirstOrDefault() ??
                "");

            // Исполнитель
            pattern = pattern.Replace("{performer}",
                tag.Performers?.FirstOrDefault() ??
                "");

            // Композитор
            pattern = pattern.Replace("{composer}",
                tag.Composers?.FirstOrDefault() ??
                "");

            // Сохраняем структуру каталогов
            if (pattern.Contains("{directory}"))
            {
                string relativePath = Path.GetRelativePath(_config.InputDirectory, Path.GetDirectoryName(inputFile));
                pattern = pattern.Replace("{directory}", relativePath.Replace(Path.DirectorySeparatorChar, '-'));
            }

            // Оригинальное имя файла
            pattern = pattern.Replace("{filename}",
                Path.GetFileNameWithoutExtension(inputFile));

            // Оригинальное расширение
            pattern = pattern.Replace("{extension}",
                Path.GetExtension(inputFile).TrimStart('.'));

            // Убираем пустые скобки и лишние разделители
            pattern = Regex.Replace(pattern, @"\{\w+\}", "");
            pattern = Regex.Replace(pattern, @"\s*-\s*-", "-");
            pattern = Regex.Replace(pattern, @"^-+|-+$", "");
            pattern = pattern.Trim(' ', '-', '_', '.');

            return pattern;
        }

        private string CleanFileName(string fileName)
        {
            string invalidChars = new string(Path.GetInvalidFileNameChars());
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c.ToString(), "_");
            }

            // Убираем множественные подчеркивания
            fileName = Regex.Replace(fileName, @"_{2,}", "_");

            return fileName;
        }

        private string CleanPath(string path)
        {
            string invalidChars = new string(Path.GetInvalidPathChars());
            foreach (char c in invalidChars)
            {
                path = path.Replace(c.ToString(), "_");
            }

            // Убираем множетельные подчеркивания
            path = Regex.Replace(path, @"_{2,}", "_");

            return path;
        }

        public void IncrementProcessed()
        {
            Interlocked.Increment(ref _processedFiles);
        }

        public void IncrementSuccessful()
        {
            Interlocked.Increment(ref _successfulFiles);
        }

        public void IncrementFailed()
        {
            Interlocked.Increment(ref _failedFiles);
        }

        public string GetProgressText()
        {
            if (_totalFiles == 0) return "Нет файлов для обработки";

            double percentage = (_processedFiles / (double)_totalFiles) * 100;
            TimeSpan elapsed = DateTime.Now - _startTime;

            if (_processedFiles > 0)
            {
                TimeSpan estimatedTotal = TimeSpan.FromSeconds(elapsed.TotalSeconds / (_processedFiles / (double)_totalFiles));
                TimeSpan remaining = estimatedTotal - elapsed;
                return $"Прогресс: [{GetProgressBar(percentage)}] {percentage:F1}% " +
                    $"({_processedFiles}/{_totalFiles}) " +
                    $"Прошло: {FormatTimeSpan(elapsed)} " +
                    $"Осталось: {FormatTimeSpan(remaining)}";
            }
            else
            {
                return $"Прогресс: [{GetProgressBar(percentage)}] {percentage:F1}% " +
                    $"({_processedFiles}/{_totalFiles}) " +
                    $"Прошло: {FormatTimeSpan(elapsed)}";
            }
        }

        public string GetSummary()
        {
            TimeSpan totalTime = DateTime.Now - _startTime;
            return $"Успешно: {_successfulFiles} файлов\n" +
                $"Пропущено: {_totalFiles - _processedFiles} файлов\n" +
                $"Ошибок: {_failedFiles} файлов\n" +
                $"Общее время: {FormatTimeSpan(totalTime)}\n" +
                $"Среднее время на файл: {FormatTimeSpan(TimeSpan.FromSeconds(totalTime.TotalSeconds / Math.Max(1, _processedFiles)))}";
        }

        private string GetProgressBar(double percentage)
        {
            int width = 20;
            int filled = (int)(percentage / 100 * width);
            int empty = width - filled;
            if (empty < 0) return string.Empty;
            return new string('█', filled) + new string('░', empty);
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
            else if (timeSpan.TotalMinutes >= 1)
                return $"{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
            else
                return $"{timeSpan.Seconds:00} сек";
        }

        public int TotalFiles => _totalFiles;
        public int ProcessedFiles => _processedFiles;
        public int SuccessfulFiles => _successfulFiles;
        public int FailedFiles => _failedFiles;
    }
}