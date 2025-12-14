using Strimer.Core;
using System.Reflection;

namespace Strimer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("=== Strimer Radio Streamer ===");

                // Получаем версию из сборки
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                Console.WriteLine($"Version: {version?.Major}.{version?.Minor}.{version?.Build}");
                Console.WriteLine($"Текущее время сервера: {DateTime.Now}");

                // Создаем и запускаем приложение
                var app = new StrimerApp();
                app.Run(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nFATAL ERROR: {ex.Message}");
                Logger.Log($"Fatal error: {ex}");
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
        }
    }
}