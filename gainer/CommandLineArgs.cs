using System;
using System.IO;

namespace gainer.Processing
{
    public class CommandLineArgs
    {
        public bool UseKFilter { get; private set; }
        public bool UseCustomTag { get; private set; }
        public double TargetLufs { get; private set; }
        public string FolderPath { get; private set; } = string.Empty;
        public bool AutoTagEnabled { get; private set; }

        public static CommandLineArgs Parse(string[] args)
        {
            var parsedArgs = new CommandLineArgs
            {
                TargetLufs = -23.0 // значение по умолчанию
            };

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "-gain" when i + 1 < args.Length:
                        parsedArgs.UseKFilter = args[i + 1].ToLower() == "k";
                        i++;
                        break;

                    case "-tag" when i + 1 < args.Length:
                        parsedArgs.UseCustomTag = args[i + 1].ToLower() == "c";
                        i++;
                        break;

                    case "-target" when i + 1 < args.Length:
                        if (double.TryParse(args[i + 1], out double target))
                            parsedArgs.TargetLufs = target;
                        i++;
                        break;

                    case "-autotag" when i + 1 < args.Length:
                        parsedArgs.AutoTagEnabled = args[i + 1].ToLower() == "on";
                        i++;
                        break;

                    default:
                        // Если аргумент не начинается с "-", считаем его путем
                        if (!args[i].StartsWith("-") && parsedArgs.FolderPath == string.Empty)
                        {
                            parsedArgs.FolderPath = args[i].Trim('"');
                        }
                        break;
                }
            }

            return parsedArgs;
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(FolderPath))
                throw new ArgumentException("Не указан путь к папке");

            if (!Directory.Exists(FolderPath))
                throw new DirectoryNotFoundException($"Папка не найдена: {FolderPath}");

            if (TargetLufs > 0)
                throw new ArgumentException("Целевое значение LUFS должно быть отрицательным");
        }
    }
}