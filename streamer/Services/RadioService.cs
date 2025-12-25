using Strimer.App;
using Strimer.Audio;
using Strimer.Broadcast;
using Strimer.Core;

namespace Strimer.Services
{
    public class RadioService
    {
        private readonly AppConfig _config;               // Конфигурация приложения
        private readonly Player _player;                  // Проигрыватель аудио
        private readonly ScheduleManager _scheduleManager;// Менеджер расписания
        private readonly Playlist? _fallbackPlaylist;     // Резервный плейлист (если расписание отключено)
        private readonly IceCastClient _iceCast;          // Клиент для IceCast стриминга
        private readonly MyServerClient _myServer;        // Клиент для внешнего сервера
        private readonly JingleService _jingleService;    // Сервис джинглов

        private Thread _playbackThread;                   // Поток воспроизведения
        private bool _isRunning;                          // Флаг работы сервиса
        private bool _isPaused;                           // Флаг паузы
        private int _trackCounter;                        // Счетчик треков для джингло

        // Текущая информация о треке
        private TrackInfo? _currentTrack;                 // Текущий воспроизводимый трек
        private DateTime _trackStartTime;                 // Время начала текущего трека
        private bool _skipToNextTrack = false;

        public bool IsRunning => _isRunning;              // Свойство: работает ли сервис
        public bool IsPaused => _isPaused;                // Свойство: на паузе ли воспроизведение
        public TrackInfo? CurrentTrack => _currentTrack;  // Свойство: текущий трек
        public IceCastClient IceCast => _iceCast;         // Свойство: клиент IceCast

        public RadioService(AppConfig config)
        {
            _config = config;                             // Сохраняем конфигурацию

            Logger.Info("[RadioService] Инициализация радио сервиса...");// Лог: начало инициализации

            // Инициализация компонентов
            _jingleService = new JingleService(config);   // Создаем сервис джинглов
            _player = new Player(config, _jingleService); // Создаем проигрыватель
            _iceCast = new IceCastClient(config);         // Создаем клиент IceCast
            _myServer = new MyServerClient(config);       // Создаем клиент внешнего сервера
            _iceCast.ConnectionRestored += _iceCast_ConnectionRestored;

            _scheduleManager = new ScheduleManager(config);// Создаем менеджер расписания

            // Резервный плейлист (если расписание отключено или не настроено)
            if (!config.ScheduleEnable || _scheduleManager.CurrentPlaylist == null)
            {
                _fallbackPlaylist = new Playlist(         // Создаем резервный плейлист
                    config.PlaylistFile,
                    config.SavePlaylistHistory,
                    config.DynamicPlaylist
                );
                Logger.Info($"[RadioService] Резервный плейлист: {_fallbackPlaylist.TotalTracks} треков"); // Лог: информация о плейлисте
            }

            Logger.Info($"[RadioService] Расписание включено: {config.ScheduleEnable}"); // Лог: статус расписания
        }

        private void _iceCast_ConnectionRestored()
        {
            _skipToNextTrack = true;
        }

        // Получение текущего активного плейлиста
        private Playlist? GetCurrentPlaylist()
        {
            return _config.ScheduleEnable                // Если расписание включено
                ? _scheduleManager.CurrentPlaylist       // Берем плейлист из расписания
                : _fallbackPlaylist;                     // Иначе берем резервный
        }

        // Получение следующего трека для воспроизведения
        private string? GetNextTrackFromPlaylist()
        {
            return _config.ScheduleEnable                // Если расписание включено
                ? _scheduleManager.GetNextTrack()        // Берем трек из расписания
                : _fallbackPlaylist?.GetRandomTrack();   // Иначе случайный из резервного
        }

        // Запуск сервиса
        public void Start()
        {
            if (_isRunning)                              // Если уже работает
                return;                                  // Выход

            Logger.Info("[RadioService] Запуск...");     // Лог: запуск

            _isRunning = true;                           // Устанавливаем флаг работы
            _isPaused = false;                           // Снимаем флаг паузы

            // Инициализируем аудио систему
            _player.Initialize();                        // Инициализация проигрывателя

            // Инициализируем IceCast
            _iceCast.Initialize(_player.Mixer);          // Передаем микшер в IceCast

            // Запускаем поток воспроизведения
            _playbackThread = new Thread(PlaybackLoop);  // Создаем поток для цикла воспроизведения
            _playbackThread.Start();                     // Запускаем поток
        }

        // Остановка сервиса
        public void Stop()
        {
            if (!_isRunning)                             // Если не работает
                return;                                  // Выход

            Logger.Info("[RadioService] Остановка радио сервиса...");   // Лог: остановка

            _isRunning = false;                          // Сбрасываем флаг работы
            _playbackThread?.Join(3000);                 // Ждем завершения потока (3 секунды)

            _player.Stop();                              // Останавливаем проигрыватель
            _iceCast.Dispose();                          // Освобождаем ресурсы IceCast

            Logger.Info("[RadioService] Радио сервис остановлен");      // Лог: сервис остановлен
        }

