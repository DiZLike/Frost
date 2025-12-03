using Strimer.Services;
using System.Runtime.InteropServices;

namespace Strimer.Core
{
    public class StrimerApp
    {
        private AppConfig _config;
        private RadioService _radioService;
        private bool _isRunning;

        public void Run(string[] args)
        {
            Logger.Info("=== Strimer Radio Starting ===");

            // 1. Загрузка конфигурации
            _config = new AppConfig();

            // 2. Копируем библиотеки для текущей ОС, если это первая настройка
            if (!_config.IsConfigured)
            {
                CopyNativeLibraries();
                RunSetupWizard();
            }

            // 3. Инициализируем и запускаем радио-сервис
            _radioService = new RadioService(_config);
            _radioService.Start();

            // 4. Главный цикл управления
            RunMainLoop();

            // 5. Очистка при завершении
            _radioService.Stop();
            Logger.Info("=== Strimer Radio Stopped ===");
        }

        private void CopyNativeLibraries()
        {
            Logger.Info("Copying native libraries for current platform...");

            try
            {
                // Определяем папку с библиотеками для текущей платформы
                string platformFolder = GetPlatformFolder();
                string libsFolder = Path.Combine(_config.BaseDirectory, "libs", platformFolder);

                if (!Directory.Exists(libsFolder))
                {
                    Logger.Error($"Library folder not found: {libsFolder}");
                    return;
                }

                // Копируем все библиотеки
                var files = Directory.GetFiles(libsFolder);
                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file);
                    string destPath = Path.Combine(_config.BaseDirectory, fileName);

                    try
                    {
                        File.Copy(file, destPath, true);
                        Logger.Info($"  Copied: {fileName}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"  Failed to copy {fileName}: {ex.Message}");
                    }
                }

                Logger.Info("Native libraries copied successfully");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to copy libraries: {ex.Message}");
            }
        }

        private string GetPlatformFolder()
        {
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            string arch = RuntimeInformation.ProcessArchitecture.ToString().ToLower();

            if (isWindows)
            {
                return arch == "x64" ? "win_x64" : "win_x86";
            }
            else // Linux
            {
                if (arch == "x64") return "linux_x64";
                if (arch == "x86") return "linux_x86";
                if (arch == "arm64") return "linux_arm64";
                if (arch == "arm") return "linux_arm";
            }

            return "unknown";
        }

        private void RunSetupWizard()
        {
            Logger.Info("\n=== SETUP WIZARD ===");

            Console.WriteLine("\nIceCast Server Configuration:");
            Console.Write($"Server [{_config.IceCastServer}]: ");
            var server = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(server))
                _config.IceCastServer = server;

            Console.Write($"Port [{_config.IceCastPort}]: ");
            var port = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(port))
                _config.IceCastPort = port;

            Console.Write($"Mount point [{_config.IceCastMount}]: ");
            var mount = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(mount))
                _config.IceCastMount = mount;

            Console.Write($"Username [{_config.IceCastUser}]: ");
            var user = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(user))
                _config.IceCastUser = user;

            Console.Write($"Password [{_config.IceCastPassword}]: ");
            var pass = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(pass))
                _config.IceCastPassword = pass;

            // Сохраняем конфигурацию
            _config.Save();
            Logger.Info("Setup completed. Configuration saved.");
        }

        private void RunMainLoop()
        {
            _isRunning = true;

            Console.WriteLine("\n=== STREAMING CONTROL ===");
            Console.WriteLine("Commands:");
            Console.WriteLine("  Q - Quit");
            Console.WriteLine("  S - Status");
            Console.WriteLine("  N - Next track");
            Console.WriteLine("  P - Pause/Resume");
            Console.WriteLine("  I - Stream info");
            Console.WriteLine("=======================\n");

            while (_isRunning)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;

                    switch (key)
                    {
                        case ConsoleKey.Q:
                            _isRunning = false;
                            Console.WriteLine("\nShutting down...");
                            break;

                        case ConsoleKey.S:
                            _radioService?.ShowStatus();
                            break;

                        case ConsoleKey.N:
                            _radioService?.PlayNextTrack();
                            break;

                        case ConsoleKey.P:
                            _radioService?.TogglePause();
                            break;

                        case ConsoleKey.I:
                            _radioService?.ShowStreamInfo();
                            break;
                    }
                }

                Thread.Sleep(100);
            }
        }
    }
}