using System;

namespace RadioStationManager.Models
{
    public class AudioTag
    {
        public string Artist { get; set; }
        public string Title { get; set; }
        public string Album { get; set; }
        public string Genre { get; set; }
        public uint Year { get; set; }
        public uint TrackNumber { get; set; }
        public TimeSpan Duration { get; set; }
    }
}