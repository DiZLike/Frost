using Strimer.App;
using Strimer.Core;
using System.Diagnostics;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Tags;

namespace Strimer.Audio
{
    public class BassAudioEngine : IDisposable
    {
        private readonly AppConfig _config;
        private bool _isInitialized = false;
        private bool _isDisposed = false;

        public bool IsInitialized => _isInitialized;

        public BassAudioEngine(AppConfig config)
        {
            _config = config;
            Logger.Debug($"[BassAudioEngine] Создан с конфигурацией: Устройство={_config.AudioDevice}, Частота={_config.SampleRate}");
            Initialize();
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            Logger.Info("[BassAudioEngine] Инициализация BASS...");

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

            LoadPlugins();
            _isInitialized = true;
            Logger.Info("[BassAudioEngine] BASS успешно инициализирован");
        }

        private void LoadPlugins()
        {
            Logger.Info("[BassAudioEngine] Загрузка аудио плагинов...");

            string libPrefix = _config.OS == "Windows" ? "" : "lib";
            string libExtension = _config.OS == "Windows" ? ".dll" : ".so";

            string[] plugins = { "bassopus", "bass_aac", "bassflac", "basswv" };

            List<string> loadedPlugins = new();
            List<string> failedPlugins = new();

            foreach (var plugin in plugins)
            {
                string pluginPath = Path.Combine(
                    _config.BaseDirectory,
                    $"{libPrefix}{plugin}{libExtension}"
                );

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

            if (loadedPlugins.Count > 0)
                Logger.Info($"[BassAudioEngine] Загруженные плагины: {string.Join(", ", loadedPlugins)}");
            if (failedPlugins.Count > 0)
                Logger.Warning($"[BassAudioEngine] Не удалось загрузить плагины: {string.Join(", ", failedPlugins)}");
        }

        public int CreateStreamFromFile(string filePath)
        {
            if (!_isInitialized)
                throw new InvalidOperationException("BASS не инициализирован");

            var stopwatch = Stopwatch.StartNew();

            int stream = Bass.BASS_StreamCreateFile(
                filePath,
                0, 0,
                BASSFlag.BASS_DEFAULT | BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE
            );

            if (stream == 0)
            {
                var error = Bass.BASS_ErrorGetCode();
                Logger.Error($"[BassAudioEngine] Не удалось создать поток для {Path.GetFileName(filePath)}: {error}");
            }
            else
            {
                stopwatch.Stop();
                Logger.Debug($"[Производительность] Поток создан за {stopwatch.ElapsedMilliseconds} мс, handle: {stream}");
            }

            return stream;
        }

        public TAG_INFO GetTrackTags(string filePath)
        {
            return BassTags.BASS_TAG_GetFromFile(filePath);
        }

        public bool IsStreamPlaying(int streamHandle)
        {
            return streamHandle != 0 &&
                   Bass.BASS_ChannelIsActive(streamHandle) == BASSActive.BASS_ACTIVE_PLAYING;
        }

        public void PlayStream(int streamHandle)
        {
            Bass.BASS_ChannelPlay(streamHandle, false);
        }

        public void StopStream(int streamHandle)
        {
            if (streamHandle != 0)
            {
                Bass.BASS_ChannelStop(streamHandle);
            }
        }

        public void FreeStream(int streamHandle)
        {
            if (streamHandle != 0)
            {
                Bass.BASS_StreamFree(streamHandle);
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                if (_isInitialized)
                {
                    Bass.BASS_Free();
                    _isInitialized = false;
                }
                _isDisposed = true;
            }
        }
    }
}