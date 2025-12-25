using System.Text.Json;

namespace OpusConverter.Config
{
    public static class ConfigManager
    {
        public static AppConfig LoadConfig(string configPath, string baseDir)
        {
            AppConfig config;

            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    config = JsonSerializer.Deserialize<AppConfig>(json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка загрузки конфигурации: {ex.Message}");
                    config = new AppConfig();
                }
            }
            else
            {
                config = new AppConfig();
            }

            config.BaseDirectory = baseDir;
            config.OpusEncPath = GetOpusEncPath(baseDir);

            if (!File.Exists(configPath))
            {
                SaveConfig(config, configPath);
            }

            return config;
        }

        private static string GetOpusEncPath(string baseDir)
        {
            // Фиксированный путь к opusenc в папке bass
            string opusPath = Path.Combine(baseDir, "bass", "opusenc.exe");

            if (!File.Exists(opusPath))
            {
                // Проверяем стандартное расположение
                opusPath = Path.Combine(baseDir, "opusenc.exe");
            }

            return opusPath;
        }

        public static void SaveConfig(AppConfig config, string configPath)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(configPath, json);
                Console.WriteLine($"Конфигурация сохранена в {configPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения конфигурации: {ex.Message}");
            }
        }
    }
}