        // Основной цикл воспроизведения
        private void PlaybackLoop()
        {
            Logger.Info("[RadioService] Цикл воспроизведения запущен"); // Лог: цикл запущен
            //DateTime trackStartTime = DateTime.Now;      // Время начала загрузки трека

            while (_isRunning)                           // Пока сервис работает
            {
                try
                {
                    Logger.Debug("[RadioService] Начало цикла воспроизведения"); // Лог: начало цикла

                    if (_isPaused)                       // Если на паузе
                    {
                        Thread.Sleep(1000);              // Ждем 1 секунду
                        continue;                        // Переход к следующей итерации
                    }

                    // Шаг 0: Проверка расписания перед любым треком
                    Logger.Debug("[RadioService] Проверка расписания..."); // Лог: проверка расписания
                    _scheduleManager.CheckAndUpdatePlaylist(); // Обновление плейлиста по расписанию

                    // Проверяем, нужно ли играть джингл перед треком
                    if (_config.JinglesEnable && _jingleService.ShouldPlayJingle())
                    {
                        // ВЫЗОВ ПЕРЕМЕЩЕННОГО МЕТОДА ИЗ JingleService
                        bool jinglePlayed = _jingleService.PlayJingle(_player, _iceCast);

                        if (jinglePlayed)
                        {
                            // Ждем окончания джингла
                            WaitForTrackEnd();
                            Logger.Debug("[RadioService] Джингл завершен, переход к треку");
                        }
                    }

                    // 1. Получаем следующий трек
                    string? trackFile = GetNextTrackFromPlaylist(); // Получаем путь к следующему треку

                    if (string.IsNullOrEmpty(trackFile)) // Если трек не найден
                    {
                        Logger.Error("[RadioService] Нет доступных треков. Ожидание..."); // Лог: ошибка
                        Thread.Sleep(500);               // Ждем 0.5 секунды
                        continue;                        // Переход к следующей итерации
                    }

                    Logger.Debug($"[RadioService] Выбран трек: {Path.GetFileName(trackFile)}"); // Лог: выбранный трек

                    // 2. Проверяем подключение к IceCast перед воспроизведением
                    if (!_iceCast.IsConnected)           // Если IceCast не подключен
                    {
                        Logger.Warning("[RadioService] IceCast не подключен"); // Лог: предупреждение
                    }

                    // 2.5. Воспроизводим трек
                    Logger.Debug("[RadioService] Начало воспроизведения трека..."); // Лог: начало воспроизведения
                    //trackStartTime = DateTime.Now;       // Засекаем время начала загрузки
                    _currentTrack = _player.PlayTrackWithJingleIfSlow(trackFile); // Воспроизведение трека

                    if (_currentTrack == null)           // Если не удалось воспроизвести
                    {
                        Logger.Error("[RadioService] Не удалось воспроизвести трек. Пропускаю..."); // Лог: ошибка
                        Thread.Sleep(500);               // Ждем 0.5 секунды
                        continue;                        // Переход к следующей итерации
                    }

                    //TimeSpan loadTime = DateTime.Now - trackStartTime; // Время загрузки трека
                    //Logger.Info($"[Производительность] Трек загружен за {loadTime.TotalMilliseconds:F0}мс"); // Лог: производительность

                    _trackStartTime = DateTime.Now;      // Сохраняем время начала воспроизведения

                    // 3. Устанавливаем метаданные в поток IceCast
                    if (_currentTrack != null)           // Если есть текущий трек
                    {
                        try
                        {
                            _iceCast.SetMetadata(_currentTrack.Artist, _currentTrack.Title); // Отправка метаданных
                        }
                        catch (Exception ex)              // Обработка ошибок
                        {
                            Logger.Error($"[RadioService] Ошибка метаданных: {ex.Message}"); // Лог: ошибка
                        }
                    }

                    // 4. Отправляем информацию на внешний сервер
                    if (_config.MyServerEnabled && _currentTrack != null) // Если внешний сервер включен и есть трек
                    {
                        var currentPlaylist = GetCurrentPlaylist(); // Получаем текущий плейлист
                        _myServer.SendTrackInfo(           // Отправка информации о треке
                            (currentPlaylist?.CurrentIndex + 1) ?? 0, // Номер трека (начинается с 1)
                            _currentTrack.Artist,          // Исполнитель
                            _currentTrack.Title,           // Название трека
                            Path.GetFileName(trackFile)    // Имя файла
                        );
                    }

                    // 5. Ждем окончания трека
                    Logger.Debug("[RadioService] Ожидание окончания трека..."); // Лог: ожидание окончания
                    WaitForTrackEnd();                    // Ожидание завершения трека

                    // Увеличиваем счетчик треков
                    _jingleService.IncrementTrackCounter();

                    Logger.Debug($"[RadioService] Трек завершен, счетчик: {_jingleService.GetTrackCounter()}");
                }
                catch (Exception ex)                       // Общая обработка ошибок
                {
                    Logger.Error($"[RadioService] Ошибка в цикле воспроизведения: {ex.Message}"); // Лог: ошибка
                    Thread.Sleep(2000);                   // Пауза 5 секунд перед повторной попыткой
                }
            }

            Logger.Info("[RadioService] Цикл воспроизведения завершен"); // Лог: цикл завершен
        }

        // Ожидание завершения текущего трека
        private void WaitForTrackEnd()
        {
            while (_player.IsPlaying && _isRunning && !_isPaused && !_skipToNextTrack)
            {
                Thread.Sleep(1000);
            }
            if (_skipToNextTrack)
            {
                Logger.Debug("[RadioService] Прерывание воспроизведения по флагу skip");
                _skipToNextTrack = false;
            }
        }
    }
}