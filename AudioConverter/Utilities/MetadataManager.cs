namespace OpusConverter.Utilities
{
    public static class MetadataManager
    {
        public static bool CopyMetadata(string sourcePath, string targetPath)
        {
            try
            {
                using (var sourceFile = TagLib.File.Create(sourcePath))
                using (var targetFile = TagLib.File.Create(targetPath))
                {
                    // Копируем основные теги
                    targetFile.Tag.Title = sourceFile.Tag.Title;
                    targetFile.Tag.Performers = sourceFile.Tag.Performers;
                    targetFile.Tag.AlbumArtists = sourceFile.Tag.AlbumArtists;
                    targetFile.Tag.Album = sourceFile.Tag.Album;
                    targetFile.Tag.Year = sourceFile.Tag.Year;
                    targetFile.Tag.Track = sourceFile.Tag.Track;
                    targetFile.Tag.TrackCount = sourceFile.Tag.TrackCount;
                    targetFile.Tag.Disc = sourceFile.Tag.Disc;
                    targetFile.Tag.DiscCount = sourceFile.Tag.DiscCount;
                    targetFile.Tag.Genres = sourceFile.Tag.Genres;
                    targetFile.Tag.Comment = sourceFile.Tag.Comment;
                    targetFile.Tag.Composers = sourceFile.Tag.Composers;
                    targetFile.Tag.Copyright = sourceFile.Tag.Copyright;
                    targetFile.Tag.Lyrics = sourceFile.Tag.Lyrics;

                    // Копируем обложки
                    if (sourceFile.Tag.Pictures != null && sourceFile.Tag.Pictures.Length > 0)
                    {
                        targetFile.Tag.Pictures = sourceFile.Tag.Pictures;
                    }

                    // Сохраняем
                    targetFile.Save();

                    Console.WriteLine("        Метаданные скопированы");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"        Ошибка копирования метаданных: {ex.Message}");
                return false;
            }
        }
    }
}