using OpusConverter.Config;
using OpusConverter.Core;
using OpusConverter.Utilities;

namespace OpusConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ConsoleUI.PrintSectionHeader("Audio Opus Converter", 50);

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string configPath = Path.Combine(baseDir, "config.json");

                // Если указан конфиг в аргументах
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "--config" && i + 1 < args.Length)
                    {
                        configPath = args[++i];
                        break;
                    }
                }

                var config = ConfigManager.LoadConfig(configPath, baseDir);

                // Применяем аргументы командной строки
                ApplyCommandLineArgs(args, config);

                DependencyManager.SetupDirectoryStructure(baseDir, config);

                using (var converter = new AudioConverter(config))
                {
                    converter.ConvertAll();
                }
            }
            catch (Exception ex)
            {
                ConsoleUI.PrintError($"Ошибка: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        static void ApplyCommandLineArgs(string[] args, AppConfig config)
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--input":
                        if (i + 1 < args.Length)
                            config.InputDirectory = args[++i];
                        break;
                    case "--output":
                        if (i + 1 < args.Length)
                            config.OutputDirectory = args[++i];
                        break;
                    case "--bitrate":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int bitrate))
                            config.OpusSettings.Bitrate = bitrate;
                        break;
                    case "--help":
                    case "-h":
                        ShowHelp();
                        Environment.Exit(0);
                        break;
                }
            }
        }

        static void ShowHelp()
        {
            ConsoleUI.PrintSectionHeader("Справка", 40);
            Console.WriteLine("Использование: AudioOpusConverter [параметры]");
            Console.WriteLine();
            Console.WriteLine("Плейсхолдеры для имен файлов:");
            ConsoleUI.PrintInfo("{artist}", "Исполнитель");
            ConsoleUI.PrintInfo("{title}", "Название трека");
            ConsoleUI.PrintInfo("{album}", "Альбом");
            ConsoleUI.PrintInfo("{year}", "Год");
            ConsoleUI.PrintInfo("{track}", "Номер трека (с ведущим нулём)");
            ConsoleUI.PrintInfo("{genre}", "Жанр");
            ConsoleUI.PrintInfo("{performer}", "Исполнитель (первый)");
            ConsoleUI.PrintInfo("{composer}", "Композитор");
            ConsoleUI.PrintInfo("{directory}", "Относительный путь");
            ConsoleUI.PrintInfo("{filename}", "Имя файла без расширения");
            ConsoleUI.PrintInfo("{extension}", "Расширение исходного файла");
            Console.WriteLine();
            Console.WriteLine("Создание структуры папок:");
            ConsoleUI.PrintInfo("genre/artist/album/track - title", "Используйте / для создания папок");
            ConsoleUI.PrintInfo("artist/album/track. title", "Используйте \\ для создания папок");
            Console.WriteLine();
            ConsoleUI.PrintInfo("--config <путь>", "Путь к файлу конфигурации");
            ConsoleUI.PrintInfo("--input <путь>", "Входная директория");
            ConsoleUI.PrintInfo("--output <путь>", "Выходная директория");
            ConsoleUI.PrintInfo("--bitrate <значение>", "Битрейт в кбит/с");
            ConsoleUI.PrintInfo("--help, -h", "Показать эту справку");
        }
    }
}