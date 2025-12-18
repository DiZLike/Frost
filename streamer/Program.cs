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
                Console.WriteLine($"Версия: {version?.Major}.{version?.Minor}.{version?.Build}");
                Console.WriteLine($"Текущее время сервера: {DateTime.Now}");

                // Создаем и запускаем приложение
                var app = new StrimerApp();
                app.Run();

                // Ждем завершения
                Console.WriteLine("\nРадио стример запущен. Нажмите Ctrl+C для остановки...");

                // Используем ManualResetEvent для ожидания
                var stopEvent = new ManualResetEvent(false);
                Console.CancelKeyPress += (sender, e) => {
                    e.Cancel = true;
                    stopEvent.Set();
                };

                stopEvent.WaitOne();

                // Останавливаем приложение
                app.Stop();

                Console.WriteLine("Приложение остановлено.");
            }
            catch (Exception ex)
            {
                Core.Logger.Log($"Критическая ошибка: {ex}");
                Console.WriteLine("\nНажмите любую клавишу для выхода...");
                Console.ReadKey();
            }
        }
    }
}