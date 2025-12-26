using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Un4seen.Bass;

namespace gainer.Audio
{
    public class AudioReader : IDisposable
    {
        public event Action<double>? ProgressChanged;

        private int _streamHandle = 0;
        private bool _disposed = false;
        private int _progressUpdateCounter = 0;
        private const int PROGRESS_UPDATE_INTERVAL = 10; // Обновляем каждые 10 блоков

        public AudioReader(string filePath)
        {
            // Создаем поток для декодирования
            _streamHandle = Bass.BASS_StreamCreateFile(
                filePath,
                0, 0,
                BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT
            );

            if (_streamHandle == 0)
                throw new Exception($"Не удалось открыть файл: {Bass.BASS_ErrorGetCode()}");
        }

        public float[] ReadAllSamples()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AudioReader));

            List<float> samples = new List<float>();
            float[] buffer = new float[44100 * 2]; // 1 секунда стерео

            // Получаем общее количество сэмплов для расчета прогресса
            long totalBytes = Bass.BASS_ChannelGetLength(_streamHandle);
            long bytesProcessed = 0;

            int bytesRead;
            while ((bytesRead = Bass.BASS_ChannelGetData(_streamHandle, buffer, buffer.Length * 4)) > 0)
            {
                bytesProcessed += bytesRead;
                int samplesRead = bytesRead / 4;
                samples.AddRange(buffer.Take(samplesRead));

                // Оповещаем о прогрессе чтения (0%-50%) с ограниченной частотой
                if (totalBytes > 0 && _progressUpdateCounter % PROGRESS_UPDATE_INTERVAL == 0)
                {
                    double progress = (bytesProcessed / (double)totalBytes) * 0.5; // Чтение - половина процесса
                    ProgressChanged?.Invoke(progress);
                }
                _progressUpdateCounter++;
            }

            return samples.ToArray();
        }

        public float[] GetPCMData32()
        {
            try
            {
                return ReadAllSamples();
            }
            finally
            {
                // Гарантируем освобождение ресурсов
                Dispose();
            }
        }

        public void Dispose()
        {
            if (!_disposed && _streamHandle != 0)
            {
                Bass.BASS_StreamFree(_streamHandle);
                _streamHandle = 0;
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~AudioReader()
        {
            Dispose();
        }
    }
}