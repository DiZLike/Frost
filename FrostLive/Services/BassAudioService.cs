using System;
using Un4seen.Bass;

namespace FrostLive.Services
{
    public class BassAudioService : IDisposable
    {
        private int _streamHandle;
        private bool _isInitialized;
        private readonly string _opusPluginPath = "bassopus.dll";

        public event Action<string> StatusChanged;
        public event Action<bool> PlaybackStateChanged;
        public event Action<int> VolumeChanged;
        public event Action<string> CurrentSongChanged;

        public bool IsPlaying { get; private set; }
        public int Volume { get; private set; } = 80;
        public string CurrentSong { get; private set; } = string.Empty;

        public BassAudioService()
        {
            InitializeBass();
        }

        private void InitializeBass()
        {
            try
            {
                // Инициализация Bass с дефолтным устройством
                _isInitialized = Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);

                if (!_isInitialized)
                {
                    throw new Exception($"BASS initialization failed: {Bass.BASS_ErrorGetCode()}");
                }

                // Загрузка плагина Opus
                if (System.IO.File.Exists(_opusPluginPath))
                {
                    int pluginHandle = Bass.BASS_PluginLoad(_opusPluginPath);
                    if (pluginHandle == 0)
                    {
                        Console.WriteLine($"Failed to load Opus plugin: {Bass.BASS_ErrorGetCode()}");
                    }
                }

                StatusChanged?.Invoke("READY");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"ERROR: {ex.Message}");
            }
        }

        public bool PlayStream(string url)
        {
            try
            {
                Stop();

                // Создание потока с URL
                _streamHandle = Bass.BASS_StreamCreateURL(url, 0, BASSFlag.BASS_DEFAULT, null, IntPtr.Zero);

                if (_streamHandle == 0)
                {
                    StatusChanged?.Invoke($"Failed to create stream: {Bass.BASS_ErrorGetCode()}");
                    return false;
                }

                // Установка громкости
                Bass.BASS_ChannelSetAttribute(_streamHandle, BASSAttribute.BASS_ATTRIB_VOL, Volume / 100f);

                // Воспроизведение
                bool playSuccess = Bass.BASS_ChannelPlay(_streamHandle, false);

                if (playSuccess)
                {
                    IsPlaying = true;
                    PlaybackStateChanged?.Invoke(true);
                    StatusChanged?.Invoke("PLAYING");
                    return true;
                }
                else
                {
                    StatusChanged?.Invoke($"Play failed: {Bass.BASS_ErrorGetCode()}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"Play error: {ex.Message}");
                return false;
            }
        }

        public void Pause()
        {
            if (_streamHandle != 0 && IsPlaying)
            {
                Bass.BASS_ChannelPause(_streamHandle);
                IsPlaying = false;
                PlaybackStateChanged?.Invoke(false);
                StatusChanged?.Invoke("PAUSED");
            }
        }

        public void Resume()
        {
            if (_streamHandle != 0 && !IsPlaying)
            {
                Bass.BASS_ChannelPlay(_streamHandle, false);
                IsPlaying = true;
                PlaybackStateChanged?.Invoke(true);
                StatusChanged?.Invoke("PLAYING");
            }
        }

        public void Stop()
        {
            if (_streamHandle != 0)
            {
                Bass.BASS_ChannelStop(_streamHandle);
                Bass.BASS_StreamFree(_streamHandle);
                _streamHandle = 0;
                IsPlaying = false;
                PlaybackStateChanged?.Invoke(false);
                StatusChanged?.Invoke("STOPPED");
            }
        }

        public void SetVolume(int volume)
        {
            Volume = Math.Max(0, Math.Min(100, volume));

            if (_streamHandle != 0)
            {
                Bass.BASS_ChannelSetAttribute(_streamHandle, BASSAttribute.BASS_ATTRIB_VOL, Volume / 100f);
            }

            VolumeChanged?.Invoke(Volume);
        }

        public string GetPlaybackTime()
        {
            if (_streamHandle == 0) return "00:00";

            double seconds = Bass.BASS_ChannelBytes2Seconds(_streamHandle,
                Bass.BASS_ChannelGetPosition(_streamHandle));
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            return string.Format("{0:D2}:{1:D2}", time.Minutes, time.Seconds);
        }

        public void UpdateCurrentSong(string song)
        {
            CurrentSong = song;
            CurrentSongChanged?.Invoke(song);
        }

        public (float left, float right) GetLevels()
        {
            float offset = 0.2f;
            if (_streamHandle == 0 || !IsPlaying)
                return (0, 0);

            float left = 0, right = 0;
            try
            {
                // Получаем уровни с BASS
                float[] levels = Bass.BASS_ChannelGetLevels(_streamHandle, 0.02f);
                if (levels != null && levels.Length == 2)
                {
                    left = levels[0];
                    right = levels[1];
                }
            }
            catch
            {
                // Игнорируем ошибки
            }

            // Конвертируем линейные уровни в децибелы
            float leftDb = LinearToDecibels(left);
            float rightDb = LinearToDecibels(right);

            // Нормализуем децибелы в диапазон 0-1 с экспоненциальным преобразованием
            float leftNormalized = DecibelsToNormalizedExponential(leftDb);
            float rightNormalized = DecibelsToNormalizedExponential(rightDb);

            if (left > 0) left += offset;
            if (right > 0) right += offset;

            // Ограничиваем и нормализуем значения
            left = Math.Min(1, Math.Max(0, left));
            right = Math.Min(1, Math.Max(0, right));

            return (left, right);
        }

        private float LinearToDecibels(float linearValue)
        {
            if (linearValue <= 0)
                return -96f;

            // Формула: dB = 20 * log10(linearValue)
            return 20f * (float)Math.Log10(linearValue);
        }

        private float DecibelsToNormalizedExponential(float dbValue)
        {
            // Диапазон децибел: от -96 dB (тишина) до 0 dB (максимум)
            const float minDb = -96f;
            const float maxDb = 0f;

            // Ограничиваем значение
            dbValue = Math.Max(minDb, Math.Min(maxDb, dbValue));

            // Сначала линейно нормализуем в диапазон 0-1
            float normalizedLinear = (dbValue - minDb) / (maxDb - minDb);
            return normalizedLinear;
        }

        public void Dispose()
        {
            Stop();

            if (_isInitialized)
            {
                Bass.BASS_Free();
                _isInitialized = false;
            }
        }
    }
}