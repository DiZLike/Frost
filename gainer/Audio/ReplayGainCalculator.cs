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
        private const int PROGRESS_UPDATE_INTERVAL = 50;

        public ReplayGainCalculator(int sampleRate, double targetLufs)
        {
            _sampleRate = sampleRate;
            _targetLufs = targetLufs;
        }

        public (double replayGain, double integratedLoudness) Calculate(float[] pcmData)
        {
            int blockSize = _sampleRate * 400 / 1000;
            int numBlocks = pcmData.Length / (blockSize * 2);

            if (numBlocks == 0)
                return (0, -120);

            double sumLoudness = 0;
            int validBlocks = 0;

            for (int i = 0; i < numBlocks; i++)
            {
                float[] leftBlock = GetChannelBlock(pcmData, i * blockSize * 2, blockSize, 0, 2);
                float[] rightBlock = GetChannelBlock(pcmData, i * blockSize * 2, blockSize, 1, 2);

                double rmsLeft = CalculateRMS(leftBlock);
                double rmsRight = CalculateRMS(rightBlock);
                double rms = Math.Sqrt((rmsLeft * rmsLeft + rmsRight * rmsRight) / 2);

                double momentaryLoudness = -0.691 + 20 * Math.Log10(rms);

                if (momentaryLoudness > -70)
                {
                    sumLoudness += Math.Pow(10, momentaryLoudness / 10);
                    validBlocks++;
                }

                if (_progressUpdateCounter % PROGRESS_UPDATE_INTERVAL == 0 || i == numBlocks - 1)
                {
                    double blockProgress = i / (double)numBlocks;
                    double totalProgress = 0.5 + (blockProgress * 0.4);
                    ProgressChanged?.Invoke(totalProgress, $"LUFS: {i + 1}/{numBlocks} блоков");
                }
                _progressUpdateCounter++;
            }

            double integratedLoudness = validBlocks > 0
                ? Math.Round(-0.691 + 10 * Math.Log10(sumLoudness / validBlocks), 2)
                : -120;

            double replayGain = Math.Round(_targetLufs - integratedLoudness, 2);

            return (replayGain, integratedLoudness);
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
            if (samples == null || samples.Length == 0)
                return 0;

            double sumOfSquares = 0;
            for (int i = 0; i < samples.Length; i++)
                sumOfSquares += samples[i] * samples[i];

            double rms = Math.Sqrt(sumOfSquares / samples.Length);

            // Если RMS слишком мал, возвращаем -120 dB (тишина)
            return rms < 1e-6 ? 1e-6 : rms;
        }
    }
}