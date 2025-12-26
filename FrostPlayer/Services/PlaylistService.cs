using FrostPlayer.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace FrostPlayer.Services
{
    public class PlaylistService
    {
        private readonly string _playlistPath;

        public PlaylistService()
        {
            _playlistPath = Path.Combine(Application.StartupPath, "playlist.json");
        }

        public Playlist LoadPlaylist()
        {
            var playlist = new Playlist();

            try
            {
                if (File.Exists(_playlistPath))
                {
                    var filePaths = JsonConvert.DeserializeObject<List<string>>(
                        File.ReadAllText(_playlistPath));

                    // Фильтруем только существующие файлы
                    foreach (var filePath in filePaths.Where(File.Exists))
                    {
                        playlist.FilePaths.Add(filePath);
                    }

                    if (playlist.FilePaths.Count > 0)
                    {
                        playlist.CurrentTrackIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки плейлиста: {ex.Message}");
            }

            return playlist;
        }

        public void SavePlaylist(Playlist playlist)
        {
            try
            {
                var json = JsonConvert.SerializeObject(playlist.FilePaths, Formatting.Indented);
                File.WriteAllText(_playlistPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения плейлиста: {ex.Message}");
            }
        }

        public bool IsSupportedFormat(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLower();
            return ext == ".mp3" || ext == ".wav" || ext == ".flac" ||
                   ext == ".ogg" || ext == ".opus" || ext == ".m4a";
        }
    }
}