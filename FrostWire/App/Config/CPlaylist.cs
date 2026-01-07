using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostWire.App.Config
{
    public class CPlaylist
    {
        public string PlaylistFile { get; set; } = "playlist.txt";
        public bool DynamicPlaylist { get; set; } = false;
        public bool SavePlaylistHistory { get; set; } = true;
        public bool ArtistGenreHistoryEnable { get; set; } = true;
        public int MaxHistory { get; set; } = 10;
        public int MaxAttempts { get; set; } = 5;
        public bool ScheduleEnable { get; set; } = true;
        public string ScheduleFile { get; set; } = String.Empty;
    }
}