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

            // Жанр (добавлено)
            pattern = pattern.Replace("{genre}",
                tag.Genres?.FirstOrDefault() ??
                "");

            // Получаем первый исполнителя из массива
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

            // Очищаем от недопустимых символов в именах файлов
            string invalidChars = new string(Path.GetInvalidFileNameChars());
            foreach (char c in invalidChars)
            {
                pattern = pattern.Replace(c.ToString(), "_");
            }

            // Убираем множественные подчеркивания
            pattern = Regex.Replace(pattern, @"_{2,}", "_");

            // Убираем пробелы в начале и конце
            pattern = pattern.Trim();

            // Если после всех замен получилась пустая строка, используем оригинальное имя
            if (string.IsNullOrWhiteSpace(pattern))
                pattern = Path.GetFileNameWithoutExtension(inputFile);

            return pattern + ".opus";
        }
        public string GenerateOutputFilePath(string inputFile, TagLib.Tag tag)
        {
            // Получаем имя файла из шаблона
            string fileName = GenerateOutputFileName(inputFile, tag);

            // Если в шаблоне есть разделители папок (например: "{genre}/{artist}/{album}")
            // их нужно обработать отдельно
            string pattern = _config.OutputFilenamePattern;

            // Извлекаем путь к папкам из шаблона ДО обработки тегов
            // Находим последний слеш или обратный слеш
            int lastSlash = Math.Max(
                pattern.LastIndexOf('/'),
                pattern.LastIndexOf('\\')
            );

            // Если нет разделителей папок, возвращаем простое имя файла
            if (lastSlash == -1)
            {
                return Path.Combine(_config.OutputDirectory, fileName);
            }

            // Извлекаем часть шаблона для пути (до последнего слеша)
            string folderPattern = pattern.Substring(0, lastSlash);

            // Обрабатываем эту часть как путь с подстановкой тегов
            folderPattern = folderPattern.Replace("{artist}",
                tag.AlbumArtists?.FirstOrDefault() ??
                tag.Performers?.FirstOrDefault() ??
                tag.Composers?.FirstOrDefault() ??
                "Unknown Artist");

            folderPattern = folderPattern.Replace("{title}",
                !string.IsNullOrWhiteSpace(tag.Title) ?
                tag.Title :
                Path.GetFileNameWithoutExtension(inputFile));

            folderPattern = folderPattern.Replace("{album}",
                !string.IsNullOrWhiteSpace(tag.Album) ?
                tag.Album :
                "Unknown Album");

            folderPattern = folderPattern.Replace("{year}",
                tag.Year > 0 ?
                tag.Year.ToString() :
                "");

            folderPattern = folderPattern.Replace("{track}",
                tag.Track > 0 ?
                tag.Track.ToString("D2") :
                "");

            folderPattern = folderPattern.Replace("{genre}",
                tag.Genres?.FirstOrDefault() ??
                "");

            folderPattern = folderPattern.Replace("{performer}",
                tag.Performers?.FirstOrDefault() ??
                "");

            folderPattern = folderPattern.Replace("{composer}",
                tag.Composers?.FirstOrDefault() ??
                "");

            // Очищаем путь от недопустимых символов
            string invalidPathChars = new string(Path.GetInvalidPathChars());
            foreach (char c in invalidPathChars)
            {
                folderPattern = folderPattern.Replace(c.ToString(), "_");
            }

            // Заменяем все слеши на правильные разделители
            folderPattern = folderPattern.Replace('/', Path.DirectorySeparatorChar)
                                         .Replace('\\', Path.DirectorySeparatorChar);

            // Собираем полный путь
            string fullPath = Path.Combine(_config.OutputDirectory, folderPattern);

            // Создаем папки (если их нет)
            Directory.CreateDirectory(fullPath);

            // Возвращаем полный путь к файлу
            return Path.Combine(fullPath, fileName);
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
            double percentage = (_processedFiles / (double)_totalFiles) * 100;
            TimeSpan elapsed = DateTime.Now - _startTime;
            TimeSpan estimatedTotal = TimeSpan.FromSeconds(elapsed.TotalSeconds / (_processedFiles / (double)_totalFiles));
            TimeSpan remaining = estimatedTotal - elapsed;

            return $"Прогресс: [{GetProgressBar(percentage)}] {percentage:F1}% " +
                   $"({_processedFiles}/{_totalFiles}) " +
                   $"Прошло: {FormatTimeSpan(elapsed)} " +
                   $"Осталось: {FormatTimeSpan(remaining)}";
        }

        public string GetSummary()
        {
            TimeSpan totalTime = DateTime.Now - _startTime;
            return $"Успешно: {_successfulFiles} файлов\n" +
                   $"Пропущено: {_totalFiles - _processedFiles} файлов\n" +
                   $"Ошибок: {_failedFiles} файлов\n" +
                   $"Общее время: {FormatTimeSpan(totalTime)}\n" +
                   $"Среднее время на файл: {FormatTimeSpan(TimeSpan.FromSeconds(totalTime.TotalSeconds / _processedFiles))}";
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