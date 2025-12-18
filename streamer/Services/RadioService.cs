using Strimer.App;
using Strimer.Audio;
using Strimer.Broadcast;
using Strimer.Core;

namespace Strimer.Services
{
    public class RadioService
    {
        private readonly AppConfig _config;
        private readonly Player _player;
        private readonly ScheduleManager _scheduleManager;
        private readonly Playlist? _fallbackPlaylist;
        private readonly IceCastClient _iceCast;
        private readonly MyServerClient _myServer;

        private Thread _playbackThread;
        private bool _isRunning;
        private bool _isPaused;

        // Текущая информация о треке
        private TrackInfo? _currentTrack;
        private DateTime _trackStartTime;

        public bool IsRunning => _isRunning;
        public bool IsPaused => _isPaused;
        public TrackInfo? CurrentTrack => _currentTrack;
        public IceCastClient IceCast => _iceCast;

        public RadioService(AppConfig config)
        {
            _config = config;

            Logger.Info("Инициализация радио сервиса...");

            // Инициализация компонентов
            _player = new Player(config);
            _iceCast = new IceCastClient(config);
            _myServer = new MyServerClient(config);

            // Инициализация менеджера расписания
            _scheduleManager = new ScheduleManager(config);

            // Резервный плейлист (если расписание отключено или не настроено)
            if (!config.ScheduleEnable || _scheduleManager.CurrentPlaylist == null)
            {
                _fallbackPlaylist = new Playlist(
                    config.PlaylistFile,
                    config.SavePlaylistHistory,
                    config.DynamicPlaylist
                );
                Logger.Info($"Резервный плейлист загружен: {_fallbackPlaylist.TotalTracks} треков");
            }

            Logger.Info($"Расписание включено: {config.ScheduleEnable}");
        }

        private Playlist? GetCurrentPlaylist()
        {
            if (_config.ScheduleEnable)
            {
                return _scheduleManager.CurrentPlaylist;
            }
            else
            {
                return _fallbackPlaylist;
            }
        }

        private string? GetNextTrackFromPlaylist()
        {
            if (_config.ScheduleEnable)
            {
                return _scheduleManager.GetNextTrack();
            }
            else if (_fallbackPlaylist != null)
            {
                return _fallbackPlaylist.GetRandomTrack();
            }

            return null;
        }

        public void Start()
        {
            if (_isRunning)
                return;

            Logger.Info("[RadioService] Запуск...");

            _isRunning = true;
            _isPaused = false;

            // Инициализируем аудио систему
            _player.Initialize();

            // Инициализируем IceCast
            _iceCast.Initialize(_player.Mixer);

            // Запускаем поток воспроизведения
            _playbackThread = new Thread(PlaybackLoop);
            _playbackThread.Start();
        }

        public void Stop()
        {
            if (!_isRunning)
                return;

            Logger.Info("Остановка радио сервиса...");

            _isRunning = false;
            _playbackThread?.Join(3000); // Ждем завершения потока

            _player.Stop();
            _iceCast.Dispose();

            Logger.Info("Радио сервис остановлен");
        }

