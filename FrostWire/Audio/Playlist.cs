using Strimer.Core;

namespace Strimer.Audio
{
    public class Playlist
    {
        private readonly string _playlistFile;
        private readonly bool _saveHistory;
        private readonly bool _dynamic;
        private List<string> _tracks = new();
        private List<string> _playedTracks = new();
        private DateTime _lastFileWriteTime;
        private int _currentIndex = -1;
        private static readonly Random _random = new();             // Статический для лучшего распределения случайных чисел

        // Свойства для доступа к состоянию плейлиста
        public int TotalTracks => _tracks.Count;                    // Общее количество треков в плейлисте
        public int CurrentIndex => _currentIndex;                   // Индекс текущего трека
        public int PlayedCount => _playedTracks.Count;              // Количество воспроизведенных треков
        public string PlaylistFilePath => _playlistFile;            // Полный путь к файлу плейлиста

        public Playlist(string playlistFile, bool saveHistory, bool dynamic = false)
        {
            _playlistFile = playlistFile;
            _saveHistory = saveHistory;
            _dynamic = dynamic;

            LoadPlaylist();     // Загрузка треков из файла
            if (_saveHistory)   // Загрузка истории воспроизведения если требуется
                LoadHistory();
        }

        // Загружает треки из файла плейлиста
        private void LoadPlaylist()
        {
            try
            {
                if (File.Exists(_playlistFile))
                {
                    var lines = File.ReadAllLines(_playlistFile);
                    _tracks.Clear();
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                            continue; // Пропускаем пустые строки и комментарии

                        // Обработка формата track=C:\path\to\file.mp3?;
                        string trackPath = line.Trim();
                        if (trackPath.StartsWith("track="))
                        {
                            trackPath = trackPath.Substring(6);                 // Убираем "track="
                            trackPath = trackPath.TrimEnd('?', ';');            // Убираем завершающие символы
                        }

                        _tracks.Add(trackPath);
                    }

                    _lastFileWriteTime = File.GetLastWriteTime(_playlistFile);  // Запоминаем время изменения файла
                    Logger.Info($"[Playlist] Плейлист загружен: {_tracks.Count} треков");
                }
                else
                {
                    Logger.Error($"[Playlist] Файл плейлиста не найден: {_playlistFile}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[Playlist] Не удалось загрузить плейлист: {ex.Message}");
            }
        }

        // Проверяет и перезагружает плейлист если файл изменился (только для динамических плейлистов)
        private void CheckAndReload()
        {
            if (!_dynamic || !File.Exists(_playlistFile)) // Проверка только для динамических плейлистов
                return;

            try
            {
                var currentWriteTime = File.GetLastWriteTime(_playlistFile);

                if (currentWriteTime != _lastFileWriteTime) // Если файл изменился
                {
                    int oldCount = _tracks.Count;
                    LoadPlaylist(); // Перезагружаем плейлист
                    Logger.Info($"[Playlist] Плейлист обновлен: {oldCount} -> {_tracks.Count} треков");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[Playlist] Не удалось проверить обновления плейлиста: {ex.Message}");
            }
        }

        // Возвращает случайный трек из плейлиста, который еще не воспроизводился
        // Обеспечивает неповторяющееся воспроизведение до очистки истории
        public string? GetRandomTrack()
        {
            CheckAndReload(); // Проверяем обновления перед выбором трека

            if (_tracks.Count == 0)
            {
                Logger.Warning("[Playlist] Плейлист пуст");
                return null;
            }

            // Очищаем историю если сыграно более 80% треков (предотвращает долгий поиск)
            if (_playedTracks.Count >= _tracks.Count * 0.8)
            {
                _playedTracks.Clear();
                Logger.Info("[Playlist] История плейлиста очищена (сыграно более 80% треков)");
            }

            // Создаем список еще не сыгранных треков
            var availableTracks = _tracks.Where(t => !_playedTracks.Contains(t)).ToList();

            if (availableTracks.Count == 0) // На всякий случай, если все треки сыграны
            {
                _playedTracks.Clear();
                availableTracks = _tracks.ToList();
            }

            // Выбираем случайный трек из доступных
            int availableIndex = _random.Next(0, availableTracks.Count);
            string selectedTrack = availableTracks[availableIndex];

            // Находим индекс в основном списке для CurrentIndex
            _currentIndex = _tracks.IndexOf(selectedTrack);

            _playedTracks.Add(selectedTrack); // Добавляем в историю

            if (_saveHistory) // Сохраняем историю если требуется
                SaveHistory();

            return selectedTrack;
        }

        // Генерирует уникальное имя файла истории на основе пути к плейлисту
        // Использует MD5 хэш полного пути для обеспечения уникальности
        private string GetHistoryFileName()
        {
            string playlistName = Path.GetFileNameWithoutExtension(_playlistFile);

            // Создаем хэш полного пути для уникальности имени файла
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] pathBytes = System.Text.Encoding.UTF8.GetBytes(_playlistFile);
                byte[] hashBytes = md5.ComputeHash(pathBytes);

                // Берем первые 4 байта хэша (8 hex символов) для короткого и читаемого имени
                string hash = BitConverter.ToString(hashBytes, 0, 4)
                    .Replace("-", "")
                    .ToLowerInvariant();

                return $"{playlistName}_{hash}.history"; // Формат: имя_плейлиста_хэш.history
            }
        }

        /// Загружает историю воспроизведения из файла
        private void LoadHistory()
        {
            try
            {
                string historyFile = Path.Combine(
                    Path.GetDirectoryName(_playlistFile) ?? AppDomain.CurrentDomain.BaseDirectory,
                    GetHistoryFileName()
                );

                if (File.Exists(historyFile))
                {
                    _playedTracks.Clear(); // Очищаем текущую историю

                    var lines = File.ReadAllLines(historyFile);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                            continue; // Пропускаем пустые строки и комментарии

                        // Ищем строку с file= для извлечения пути к треку
                        if (line.StartsWith("file="))
                        {
                            string trackPath = line.Substring(5); // Убираем "file="
                            trackPath = trackPath.TrimEnd(';');   // Убираем завершающую точку с запятой

                            if (!string.IsNullOrWhiteSpace(trackPath))
                            {
                                _playedTracks.Add(trackPath);
                            }
                        }
                    }

                    Logger.Info($"[Playlist] История плейлиста загружена: {_playedTracks.Count} воспроизведенных треков");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[Playlist] Не удалось загрузить историю плейлиста: {ex.Message}");
            }
        }

        /// Сохраняет историю воспроизведения в файл
        private void SaveHistory()
        {
            try
            {
                string historyFile = Path.Combine(
                    Path.GetDirectoryName(_playlistFile) ?? AppDomain.CurrentDomain.BaseDirectory,
                    GetHistoryFileName()
                );

                // Создаем список строк для записи в формате date=Время;file=Трек
                var historyLines = new List<string>();

                foreach (string track in _playedTracks)
                {
                    // Формируем запись в формате: date=2024-01-01T12:00:00;file=C:\path\to\track.mp3;
                    string historyEntry = $"date={DateTime.Now:yyyy-MM-ddTHH:mm:ss};\nfile={track};";
                    historyLines.Add(historyEntry);
                }

                File.WriteAllLines(historyFile, historyLines);
            }
            catch (Exception ex)
            {
                Logger.Error($"[Playlist] Не удалось сохранить историю плейлиста: {ex.Message}");
            }
        }
    }
}