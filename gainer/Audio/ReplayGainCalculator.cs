using System;
using System.Linq;

namespace gainer.Audio
{
    public class ReplayGainCalculator
    {
        public event Action<double, string>? ProgressChanged;

        private readonly int _sampleRate;
        private readonly double _targetLufs;
        private int _progressUpdateCounter = 0;
        private const int PROGRESS_UPDATE_INTERVAL = 50; // Обновляем каждые 50 блоков (было 10)

        public ReplayGainCalculator(int sampleRate, double targetLufs)
        {
            _sampleRate = sampleRate;
            _targetLufs = targetLufs;
        }

        public double Calculate(float[] pcmData)
        {
            double integratedLoudness = CalculateIntegratedLoudness(pcmData);
            double replayGain = _targetLufs - integratedLoudness;

            return Math.Round(replayGain, 2);
        }

        public double CalculateWithKFilter(float[] pcmData)
        {
            var kFilter = new KFilter();
            pcmData = kFilter.ApplyFilter(pcmData);

            return Calculate(pcmData);
        }

        private double CalculateIntegratedLoudness(float[] pcmData)
        {
            int blockSize = _sampleRate * 400 / 1000; // 400ms блоки
            int numBlocks = pcmData.Length / (blockSize * 2); // Учитываем стерео

            if (numBlocks == 0)
                return -120; // Минимальное значение для пустого аудио

            double sumLoudness = 0;
            int validBlocks = 0;

            for (int i = 0; i < numBlocks; i++)
            {
                // Получаем блок для левого и правого каналов
                float[] leftBlock = GetChannelBlock(pcmData, i * blockSize * 2, blockSize, 0, 2);
                float[] rightBlock = GetChannelBlock(pcmData, i * blockSize * 2, blockSize, 1, 2);

                // Рассчитываем RMS для каждого канала
                double rmsLeft = CalculateRMS(leftBlock);
                double rmsRight = CalculateRMS(rightBlock);

                // Усредняем RMS для стерео
                double rms = Math.Sqrt((rmsLeft * rmsLeft + rmsRight * rmsRight) / 2);

                // Переводим в LUFS
                double momentaryLoudness = -0.691 + 20 * Math.Log10(rms);

                // Игнорируем тихие блоки (< -70 LUFS)
                if (momentaryLoudness > -70)
                {
                    sumLoudness += Math.Pow(10, momentaryLoudness / 10);
                    validBlocks++;
                }

                // Оповещаем о прогрессе анализа (50%-90%) с ограниченной частотой
                if (_progressUpdateCounter % PROGRESS_UPDATE_INTERVAL == 0 || i == numBlocks - 1)
                {
                    double blockProgress = i / (double)numBlocks;
                    double totalProgress = 0.5 + (blockProgress * 0.4); // Анализ - 40% процесса
                    ProgressChanged?.Invoke(totalProgress, $"Анализ LUFS: {i + 1}/{numBlocks} блоков");
                }
                _progressUpdateCounter++;
            }

            if (validBlocks == 0)
                return -120;

            double integratedLoudness = -0.691 + 10 * Math.Log10(sumLoudness / validBlocks);
            return integratedLoudness;
        }

        private float[] GetChannelBlock(float[] pcmData, int startIndex, int blockSize, int channel, int numChannels)
        {
            float[] block = new float[blockSize];
            int blockPos = 0;

            for (int i = startIndex + channel; i < pcmData.Length && blockPos < blockSize; i += numChannels)
            {
                block[blockPos++] = pcmData[i];
            }

            // Если данных не хватило, заполняем нулями
            while (blockPos < blockSize)
            {
                block[blockPos++] = 0;
            }

            return block;
        }

        private double CalculateRMS(float[] samples)
        {
            double sumOfSquares = 0;
            int count = 0;

            for (int i = 0; i < samples.Length; i++)
            {
                sumOfSquares += samples[i] * samples[i];
                count++;
            }

            if (count == 0)
                return 0;

            return Math.Sqrt(sumOfSquares / count);
        }
    }
}