namespace CueSplitter.Services
{
    public class FileService
    {
        public string? FindAudioFile(string cueFilePath)
        {
            string? cueDirectory = Path.GetDirectoryName(cueFilePath);
            if (string.IsNullOrEmpty(cueDirectory))
                return null;

            // Пробуем найти аудиофайл по расширениям
            string[] audioExtensions = { ".flac", ".wav", ".mp3", ".ape", ".wv", ".m4a", ".ogg" };

            foreach (var ext in audioExtensions)
            {
                var audioFile = Path.Combine(cueDirectory, Path.GetFileNameWithoutExtension(cueFilePath) + ext);
                if (File.Exists(audioFile))
                    return audioFile;
            }

            // Если не нашли по имени, ищем все аудиофайлы в директории
            foreach (var ext in audioExtensions)
            {
                var audioFiles = Directory.GetFiles(cueDirectory, $"*{ext}", SearchOption.TopDirectoryOnly);
                if (audioFiles.Length == 1) // Если только один файл с таким расширением
                    return audioFiles[0];
            }

            return null;
        }
    }
}