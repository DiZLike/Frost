namespace CueSplitter.Models
{
    public class CueSheet
    {
        public string Title { get; set; } = "";
        public string Artist { get; set; } = "";
        public string Performer { get; set; } = "";
        public string Genre { get; set; } = "";
        public int Year { get; set; }
        public List<CueTrack> Tracks { get; set; } = new();
        public string SourceCueFile { get; set; } = "";
        public string AudioFile { get; set; } = "";
    }
}