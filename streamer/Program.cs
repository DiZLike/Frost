using Strimer.App;
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
                app.Run();

                // Ждем завершения
                Console.WriteLine("\nRadio streamer is running. Press Ctrl+C to stop...");

                // Используем ManualResetEvent для ожидания
                var stopEvent = new ManualResetEvent(false);
                Console.CancelKeyPress += (sender, e) => {
                    e.Cancel = true;
                    stopEvent.Set();
                };

                stopEvent.WaitOne();

                // Останавливаем приложение
                app.Stop();

                Console.WriteLine("Application stopped.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nFATAL ERROR: {ex.Message}");
                Core.Logger.Log($"Fatal error: {ex}");
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
        }
    }
}