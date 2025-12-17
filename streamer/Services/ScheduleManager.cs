using Strimer.App;
using Strimer.Audio;
using Strimer.Core;

namespace Strimer.Services
{
    public class ScheduleManager
    {
        private readonly AppConfig _config;
        private ScheduleConfig _scheduleConfig;
        private Playlist? _currentPlaylist;
        private ScheduleItem? _currentSchedule;
        private DateTime _lastCheckTime;
        private string _lastPlaylistPath = "";

        public Playlist? CurrentPlaylist => _currentPlaylist;
        public ScheduleItem? CurrentSchedule => _currentSchedule;

        public ScheduleManager(AppConfig config)
        {
            _config = config;
            LoadSchedule();
        }

        private void LoadSchedule()
        {
            string scheduleFile = _config.ScheduleFile;

            // Если путь относительный - добавляем базовую директорию
            if (!Path.IsPathRooted(scheduleFile))
            {
                scheduleFile = Path.Combine(_config.BaseDirectory, scheduleFile);
            }

            Logger.Info($"Looking for schedule file: {scheduleFile}");
            Logger.Info($"File exists: {File.Exists(scheduleFile)}");

            if (!File.Exists(scheduleFile))
            {
                Logger.Warning($"Schedule file not found: {scheduleFile}");
                _scheduleConfig = new ScheduleConfig();
                return;
            }

            try
            {
                // Читаем и логируем содержимое файла для отладки
                string json = File.ReadAllText(scheduleFile);
                Logger.Info($"Schedule file size: {json.Length} chars");

                // Показываем начало файла для диагностики
                string firstLines = string.Join("\n", json.Split('\n').Take(5));
                Logger.Info($"First 5 lines:\n{firstLines}");

                // Проверяем на BOM
                if (json.Length > 0 && json[0] == '\uFEFF')
                {
                    Logger.Warning("File has UTF-8 BOM, removing it...");
                    json = json.Substring(1);
                }

                // Пробуем парсить с опциями
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip
                };

                _scheduleConfig = System.Text.Json.JsonSerializer.Deserialize<ScheduleConfig>(json, options) ?? new ScheduleConfig();

                Logger.Info($"Schedule loaded: {_scheduleConfig.ScheduleItems.Count} items from {scheduleFile}");

                if (_scheduleConfig.ScheduleItems.Count == 0)
                {
                    Logger.Warning("Schedule loaded but contains 0 items. Something wrong with JSON parsing.");

                    // Пробуем десериализовать вручную для диагностики
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(json);
                        Logger.Info($"JSON root element: {doc.RootElement.ValueKind}");

                        if (doc.RootElement.TryGetProperty("ScheduleItems", out var items))
                        {
                            Logger.Info($"ScheduleItems found: {items.ValueKind}, count: {items.GetArrayLength()}");
                        }
                        else
                        {
                            Logger.Error("No 'ScheduleItems' property found in JSON");
                        }
                    }
                    catch (Exception parseEx)
                    {
                        Logger.Error($"Manual JSON parse failed: {parseEx.Message}");
                    }
                }
                else
                {
                    // Логируем все элементы расписания
                    foreach (var item in _scheduleConfig.ScheduleItems)
                    {
                        Logger.Info($"  - {item.Name}: {item.StartHour:00}:{item.StartMinute:00} - {item.EndHour:00}:{item.EndMinute:00}");
                        Logger.Info($"    Playlist: {item.PlaylistPath}");
                        Logger.Info($"    Days: {string.Join(", ", item.DaysOfWeek)}");
                    }
                }
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                Logger.Error($"JSON parsing error: {jsonEx.Message}");
                Logger.Error($"Line: {jsonEx.LineNumber}, Position: {jsonEx.BytePositionInLine}");
                _scheduleConfig = new ScheduleConfig();
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load schedule: {ex.Message}");
                Logger.Error($"Exception type: {ex.GetType().FullName}");

                if (ex.InnerException != null)
                {
                    Logger.Error($"Inner exception: {ex.InnerException.Message}");
                }

