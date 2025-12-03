using Strimer.Audio;
using Strimer.Broadcast;
using Strimer.Core;
using System.Numerics;

namespace Strimer.Services
{
    public class RadioService
    {
        private readonly AppConfig _config;
        private readonly Player _player;
        private readonly Playlist _playlist;
        private readonly IceCastClient _iceCast;
        private readonly MyServerClient _myServer;

        private Thread _playbackThread;
        private bool _isRunning;
        private bool _isPaused;

        // Текущая информация о треке
        private TrackInfo _currentTrack;
        private DateTime _trackStartTime;

        public RadioService(AppConfig config)
        {
            _config = config;

            Logger.Info("Initializing Radio Service...");

            // Инициализация компонентов
            _playlist = new Playlist(config.PlaylistFile, config.SavePlaylistHistory);
            _player = new Player(config);
            _iceCast = new IceCastClient(config);
            _myServer = new MyServerClient(config);

            Logger.Info($"Playlist loaded: {_playlist.TotalTracks} tracks");
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

                    // 1. Получаем следующий трек из плейлиста
                    string trackFile = _playlist.GetRandomTrack();

                    // 2. Воспроизводим трек
                    _currentTrack = _player.PlayTrack(trackFile);

                    if (_currentTrack == null)
                    {
                        Logger.Error("Failed to play track. Skipping...");
                        Thread.Sleep(5000);
                        continue;
                    }

                    _trackStartTime = DateTime.Now;

                    // 3. Устанавливаем метаданные в поток IceCast
                    _iceCast.SetMetadata(_currentTrack.Artist, _currentTrack.Title);

                    // 4. Отправляем информацию на внешний сервер
                    if (_config.MyServerEnabled)
                    {
                        _myServer.SendTrackInfo(
                            _playlist.CurrentIndex + 1,
                            _currentTrack.Artist,
                            _currentTrack.Title,
                            Path.GetFileName(trackFile)
                        );
                    }

                    // 5. Отображаем информацию о треке
                    DisplayTrackInfo();

                    // 6. Ждем окончания трека
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
                // Обновляем отображение времени каждую секунду
                UpdateTimeDisplay();
                Thread.Sleep(1000);
            }
        }

        private void DisplayTrackInfo()
        {
            Console.Clear();
            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║        STRIMER RADIO - LIVE STREAM     ║");
            Console.WriteLine("╠════════════════════════════════════════╣");
            Console.WriteLine($"║ Now Playing: {_currentTrack.Artist,-30}");
            Console.WriteLine($"║            : {_currentTrack.Title,-30}");
            Console.WriteLine("╠════════════════════════════════════════╣");
            Console.WriteLine($"║ Track: {_playlist.CurrentIndex + 1,3}/{_playlist.TotalTracks,-3}" +
                            $"Listeners: {_iceCast.Listeners} (Peak: {_iceCast.PeakListeners})");
            Console.WriteLine("╚════════════════════════════════════════╝");
            Console.WriteLine("\nCommands: [Q]uit [S]tatus [N]ext [P]ause [I]nfo");
        }

        private void UpdateTimeDisplay()
        {
            if (_currentTrack == null)
                return;

            string elapsed = _player.GetCurrentTime();
            string total = _player.GetTotalTime();

            Console.SetCursorPosition(0, 5);
            Console.WriteLine($"║ Time: {elapsed} / {total,-35}");

            // Также обновляем статистику слушателей
            Console.SetCursorPosition(0, 6);
            Console.WriteLine($"║ Track: {_playlist.CurrentIndex + 1,3}/{_playlist.TotalTracks,-3} " +
                            $"Listeners: {_iceCast.Listeners} (Peak: {_iceCast.PeakListeners})");
        }

        public void ShowStatus()
        {
            Console.WriteLine("\n=== STATUS ===");
            Console.WriteLine($"Service: {(_isRunning ? "Running" : "Stopped")}");
            Console.WriteLine($"Playback: {(_isPaused ? "Paused" : "Playing")}");
            Console.WriteLine($"IceCast: {(_iceCast.IsConnected ? "Connected" : "Disconnected")}");
            Console.WriteLine($"Playlist: {_playlist.TotalTracks} tracks");
            Console.WriteLine($"Current: {_playlist.CurrentIndex + 1}/{_playlist.TotalTracks}");
            Console.WriteLine($"Memory: {GC.GetTotalMemory(false) / 1024 / 1024} MB");
            Console.WriteLine("==============");
        }

        public void ShowStreamInfo()
        {
            Console.WriteLine("\n=== STREAM INFO ===");
            Console.WriteLine($"Server: {_config.IceCastServer}:{_config.IceCastPort}");
            Console.WriteLine($"Mount: /{_config.IceCastMount}");
            Console.WriteLine($"Bitrate: {_config.OpusBitrate} kbps ({_config.OpusMode})");
            Console.WriteLine($"Listeners: {_iceCast.Listeners}");
            Console.WriteLine($"Peak: {_iceCast.PeakListeners}");

            if (_currentTrack != null)
            {
                Console.WriteLine($"\nCurrent Track:");
                Console.WriteLine($"  Artist: {_currentTrack.Artist}");
                Console.WriteLine($"  Title: {_currentTrack.Title}");
                Console.WriteLine($"  Duration: {_player.GetTotalTime()}");
            }

            Console.WriteLine("===================");
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

    public class TrackInfo
    {
        public string Artist { get; set; } = "Unknown Artist";
        public string Title { get; set; } = "Unknown Title";
        public string Album { get; set; } = "";
        public int Year { get; set; }
        public string Genre { get; set; } = "";
        public float ReplayGain { get; set; }
        public string Comment { get; set; } = "";
    }
}