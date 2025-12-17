using Strimer.Services;
using System.Runtime.InteropServices;

namespace Strimer.App
{
    public class StrimerApp
    {
        private AppConfig _config;
        private RadioService _radioService;
        private bool _isRunning;

        public RadioService RadioService => _radioService;

        public void Run()
        {
            Core.Logger.Info("=== Strimer Radio Starting ===");

            // 1. Загрузка конфигурации
            _config = new AppConfig();

            // 2. Показываем реальный статус
            Console.WriteLine($"\n=== CONFIGURATION STATUS ===");
            Console.WriteLine($"IsConfigured property: {_config.IsConfigured}");
            Console.WriteLine($"Config file: {Path.Combine(_config.ConfigDirectory, "strimer.conf")}");
            Console.WriteLine($"File exists: {File.Exists(Path.Combine(_config.ConfigDirectory, "strimer.conf"))}");

            // 3. Если не настроено - запускаем мастер настройки
            if (!_config.IsConfigured)
            {
                Console.WriteLine("\n" + new string('═', 50));
                Console.WriteLine("  STRIMER RADIO - FIRST TIME SETUP");
                Console.WriteLine(new string('═', 50));

                Console.WriteLine("\nThe application needs to be configured before use.");
                Console.WriteLine("Press any key to start setup wizard...");
                Console.ReadKey();

                // Копируем библиотеки
                CopyNativeLibraries();

                // Запускаем мастер настройки
                RunSetupWizard();

                // После мастера настройки снова загружаем конфиг
                _config = new AppConfig();
            }
            else
            {
                Console.WriteLine("\nConfiguration already set up.");
            }

            // 4. Запускаем радио сервис
            Console.WriteLine("\n" + new string('═', 50));
            Console.WriteLine("  STARTING RADIO STREAM");
            Console.WriteLine(new string('═', 50) + "\n");

            _radioService = new RadioService(_config);
            _radioService.Start();

            _isRunning = true;
            Core.Logger.Info("=== Strimer Radio Started ===");
        }

        public void Stop()
        {
            if (!_isRunning)
                return;

            _radioService.Stop();
            _isRunning = false;
            Core.Logger.Info("=== Strimer Radio Stopped ===");
        }

        private void CopyNativeLibraries()
        {
            Core.Logger.Info("Copying native libraries for current platform...");

            try
            {
                // Определяем папку с библиотеками для текущей платформы
                string platformFolder = GetPlatformFolder();
                string libsFolder = Path.Combine(_config.BaseDirectory, "libs", platformFolder);

                if (!Directory.Exists(libsFolder))
                {
                    Core.Logger.Error($"Library folder not found: {libsFolder}");
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
                        Core.Logger.Info($"  Copied: {fileName}");
                    }
                    catch (Exception ex)
                    {
                        Core.Logger.Error($"  Failed to copy {fileName}: {ex.Message}");
                    }
                }

                Core.Logger.Info("Native libraries copied successfully");
            }
            catch (Exception ex)
            {
                Core.Logger.Error($"Failed to copy libraries: {ex.Message}");
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
            try
            {
                Console.Clear();
                Console.WriteLine("╔══════════════════════════════════════════════╗");
                Console.WriteLine("║        STRIMER RADIO - SETUP WIZARD         ║");
                Console.WriteLine("╚══════════════════════════════════════════════╝\n");

                Console.WriteLine("Step 1 of 4: IceCast Server Settings");
                Console.WriteLine("-------------------------------------");

                // IceCast настройки
                Console.Write($"Server address [{_config.IceCastServer}]: ");
                var server = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(server))
                    _config.IceCastServer = server;

                Console.Write($"Port [{_config.IceCastPort}]: ");
                var port = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(port))
                    _config.IceCastPort = port;

                Console.Write($"Mount point (stream URL) [{_config.IceCastMount}]: ");
                var mount = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(mount))
                    _config.IceCastMount = mount;

                Console.Write($"Stream name [{_config.IceCastName}]: ");
                var name = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(name))
                    _config.IceCastName = name;

                Console.Write($"Username [{_config.IceCastUser}]: ");
                var user = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(user))
                    _config.IceCastUser = user;

                Console.Write($"Password [{_config.IceCastPassword}]: ");
                var pass = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(pass))
                    _config.IceCastPassword = pass;

                Console.WriteLine("\nStep 2 of 4: Playlist Settings");
                Console.WriteLine("--------------------------------");

                // Playlist
                Console.Write($"Path to playlist file [{_config.PlaylistFile}]: ");
                var playlist = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(playlist))
                    _config.PlaylistFile = playlist;

                // Проверяем плейлист
                if (File.Exists(_config.PlaylistFile))
                {
                    Console.WriteLine($"✓ Playlist file found: {_config.PlaylistFile}");
                }
                else
                {
                    Console.WriteLine($"✗ Playlist file NOT found: {_config.PlaylistFile}");
                    Console.WriteLine("Please create a playlist file with this format:");
                    Console.WriteLine("  track=C:\\Music\\song1.mp3?;");
                    Console.WriteLine("  track=C:\\Music\\song2.mp3?;");
                    Console.Write("Create empty playlist file now? (y/n): ");

                    if (Console.ReadKey().Key == ConsoleKey.Y)
                    {
                        try
                        {
                            string dir = Path.GetDirectoryName(_config.PlaylistFile);
                            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                                Directory.CreateDirectory(dir);

                            File.WriteAllText(_config.PlaylistFile, "# Strimer Playlist\n# Add tracks in format: track=C:\\path\\to\\file.mp3?;\n");
                            Console.WriteLine($"\n✓ Empty playlist created: {_config.PlaylistFile}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"\n✗ Failed to create playlist: {ex.Message}");
                        }
                    }
                    Console.WriteLine();
                }

                Console.WriteLine("\nStep 3 of 4: Encoder Settings");
                Console.WriteLine("--------------------------------");

                // Encoder settings
                Console.Write($"Bitrate (kbps) [{_config.OpusBitrate}]: ");
                var bitrate = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(bitrate) && int.TryParse(bitrate, out int bitrateValue))
                    _config.OpusBitrate = bitrateValue;

                Console.Write($"Mode (vbr/cvbr/hard-cbr) [{_config.OpusMode}]: ");
                var mode = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(mode))
                    _config.OpusMode = mode;

                Console.WriteLine("\nStep 4 of 4: Saving Configuration");
                Console.WriteLine("-----------------------------------");

                // Сохраняем конфигурацию
                _config.Save();
                _config.IsConfigured = true;

                Console.WriteLine("\n✓ Configuration saved successfully!");
                Console.WriteLine("\nSummary:");
                Console.WriteLine($"  Server: {_config.IceCastServer}:{_config.IceCastPort}/{_config.IceCastMount}");
                Console.WriteLine($"  Playlist: {_config.PlaylistFile}");
                Console.WriteLine($"  Bitrate: {_config.OpusBitrate} kbps ({_config.OpusMode})");

                Console.WriteLine("\nPress any key to start streaming...");
                Console.ReadKey();
                Console.Clear();
            }
            catch (Exception ex)
            {
                Core.Logger.Error($"Setup wizard failed: {ex.Message}");
                Console.WriteLine($"\nSetup failed: {ex.Message}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                Environment.Exit(1);
            }
        }
    }
}