using System;
using System.IO;

namespace Strimer.Core
{
    public static class Logger
    {
        private static readonly string LogsDirectory;

        static Logger()
        {
            // Определяем путь к папке logs
            LogsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

            // Создаем папку logs, если она не существует
            EnsureLogsDirectoryExists();
        }

        private static void EnsureLogsDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(LogsDirectory))
                {
                    Directory.CreateDirectory(LogsDirectory);
                }
            }
            catch (Exception ex)
            {
                // Записываем в консоль, если не удалось создать папку
                Console.WriteLine($"[ERROR] Не удалось создать папку логов: {ex.Message}");
            }
        }

        private static string GetDailyLogFileName()
        {
            // Формируем имя файла с датой: strimer_2023-12-25.log
            string date = DateTime.Now.ToString("yyyy-MM-dd");
            return Path.Combine(LogsDirectory, $"strimer_{date}.log");
        }

        public static void Log(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logMessage = $"[{timestamp}] {message}";

            // В консоль
            Console.WriteLine(logMessage);

            // В файл
            try
            {
                string logFile = GetDailyLogFileName();
                File.AppendAllText(logFile, logMessage + Environment.NewLine);
            }
            catch
            {
                // Не падаем, если не можем записать в файл
            }
        }

        public static void Error(string message)
        {
            Log($"[ERROR] {message}");
        }

        public static void Info(string message)
        {
            Log($"[INFO] {message}");
        }

        public static void Debug(string message)
        {
            Log($"[DEBUG] {message}");
        }

        public static void Warning(string message)
        {
            Log($"[WARNING] {message}");
        }
    }
}