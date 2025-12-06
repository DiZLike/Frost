using Strimer.Core;
using Strimer.Services;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;
using Un4seen.Bass.AddOn.Tags;

namespace Strimer.Audio
{
    public class Player : IDisposable
    {
        private readonly AppConfig _config;
        private Mixer _mixer; // ← Сделать не readonly
        private ReplayGain _replayGain;

        private int _currentStream;
        private bool _isInitialized;

        public bool IsPlaying => _currentStream != 0 &&
            Bass.BASS_ChannelIsActive(_currentStream) == BASSActive.BASS_ACTIVE_PLAYING;

        public bool IsStopped => _currentStream == 0 ||
            Bass.BASS_ChannelIsActive(_currentStream) == BASSActive.BASS_ACTIVE_STOPPED;

        public Mixer Mixer => _mixer;

        public Player(AppConfig config)
        {
            _config = config;
            // НЕ создаем Mixer и ReplayGain здесь!
        }

        public void Initialize()
        {
            if (_isInitialized)
                return;

            Logger.Info("Initializing audio system...");

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
                throw new Exception($"Failed to initialize BASS: {error}");
            }

            // 2. Создаем микшер ПОСЛЕ инициализации BASS
            _mixer = new Mixer(_config.SampleRate);

            // 3. Создаем ReplayGain
            _replayGain = new ReplayGain(_config.UseReplayGain, _config.UseCustomGain, _mixer.Handle);

            // 4. Загружаем плагины
            LoadPlugins();

            _isInitialized = true;
            Logger.Info("Audio system initialized successfully");
        }

        private void LoadPlugins()
        {
            Logger.Info("Loading audio plugins...");

            string libPrefix = _config.OS == "Windows" ? "" : "lib";
            string libExtension = _config.OS == "Windows" ? ".dll" : ".so";

            // Загружаем плагины для разных форматов
            string[] plugins = { "bassopus", "bassaac", "bassflac", "basswv" };

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
                        Logger.Info($"  Loaded: {plugin}");
                    }
                    else
                    {
                        Logger.Warning($"  Failed to load: {plugin}");
                    }
                }
            }
        }

        public TrackInfo PlayTrack(string filePath)
        {
            // Останавливаем текущий трек
            StopCurrentTrack();

            // Проверяем файл
            if (!File.Exists(filePath))
            {
                Logger.Error($"File not found: {filePath}");
                return null;
            }

            // Создаем поток для файла - УБЕРИТЕ BASS_STREAM_DECODE!
            _currentStream = Bass.BASS_StreamCreateFile(
                filePath,
                0, 0,
                BASSFlag.BASS_DEFAULT | BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE
            );

            if (_currentStream == 0)
            {
                var error = Bass.BASS_ErrorGetCode();
                Logger.Error($"Failed to create stream for {Path.GetFileName(filePath)}: {error}");
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

            Logger.Info($"Now playing: {trackInfo.Artist} - {trackInfo.Title}");

            // Логируем ReplayGain значение для отладки
            Logger.Info($"ReplayGain: {trackInfo.ReplayGain} dB, UseReplayGain: {_config.UseReplayGain}");

            return trackInfo;
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
                Artist = !string.IsNullOrWhiteSpace(tagInfo.artist) ? tagInfo.artist : "Unknown Artist",
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
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}