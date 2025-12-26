using System;
using System.Collections.Generic;

namespace gainer.Processing
{
    public class StatisticsCollector
    {
        public int TotalFiles { get; set; }
        public int ProcessedFiles { get; set; }
        public int ProcessedSuccessfully { get; set; }
        public List<string> FailedFiles { get; } = new List<string>();
        public List<string> SuccessFiles { get; } = new List<string>();

        public void IncrementProcessed() => ProcessedFiles++;
        public void IncrementSuccess() => ProcessedSuccessfully++;

        public void AddSuccess(string fileInfo)
        {
            lock (SuccessFiles)
            {
                SuccessFiles.Add(fileInfo);
            }
        }

        public void AddFailed(string fileInfo)
        {
            lock (FailedFiles)
            {
                FailedFiles.Add(fileInfo);
            }
        }

        public double GetSuccessPercentage()
        {
            if (TotalFiles == 0) return 0;
            return (ProcessedSuccessfully * 100.0) / TotalFiles;
        }

        public void PrintStatistics()
        {
            Console.WriteLine();
            Console.WriteLine("=== ИТОГОВАЯ СТАТИСТИКА ===");
            Console.WriteLine();

            Console.WriteLine($"Всего файлов: {TotalFiles}");
            Console.WriteLine($"Успешно обработано: {ProcessedSuccessfully}");
            Console.WriteLine($"С ошибками: {FailedFiles.Count}");
            Console.WriteLine($"Процент успеха: {GetSuccessPercentage():F1}%");
            Console.WriteLine();

            if (SuccessFiles.Count > 0)
            {
                Console.WriteLine("=== УСПЕШНО ОБРАБОТАННЫЕ ФАЙЛЫ ===");
                Console.ForegroundColor = ConsoleColor.Green;
                foreach (var file in SuccessFiles)
                {
                    Console.WriteLine($"  {file}");
                }
                Console.ResetColor();
                Console.WriteLine();
            }

            if (FailedFiles.Count > 0)
            {
                Console.WriteLine("=== ФАЙЛЫ С ОШИБКАМИ ===");
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (var file in FailedFiles)
                {
                    Console.WriteLine($"  {file}");
                }
                Console.ResetColor();
                Console.WriteLine();
            }
        }
    }
}