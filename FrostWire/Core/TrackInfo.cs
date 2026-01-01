namespace FrostWire.Core
{
    public class TrackInfo
    {
        public string Artist { get; set; } = "Unknown Artist";
        public string Title { get; set; } = "Unknown Title";
        public string Album { get; set; } = "";
        public int Year { get; set; }
        public string Genre { get; set; } = "";
        public float ReplayGain { get; set; }
        public string Comment { get; set; } = "";
    }
}