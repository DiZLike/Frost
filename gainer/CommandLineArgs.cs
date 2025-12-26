using System;

namespace gainer
{
    public class CommandLineArgs
    {
        public bool UseKFilter { get; set; }
        public bool UseCustomTag { get; set; }
        public double TargetLufs { get; set; } = -23;
        public string FolderPath { get; set; } = string.Empty;

        public static CommandLineArgs Parse(string[] args)
        {
            var parsedArgs = new CommandLineArgs();

            if (args.Length < 7)
                throw new ArgumentException("Недостаточно аргументов");

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "-gain":
                        if (i + 1 < args.Length)
                            parsedArgs.UseKFilter = (args[i + 1].ToLower() == "k");
                        break;
                    case "-tag":
                        if (i + 1 < args.Length)
                            parsedArgs.UseCustomTag = (args[i + 1].ToLower() == "c");
                        break;
                    case "-target":
                        if (i + 1 < args.Length)
                        {
                            double.TryParse(args[i + 1], out double target);
                            parsedArgs.TargetLufs = target;
                        }
                        break;
                }
            }

            // Последний аргумент должен быть путь
            if (args.Length > 0 && !args[args.Length - 1].StartsWith("-"))
            {
                parsedArgs.FolderPath = args[args.Length - 1];
            }

            return parsedArgs;
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(FolderPath))
                throw new ArgumentException("Не указан путь к папке");

            if (!System.IO.Directory.Exists(FolderPath))
                throw new ArgumentException($"Папка не существует: {FolderPath}");
        }

        public override string ToString()
        {
            return $"K-фильтр: {(UseKFilter ? "включен" : "выключен")}, " +
                   $"Теги: {(UseCustomTag ? "кастомные" : "стандартные")}, " +
                   $"Цель: {TargetLufs} LUFS, " +
                   $"Папка: {FolderPath}";
        }
    }
}