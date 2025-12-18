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

            Logger.Info($"Поиск файла расписания: {scheduleFile}");
            Logger.Info($"Файл существует: {File.Exists(scheduleFile)}");

            if (!File.Exists(scheduleFile))
            {
                Logger.Warning($"Файл расписания не найден: {scheduleFile}");
                _scheduleConfig = new ScheduleConfig();
                return;
            }

            try
            {
                // Читаем и логируем содержимое файла для отладки
                string json = File.ReadAllText(scheduleFile);
                Logger.Info($"Размер файла расписания: {json.Length} символов");

                // Показываем начало файла для диагностики
                string firstLines = string.Join("\n", json.Split('\n').Take(5));
                Logger.Info($"Первые 5 строк:\n{firstLines}");

                // Проверяем на BOM
                if (json.Length > 0 && json[0] == '\uFEFF')
                {
                    Logger.Warning("Файл имеет UTF-8 BOM, удаляю...");
                    json = json.Substring(1);
                }

                // Пробуем парсить с опциями
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip
                };

                _scheduleConfig = System.Text.Json.JsonSerializer.Deserialize<ScheduleConfig>(json, options) ?? new ScheduleConfig();

                Logger.Info($"Расписание загружено: {_scheduleConfig.ScheduleItems.Count} элементов из {scheduleFile}");

                if (_scheduleConfig.ScheduleItems.Count == 0)
                {
                    Logger.Warning("Расписание загружено, но содержит 0 элементов. Возможно проблема с парсингом JSON.");

                    // Пробуем десериализовать вручную для диагностики
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(json);
                        Logger.Info($"Корневой элемент JSON: {doc.RootElement.ValueKind}");

                        if (doc.RootElement.TryGetProperty("ScheduleItems", out var items))
                        {
                            Logger.Info($"ScheduleItems найдены: {items.ValueKind}, количество: {items.GetArrayLength()}");
                        }
                        else
                        {
                            Logger.Error("Свойство 'ScheduleItems' не найдено в JSON");
                        }
                    }
                    catch (Exception parseEx)
                    {
                        Logger.Error($"Ручной парсинг JSON не удался: {parseEx.Message}");
                    }
                }
                else
                {
                    // Логируем все элементы расписания
                    foreach (var item in _scheduleConfig.ScheduleItems)
                    {
                        Logger.Info($"  - {item.Name}: {item.StartHour:00}:{item.StartMinute:00} - {item.EndHour:00}:{item.EndMinute:00}");
                        Logger.Info($"    Плейлист: {item.PlaylistPath}");
                        Logger.Info($"    Дни: {string.Join(", ", item.DaysOfWeek)}");
                    }
                }
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                Logger.Error($"Ошибка парсинга JSON: {jsonEx.Message}");
                Logger.Error($"Строка: {jsonEx.LineNumber}, Позиция: {jsonEx.BytePositionInLine}");
                _scheduleConfig = new ScheduleConfig();
            }
            catch (Exception ex)
            {
                Logger.Error($"Не удалось загрузить расписание: {ex.Message}");
                Logger.Error($"Тип исключения: {ex.GetType().FullName}");

                if (ex.InnerException != null)
                {
                    Logger.Error($"Внутреннее исключение: {ex.InnerException.Message}");
                }

                _scheduleConfig = new ScheduleConfig();
            }
        }

        public void CheckAndUpdatePlaylist()
        {
            if (!_config.ScheduleEnable)
            {
                Logger.Info("Расписание отключено в конфигурации");
                return;
            }

            // Если нет элементов в расписании
            if (_scheduleConfig.ScheduleItems.Count == 0)
            {
                Logger.Warning("Расписание пустое. Нет плейлистов для воспроизведения.");
                return;
            }

            DateTime now = DateTime.Now;
            _lastCheckTime = now;

            Logger.Info($"Проверка расписания в {now:HH:mm} в {now.DayOfWeek}");

            // Находим активный элемент расписания
            var activeSchedule = _scheduleConfig.ScheduleItems
                .FirstOrDefault(item => item.IsActive(now));

            // Если нет активного расписания
            if (activeSchedule == null)
            {
                Logger.Warning($"Активное расписание не найдено в {now:HH:mm} в {now.DayOfWeek}");

                // Показываем все расписания для отладки
                Logger.Info("Все расписания:");
                foreach (var item in _scheduleConfig.ScheduleItems)
                {
                    Logger.Info($"  - {item.Name}: {item.StartHour:00}:{item.StartMinute:00} - {item.EndHour:00}:{item.EndMinute:00}");
                    Logger.Info($"    Дни: {string.Join(", ", item.DaysOfWeek)}");
                    Logger.Info($"    Активно: {item.IsActive(now)}");
                }

                if (_currentPlaylist != null)
                {
                    Logger.Warning("Остановка воспроизведения - нет активного расписания");
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
                Logger.Error($"Файл плейлиста не найден: {activeSchedule.PlaylistPath}");
                Logger.Error($"Файл существует: {File.Exists(activeSchedule.PlaylistPath)}");
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

                Logger.Info($"Расписание изменено на: {activeSchedule.Name}");
                Logger.Info($"Текущий плейлист: {activeSchedule.PlaylistPath} ({_currentPlaylist.TotalTracks} треков)");
                Logger.Info($"Активное время: {activeSchedule.StartTime:hh\\:mm} - {activeSchedule.EndTime:hh\\:mm}");
                Logger.Info($"Дни: {string.Join(", ", activeSchedule.DaysOfWeek)}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Не удалось переключить плейлист: {ex.Message}");
            }
        }

        public string? GetNextTrack()
        {
            if (_scheduleConfig.ScheduleItems.Count == 0)
            {
                Logger.Warning("Расписание пустое, невозможно получить трек");
                return null;
            }

            // Всегда проверяем расписание перед получением трека
            //CheckAndUpdatePlaylist();

            if (_currentPlaylist == null)
            {
                Logger.Warning("Нет активного плейлиста");
                return null;
            }

            return _currentPlaylist.GetRandomTrack();
        }

        public string GetCurrentScheduleInfo()
        {
            if (_currentSchedule == null)
                return "Нет активного расписания";

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