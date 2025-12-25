namespace CueSplitter.Models
{
    public class CueTrack
    {
        public string Title { get; set; } = "";
        public string Artist { get; set; } = "";
        public string Performer { get; set; } = "";
        public int TrackNumber { get; set; }
        public TimeSpan StartTime { get; set; } = TimeSpan.Zero;
        public TimeSpan EndTime { get; set; } = TimeSpan.Zero;
        public string FileFormat { get; set; } = "";

        public TimeSpan Duration => EndTime - StartTime;
    }
}