        private void PlaybackLoop()
        {
            Logger.Info("Цикл воспроизведения запущен");
            DateTime trackStartTime = DateTime.Now;

            while (_isRunning)
            {
                try
                {
                    if (_isPaused)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    // Шаг 0: Обязательная проверка расписания перед любым треком
                    trackStartTime = DateTime.Now;
                    _scheduleManager.CheckAndUpdatePlaylist();

                    // 1. Получаем следующий трек
                    string? trackFile = GetNextTrackFromPlaylist();

                    if (string.IsNullOrEmpty(trackFile))
                    {
                        Logger.Error("Нет доступных треков. Ожидание...");
                        Thread.Sleep(500);
                        continue;
                    }

                    // 2. Воспроизводим трек
                    //_currentTrack = _player.PlayTrack(trackFile);
                    _currentTrack = _player.PlayTrackWithSilence(trackFile);

                    if (_currentTrack == null)
                    {
                        Logger.Error("Не удалось воспроизвести трек. Пропускаю...");
                        Thread.Sleep(500);
                        continue;
                    }
                    TimeSpan loadTime = DateTime.Now - trackStartTime;
                    Logger.Info($"[Производительность] Трек загружен за {loadTime.TotalMilliseconds:F0}мс");

                    _trackStartTime = DateTime.Now;

                    // 3. Устанавливаем метаданные в поток IceCast
                    _iceCast.SetMetadata(_currentTrack.Artist, _currentTrack.Title);

                    // 4. Отправляем информацию на внешний сервер
                    if (_config.MyServerEnabled)
                    {
                        var currentPlaylist = GetCurrentPlaylist();
                        int trackNumber = currentPlaylist?.CurrentIndex + 1 ?? 0;

                        _myServer.SendTrackInfo(
                            trackNumber,
                            _currentTrack.Artist,
                            _currentTrack.Title,
                            Path.GetFileName(trackFile)
                        );
                    }

                    // 5. Ждем окончания трека
                    WaitForTrackEnd();
                }
                catch (Exception ex)
                {
                    Logger.Error($"Ошибка в цикле воспроизведения: {ex.Message}");
                    Thread.Sleep(5000); // Пауза перед повторной попыткой
                }
            }

            Logger.Info("Цикл воспроизведения завершен");
        }

        private void WaitForTrackEnd()
        {
            while (_player.IsPlaying && _isRunning && !_isPaused)
            {
                Thread.Sleep(1000);
            }
        }

        public string GetStatus()
        {
            var currentPlaylist = GetCurrentPlaylist();
            var status = new System.Text.StringBuilder();

            status.AppendLine($"Сервис: {(_isRunning ? "Работает" : "Остановлен")}");
            status.AppendLine($"Воспроизведение: {(_isPaused ? "На паузе" : "Играет")}");
            status.AppendLine($"IceCast: {(_iceCast.IsConnected ? "Подключен" : "Отключен")}");

            if (currentPlaylist != null)
            {
                status.AppendLine($"Плейлист: {currentPlaylist.TotalTracks} треков");
                status.AppendLine($"Текущий: {currentPlaylist.CurrentIndex + 1}/{currentPlaylist.TotalTracks}");
            }

            if (_currentTrack != null)
            {
                status.AppendLine($"Текущий трек: {_currentTrack.Artist} - {_currentTrack.Title}");
                status.AppendLine($"Длительность: {_player.GetCurrentTime()} / {_player.GetTotalTime()}");
            }

            status.AppendLine($"Слушателей: {_iceCast.Listeners} (Пик: {_iceCast.PeakListeners})");

            return status.ToString();
        }

        public string GetStreamInfo()
        {
            var info = new System.Text.StringBuilder();

            info.AppendLine($"Сервер: {_config.IceCastServer}:{_config.IceCastPort}");
            info.AppendLine($"Mount: /{_config.IceCastMount}");
            info.AppendLine($"Битрейт: {_config.OpusBitrate} кбит/с ({_config.OpusMode})");
            info.AppendLine($"Слушателей: {_iceCast.Listeners}");
            info.AppendLine($"Пик: {_iceCast.PeakListeners}");

            if (_currentTrack != null)
            {
                info.AppendLine();
                info.AppendLine($"Текущий трек:");
                info.AppendLine($"  Исполнитель: {_currentTrack.Artist}");
                info.AppendLine($"  Название: {_currentTrack.Title}");
                info.AppendLine($"  Длительность: {_player.GetTotalTime()}");
            }

            return info.ToString();
        }

        public void PlayNextTrack()
        {
            if (_player.IsPlaying)
            {
                _player.StopCurrentTrack();
                Logger.Info("Переход к следующему треку...");
            }
        }

        public void TogglePause()
        {
            _isPaused = !_isPaused;

            if (_isPaused)
            {
                _player.Pause();
                Logger.Info("Воспроизведение приостановлено");
            }
            else
            {
                _player.Resume();
                Logger.Info("Воспроизведение возобновлено");
            }
        }
    }
}