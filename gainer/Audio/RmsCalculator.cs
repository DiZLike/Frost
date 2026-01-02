using System;
using System.Linq;

namespace gainer.Audio
{
    public class RmsCalculator
    {
        public event Action<double, string>? ProgressChanged;

        private readonly int _sampleRate;
        private int _progressUpdateCounter = 0;
        private const int PROGRESS_UPDATE_INTERVAL = 50;

        public RmsCalculator(int sampleRate)
        {
            _sampleRate = sampleRate;
        }

        public (double rmsLinear, double rmsDb) Calculate(float[] pcmData)
        {
            int blockSize = _sampleRate * 400 / 1000;
            int numBlocks = pcmData.Length / (blockSize * 2);

            if (numBlocks == 0)
                return (0, -120);

            double sumRMS = 0;

            for (int i = 0; i < numBlocks; i++)
            {
                float[] leftBlock = GetChannelBlock(pcmData, i * blockSize * 2, blockSize, 0, 2);
                float[] rightBlock = GetChannelBlock(pcmData, i * blockSize * 2, blockSize, 1, 2);

                double rmsLeft = CalculateRMS(leftBlock);
                double rmsRight = CalculateRMS(rightBlock);
                double rms = Math.Sqrt((rmsLeft * rmsLeft + rmsRight * rmsRight) / 2);

                sumRMS += rms;

                if (_progressUpdateCounter % PROGRESS_UPDATE_INTERVAL == 0 || i == numBlocks - 1)
                {
                    double blockProgress = i / (double)numBlocks;
                    double totalProgress = 0.5 + (blockProgress * 0.2);
                    ProgressChanged?.Invoke(totalProgress, $"RMS: {i + 1}/{numBlocks} блоков");
                }
                _progressUpdateCounter++;
            }

            double averageRMS = sumRMS / numBlocks;
            double rmsDb = 20 * Math.Log10(averageRMS);

            return (Math.Round(averageRMS, 6), Math.Round(rmsDb, 2));
        }

        private float[] GetChannelBlock(float[] pcmData, int startIndex, int blockSize, int channel, int numChannels)
        {
            float[] block = new float[blockSize];
            int blockPos = 0;

            for (int i = startIndex + channel; i < pcmData.Length && blockPos < blockSize; i += numChannels)
                block[blockPos++] = pcmData[i];

            while (blockPos < blockSize)
                block[blockPos++] = 0;

            return block;
        }

        private double CalculateRMS(float[] samples)
        {
            double sumOfSquares = 0;
            for (int i = 0; i < samples.Length; i++)
                sumOfSquares += samples[i] * samples[i];

            return samples.Length > 0 ? Math.Sqrt(sumOfSquares / samples.Length) : 0;
        }
    }
}