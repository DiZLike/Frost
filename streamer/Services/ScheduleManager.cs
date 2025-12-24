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

            Logger.Debug($"[ScheduleManager] Поиск файла расписания: {scheduleFile}");
            Logger.Debug($"[ScheduleManager] Файл существует: {File.Exists(scheduleFile)}");

            if (!File.Exists(scheduleFile))
            {
                Logger.Warning($"[ScheduleManager] Файл расписания не найден: {scheduleFile}");
                _scheduleConfig = new ScheduleConfig();
                return;
            }

            try
            {
                // Читаем и логируем содержимое файла для отладки
                string json = File.ReadAllText(scheduleFile);
                Logger.Debug($"[ScheduleManager] Размер файла расписания: {json.Length} символов");

                // Проверяем на BOM
                if (json.Length > 0 && json[0] == '\uFEFF')
                {
                    Logger.Warning("[ScheduleManager] Файл имеет UTF-8 BOM, удаляю...");
                    json = json.Substring(1);
                }

                // Пробуем парсить с опциями
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip
                };

                _scheduleConfig = System.Text.Json.JsonSerializer.Deserialize<ScheduleConfig>(json, options) ?? new ScheduleConfig();

                Logger.Info($"[ScheduleManager] Расписание загружено: {_scheduleConfig.ScheduleItems.Count} элементов из {scheduleFile}");

                if (_scheduleConfig.ScheduleItems.Count == 0)
                {
                    Logger.Warning("[ScheduleManager] Расписание загружено, но содержит 0 элементов. Возможно проблема с парсингом JSON.");

                    // Пробуем десериализовать вручную для диагностики
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(json);
                        Logger.Info($"[ScheduleManager] Корневой элемент JSON: {doc.RootElement.ValueKind}");

                        if (doc.RootElement.TryGetProperty("ScheduleItems", out var items))
                        {
                            Logger.Info($"[ScheduleManager] ScheduleItems найдены: {items.ValueKind}, количество: {items.GetArrayLength()}");
                        }
                        else
                        {
                            Logger.Error("[ScheduleManager] Свойство 'ScheduleItems' не найдено в JSON");
                        }
                    }
                    catch (Exception parseEx)
                    {
                        Logger.Error($"[ScheduleManager] Ручной парсинг JSON не удался: {parseEx.Message}");
                    }
                }
                else
                {
                    // Логируем все элементы расписания
                    foreach (var item in _scheduleConfig.ScheduleItems)
                    {
                        Logger.Debug($"[ScheduleManager] {item.Name}: {item.StartHour:00}:{item.StartMinute:00} - {item.EndHour:00}:{item.EndMinute:00}");
                        Logger.Debug($"[ScheduleManager] Плейлист: {item.PlaylistPath}");
                        Logger.Debug($"[ScheduleManager] Дни: {string.Join(", ", item.DaysOfWeek)}");
                    }
                }
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                Logger.Error($"[ScheduleManager] Ошибка парсинга JSON: {jsonEx.Message}");
                Logger.Error($"[ScheduleManager] Строка: {jsonEx.LineNumber}, Позиция: {jsonEx.BytePositionInLine}");
                _scheduleConfig = new ScheduleConfig();
            }
            catch (Exception ex)
            {
                Logger.Error($"[ScheduleManager] Не удалось загрузить расписание: {ex.Message}");
                Logger.Error($"[ScheduleManager] Тип исключения: {ex.GetType().FullName}");

                if (ex.InnerException != null)
                {
                    Logger.Error($"[ScheduleManager] Внутреннее исключение: {ex.InnerException.Message}");
                }

                _scheduleConfig = new ScheduleConfig();
            }
        }

        public void CheckAndUpdatePlaylist()
        {
            if (!_config.ScheduleEnable)
            {
                Logger.Info("[ScheduleManager] Расписание отключено в конфигурации");
                return;
            }

            // Если нет элементов в расписании
            if (_scheduleConfig.ScheduleItems.Count == 0)
            {
                Logger.Warning("[ScheduleManager] Расписание пустое. Нет плейлистов для воспроизведения.");
                return;
            }

            DateTime now = DateTime.Now;
            _lastCheckTime = now;

            Logger.Info($"[ScheduleManager] Проверка расписания в {now:HH:mm} в {now.DayOfWeek}");

            // Находим активный элемент расписания
            var activeSchedule = _scheduleConfig.ScheduleItems
                .FirstOrDefault(item => item.IsActive(now));

            // Если нет активного расписания
            if (activeSchedule == null)
            {
                Logger.Warning($"[ScheduleManager] Активное расписание не найдено в {now:HH:mm} в {now.DayOfWeek}");

                // Показываем все расписания для отладки
                Logger.Info("Все расписания:");
                foreach (var item in _scheduleConfig.ScheduleItems)
                {
                    Logger.Info($"[ScheduleManager] {item.Name}: {item.StartHour:00}:{item.StartMinute:00} - {item.EndHour:00}:{item.EndMinute:00}");
                    Logger.Info($"[ScheduleManager] Дни: {string.Join(", ", item.DaysOfWeek)}");
                    Logger.Info($"[ScheduleManager] Активно: {item.IsActive(now)}");
                }

                if (_currentPlaylist != null)
                {
                    Logger.Warning("[ScheduleManager] Остановка воспроизведения - нет активного расписания");
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
                Logger.Error($"[ScheduleManager] Файл плейлиста не найден: {activeSchedule.PlaylistPath}");
                Logger.Error($"[ScheduleManager] Файл существует: {File.Exists(activeSchedule.PlaylistPath)}");
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

                Logger.Info($"[ScheduleManager] Расписание изменено на: {activeSchedule.Name}");
                Logger.Info($"[ScheduleManager] Текущий плейлист: {activeSchedule.PlaylistPath} ({_currentPlaylist.TotalTracks} треков)");
                Logger.Info($"[ScheduleManager] Активное время: {activeSchedule.StartTime:hh\\:mm} - {activeSchedule.EndTime:hh\\:mm}");
                Logger.Info($"[ScheduleManager] Дни: {string.Join(", ", activeSchedule.DaysOfWeek)}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[ScheduleManager] Не удалось переключить плейлист: {ex.Message}");
            }
        }

        public string? GetNextTrack()
        {
            if (_scheduleConfig.ScheduleItems.Count == 0)
            {
                Logger.Warning("[ScheduleManager] Расписание пустое, невозможно получить трек");
                return null;
            }

            // Всегда проверяем расписание перед получением трека
            //CheckAndUpdatePlaylist();

            if (_currentPlaylist == null)
            {
                Logger.Warning("[ScheduleManager] Нет активного плейлиста");
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
    }
}