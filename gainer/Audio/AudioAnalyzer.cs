using System;

namespace gainer.Audio
{
    public class AudioAnalyzer
    {
        public event Action<double, string>? ProgressChanged;

        private readonly ReplayGainCalculator _replayGainCalc;
        private readonly RmsCalculator _rmsCalc;
        private readonly bool _useKFilter;

        public AudioAnalyzer(int sampleRate, double targetLufs, bool useKFilter = false)
        {
            _replayGainCalc = new ReplayGainCalculator(sampleRate, targetLufs);
            _rmsCalc = new RmsCalculator(sampleRate);
            _useKFilter = useKFilter;
        }

        public AudioAnalysisResult Analyze(float[] pcmData)
        {
            // Применяем K-фильтр если нужно
            if (_useKFilter)
            {
                var kFilter = new KFilter();
                pcmData = kFilter.ApplyFilter(pcmData);
            }

            // Рассчитываем оба показателя
            var replayGainResult = _replayGainCalc.Calculate(pcmData);
            var rmsResult = _rmsCalc.Calculate(pcmData);

            return new AudioAnalysisResult
            {
                ReplayGain = replayGainResult.replayGain,
                IntegratedLoudness = replayGainResult.integratedLoudness,
                RmsLinear = rmsResult.rmsLinear,
                RmsDb = rmsResult.rmsDb
            };
        }

        private void OnProgressChanged(double progress, string message)
        {
            ProgressChanged?.Invoke(progress, message);
        }
    }
}