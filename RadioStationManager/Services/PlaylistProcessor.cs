using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RadioStationManager.Configuration;

namespace RadioStationManager.Services
{
    public class PlaylistProcessor
    {
        private readonly RadioConfig _config;
        private readonly List<string> _playlistLines = new List<string>();

        public PlaylistProcessor(RadioConfig config)
        {
            _config = config;

            if (File.Exists(_config.PlaylistPath))
            {
                _playlistLines = File.ReadAllLines(_config.PlaylistPath).ToList();
            }
        }

        public void AddTrack(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            string formattedLine = $"track={_config.ServerPath}{fileName}?;";
            _playlistLines.Add(formattedLine);
        }

        public void Save(string filePath = null)
        {
            string savePath = filePath ?? _config.PlaylistPath;
            Directory.CreateDirectory(Path.GetDirectoryName(savePath));
            File.WriteAllLines(savePath, _playlistLines);
        }
    }
}