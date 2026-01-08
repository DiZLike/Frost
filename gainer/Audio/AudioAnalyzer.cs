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

        public AudioAnalysisResult Analyze(float[] pcmDataMain, float[] pcmDataSub,
                                          float[] pcmDataLow, float[] pcmDataMid,
                                          float[] pcmDataHigh)
        {
            // Применяем K-фильтр если нужно (только к основным данным для ReplayGain)
            if (_useKFilter)
            {
                var kFilter = new KFilter();
                pcmDataMain = kFilter.ApplyFilter(pcmDataMain);
            }

            // Рассчитываем ReplayGain (только для основных данных)
            var replayGainResult = _replayGainCalc.Calculate(pcmDataMain);

            // Рассчитываем RMS для всех полос
            var mainResult = _rmsCalc.CalculateStereo(pcmDataMain);
            var subResult = _rmsCalc.CalculateStereo(pcmDataSub);
            var lowResult = _rmsCalc.CalculateStereo(pcmDataLow);
            var midResult = _rmsCalc.CalculateStereo(pcmDataMid);
            var highResult = _rmsCalc.CalculateStereo(pcmDataHigh);

            return new AudioAnalysisResult
            {
                ReplayGain = replayGainResult.replayGain,
                IntegratedLoudness = replayGainResult.integratedLoudness,
                RmsLinear = mainResult.overallRmsLinear,
                RmsDb = mainResult.overallRmsDb,

                MainBand = new BandAnalysis
                {
                    LeftRmsLinear = mainResult.leftRmsLinear,
                    RightRmsLinear = mainResult.rightRmsLinear,
                    LeftRmsDb = mainResult.leftRmsDb,
                    RightRmsDb = mainResult.rightRmsDb,
                    OverallRmsLinear = mainResult.overallRmsLinear,
                    OverallRmsDb = mainResult.overallRmsDb
                },
                SubBand = new BandAnalysis
                {
                    LeftRmsLinear = subResult.leftRmsLinear,
                    RightRmsLinear = subResult.rightRmsLinear,
                    LeftRmsDb = subResult.leftRmsDb,
                    RightRmsDb = subResult.rightRmsDb,
                    OverallRmsLinear = subResult.overallRmsLinear,
                    OverallRmsDb = subResult.overallRmsDb
                },
                LowBand = new BandAnalysis
                {
                    LeftRmsLinear = lowResult.leftRmsLinear,
                    RightRmsLinear = lowResult.rightRmsLinear,
                    LeftRmsDb = lowResult.leftRmsDb,
                    RightRmsDb = lowResult.rightRmsDb,
                    OverallRmsLinear = lowResult.overallRmsLinear,
                    OverallRmsDb = lowResult.overallRmsDb
                },
                MidBand = new BandAnalysis
                {
                    LeftRmsLinear = midResult.leftRmsLinear,
                    RightRmsLinear = midResult.rightRmsLinear,
                    LeftRmsDb = midResult.leftRmsDb,
                    RightRmsDb = midResult.rightRmsDb,
                    OverallRmsLinear = midResult.overallRmsLinear,
                    OverallRmsDb = midResult.overallRmsDb
                },
                HighBand = new BandAnalysis
                {
                    LeftRmsLinear = highResult.leftRmsLinear,
                    RightRmsLinear = highResult.rightRmsLinear,
                    LeftRmsDb = highResult.leftRmsDb,
                    RightRmsDb = highResult.rightRmsDb,
                    OverallRmsLinear = highResult.overallRmsLinear,
                    OverallRmsDb = highResult.overallRmsDb
                }
            };
        }

        private void OnProgressChanged(double progress, string message)
        {
            ProgressChanged?.Invoke(progress, message);
        }
    }
}