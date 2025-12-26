using System.Collections.Generic;
using System.IO;

namespace FrostPlayer.Utilities
{
    public static class FileHelper
    {
        public static bool IsAudioFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;

            var ext = Path.GetExtension(filePath).ToLower();
            return SupportedFormats.Contains(ext);
        }

        public static readonly HashSet<string> SupportedFormats = new HashSet<string>
        {
            ".mp3", ".wav", ".flac", ".ogg", ".opus", ".m4a"
        };

        public static string[] FilterAudioFiles(string[] files)
        {
            var result = new List<string>();
            foreach (var file in files)
            {
                if (IsAudioFile(file) && File.Exists(file))
                {
                    result.Add(file);
                }
            }
            return result.ToArray();
        }
    }
}