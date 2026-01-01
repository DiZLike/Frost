using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostLive.Services
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Windows;

    public class UpdateService
    {
        public static string GetAssemblyVersion()
        {
            var assembly = Assembly.GetEntryAssembly() ??
                          Assembly.GetCallingAssembly() ??
                          Assembly.GetExecutingAssembly();

            var version = assembly.GetName().Version;
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }
        public static void CheckAndUpdate()
        {
            try
            {
                string currentVersion = GetAssemblyVersion();
                string actualVersionUrl = "http://r.dlike.ru/get-desktop-version";
                string zipUrl = "http://r.dlike.ru/files/frostlive/app.7z";
                string appPath = Process.GetCurrentProcess().MainModule.FileName;

                // Путь к FrostUpdater.exe (предполагаем, что он в той же папке)
                string updaterPath = Path.Combine(Path.GetDirectoryName(appPath),"FrostUpdater.exe");
                string tmpUpdaterPath = Path.Combine(Path.GetDirectoryName(appPath), "tmp");
                string tmpUpdaterExe = Path.Combine(tmpUpdaterPath, "FrostUpdater.exe");

                // Проверяем наличие обновлятора
                if (!File.Exists(updaterPath))
                {
                    Console.WriteLine("FrostUpdater не найден!");
                    return;
                }

                // Создаем временную папку
                if (!Directory.Exists(tmpUpdaterPath))
                    Directory.CreateDirectory(tmpUpdaterPath);
                if (!Directory.Exists(tmpUpdaterPath))
                    return;

                // Копируем апдейтер
                File.Copy(Path.Combine(Path.GetDirectoryName(appPath), "FrostUpdater.exe"), Path.Combine(tmpUpdaterPath,  "FrostUpdater.exe"), true);
                File.Copy(Path.Combine(Path.GetDirectoryName(appPath), "7z.exe"), Path.Combine(tmpUpdaterPath, "7z.exe"), true);
                File.Copy(Path.Combine(Path.GetDirectoryName(appPath), "7z.dll"), Path.Combine(tmpUpdaterPath, "7z.dll"), true);

                // Формируем аргументы для тихого режима
                string arguments = $"/silent {currentVersion} \"{actualVersionUrl}\" \"{zipUrl}\" \"{appPath}\"";

                // Запускаем обновлятор
                Process start = new Process();
                start.StartInfo.FileName = tmpUpdaterExe;
                start.StartInfo.WorkingDirectory = tmpUpdaterPath;
                start.StartInfo.Arguments = arguments;
                start.StartInfo.UseShellExecute = false;
                start.StartInfo.CreateNoWindow = true;
                start.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                Console.WriteLine("Запуск обновления...");
                bool ok = start.Start();
                //start.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при запуске обновления: {ex.Message}");
            }
        }
    }
}
