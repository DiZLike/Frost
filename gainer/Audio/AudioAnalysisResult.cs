namespace gainer.Audio
{
    public class BandAnalysis
    {
        public double LeftRmsDb { get; set; }
        public double RightRmsDb { get; set; }
        public double LeftRmsLinear { get; set; }
        public double RightRmsLinear { get; set; }
        public double OverallRmsDb { get; set; }
        public double OverallRmsLinear { get; set; }
    }

    public class AudioAnalysisResult
    {
        public double ReplayGain { get; set; }      // ReplayGain в dB
        public double RmsDb { get; set; }           // RMS главного канала в dB
        public double IntegratedLoudness { get; set; } // LUFS
        public double RmsLinear { get; set; }       // RMS главного канала (0.0-1.0)

        // Результаты по полосам
        public BandAnalysis MainBand { get; set; } = new BandAnalysis();    // Полный спектр
        public BandAnalysis SubBand { get; set; } = new BandAnalysis();     // 0-120 Гц
        public BandAnalysis LowBand { get; set; } = new BandAnalysis();     // 120-500 Гц
        public BandAnalysis MidBand { get; set; } = new BandAnalysis();     // 500-4000 Гц
        public BandAnalysis HighBand { get; set; } = new BandAnalysis();    // 4000+ Гц

        public override string ToString()
        {
            return $"ReplayGain: {ReplayGain:F2} dB, RMS: {RmsDb:F1} dB, LUFS: {IntegratedLoudness:F1}";
        }

        public string GetBandsInfo()
        {
            string FormatBandValue(double value)
            {
                return double.IsNaN(value) || value < -119.9 ? "-inf" : $"{value:F1}";
            }

            return $"Полосы: M[{FormatBandValue(MainBand.OverallRmsDb)}] " +
                   $"S[{FormatBandValue(SubBand.OverallRmsDb)}] " +
                   $"L[{FormatBandValue(LowBand.OverallRmsDb)}] " +
                   $"M[{FormatBandValue(MidBand.OverallRmsDb)}] " +
                   $"H[{FormatBandValue(HighBand.OverallRmsDb)}] dB";
        }
    }
}