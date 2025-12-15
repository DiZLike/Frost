using System;
using System.IO;
using TagLib;
using RadioStationManager.Models;

namespace RadioStationManager.Services
{
    public class AudioTagReader
    {
        public static AudioTag ReadTags(string filePath)
        {
            try
            {
                return ReadTagsWithTagLib(filePath);
            }
            catch (Exception ex)
            {
                return new AudioTag
                {
                    Artist = "Unknown",
                    Title = Path.GetFileNameWithoutExtension(filePath),
                    Album = "",
                    Genre = "",
                    Year = 0,
                    TrackNumber = 0,
                    Duration = TimeSpan.Zero
                };
            }
        }

        private static AudioTag ReadTagsWithTagLib(string filePath)
        {
            using var file = TagLib.File.Create(filePath);

            return new AudioTag
            {
                Artist = file.Tag.JoinedPerformers ??
                        file.Tag.FirstPerformer ??
                        "Unknown",
                Title = file.Tag.Title ??
                       Path.GetFileNameWithoutExtension(filePath),
                Album = file.Tag.Album ?? "",
                Genre = string.Join(", ", file.Tag.Genres) ?? "",
                Year = file.Tag.Year,
                TrackNumber = file.Tag.Track,
                Duration = file.Properties.Duration
            };
        }
    }
}