using Strimer.App;
using Strimer.Core;
using System.Reflection;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;
using Un4seen.Bass.AddOn.Mix;
using Un4seen.Bass.AddOn.Tags;

namespace Strimer.Audio
{
    public class Player : IDisposable
    {
        private readonly AppConfig _config;
        private Mixer _mixer;
        private ReplayGain _replayGain;

        private int _currentStream;
        private bool _isInitialized;
        private bool _loadCompleted = false;
        private bool _isDisposed = false;

        public bool IsPlaying => _currentStream != 0 &&
            Bass.BASS_ChannelIsActive(_currentStream) == BASSActive.BASS_ACTIVE_PLAYING;

        public bool IsStopped => _currentStream == 0 ||
            Bass.BASS_ChannelIsActive(_currentStream) == BASSActive.BASS_ACTIVE_STOPPED;

        public Mixer Mixer => _mixer;

        public Player(AppConfig config)
        {
            _config = config;
            Logger.Debug($"[Плеер] Инициализирован с конфигурацией: Устройство={_config.AudioDevice}, Частота={_config.SampleRate}");
        }

        public void Initialize()
        {
            if (_isInitialized)
                return;

            Logger.Info("Инициализация аудио системы...");

            // 1. Инициализация BASS
            bool initSuccess = Bass.BASS_Init(
                _config.AudioDevice,
                _config.SampleRate,
                BASSInit.BASS_DEVICE_DEFAULT,
                IntPtr.Zero
            );

            if (!initSuccess)
            {
                var error = Bass.BASS_ErrorGetCode();
                throw new Exception($"Не удалось инициализировать BASS: {error}");
            }

            _mixer = new Mixer(_config.SampleRate);
            _replayGain = new ReplayGain(_config.UseReplayGain, _config.UseCustomGain, _mixer.Handle);
            LoadPlugins();

            _isInitialized = true;
            Logger.Info("Аудио система успешно инициализирована");
        }

        private void LoadPlugins()
        {
            Logger.Info("Загрузка аудио плагинов...");

            string libPrefix = _config.OS == "Windows" ? "" : "lib";
            string libExtension = _config.OS == "Windows" ? ".dll" : ".so";

            // Загружаем плагины для разных форматов
            string[] plugins = { "bassopus", "bassaac", "bassflac", "basswv" };

            List<string> loadedPlugins = new();
            List<string> failedPlugins = new();
            foreach (var plugin in plugins)
            {
                string pluginPath = Path.Combine(
                    _config.BaseDirectory,
                    $"{libPrefix}{plugin}{libExtension}"
                );

                if (File.Exists(pluginPath))
                {
                    int pluginHandle = Bass.BASS_PluginLoad(pluginPath);
                    if (pluginHandle != 0)
                    {
                        loadedPlugins.Add(plugin);
                    }
                    else
                    {
                        failedPlugins.Add(plugin);
                    }
                }
            }
            if (loadedPlugins.Count > 0)
                Logger.Info($"[Плеер] Загруженные плагины: {string.Join(", ", loadedPlugins)}");
            if (failedPlugins.Count > 0)
                Logger.Warning($"[Плеер] Не удалось загрузить плагины: {string.Join(", ", failedPlugins)}");
        }

        public TrackInfo PlayTrack(string filePath)
        {
            // Останавливаем текущий трек
            StopCurrentTrack();

            // Не проверяем больше наличие файла. BASS_StreamCreateFile сам определит
            _currentStream = Bass.BASS_StreamCreateFile(
                filePath,
                0, 0,
                BASSFlag.BASS_DEFAULT | BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE
            );

            if (_currentStream == 0)
            {
                var error = Bass.BASS_ErrorGetCode();
                Logger.Error($"Не удалось создать поток для {Path.GetFileName(filePath)}: {error}");
                return null;
            }

            // Получаем информацию о треке
            var tagInfo = BassTags.BASS_TAG_GetFromFile(filePath);
            var trackInfo = CreateTrackInfo(tagInfo, filePath);

            // Применяем Replay Gain
            _replayGain.SetGain(tagInfo);
            _replayGain.ApplyGain();

            // Добавляем поток в микшер
            _mixer.AddStream(_currentStream);

            // Запускаем воспроизведение через микшер
            Bass.BASS_ChannelPlay(_mixer.Handle, false);

            Logger.Info($"Сейчас играет: {trackInfo.Artist} - {trackInfo.Title}");

            // Логируем ReplayGain значение для отладки
            //Logger.Info($"ReplayGain: {trackInfo.ReplayGain} дБ, UseReplayGain: {_config.UseReplayGain}");

            return trackInfo;
        }

