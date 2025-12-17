using System;
using System.IO;

namespace Strimer.Core
{
    public static class Logger
    {
        private static readonly string LogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "strimer.log");

        public static void Log(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logMessage = $"[{timestamp}] {message}";

            // В консоль
            Console.WriteLine(logMessage);

            // В файл
            try
            {
                File.AppendAllText(LogFile, logMessage + Environment.NewLine);
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

        public static void Warning(string message)
        {
            Log($"[WARN] {message}");
        }
    }
}