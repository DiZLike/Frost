using FrostWire.App;
using FrostWire.Audio;
using FrostWire.Broadcast;
using FrostWire.Core;
using System.Collections.Concurrent;

namespace FrostWire.Services
{
    public class RadioService
    {
        private readonly AppConfig _config;               // Конфигурация приложения
        private readonly Player _player;                  // Проигрыватель аудио
        private readonly ScheduleManager _scheduleManager;// Менеджер расписания
        private readonly Playlist? _fallbackPlaylist;     // Резервный плейлист (если расписание отключено)
        private readonly MultiCastClient _multiCast;
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

        // История последних исполнителей и жанров (для предотвращения повторений)
        private readonly ConcurrentQueue<string> _lastArtists = new();
        private readonly ConcurrentQueue<string> _lastGenres = new();
        private const int MAX_ARTIST_HISTORY = 10;         // Храним последних 5 исполнителей

        public RadioService(AppConfig config)
        {
            _config = config;                             // Сохраняем конфигурацию

            Logger.Info("[RadioService] Инициализация радио сервиса...");// Лог: начало инициализации

            // Инициализация компонентов
            _jingleService = new JingleService(config);   // Создаем сервис джинглов
            _player = new Player(config, _jingleService); // Создаем проигрыватель
            _multiCast = new MultiCastClient(config);
            _myServer = new MyServerClient(config);       // Создаем клиент внешнего сервера
            _multiCast.ConnectionRestored += multiCast_ConnectionRestored;

            _scheduleManager = new ScheduleManager(config);// Создаем менеджер расписания

            // Резервный плейлист (если расписание отключено или не настроено)
            if (!config.Playlist.ScheduleEnable || _scheduleManager.CurrentPlaylist == null)
            {
                _fallbackPlaylist = new Playlist(         // Создаем резервный плейлист
                    config.Playlist.PlaylistFile,
                    config.Playlist.SavePlaylistHistory,
                    config.Playlist.DynamicPlaylist
                );
                Logger.Info($"[RadioService] Резервный плейлист: {_fallbackPlaylist.TotalTracks} треков"); // Лог: информация о плейлисте
            }

            Logger.Info($"[RadioService] Расписание включено: {config.Playlist.ScheduleEnable}"); // Лог: статус расписания
        }

        private void multiCast_ConnectionRestored(string str)
        {
            _skipToNextTrack = true;
        }