        public TrackInfo PlayTrackWithSilence(string filePath)
        {
            if (_mixer == null)
                throw new ArgumentNullException(nameof(_mixer));

            // Запускаем тишину в отдельном потоке
            var silenceThread = new Thread(() =>
            {
                try
                {
                    Logger.Info("[Плеер] Запуск воспроизведения тишины");

                    // Создаем временный поток тишины
                    using var silence = new SilenceGenerator(_config.SampleRate);

                    // Добавляем в микшер и играем
                    BassMix.BASS_Mixer_StreamAddChannel(_mixer.Handle, silence.Handle, BASSFlag.BASS_DEFAULT);
                    Bass.BASS_ChannelPlay(silence.Handle, true);

                    // Держим поток активным, пока не остановят извне
                    while (!_loadCompleted && !_isDisposed)
                    {
                        Thread.Sleep(100);
                    }

                    // Останавливаем тишину
                    Bass.BASS_ChannelStop(silence.Handle);
                    BassMix.BASS_Mixer_ChannelRemove(silence.Handle);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Ошибка в потоке тишины: {ex.Message}");
                }
            });

            silenceThread.IsBackground = true;
            silenceThread.Start();

            // Даем время тишине запуститься
            Thread.Sleep(100);

            try
            {
                // Загружаем трек (тишина продолжает играть)
                var trackInfo = PlayTrack(filePath);

                // Сигнализируем, что загрузка завершена
                _loadCompleted = true;

                // Даем время тишине остановиться
                Thread.Sleep(200);

                return trackInfo;
            }
            catch (Exception)
            {
                _loadCompleted = true;
                throw;
            }
        }

        private TrackInfo CreateTrackInfo(TAG_INFO tagInfo, string filePath)
        {
            // Пытаемся преобразовать год из строки в число
            int year = 0;
            if (!string.IsNullOrEmpty(tagInfo.year))
            {
                // Убираем нечисловые символы
                string yearStr = new string(tagInfo.year.Where(char.IsDigit).ToArray());
                if (!string.IsNullOrEmpty(yearStr))
                {
                    int.TryParse(yearStr, out year);
                }
            }

            return new TrackInfo
            {
                Artist = !string.IsNullOrWhiteSpace(tagInfo.artist) ? tagInfo.artist : "Неизвестный исполнитель",
                Title = !string.IsNullOrWhiteSpace(tagInfo.title) ? tagInfo.title : Path.GetFileNameWithoutExtension(filePath),
                Album = tagInfo.album ?? "",
                Year = year,
                Genre = tagInfo.genre ?? "",
                ReplayGain = tagInfo.replaygain_track_gain,
                Comment = tagInfo.comment ?? ""
            };
        }

        public void Pause()
        {
            if (_currentStream != 0)
            {
                Bass.BASS_ChannelPause(_mixer.Handle);
            }
        }

        public void Resume()
        {
            if (_currentStream != 0)
            {
                Bass.BASS_ChannelPlay(_mixer.Handle, false);
            }
        }

        public void StopCurrentTrack()
        {
            if (_currentStream != 0)
            {
                _mixer.RemoveStream(_currentStream);
                Bass.BASS_StreamFree(_currentStream);
                _currentStream = 0;
            }
        }

        public string GetCurrentTime()
        {
            if (_currentStream == 0)
                return "00:00";

            long position = Bass.BASS_ChannelGetPosition(_currentStream);
            double seconds = Bass.BASS_ChannelBytes2Seconds(_currentStream, position);

            return FormatTime(seconds);
        }

        public string GetTotalTime()
        {
            if (_currentStream == 0)
                return "00:00";

            long length = Bass.BASS_ChannelGetLength(_currentStream);
            double seconds = Bass.BASS_ChannelBytes2Seconds(_currentStream, length);

            return FormatTime(seconds);
        }

        private string FormatTime(double seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
        }

        public void Stop()
        {
            StopCurrentTrack();

            if (_isInitialized)
            {
                Bass.BASS_Free();
                _isInitialized = false;
                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}