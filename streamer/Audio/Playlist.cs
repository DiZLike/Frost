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
        private Random _random = new();

        public int TotalTracks => _tracks.Count;
        public int CurrentIndex => _currentIndex;
        public int PlayedCount => _playedTracks.Count;
        public string PlaylistFilePath => _playlistFile;

        public Playlist(string playlistFile, bool saveHistory, bool dynamic = false)
        {
            _playlistFile = playlistFile;
            _saveHistory = saveHistory;
            _dynamic = dynamic;

            LoadPlaylist();

            // Загружаем историю воспроизведения если нужно
            if (_saveHistory)
            {
                LoadHistory();
            }
        }

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
                            continue;

                        // track=C:\path\to\file.mp3?;
                        string trackPath = line.Trim();

                        if (trackPath.StartsWith("track="))
                        {
                            trackPath = trackPath.Substring(6); // убираем "track="
                            trackPath = trackPath.TrimEnd('?', ';');
                        }

                        _tracks.Add(trackPath);
                    }

                    _lastFileWriteTime = File.GetLastWriteTime(_playlistFile);
                    Logger.Info($"Плейлист загружен: {_tracks.Count} треков");
                }
                else
                {
                    Logger.Error($"Файл плейлиста не найден: {_playlistFile}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Не удалось загрузить плейлист: {ex.Message}");
            }
        }

        private void CheckAndReload()
        {
            if (!_dynamic || !File.Exists(_playlistFile))
                return;

            try
            {
                var currentWriteTime = File.GetLastWriteTime(_playlistFile);

                // Если файл изменился с момента последней загрузки
                if (currentWriteTime != _lastFileWriteTime)
                {
                    int oldCount = _tracks.Count;
                    LoadPlaylist();  // Перезагружаем плейлист
                    Logger.Info($"Плейлист обновлен: {oldCount} -> {_tracks.Count} треков");

                    // Сохраняем историю после обновления
                    if (_saveHistory)
                    {
                        SaveHistory();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Не удалось проверить обновления плейлиста: {ex.Message}");
            }
        }

        public string? GetRandomTrack()
        {
            // ПРОВЕРЯЕМ ОБНОВЛЕНИЯ перед выбором трека
            CheckAndReload();

            if (_tracks.Count == 0)
            {
                Logger.Warning("Плейлист пуст");
                return null;
            }

            if (_playedTracks.Count >= _tracks.Count)
            {
                // Все треки сыграли, начинаем заново
                _playedTracks.Clear();
                Logger.Info("История плейлиста очищена, начинается новый цикл");
            }

            string selectedTrack;
            do
            {
                _currentIndex = _random.Next(0, _tracks.Count);
                selectedTrack = _tracks[_currentIndex];
            }
            while (_playedTracks.Contains(selectedTrack) && _playedTracks.Count < _tracks.Count);

            // Добавляем в историю
            _playedTracks.Add(selectedTrack);

            // Сохраняем историю если нужно
            if (_saveHistory)
            {
                SaveHistory();
            }

            return selectedTrack;
        }

        private string GetHistoryFileName()
        {
            // Генерируем уникальное имя файла истории на основе пути к плейлисту
            string playlistName = Path.GetFileNameWithoutExtension(_playlistFile);
            string playlistDir = Path.GetDirectoryName(_playlistFile) ?? "";
            string safePath = playlistDir.Replace(Path.DirectorySeparatorChar, '_')
                                         .Replace(Path.AltDirectorySeparatorChar, '_')
                                         .Replace(':', '_');

            return $"{safePath}_{playlistName}.history";
        }

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
                    _playedTracks = File.ReadAllLines(historyFile)
                        .Where(line => !string.IsNullOrWhiteSpace(line))
                        .ToList();

                    Logger.Info($"История плейлиста загружена: {_playedTracks.Count} воспроизведенных треков");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Не удалось загрузить историю плейлиста: {ex.Message}");
            }
        }

        private void SaveHistory()
        {
            try
            {
                string historyFile = Path.Combine(
                    Path.GetDirectoryName(_playlistFile) ?? AppDomain.CurrentDomain.BaseDirectory,
                    GetHistoryFileName()
                );

                File.WriteAllLines(historyFile, _playedTracks);
            }
            catch (Exception ex)
            {
                Logger.Error($"Не удалось сохранить историю плейлиста: {ex.Message}");
            }
        }

        public void ClearHistory()
        {
            _playedTracks.Clear();
            if (_saveHistory)
            {
                SaveHistory();
            }
            Logger.Info("История плейлиста очищена");
        }

        public string[] GetRecentTracks(int count = 10)
        {
            return _playedTracks
                .TakeLast(Math.Min(count, _playedTracks.Count))
                .Reverse() // Последний сыгранный трек первым
                .Select(path => Path.GetFileName(path))
                .ToArray();
        }
    }
}