                _scheduleConfig = new ScheduleConfig();
            }
        }

        public void CheckAndUpdatePlaylist()
        {
            if (!_config.ScheduleEnable)
            {
                Logger.Info("Schedule is disabled in config");
                return;
            }

            // Если нет элементов в расписании
            if (_scheduleConfig.ScheduleItems.Count == 0)
            {
                Logger.Warning("Schedule is empty. No playlists to play.");
                return;
            }

            DateTime now = DateTime.Now;

            // Проверяем не чаще чем раз в минуту (если не принудительно)
            //if (!forceCheck && (now - _lastCheckTime).TotalMinutes < 1)
            //    return;

            _lastCheckTime = now;

            Logger.Info($"Checking schedule at {now:HH:mm} on {now.DayOfWeek}");

            // Находим активный элемент расписания
            var activeSchedule = _scheduleConfig.ScheduleItems
                .FirstOrDefault(item => item.IsActive(now));

            // Если нет активного расписания
            if (activeSchedule == null)
            {
                Logger.Warning($"No active schedule found at {now:HH:mm} on {now.DayOfWeek}");

                // Показываем все расписания для отладки
                Logger.Info("All schedules:");
                foreach (var item in _scheduleConfig.ScheduleItems)
                {
                    Logger.Info($"  - {item.Name}: {item.StartHour:00}:{item.StartMinute:00} - {item.EndHour:00}:{item.EndMinute:00}");
                    Logger.Info($"    Days: {string.Join(", ", item.DaysOfWeek)}");
                    Logger.Info($"    IsActive: {item.IsActive(now)}");
                }

                if (_currentPlaylist != null)
                {
                    Logger.Warning("Stopping playback - no active schedule");
                    _currentPlaylist = null;
                    _currentSchedule = null;
                }
                return;
            }

            // Если расписание не изменилось
            if (_currentSchedule?.Name == activeSchedule.Name &&
                _lastPlaylistPath == activeSchedule.PlaylistPath)
                return;

            // Проверяем существование файла плейлиста
            if (!File.Exists(activeSchedule.PlaylistPath))
            {
                Logger.Error($"Playlist file not found: {activeSchedule.PlaylistPath}");
                Logger.Error($"File exists: {File.Exists(activeSchedule.PlaylistPath)}");
                return;
            }

            try
            {
                // Создаем или обновляем плейлист
                _currentPlaylist = new Playlist(
                    activeSchedule.PlaylistPath,
                    _config.SavePlaylistHistory,
                    _config.DynamicPlaylist
                );

                _currentSchedule = activeSchedule;
                _lastPlaylistPath = activeSchedule.PlaylistPath;

                Logger.Info($"Schedule changed to: {activeSchedule.Name}");
                Logger.Info($"Current playlist: {activeSchedule.PlaylistPath} ({_currentPlaylist.TotalTracks} tracks)");
                Logger.Info($"Active time: {activeSchedule.StartTime:hh\\:mm} - {activeSchedule.EndTime:hh\\:mm}");
                Logger.Info($"Days: {string.Join(", ", activeSchedule.DaysOfWeek)}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to switch playlist: {ex.Message}");
            }
        }

        public string? GetNextTrack()
        {
            // Если расписание пустое, сразу возвращаем null
            if (_scheduleConfig.ScheduleItems.Count == 0)
            {
                Logger.Warning("Schedule is empty, cannot get track");
                return null;
            }

            if (_currentPlaylist == null)
            {
                Logger.Info("No current playlist, checking schedule...");
                CheckAndUpdatePlaylist();

                if (_currentPlaylist == null)
                {
                    Logger.Warning("No active playlist available after schedule check");
                    return null;
                }
            }

            // Проверяем расписание перед выбором трека
            CheckAndUpdatePlaylist();

            return _currentPlaylist.GetRandomTrack();
        }

        public string GetCurrentScheduleInfo()
        {
            if (_currentSchedule == null)
                return "No active schedule";

            return $"{_currentSchedule.Name} ({_currentSchedule.StartTime:hh\\:mm} - {_currentSchedule.EndTime:hh\\:mm})";
        }

        public string[] GetUpcomingSchedules(int count = 5)
        {
            var now = DateTime.Now;
            var upcoming = new List<string>();

            foreach (var item in _scheduleConfig.ScheduleItems)
            {
                if (item.IsActive(now) || item.StartTime > now.TimeOfDay)
                {
                    upcoming.Add($"{item.Name} ({item.StartTime:hh\\:mm}-{item.EndTime:hh\\:mm})");

                    if (upcoming.Count >= count)
                        break;
                }
            }

            return upcoming.ToArray();
        }
    }
}