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

        public (double leftRmsLinear, double rightRmsLinear, double overallRmsLinear,
                double leftRmsDb, double rightRmsDb, double overallRmsDb) CalculateStereo(float[] pcmData)
        {
            int blockSize = _sampleRate * 400 / 1000;
            int numBlocks = pcmData.Length / (blockSize * 2);

            if (numBlocks == 0)
                return (0, 0, 0, -120, -120, -120);

            double sumLeftRMS = 0;
            double sumRightRMS = 0;

            for (int i = 0; i < numBlocks; i++)
            {
                float[] leftBlock = GetChannelBlock(pcmData, i * blockSize * 2, blockSize, 0, 2);
                float[] rightBlock = GetChannelBlock(pcmData, i * blockSize * 2, blockSize, 1, 2);

                double rmsLeft = CalculateRMS(leftBlock);
                double rmsRight = CalculateRMS(rightBlock);

                sumLeftRMS += rmsLeft;
                sumRightRMS += rmsRight;

                if (_progressUpdateCounter % PROGRESS_UPDATE_INTERVAL == 0 || i == numBlocks - 1)
                {
                    double blockProgress = i / (double)numBlocks;
                    double totalProgress = 0.5 + (blockProgress * 0.2);
                    ProgressChanged?.Invoke(totalProgress, $"RMS: {i + 1}/{numBlocks} блоков");
                }
                _progressUpdateCounter++;
            }

            double avgLeftRMS = sumLeftRMS / numBlocks;
            double avgRightRMS = sumRightRMS / numBlocks;
            double avgOverallRMS = Math.Sqrt((avgLeftRMS * avgLeftRMS + avgRightRMS * avgRightRMS) / 2);

            double leftRmsDb = 20 * Math.Log10(avgLeftRMS);
            double rightRmsDb = 20 * Math.Log10(avgRightRMS);
            double overallRmsDb = 20 * Math.Log10(avgOverallRMS);

            return (Math.Round(avgLeftRMS, 6), Math.Round(avgRightRMS, 6), Math.Round(avgOverallRMS, 6),
                    Math.Round(leftRmsDb, 2), Math.Round(rightRmsDb, 2), Math.Round(overallRmsDb, 2));
        }

        public (double rmsLinear, double rmsDb) CalculateMono(float[] pcmData)
        {
            int blockSize = _sampleRate * 400 / 1000;
            int numBlocks = pcmData.Length / blockSize;

            if (numBlocks == 0)
                return (0, -120);

            double sumRMS = 0;

            for (int i = 0; i < numBlocks; i++)
            {
                float[] block = GetMonoBlock(pcmData, i * blockSize, blockSize);
                double rms = CalculateRMS(block);
                sumRMS += rms;

                if (_progressUpdateCounter % PROGRESS_UPDATE_INTERVAL == 0 || i == numBlocks - 1)
                {
                    double blockProgress = i / (double)numBlocks;
                    double totalProgress = 0.5 + (blockProgress * 0.2);
                    ProgressChanged?.Invoke(totalProgress, $"RMS: {i + 1}/{numBlocks} блоков");
                }
                _progressUpdateCounter++;
            }

            double avgRMS = sumRMS / numBlocks;
            double rmsDb = 20 * Math.Log10(avgRMS);

            return (Math.Round(avgRMS, 6), Math.Round(rmsDb, 2));
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

        private float[] GetMonoBlock(float[] pcmData, int startIndex, int blockSize)
        {
            float[] block = new float[blockSize];
            int blockPos = 0;

            for (int i = startIndex; i < pcmData.Length && blockPos < blockSize; i++)
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