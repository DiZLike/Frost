using System;
using System.Linq;
using gainer.Processing;

namespace gainer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length < 7)
                {
                    PrintUsage();
                    return;
                }

                var commandLineArgs = CommandLineArgs.Parse(args);
                commandLineArgs.Validate();

                var statistics = new StatisticsCollector();
                var progressManager = new ConsoleProgressManager(statistics);
                var threadManager = new ThreadManager(commandLineArgs, statistics, progressManager);

                threadManager.ProcessAllFiles();

                statistics.PrintStatistics();
                PrintSummary(commandLineArgs);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ошибка: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("Нажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Использование: gainer.exe -gain n/k -tag n/c -target -23 -autotag on/off \"путь_к_папке\"");
            Console.WriteLine("Пример: gainer.exe -gain k -tag c -target -23 -autotag on \"C:\\Music\"");
            Console.WriteLine();
            Console.WriteLine("Аргументы:");
            Console.WriteLine("  -gain n/k     : n - без K-фильтра, k - с K-фильтром");
            Console.WriteLine("  -tag n/c      : n - стандартные теги, c - кастомные теги");
            Console.WriteLine("  -target -23   : целевое значение LUFS (по умолчанию -23)");
            Console.WriteLine("  -autotag on/off: автоматическое заполнение тегов из пути");
            Console.WriteLine("  \"путь\"        : путь к папке с аудиофайлами");
            Console.WriteLine();
            Console.WriteLine("Результаты сохраняются в формате:");
            Console.WriteLine("  replay-gain=-3.5");
            Console.WriteLine("  rms=-25.12");
        }

        private static void PrintSummary(CommandLineArgs args)
        {
            Console.WriteLine("=== СВОДКА ===");
            Console.WriteLine($"Папка: {args.FolderPath}");
            Console.WriteLine($"Время завершения: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"K-фильтр: {(args.UseKFilter ? "ДА" : "НЕТ")}");
            Console.WriteLine($"Тип тегов: {(args.UseCustomTag ? "Кастомный" : "Стандартный")}");
            Console.WriteLine($"Целевое LUFS: {args.TargetLufs}");
            Console.WriteLine($"Авто-теги: {(args.AutoTagEnabled ? "ВКЛ" : "ВЫКЛ")}");
            Console.WriteLine($"Расчет: ReplayGain + RMS (в децибелах)");
        }
    }
}