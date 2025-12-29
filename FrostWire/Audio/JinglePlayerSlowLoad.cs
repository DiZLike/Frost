using Strimer.App;
using Strimer.Core;
using Strimer.Services;
using System.Diagnostics;

namespace Strimer.Audio
{
    public class JinglePlayerSlowLoad
    {
        private readonly AppConfig _config;
        private readonly JingleService _jingleService;
        private readonly BassAudioEngine _audioEngine;
        private readonly Mixer _mixer;
        private readonly ReplayGain _replayGain;

        private int _currentJingleStream = 0;
        private readonly object _jingleLock = new();

        public JinglePlayerSlowLoad(AppConfig config, JingleService jingleService,
                           BassAudioEngine audioEngine, Mixer mixer, ReplayGain replayGain)
        {
            _config = config;
            _jingleService = jingleService;
            _audioEngine = audioEngine;
            _mixer = mixer;
            _replayGain = replayGain;
        }

        public bool PlayJingleCycleWhileLoading(ManualResetEvent trackLoadedEvent,
                                       CancellationTokenSource jingleCts,
                                       out List<string> playedJingles,
                                       string filePath)
        {
            Logger.Info($"[JinglePlayer] Трек '{Path.GetFileName(filePath)}' грузится дольше 2 секунд, запускаю джинглы...");
            playedJingles = new List<string>();

            if (!_config.JinglesEnable || !_jingleService.HasJingles)
            {
                Logger.Debug("[JinglePlayer] Джинглы отключены или недоступны");
                return false;
            }

            var jingleStopwatch = Stopwatch.StartNew();
            try
            {
                while (!jingleCts.Token.IsCancellationRequested)
                {
                    // Получаем следующий джингл
                    string jingleFile = _config.JinglesRandom ? _jingleService.GetRandomJingle() : _jingleService.GetNextJingle();

                    if (string.IsNullOrEmpty(jingleFile))
                    {
                        Logger.Warning("[JinglePlayer] Не удалось получить джингл");
                        Thread.Sleep(10);
                        continue;
                    }

                    Logger.Info($"[JinglePlayer] Воспроизведение джингла: {Path.GetFileName(jingleFile)}");
                    playedJingles.Add(Path.GetFileName(jingleFile));

                    if (!PlaySingleJingle(jingleFile, trackLoadedEvent, jingleCts.Token))
                    {
                        break;
                    }
                }
                return playedJingles.Count > 0;
            }
            catch (OperationCanceledException)
            {
                Logger.Debug("[JinglePlayer] Цикл джинглов отменен");
                return playedJingles.Count > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"[JinglePlayer] Ошибка в цикле джинглов: {ex.Message}");
                return playedJingles.Count > 0;
            }
            finally
            {
                jingleStopwatch.Stop();
                Logger.Debug($"[JinglePlayer] Джингл(ы) воспроизведены за {jingleStopwatch.ElapsedMilliseconds} мс");
                CleanupCurrentJingle();
            }
        }

        private bool PlaySingleJingle(string jingleFile, ManualResetEvent trackLoadedEvent,
                                     CancellationToken cancellationToken)
        {
            // Загружаем джингл
            int jingleStream = _audioEngine.CreateStreamFromFile(jingleFile);
            if (jingleStream == 0)
            {
                Logger.Warning($"[JinglePlayer] Не удалось загрузить джингл: {Path.GetFileName(jingleFile)}");
                return true; // Продолжаем с другим джинглом
            }

            lock (_jingleLock)
            {
                _currentJingleStream = jingleStream;
            }

            try
            {
                // Настраиваем джингл
                var jingleTagInfo = _audioEngine.GetTrackTags(jingleFile);
                if (jingleTagInfo != null)
                {
                    _replayGain.SetGain(jingleTagInfo);
                }

                // Добавляем в микшер и воспроизводим
                _mixer.AddStream(jingleStream);
                _audioEngine.PlayStream(_mixer.Handle);

                // Воспроизводим до конца
                while (true)
                {

                    // Проверяем состояние джингла
                    if (!_audioEngine.IsStreamPlaying(jingleStream))
                    {
                        break; // Джингл закончился
                    }

                    Thread.Sleep(50);
                }

                Logger.Debug($"[JinglePlayer] Джингл {Path.GetFileName(jingleFile)} завершил воспроизведение");
                return true; // Продолжаем цикл
            }
            finally
            {
                CleanupJingleStream(jingleStream);
            }
        }

        private void CleanupJingleStream(int streamHandle)
        {
            if (streamHandle != 0)
            {
                _mixer.RemoveStream(streamHandle);
                _audioEngine.FreeStream(streamHandle);

                lock (_jingleLock)
                {
                    if (_currentJingleStream == streamHandle)
                    {
                        _currentJingleStream = 0;
                    }
                }
            }
        }

        public void CleanupCurrentJingle()
        {
            lock (_jingleLock)
            {
                if (_currentJingleStream != 0)
                {
                    CleanupJingleStream(_currentJingleStream);
                }
            }
        }
    }
}