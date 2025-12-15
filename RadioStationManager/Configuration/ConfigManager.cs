using System;
using System.IO;
using System.Xml.Serialization;
using RadioStationManager.Configuration;

namespace RadioStationManager.Configuration
{
    public static class ConfigManager
    {
        private static readonly string ConfigPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "RadioStationManager", "config.xml");

        public static RadioConfig LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
                    return new RadioConfig();
                }

                var serializer = new XmlSerializer(typeof(RadioConfig));
                using var reader = new StreamReader(ConfigPath);
                return (RadioConfig)serializer.Deserialize(reader);
            }
            catch
            {
                return new RadioConfig();
            }
        }

        public static void SaveConfig(RadioConfig config)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(RadioConfig));
                using var writer = new StreamWriter(ConfigPath);
                serializer.Serialize(writer, config);
            }
            catch
            {
                // Игнорируем ошибки сохранения конфигурации
            }
        }
    }
}