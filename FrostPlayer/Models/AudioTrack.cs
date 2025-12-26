using System;

namespace FrostPlayer.Models
{
    public class AudioTrack
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public double Duration { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }

        public AudioTrack(string filePath)
        {
            FilePath = filePath;
            FileName = System.IO.Path.GetFileName(filePath);
        }

        public string GetFormattedDuration()
        {
            return TimeSpan.FromSeconds(Duration).ToString(@"mm\:ss");
        }
    }
}