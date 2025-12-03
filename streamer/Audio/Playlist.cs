using Strimer.Core;

namespace Strimer.Audio
{
    public class Playlist
    {
        private List<string> _tracks = new();
        private List<string> _history = new();
        private Random _random = new();
        private int _currentIndex = -1;
        private bool _saveHistory;

        public int TotalTracks => _tracks.Count;
        public int CurrentIndex => _currentIndex;

        public Playlist(string playlistFile, bool saveHistory = true)
        {
            _saveHistory = saveHistory;
            LoadPlaylist(playlistFile);

            if (_saveHistory)
                LoadHistory();
        }

        private void LoadPlaylist(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Playlist file not found: {filePath}");

            Logger.Info($"Loading playlist from: {filePath}");

            string content = File.ReadAllText(filePath);
            _tracks = ExtractTracks(content);

            if (_tracks.Count == 0)
                throw new InvalidDataException("No tracks found in playlist");

            Logger.Info($"Loaded {_tracks.Count} tracks");
        }

        private List<string> ExtractTracks(string content)
        {
            var tracks = new List<string>();

            // Ищем все track=...?; в строке
            int startIndex = 0;
            while (startIndex < content.Length)
            {
                // Ищем начало трека
                int trackStart = content.IndexOf("track=", startIndex, StringComparison.Ordinal);
                if (trackStart == -1) break;

                trackStart += 6; // Длина "track="

                // Ищем конец трека
                int trackEnd = content.IndexOf("?;", trackStart, StringComparison.Ordinal);
                if (trackEnd == -1) break;

                string track = content.Substring(trackStart, trackEnd - trackStart).Trim();

                // Проверяем, что трек не пустой
                if (!string.IsNullOrWhiteSpace(track))
                {
                    // Проверяем существование файла
                    if (File.Exists(track))
                    {
                        tracks.Add(track);
                    }
                    else
                    {
                        Logger.Warning($"Track file not found: {track}");
                    }
                }

                startIndex = trackEnd + 2;
            }

            return tracks;
        }

        public string GetRandomTrack()
        {
            if (_tracks.Count == 0)
                throw new InvalidOperationException("Playlist is empty");

            // Получаем треки, которых нет в истории
            List<string> availableTracks;

            if (_history.Count >= _tracks.Count)
            {
                // Все треки уже были в истории, очищаем историю
                availableTracks = _tracks.ToList();
                _history.Clear();
                Logger.Info("History cleared, starting playlist over");
            }
            else
            {
                availableTracks = _tracks.Except(_history).ToList();
            }

            // Выбираем случайный трек из доступных
            int randomIndex = _random.Next(availableTracks.Count);
            string selectedTrack = availableTracks[randomIndex];

            // Обновляем историю
            _history.Add(selectedTrack);
            _currentIndex = _tracks.IndexOf(selectedTrack);

            // Сохраняем историю в файл
            if (_saveHistory)
                SaveHistory();

            Logger.Info($"Selected track: {Path.GetFileName(selectedTrack)} " +
                       $"(Index: {_currentIndex + 1}/{_tracks.Count})");

            return selectedTrack;
        }

        private void LoadHistory()
        {
            string historyFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "history.pls");

            if (!File.Exists(historyFile))
                return;

            try
            {
                var lines = File.ReadAllLines(historyFile);
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line) && File.Exists(line))
                        _history.Add(line);
                }

                Logger.Info($"Loaded {_history.Count} tracks from history");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load history: {ex.Message}");
            }
        }

        private void SaveHistory()
        {
            try
            {
                string configDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
                Directory.CreateDirectory(configDir);

                string historyFile = Path.Combine(configDir, "history.pls");
                File.WriteAllLines(historyFile, _history);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to save history: {ex.Message}");
            }
        }

        public void ClearHistory()
        {
            _history.Clear();

            if (_saveHistory)
                SaveHistory();

            Logger.Info("Playlist history cleared");
        }
    }
}