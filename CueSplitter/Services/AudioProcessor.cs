using System.Diagnostics;
using CueSplitter.Models;

namespace CueSplitter.Services
{
    public interface IAudioProcessor
    {
        void ConvertToFlac(string inputPath, string outputPath, CueSheet cueSheet, CueTrack track);
    }

    public class AudioProcessor : IAudioProcessor
    {
        private readonly IMetadataService _metadataService;

        public AudioProcessor(IMetadataService metadataService)
        {
            _metadataService = metadataService;
        }

        public void ConvertToFlac(string inputPath, string outputPath,
            CueSheet cueSheet, CueTrack track)
        {
            if (!File.Exists(inputPath))
                throw new FileNotFoundException($"Входной файл не найден: {inputPath}");

            // Создаем метаданные для ffmpeg
            var metadata = new Dictionary<string, string>
            {
                ["title"] = track.Title,
                ["artist"] = !string.IsNullOrEmpty(track.Artist) ? track.Artist : cueSheet.Artist,
                ["album"] = cueSheet.Title,
                ["track"] = track.TrackNumber.ToString(),
                ["genre"] = cueSheet.Genre ?? "",
                ["date"] = cueSheet.Year > 0 ? cueSheet.Year.ToString() : "",
                ["comment"] = $"Split from {Path.GetFileName(cueSheet.SourceCueFile)}"
            };

            // Запускаем ffmpeg синхронно
            ExecuteFfmpeg(inputPath, outputPath, metadata);

            // Удаляем временный WAV файл
            File.Delete(inputPath);
        }

        private void ExecuteFfmpeg(string inputPath, string outputPath, Dictionary<string, string> metadata)
        {
            // Формируем аргументы метаданных
            var metadataArgs = string.Join(" ", metadata
                .Where(kv => !string.IsNullOrEmpty(kv.Value))
                .Select(kv => $"-metadata {kv.Key}=\"{EscapeArgument(kv.Value)}\""));

            // Аргументы для ffmpeg
            var arguments = $"-i \"{inputPath}\" -c:a flac -compression_level 8 {metadataArgs} \"{outputPath}\"";

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using (var process = new Process { StartInfo = processStartInfo })
            {
                Console.WriteLine($"Запуск ffmpeg: {arguments}");

                process.Start();

                // Читаем вывод в реальном времени
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"Ошибка ffmpeg (код {process.ExitCode}): {error}");
                }

                if (!string.IsNullOrEmpty(error) && !error.Contains("Duration:"))
                {
                    Console.WriteLine($"Предупреждение ffmpeg: {error}");
                }
            }
        }

        private string EscapeArgument(string argument)
        {
            return argument.Replace("\"", "\\\"");
        }
    }
}