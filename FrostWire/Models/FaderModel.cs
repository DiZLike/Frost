using Timer = System.Timers.Timer;

namespace FrostWire.Models
{
    public class FaderModel
    {
        public float Duration { get; set; } = 5f;
        public float FadeStartVolume { get; set; } = 0f;
        public float FadeEndVolume { get; set; } = -60f;
        public bool IsFading { get; set; } = false;
        public float FadeElapsedSeconds { get; set; } = 0f;
        public Timer FaderTimer;
        public FaderModel()
        {
            FadeElapsedSeconds = 0;
            FaderTimer = new Timer(50);
            FaderTimer.AutoReset = true;
        }
    }
}