        // Получение текущего активного плейлиста
        private Playlist? GetCurrentPlaylist()
        {
            return _config.Playlist.ScheduleEnable                // Если расписание включено
                ? _scheduleManager.CurrentPlaylist       // Берем плейлист из расписания
                : _fallbackPlaylist;                     // Иначе берем резервный
        }
        // Получение следующего трека для воспроизведения
        private string? GetNextTrackFromPlaylist()
        {
            return _config.Playlist.ScheduleEnable                // Если расписание включено
                ? _scheduleManager.GetNextTrack()        // Берем трек из расписания
                : _fallbackPlaylist?.GetRandomTrack();   // Иначе случайный из резервного
        }
        // Получение случайного трека с проверкой на повтор исполнителя и жанра
        private string? GetRandomTrackWithCheck()
        {
            if (_fallbackPlaylist == null)
                return null;

            string? selectedTrack = null;
            string? selectedArtist = null;
            string? selectedGenre = null;
            int attempts = 0;
            const int MAX_ATTEMPTS = 5; // Максимальное количество попыток найти трек с другим исполнителем

            do
            {
                // Получаем случайный трек из плейлиста
                selectedTrack = GetNextTrackFromPlaylist();

                if (string.IsNullOrEmpty(selectedTrack))
                {
                    Logger.Warning("[RadioService] Не удалось получить трек из плейлиста");
                    return null;
                }

                // Пытаемся загрузить теги для проверки исполнителя ДО воспроизведения
                try
                {
                    var tagInfo = _player.GetTrackTags(selectedTrack);
                    if (tagInfo != null)
                    {
                        selectedArtist = !string.IsNullOrWhiteSpace(tagInfo.artist)
                            ? tagInfo.artist
                            : "Other";
                        selectedGenre = !string.IsNullOrWhiteSpace(tagInfo.genre)
                            ? tagInfo.genre
                            : "Other";

                        if (selectedArtist == "Other" || selectedGenre == "Other")
                        {
                            // Определяем, что именно неизвестно
                            bool isArtistUnknown = selectedArtist == "Other";
                            bool isGenreUnknown = selectedGenre == "Other";

                            // Если неизвестно и то и другое, используем трек как есть
                            if (isArtistUnknown && isGenreUnknown)
                            {
                                Logger.Info($"[RadioService] Используем трек с неизвестным исполнителем и жанром: {Path.GetFileName(selectedTrack)}");
                                break;
                            }

                            // Если неизвестен только исполнитель - проверяем только жанр
                            if (isArtistUnknown)
                            {
                                bool isGenreRepeated = _lastGenres.Contains(selectedGenre);

                                if (attempts < MAX_ATTEMPTS - 1 && isGenreRepeated)
                                {
                                    Logger.Info($"[RadioService] Пропускаем трек {Path.GetFileName(selectedTrack)} - " +
                                              $"жанр '{selectedGenre}' был недавно (исполнитель неизвестен, попытка {attempts + 1})");
                                    attempts++;
                                    continue;
                                }
                                else
                                {
                                    // Подходящий трек с неизвестным исполнителем, но уникальным жанром
                                    AddGenreToHistory(selectedGenre);
                                    break;
                                }
                            }

                            // Если неизвестен только жанр - проверяем только исполнителя
                            if (isGenreUnknown)
                            {
                                bool isArtistRepeated = _lastArtists.Contains(selectedArtist);

                                if (attempts < MAX_ATTEMPTS - 1 && isArtistRepeated)
                                {
                                    Logger.Info($"[RadioService] Пропускаем трек {Path.GetFileName(selectedTrack)} - " +
                                              $"исполнитель '{selectedArtist}' был недавно (жанр неизвестен, попытка {attempts + 1})");
                                    attempts++;
                                    continue;
                                }
                                else
                                {
                                    // Подходящий трек с неизвестным жанром, но уникальным исполнителем
                                    AddArtistToHistory(selectedArtist);
                                    break;
                                }
                            }
                        }

                        // Если оба известны - проверяем и исполнителя, и жанр
                        bool isArtistRepeatedFull = _lastArtists.Contains(selectedArtist);
                        bool isGenreRepeatedFull = _lastGenres.Contains(selectedGenre);

                        if (attempts < MAX_ATTEMPTS - 1 && (isArtistRepeatedFull || isGenreRepeatedFull))
                        {
                            Logger.Info($"[RadioService] Пропускаем трек {Path.GetFileName(selectedTrack)} - " +
                                      $"исполнитель или жанр был недавно: {selectedArtist}, {selectedGenre} (попытка {attempts + 1})");
                            attempts++;
                            continue;
                        }
                        else
                        {
                            // Нашли подходящий трек с известными исполнителем и жанром
                            AddArtistToHistory(selectedArtist);
                            AddGenreToHistory(selectedGenre);
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"[RadioService] Ошибка при проверке тегов трека {selectedTrack}: {ex.Message}");
                    selectedArtist = "Other";
                    selectedGenre = "Other";
                    break;
                }

            } while (attempts < MAX_ATTEMPTS);

            if (attempts >= MAX_ATTEMPTS)
            {
                Logger.Warning($"[RadioService] Не удалось найти трек с другим исполнителем или жанром после {MAX_ATTEMPTS} попыток. " +
                              $"Использую последний найденный: {Path.GetFileName(selectedTrack)}");
            }

            return selectedTrack;
        }

        // Добавление исполнителя в историю
        private void AddArtistToHistory(string artist)
        {
            _lastArtists.Enqueue(artist);

            // Поддерживаем максимальный размер очереди
            while (_lastArtists.Count > MAX_ARTIST_HISTORY)
            {
                _lastArtists.TryDequeue(out _);
            }

            Logger.Debug($"[RadioService] Исполнитель добавлен в историю: {artist} (всего: {_lastArtists.Count})");
        }
        private void AddGenreToHistory(string genre)
        {
            _lastGenres.Enqueue(genre);

            // Поддерживаем максимальный размер очереди
            while (_lastGenres.Count > MAX_ARTIST_HISTORY)
            {
                _lastGenres.TryDequeue(out _);
            }

            Logger.Debug($"[RadioService] Жанр добавлен в историю: {genre} (всего: {_lastGenres.Count})");
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
            _multiCast.Initialize(_player.Mixer);

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
            _multiCast.Dispose();                        // Освобождаем ресурсы IceCast

            Logger.Info("[RadioService] Радио сервис остановлен");      // Лог: сервис остановлен
        }

        // Основной цикл воспроизведения
        private void PlaybackLoop()
        {
            Logger.Info("[RadioService] Цикл воспроизведения запущен"); // Лог: цикл запущен

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
                    if (_config.Jingles.JinglesEnable && _jingleService.ShouldPlayJingle())
                    {
                        // ВЫЗОВ ПЕРЕМЕЩЕННОГО МЕТОДА ИЗ JingleService
                        bool jinglePlayed = _jingleService.PlayJingle(_player, _multiCast);

                        if (jinglePlayed)
                        {
                            // Ждем окончания джингла
                            WaitForTrackEnd();
                            Logger.Debug("[RadioService] Джингл завершен, переход к треку");
                        }
                    }

                    // 1. Получаем следующий трек с проверкой исполнителя
                    string? trackFile = GetRandomTrackWithCheck();

                    if (string.IsNullOrEmpty(trackFile)) // Если трек не найден
                    {
                        Logger.Error("[RadioService] Нет доступных треков. Ожидание..."); // Лог: ошибка
                        Thread.Sleep(500);               // Ждем 0.5 секунды
                        continue;                        // Переход к следующей итерации
                    }

                    Logger.Info($"[RadioService] Выбран трек: {Path.GetFileName(trackFile)}"); // Лог: выбранный трек

                    // 2. Проверяем подключение к IceCast перед воспроизведением
                    if (!_multiCast.IsConnected)           // Если IceCast не подключен
                    {
                        Logger.Warning("[RadioService] IceCast не подключен"); // Лог: предупреждение
                    }

                    // 3. Воспроизводим трек (исполнитель уже проверен)
                    Logger.Debug("[RadioService] Начало воспроизведения трека..."); // Лог: начало воспроизведения
                    _currentTrack = _player.PlayTrackWithJingleIfSlow(trackFile);

                    if (_currentTrack == null)           // Если не удалось воспроизвести
                    {
                        Logger.Error("[RadioService] Не удалось воспроизвести трек. Пропускаю..."); // Лог: ошибка
                        Thread.Sleep(500);               // Ждем 0.5 секунды
                        continue;                        // Переход к следующей итерации
                    }

                    _trackStartTime = DateTime.Now;      // Сохраняем время начала воспроизведения

                    // 4. Устанавливаем метаданные в поток IceCast
                    if (_currentTrack != null)           // Если есть текущий трек
                    {
                        try
                        {
                            _multiCast.SetMetadata(_currentTrack.Artist, _currentTrack.Title); // Отправка метаданных
                        }
                        catch (Exception ex)              // Обработка ошибок
                        {
                            Logger.Error($"[RadioService] Ошибка метаданных: {ex.Message}"); // Лог: ошибка
                        }
                    }

                    // 5. Отправляем информацию на внешний сервер
                    if (_config.MyServer.MyServerEnabled && _currentTrack != null) // Если внешний сервер включен и есть трек
                    {
                        var currentPlaylist = GetCurrentPlaylist(); // Получаем текущий плейлист
                        _myServer.SendTrackInfo(           // Отправка информации о треке
                            (currentPlaylist?.CurrentIndex + 1) ?? 0, // Номер трека (начинается с 1)
                            _currentTrack.Artist,          // Исполнитель
                            _currentTrack.Title,           // Название трека
                            trackFile    // Имя файла
                        );
                    }

                    // 6. Ждем окончания трека
                    Logger.Debug("[RadioService] Ожидание окончания трека..."); // Лог: ожидание окончания
                    WaitForTrackEnd();                    // Ожидание завершения трека

                    // Увеличиваем счетчик треков
                    _jingleService.IncrementTrackCounter();

                    Logger.Debug($"[RadioService] Трек завершен, счетчик: {_jingleService.GetTrackCounter()}");
                }
                catch (Exception ex)                       // Общая обработка ошибок
                {
                    Logger.Error($"[RadioService] Ошибка в цикле воспроизведения: {ex.Message}"); // Лог: ошибка
                    Thread.Sleep(2000);                   // Пауза 2 секунды перед повторной попыткой
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