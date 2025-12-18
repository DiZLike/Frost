using Strimer.Core;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Mix;

namespace Strimer.Audio
{
    public class SilenceGenerator : IDisposable
    {
        private int _silenceStream;
        private readonly int _sampleRate;
        private bool _isPlaying;
        private bool _isDisposed;
        private Thread? _playbackThread;
        private readonly ManualResetEvent _stopEvent = new(false);

        public int Handle => _silenceStream;
        public bool IsPlaying => _isPlaying;

        public SilenceGenerator(int sampleRate)
        {
            _sampleRate = sampleRate;
            CreateSilenceStream();
        }

        private void CreateSilenceStream()
        {
            try
            {
                _silenceStream = Bass.BASS_StreamCreate(_sampleRate, 2,
                    BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE,
                    StreamProc, IntPtr.Zero);

                if (_silenceStream == 0)
                {
                    var error = Bass.BASS_ErrorGetCode();
                    Logger.Error($"Не удалось создать поток тишины: {error}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка при создании потока тишины: {ex.Message}");
            }
        }

        private static int StreamProc(int handle, IntPtr buffer, int length, IntPtr user)
        {
            // Заполняем буфер нулями (тишина)
            unsafe
            {
                byte* bufferPtr = (byte*)buffer.ToPointer();
                for (int i = 0; i < length; i++)
                {
                    bufferPtr[i] = 0;
                }
            }
            return length;
        }

        public void StartPlaying(Mixer mixer)
        {
            if (_isPlaying || _silenceStream == 0 || mixer == null)
                return;

            _stopEvent.Reset();
            _isPlaying = true;

            _playbackThread = new Thread(() =>
            {
                try
                {
                    Logger.Debug("Запуск воспроизведения тишины");

                    // Добавляем поток тишины в микшер
                    if (!BassMix.BASS_Mixer_StreamAddChannel(mixer.Handle, _silenceStream, BASSFlag.BASS_DEFAULT))
                    {
                        Logger.Error("Не удалось добавить тишину в микшер");
                        return;
                    }

                    // Запускаем воспроизведение
                    if (!Bass.BASS_ChannelPlay(_silenceStream, true))
                    {
                        Logger.Error("Не удалось воспроизвести тишину");
                        return;
                    }

                    Logger.Debug("Тишина теперь воспроизводится");

                    // Ждем сигнала остановки
                    _stopEvent.WaitOne();
                }
                catch (Exception ex)
                {
                    Logger.Error($"Ошибка в потоке воспроизведения тишины: {ex.Message}");
                }
                finally
                {
                    StopInternal(mixer);
                }
            });

            _playbackThread.IsBackground = true;
            _playbackThread.Start();
        }

        public void StopPlaying(Mixer mixer)
        {
            if (!_isPlaying)
                return;

            Logger.Debug("Остановка воспроизведения тишины");
            _stopEvent.Set();
            _isPlaying = false;

            // Даем потоку время завершиться
            _playbackThread?.Join(1000);
        }

        private void StopInternal(Mixer mixer)
        {
            try
            {
                if (_silenceStream != 0 && mixer != null)
                {
                    Bass.BASS_ChannelStop(_silenceStream);
                    BassMix.BASS_Mixer_ChannelRemove(_silenceStream);
                    Logger.Debug("Тишина остановлена и удалена из микшера");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка при остановке тишины: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            _isDisposed = true;
            _stopEvent.Set();

            if (_silenceStream != 0)
            {
                Bass.BASS_StreamFree(_silenceStream);
                _silenceStream = 0;
            }

            _stopEvent.Dispose();
        }
    }
}