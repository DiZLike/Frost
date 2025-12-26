using System.Collections.Generic;

namespace FrostPlayer.Models
{
    public class Playlist
    {
        public List<string> FilePaths { get; set; } = new List<string>();
        public string Name { get; set; } = "Default Playlist";
        public int CurrentTrackIndex { get; set; } = -1;

        public void AddTrack(string filePath)
        {
            if (!FilePaths.Contains(filePath))
            {
                FilePaths.Add(filePath);
            }
        }

        public void RemoveTrack(int index)
        {
            if (index >= 0 && index < FilePaths.Count)
            {
                FilePaths.RemoveAt(index);
                if (index <= CurrentTrackIndex && CurrentTrackIndex >= 0)
                {
                    CurrentTrackIndex--;
                }
            }
        }

        public void Clear()
        {
            FilePaths.Clear();
            CurrentTrackIndex = -1;
        }
    }
}