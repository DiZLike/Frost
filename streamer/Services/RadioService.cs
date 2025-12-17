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

            Logger.Info("Initializing Radio Service...");

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
                Logger.Info($"Fallback playlist loaded: {_fallbackPlaylist.TotalTracks} tracks");
            }

            Logger.Info($"Schedule enabled: {config.ScheduleEnable}");
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

            Logger.Info("Starting radio service...");

            _isRunning = true;
            _isPaused = false;

            // Инициализируем аудио систему
            _player.Initialize();

            // Инициализируем IceCast
            _iceCast.Initialize(_player.Mixer);

            // Запускаем поток воспроизведения
            _playbackThread = new Thread(PlaybackLoop);
            _playbackThread.Start();

            Logger.Info("Radio service started successfully");
        }

        public void Stop()
        {
            if (!_isRunning)
                return;

            Logger.Info("Stopping radio service...");

            _isRunning = false;
            _playbackThread?.Join(3000); // Ждем завершения потока

            _player.Stop();
            _iceCast.Dispose();

            Logger.Info("Radio service stopped");
        }

        private void PlaybackLoop()
        {
            Logger.Info("Playback loop started");

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
                    _scheduleManager.CheckAndUpdatePlaylist();

                    // 1. Получаем следующий трек
                    string? trackFile = GetNextTrackFromPlaylist();

                    if (string.IsNullOrEmpty(trackFile))
                    {
                        Logger.Error("No track available. Waiting...");
                        Thread.Sleep(500);
                        continue;
                    }

                    // 2. Воспроизводим трек
                    _currentTrack = _player.PlayTrack(trackFile);

                    if (_currentTrack == null)
                    {
                        Logger.Error("Failed to play track. Skipping...");
                        Thread.Sleep(500);
                        continue;
                    }

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
                    Logger.Error($"Error in playback loop: {ex.Message}");
                    Thread.Sleep(5000); // Пауза перед повторной попыткой
                }
            }

            Logger.Info("Playback loop ended");
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

            status.AppendLine($"Service: {(_isRunning ? "Running" : "Stopped")}");
            status.AppendLine($"Playback: {(_isPaused ? "Paused" : "Playing")}");
            status.AppendLine($"IceCast: {(_iceCast.IsConnected ? "Connected" : "Disconnected")}");

            if (currentPlaylist != null)
            {
                status.AppendLine($"Playlist: {currentPlaylist.TotalTracks} tracks");
                status.AppendLine($"Current: {currentPlaylist.CurrentIndex + 1}/{currentPlaylist.TotalTracks}");
            }

            if (_currentTrack != null)
            {
                status.AppendLine($"Current Track: {_currentTrack.Artist} - {_currentTrack.Title}");
                status.AppendLine($"Duration: {_player.GetCurrentTime()} / {_player.GetTotalTime()}");
            }

            status.AppendLine($"Listeners: {_iceCast.Listeners} (Peak: {_iceCast.PeakListeners})");

            return status.ToString();
        }

        public string GetStreamInfo()
        {
            var info = new System.Text.StringBuilder();

            info.AppendLine($"Server: {_config.IceCastServer}:{_config.IceCastPort}");
            info.AppendLine($"Mount: /{_config.IceCastMount}");
            info.AppendLine($"Bitrate: {_config.OpusBitrate} kbps ({_config.OpusMode})");
            info.AppendLine($"Listeners: {_iceCast.Listeners}");
            info.AppendLine($"Peak: {_iceCast.PeakListeners}");

            if (_currentTrack != null)
            {
                info.AppendLine();
                info.AppendLine($"Current Track:");
                info.AppendLine($"  Artist: {_currentTrack.Artist}");
                info.AppendLine($"  Title: {_currentTrack.Title}");
                info.AppendLine($"  Duration: {_player.GetTotalTime()}");
            }

            return info.ToString();
        }

        public void PlayNextTrack()
        {
            if (_player.IsPlaying)
            {
                _player.StopCurrentTrack();
                Logger.Info("Skipping to next track...");
            }
        }

        public void TogglePause()
        {
            _isPaused = !_isPaused;

            if (_isPaused)
            {
                _player.Pause();
                Logger.Info("Playback paused");
            }
            else
            {
                _player.Resume();
                Logger.Info("Playback resumed");
            }
        }
    }
}