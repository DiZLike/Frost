using FrostWire.App;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FrostWire.Core
{
    public static class Logger
    {
        private static readonly string LogsDirectory;

        public static AppConfig AppConfig { get; set; }

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

        private static string GetCallerChain(int skipFrames = 2, int maxDepth = 5)
        {
            if (AppConfig == null || (!AppConfig.Debug.DebugEnable && !AppConfig.Debug.DebugStackView))
                return "";

            try
            {
                var stackTrace = new StackTrace(skipFrames: skipFrames, fNeedFileInfo: false);
                var frames = stackTrace.GetFrames();

                if (frames == null || frames.Length == 0)
                    return "";

                // Получаем методы в обратном порядке (от родителя к вызывающему)
                var methods = frames
                    .Take(maxDepth)
                    .Where(frame => frame.GetMethod() != null)
                    .Select(frame => frame.GetMethod())
                    .Where(method =>
                        method != null &&
                        method.DeclaringType != null &&
                        !method.DeclaringType.FullName.StartsWith("System.") &&
                        !method.DeclaringType.FullName.StartsWith("Microsoft."))
                    .Reverse() // Меняем порядок на обратный
                    .ToList();

                if (methods.Count == 0)
                    return "";

                var sb = new StringBuilder();

                for (int i = 0; i < methods.Count; i++)
                {
                    var method = methods[i];
                    string className = method.DeclaringType.Name;
                    string methodName = method.Name;

                    // Пропускаем методы самого Logger
                    if (className == "Logger" && methodName.StartsWith("Log"))
                        continue;

                    // Добавляем разделитель между методами
                    if (sb.Length > 0)
                    {
                        sb.Append(" > ");
                    }

                    sb.Append($"{className}.{methodName}");
                }

                // Если получилась пустая строка (все методы были Logger)
                if (sb.Length == 0)
                {
                    var firstMethod = frames.FirstOrDefault()?.GetMethod();
                    if (firstMethod != null)
                    {
                        sb.Append($"{firstMethod.DeclaringType?.Name}.{firstMethod.Name}");
                    }
                }

                return sb.ToString();
            }
            catch
            {
                return "";
            }
        }

        private static string FormatMessage(string level, string message, bool includeCaller = true)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            if (AppConfig != null && AppConfig.Debug.DebugStackView && includeCaller)
            {
                string callerChain = GetCallerChain(3);
                if (!string.IsNullOrEmpty(callerChain))
                {
                    return $"[{timestamp}] [{level}] [{callerChain}]\r\n{message}";
                }
            }

            return $"[{timestamp}] [{level}] {message}";
        }

        private static void WriteToLog(string message, string consoleMessage = null)
        {
            // В консоль - упрощенный формат с временем в формате HH:mm:ss
            string consoleTime = DateTime.Now.ToString("HH:mm:ss");
            string formattedConsoleMessage = $"[{consoleTime}] {consoleMessage ?? message}";

            Console.WriteLine(formattedConsoleMessage);

            // В файл - полный формат с датой и временем
            try
            {
                string logFile = GetDailyLogFileName();
                File.AppendAllText(logFile, message + Environment.NewLine);
            }
            catch
            {
                // Не падаем, если не можем записать в файл
            }
        }

        public static void Log(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string consoleMessage = $"[LOG]\t{message}";
            string fileMessage = FormatMessage("LOG", message, false);
            WriteToLog(fileMessage, consoleMessage);
        }

        public static void Error(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string consoleMessage = $"[ERROR]\t{message}";
            string fileMessage = FormatMessage("ERROR", message);
            WriteToLog(fileMessage, consoleMessage);
        }

        public static void Info(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string consoleMessage = $"[INFO]\t{message}";
            string fileMessage = FormatMessage("INFO", message);
            WriteToLog(fileMessage, consoleMessage);
        }

        public static void Debug(string message)
        {
            if (AppConfig != null && AppConfig.Debug.DebugEnable)
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string consoleMessage = $"[DEBUG]\t{message}";
                string fileMessage = FormatMessage("DEBUG", message);
                WriteToLog(fileMessage, consoleMessage);
            }
        }

        public static void Warning(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string consoleMessage = $"[WARNING]\t{message}";
            string fileMessage = FormatMessage("WARNING", message);
            WriteToLog(fileMessage, consoleMessage);
        }

        // Метод для прямого вызова без форматирования
        public static void Raw(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logMessage = $"[{timestamp}] {message}";
            string consoleMessage = message; // Для Raw выводим без временной метки в начале
            WriteToLog(logMessage, consoleMessage);
        }
    }
}