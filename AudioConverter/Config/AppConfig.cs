using System.Text.Json.Serialization;

namespace OpusConverter.Config
{
    public class AppConfig
    {
        // Основные настройки
        [JsonPropertyName("input_directory")]
        public string InputDirectory { get; set; }

        [JsonPropertyName("output_directory")]
        public string OutputDirectory { get; set; }

        [JsonPropertyName("file_extensions")]
        public string[] SupportedExtensions { get; set; }

        // Настройки Opus (вложенный объект)
        [JsonPropertyName("opus_settings")]
        public OpusSettings OpusSettings { get; set; }

        // Дополнительные настройки
        [JsonPropertyName("copy_tags")]
        public bool CopyMetadata { get; set; }

        [JsonPropertyName("delete_source")]
        public bool DeleteSource { get; set; }

        [JsonPropertyName("overwrite_existing")]
        public bool OverwriteExisting { get; set; }

        // Паттерн названия выходных файлов
        [JsonPropertyName("output_filename_pattern")]
        public string OutputFilenamePattern { get; set; }

        // Пути (только для чтения)
        public string BaseDirectory { get; internal set; }
        public string OpusEncPath { get; internal set; }

        public AppConfig()
        {
            SupportedExtensions = new[] { ".mp3", ".wav", ".flac", ".m4a", ".aac", ".ogg" };
            OpusSettings = new OpusSettings();
            CopyMetadata = true;
            DeleteSource = false;
            OverwriteExisting = false;
            OutputFilenamePattern = "{artist} - {title}"; // Паттерн по умолчанию
        }
    }
}