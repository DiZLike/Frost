using System;
using System.IO;
using System.Linq;

namespace gainer.Tags
{
    public class AutoTagger
    {
        public AudioTags ExtractFromPath(string filePath)
        {
            var tags = new AudioTags();

            try
            {
                var fullPath = Path.GetFullPath(filePath);
                var directory = Path.GetDirectoryName(fullPath);

                if (string.IsNullOrEmpty(directory))
                    return tags;

                // Получаем части пути
                var pathParts = directory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToList();

                // Извлекаем теги из структуры пути
                // Формат: MUSIC\жанр\исполнитель\файл.mp3

                // Название трека из имени файла (без расширения)
                tags.Title = Path.GetFileNameWithoutExtension(filePath);

                // Исполнитель из последней папки
                if (pathParts.Count >= 2)
                {
                    tags.Artist = pathParts[^1]; // последний элемент
                }

                // Жанр из предпоследней папки
                if (pathParts.Count >= 3)
                {
                    tags.Genre = pathParts[^2]; // предпоследний элемент
                }
            }
            catch (Exception)
            {
                // В случае ошибки возвращаем пустые теги
                // Не прерываем выполнение программы
            }

            return tags;
        }

        public void ApplyTags(TagLib.File file, AudioTags tags)
        {
            if (file == null || tags == null)
                return;

            try
            {
                // Заполняем только пустые теги
                if (string.IsNullOrWhiteSpace(file.Tag.Title) && !string.IsNullOrWhiteSpace(tags.Title))
                    file.Tag.Title = tags.Title;

                if (string.IsNullOrWhiteSpace(file.Tag.FirstPerformer) && !string.IsNullOrWhiteSpace(tags.Artist))
                    file.Tag.Performers = new[] { tags.Artist };

                if (string.IsNullOrWhiteSpace(file.Tag.FirstGenre) && !string.IsNullOrWhiteSpace(tags.Genre))
                    file.Tag.Genres = new[] { tags.Genre };
            }
            catch (Exception)
            {
                // Игнорируем ошибки при записи тегов
            }
        }
    }
}