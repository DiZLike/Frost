using Strimer.App;
using Strimer.Audio;
using Strimer.Core;
using System.Text;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Enc;
using Un4seen.Bass.AddOn.EncOpus;

namespace Strimer.Broadcast.Encoders
{
    public class OpusEncoder : IDisposable
    {
        private readonly AppConfig _config;
        private readonly Mixer _mixer;
        private int _encoderHandle;
        private string _encoderExe;

        private bool _disposed = false;
        private readonly object _disposeLock = new object();
        private bool _isValid = true;
        public int Handle => _encoderHandle;

        public OpusEncoder(AppConfig config, Mixer mixer)
        {
            _config = config;
            _mixer = mixer;

            Initialize();
        }

        private void Initialize()
        {
            Logger.Info("Инициализация Opus энкодера...");

            // Определяем путь к opusenc
            _encoderExe = GetOpusEncPath();

            if (!File.Exists(_encoderExe))
            {
                throw new FileNotFoundException($"opusenc не найден по пути: {_encoderExe}");
            }

            // Создаем строку параметров для opusenc
            string parameters = BuildParameters();

            // Создаем энкодер
            _encoderHandle = BassEnc_Opus.BASS_Encode_OPUS_Start(
                _mixer.Handle,
                parameters,
                BASSEncode.BASS_ENCODE_FP_16BIT,
                null,
                IntPtr.Zero
            );

            if (_encoderHandle == 0)
            {
                var error = Bass.BASS_ErrorGetCode();
                throw new Exception($"Не удалось создать Opus энкодер: {error}");
            }

            Logger.Info($"Opus энкодер инициализирован: {_config.OpusBitrate}кбит/с {_config.OpusMode}");
        }
        public bool IsValid()
        {
            lock (_disposeLock)
            {
                return !_disposed && _encoderHandle != 0 && _isValid;
            }
        }

        private string GetOpusEncPath()
        {
            string baseDir = _config.BaseDirectory;

            if (_config.OS == "Windows")
            {
                string archFolder = _config.Architecture == "X64" ? "win64" : "win32";
                return Path.Combine(baseDir, "encs", "opus", archFolder, "opusenc.exe");
            }
            else // Linux
            {
                return Path.Combine("/usr/bin/", "opusenc");
            }
        }

        private string BuildParameters()
        {
            var parameters = new StringBuilder();

            parameters.Append(_encoderExe);
            parameters.Append($" --bitrate {_config.OpusBitrate}");
            parameters.Append($" --{_config.OpusMode}");
            parameters.Append($" --{_config.OpusContentType}");
            parameters.Append($" --comp {_config.OpusComplexity}");
            parameters.Append($" --framesize {_config.OpusFrameSize}");
            parameters.Append(" - -"); // stdin -> stdout

            return parameters.ToString();
        }

        public bool SetMetadata(string artist, string title)
        {
            lock (_disposeLock)
            {
                if (_disposed || _encoderHandle == 0)
                {
                    Logger.Warning("Попытка установить метаданные на недоступном энкодере");
                    return false;
                }

                try
                {
                    string metadata = $"--artist \"{artist}\" --title \"{title}\"";

                    bool success = BassEnc_Opus.BASS_Encode_OPUS_NewStream(
                        _encoderHandle,
                        metadata,
                        BASSEncode.BASS_ENCODE_FP_16BIT
                    );

                    if (!success)
                    {
                        var error = Bass.BASS_ErrorGetCode();
                        Logger.Error($"Ошибка установки метаданных: {error}");
                        _isValid = false;
                    }
                    return success;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Исключение при установке метаданных: {ex.Message}");
                    _isValid = false;
                    return false;
                }
            }
        }

        public void Dispose()
        {
            lock (_disposeLock)
            {
                if (_disposed) return;
                _disposed = true;
                _isValid = false;

                try
                {
                    // Принудительно останавливаем все
                    if (_encoderHandle != 0)
                    {
                        BassEnc.BASS_Encode_Stop(_encoderHandle);
                        Bass.BASS_StreamFree(_encoderHandle);
                        _encoderHandle = 0;
                        Logger.Debug("Ресурсы энкодера освобождены");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Ошибка при освобождении энкодера: {ex.Message}");
                }
            }
        }
    }
}