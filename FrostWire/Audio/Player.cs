using FrostWire.App;
using FrostWire.Audio.FX;
using FrostWire.Core;
using FrostWire.Services;
using System.Diagnostics;
using Un4seen.Bass.AddOn.Tags;

namespace FrostWire.Audio
{
    public class Player : IDisposable
    {
        private readonly AppConfig _config;
        private readonly JingleService _jingleService;
        private readonly BassAudioEngine _audioEngine;
        private readonly Mixer _mixer;
        private readonly FXManager _fx;
        private readonly TrackLoader _trackLoader;
        private readonly JinglePlayerSlowLoad _jinglePlayer;

        private int _currentStream;
        private bool _isDisposed = false;
        private CancellationTokenSource _silenceCts;

        public bool IsPlaying => _audioEngine.IsStreamPlaying(_currentStream);
        public Mixer Mixer => _mixer;

        public Player(AppConfig config, JingleService jingleService)
        {
            _config = config;
            _jingleService = jingleService;

            _audioEngine = new BassAudioEngine(config);
            _mixer = new Mixer(_config.Audio.SampleRate);
            _fx = new FXManager(_mixer.Handle, _config);
            _trackLoader = new TrackLoader(_audioEngine, _fx);
            _jinglePlayer = new JinglePlayerSlowLoad(config, jingleService, _audioEngine, _mixer, _fx);

            Logger.Debug("[Player] Инициализирован с модульной архитектурой");
        }

        public void Initialize()
        {
            _audioEngine.Initialize();
            _audioEngine.TrackPositionChanged += _audioEngine_TrackPositionChanged;
        }

        private void _audioEngine_TrackPositionChanged(double arg1, double arg2)
        {
            _fx.FadeStart(arg1, arg2);
        }

        public TrackInfo PlayTrack(string filePath)
        {
            StopCurrentTrack();

            var loadedTrack = _trackLoader.LoadTrack(filePath);
            if (loadedTrack == null || loadedTrack.StreamHandle == 0)
                return null;

            _currentStream = loadedTrack.StreamHandle;
            _mixer.AddStream(_currentStream);
            _audioEngine.PlayStream(_mixer.Handle);

            Logger.Info($"[Player] Сейчас играет: {loadedTrack.TrackInfo.Artist} - {loadedTrack.TrackInfo.Title}");

            return loadedTrack.TrackInfo;
        }

        public TrackInfo PlayTrackWithJingleIfSlow(string filePath)
        {
            if (!_audioEngine.IsInitialized)
                throw new InvalidOperationException("Аудиосистема не инициализирована");

            TrackInfo trackInfo = null;
            int trackStream = 0;
            bool jinglePlayed = false;
            List<string> playedJingles = new();

            using (ManualResetEvent trackLoadedEvent = new ManualResetEvent(false))
            using (CancellationTokenSource jingleCts = new CancellationTokenSource())
            using (ManualResetEvent jingleCycleFinishedEvent = new ManualResetEvent(false))
            {
                Thread loadThread = null;
                Thread jingleThread = null;

                try
                {
                    // 1. Запускаем загрузку трека в отдельном потоке
                    loadThread = new Thread(() => LoadTrackInBackground(filePath, trackLoadedEvent, ref trackInfo, ref trackStream));
                    loadThread.IsBackground = true;
                    loadThread.Start();

                    // 2. Ждем 2 секунды
                    bool loadedInTime = trackLoadedEvent.WaitOne(TimeSpan.FromSeconds(2));

                    // 3. Если медленно и есть джинглы - запускаем цикл джинглов
                    if (!loadedInTime && _config.Jingles.JinglesEnable && _jingleService.HasJingles)
                    {
                        jinglePlayed = true;

                        jingleThread = new Thread(() =>
                        {
                            _jinglePlayer.PlayJingleCycleWhileLoading(trackLoadedEvent, jingleCts, out playedJingles, filePath);
                            jingleCycleFinishedEvent.Set();
                        });

                        jingleThread.IsBackground = true;
                        jingleThread.Start();

                        // 4. Ждем загрузки трека
                        trackLoadedEvent.WaitOne();

                        // 5. Отменяем будущие джинглы
                        jingleCts.Cancel();

                        // 6. Ждем завершения текущего джингла
                        jingleCycleFinishedEvent.WaitOne(TimeSpan.FromSeconds(30));
                    }
                    else if (!loadedInTime)
                    {
                        // Просто ждем загрузки если джинглы отключены
                        trackLoadedEvent.WaitOne();
                    }

                    // 7. Воспроизводим загруженный трек
                    if (trackInfo != null && trackStream != 0)
                    {
                        return PlayLoadedTrack(trackInfo, trackStream, jinglePlayed);
                    }

                    return null;
                }
                finally
                {
                    // Гарантированная очистка
                    jingleCts.Cancel();
                    _jinglePlayer.CleanupCurrentJingle();

                    WaitForThread(loadThread, "загрузки трека", 5);
                    WaitForThread(jingleThread, "джинглов", 2);
                }
            }
        }

        private void LoadTrackInBackground(string filePath, ManualResetEvent trackLoadedEvent,
                                          ref TrackInfo trackInfo, ref int trackStream)
        {
            try
            {
                StopCurrentTrack();
                var loadedTrack = _trackLoader.LoadTrack(filePath);

                if (loadedTrack != null)
                {
                    trackInfo = loadedTrack.TrackInfo;
                    trackStream = loadedTrack.StreamHandle;
                }
            }
            finally
            {
                trackLoadedEvent.Set();
            }
        }

        private TrackInfo PlayLoadedTrack(TrackInfo trackInfo, int streamHandle, bool cleanupJingle)
        {
            Logger.Info($"[Player] Воспроизведение трека: {trackInfo.Artist} - {trackInfo.Title}");

            StopCurrentTrack();
            _currentStream = streamHandle;

            if (cleanupJingle)
            {
                _jinglePlayer.CleanupCurrentJingle();
            }

            _mixer.AddStream(_currentStream);
            _audioEngine.PlayStream(_mixer.Handle);

            return trackInfo;
        }

        private void WaitForThread(Thread thread, string threadName, int seconds)
        {
            if (thread != null && thread.IsAlive && !thread.Join(TimeSpan.FromSeconds(seconds)))
            {
                Logger.Warning($"[Player] Поток {threadName} не завершился за {seconds} секунд");
            }
        }
        public TAG_INFO? GetTrackTags(string filePath)
        {
            try
            {
                return _audioEngine.GetTrackTags(filePath);
            }
            catch (Exception ex)
            {
                Logger.Error($"[Player] Ошибка при получении тегов: {ex.Message}");
                return null;
            }
        }
        public void StopCurrentTrack()
        {
            if (_currentStream != 0)
            {
                _audioEngine.StopStream(_currentStream);
                _mixer.RemoveStream(_currentStream);
                _audioEngine.FreeStream(_currentStream);
                _currentStream = 0;
            }
        }

        public void Stop()
        {
            StopCurrentTrack();
            _jinglePlayer.CleanupCurrentJingle();
            _silenceCts?.Cancel();
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                Stop();
                _silenceCts?.Dispose();
                _audioEngine.Dispose();
                _isDisposed = true;
            }
        }
    }
}