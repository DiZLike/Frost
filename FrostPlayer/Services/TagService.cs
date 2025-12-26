using FrostPlayer.Models;
using System;
using TagLib;

namespace FrostPlayer.Services
{
    public class TagService
    {
        /// <summary>
        /// Получает информацию о треке из файла
        /// </summary>
        /// <param name="filePath">Путь к аудиофайлу</param>
        /// <returns>Информация о треке</returns>
        public AudioTrack GetTrackInfo(string filePath)
        {
            try
            {
                // Используем TagLib для чтения метаданных
                using (var file = TagLib.File.Create(filePath))
                {
                    // Создаем объект трека
                    var track = new AudioTrack(filePath);

                    // 1. Получаем длительность
                    track.Duration = file.Properties.Duration.TotalSeconds;

                    // 2. Получаем название трека
                    // Если тег Title пустой, используем имя файла
                    track.Title = !string.IsNullOrEmpty(file.Tag.Title)
                        ? file.Tag.Title
                        : System.IO.Path.GetFileNameWithoutExtension(filePath);

                    // 3. Получаем исполнителя
                    // Если тег FirstPerformer пустой, используем "Неизвестный исполнитель"
                    track.Artist = !string.IsNullOrEmpty(file.Tag.FirstPerformer)
                        ? file.Tag.FirstPerformer
                        : "Неизвестный исполнитель";

                    // 4. Получаем название альбома
                    track.Album = !string.IsNullOrEmpty(file.Tag.Album)
                        ? file.Tag.Album
                        : "Неизвестный альбом";

                    // Дополнительно можно получить:
                    // - Год выпуска: file.Tag.Year
                    // - Жанр: file.Tag.FirstGenre
                    // - Номер трека: file.Tag.Track
                    // - Обложка: file.Tag.Pictures

                    return track;
                }
            }
            catch (Exception ex)
            {
                // Если произошла ошибка при чтении тегов
                Console.WriteLine($"Ошибка чтения тегов файла {filePath}: {ex.Message}");

                // Возвращаем трек с базовой информацией из имени файла
                return new AudioTrack(filePath)
                {
                    Duration = 0,
                    Title = System.IO.Path.GetFileNameWithoutExtension(filePath),
                    Artist = "Неизвестный исполнитель",
                    Album = "Неизвестный альбом"
                };
            }
        }

        /// <summary>
        /// Получает информацию о нескольких треках
        /// </summary>
        public List<AudioTrack> GetMultipleTrackInfo(
            List<string> filePaths)
        {
            var tracks = new System.Collections.Generic.List<AudioTrack>();

            foreach (var filePath in filePaths)
            {
                tracks.Add(GetTrackInfo(filePath));
            }

            return tracks;
        }
    }
}