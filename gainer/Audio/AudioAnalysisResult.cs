namespace gainer.Audio
{
    public class AudioAnalysisResult
    {
        public double ReplayGain { get; set; }      // ReplayGain в dB
        public double RmsDb { get; set; }           // RMS в dB
        public double IntegratedLoudness { get; set; } // LUFS
        public double RmsLinear { get; set; }       // RMS (0.0-1.0)

        public override string ToString()
        {
            return $"ReplayGain: {ReplayGain:F2} dB, RMS: {RmsDb:F1} dB, LUFS: {IntegratedLoudness:F1}";
        }
    }
}