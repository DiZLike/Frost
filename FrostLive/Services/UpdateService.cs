using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace FrostLive.Services
{
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
                string appPath = Application.ExecutablePath;

                // Путь к FrostUpdater.exe (предполагаем, что он в той же папке)
                string updaterPath = Path.Combine(Path.GetDirectoryName(appPath), "FrostUpdater.exe");
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

                // Копируем апдейтер
                File.Copy(updaterPath, tmpUpdaterExe, true);
                File.Copy(Path.Combine(Path.GetDirectoryName(appPath), "7z.exe"),
                         Path.Combine(tmpUpdaterPath, "7z.exe"), true);
                File.Copy(Path.Combine(Path.GetDirectoryName(appPath), "7z.dll"),
                         Path.Combine(tmpUpdaterPath, "7z.dll"), true);

                // Формируем аргументы для тихого режима
                string arguments = $"/silent {currentVersion} \"{actualVersionUrl}\" \"{zipUrl}\" \"{appPath}\"";

                // Запускаем обновлятор
                Process start = new Process();
                start.StartInfo.FileName = tmpUpdaterExe;
                start.StartInfo.WorkingDirectory = tmpUpdaterPath;
                start.StartInfo.Arguments = arguments;
                start.StartInfo.UseShellExecute = false;
                start.StartInfo.CreateNoWindow = true;

                Console.WriteLine("Запуск обновления...");
                bool ok = start.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при запуске обновления: {ex.Message}");
            }
        }
    }
}