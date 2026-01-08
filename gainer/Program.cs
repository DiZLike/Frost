using System;
using System.Linq;
using gainer.Processing;

namespace gainer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //-gain n -tag c -target -23 -autotag off "C:\Users\Evgeny\Desktop\777"
            args = new string[] { "-gain", "n", "-tag", "c", "-target", "-23", "-autotag", "off", "\"C:\\Users\\Evgeny\\Desktop\\777\"" };
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
            Console.WriteLine("  rms_main=-25.12");
            Console.WriteLine("  main_L=-25.1 main_R=-25.2");
            Console.WriteLine("  sub_L=-30.1 sub_R=-30.3");
            Console.WriteLine("  low_L=-28.5 low_R=-28.7");
            Console.WriteLine("  mid_L=-26.2 mid_R=-26.4");
            Console.WriteLine("  high_L=-27.8 high_R=-27.9");
            Console.WriteLine();
            Console.WriteLine("Полосы частот:");
            Console.WriteLine("  Main: 20-20000 Гц (полный спектр)");
            Console.WriteLine("  Sub: 0-120 Гц (саб)");
            Console.WriteLine("  Low: 120-500 Гц (басы)");
            Console.WriteLine("  Mid: 500-4000 Гц (средние)");
            Console.WriteLine("  High: 4000+ Гц (верха)");
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
            Console.WriteLine($"Расчет: ReplayGain + RMS по полосам");
            Console.WriteLine($"Полосы: Main, Sub, Low, Mid, High");
            Console.WriteLine($"Каналы: L/R раздельно");
        }
    }
}