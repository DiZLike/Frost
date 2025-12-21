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
            Core.Logger.Info("[Приложение] Запуск Strimer Radio...");

            // 1. Загрузка конфигурации
            _config = new AppConfig();

            // 2. Если не настроено - автоматически копируем библиотеки и устанавливаем базовые настройки
            if (!_config.IsConfigured)
            {
                Core.Logger.Info("Приложение не настроено. Выполняется автоматическая настройка...");

                // Копируем нативные библиотеки
                CopyNativeLibraries();

                // Устанавливаем базовые параметры
                SetDefaultConfiguration();

                // Перезагружаем конфигурацию
                _config = new AppConfig();

                Core.Logger.Info("Автоматическая настройка завершена");

                // 3. Выводим сообщение о необходимости настройки и завершаем работу
                Console.WriteLine("\n" + new string('═', 50));
                Console.WriteLine("  НАСТРОЙКА ЗАВЕРШЕНА");
                Console.WriteLine(new string('═', 50));
                Console.WriteLine("\nАвтоматическая настройка завершена.");
                Console.WriteLine("Пожалуйста, настройте приложение перед запуском:");
                Console.WriteLine();
                Console.WriteLine("1. Отредактируйте файл конфигурации:");
                Console.WriteLine($"   {Path.Combine(_config.ConfigDirectory, "strimer.conf")}");
                Console.WriteLine();
                Console.WriteLine("2. Добавьте музыкальные файлы в плейлист:");
                Console.WriteLine($"   {_config.PlaylistFile}");
                Console.WriteLine("\n   Формат: track=C:\\путь\\к\\файлу.mp3?;");
                Console.WriteLine();
                Console.WriteLine("3. Настройте параметры IceCast сервера при необходимости.");

                Environment.Exit(0);
                return; // Дополнительная гарантия выхода
            }

            // 4. Запускаем радио сервис (только если уже настроено)
            Console.WriteLine("\n" + new string('═', 50));
            Console.WriteLine("  ЗАПУСК РАДИО-СТРИМА");
            if (_config.DebubEnable)
                Console.WriteLine($"Приложение запущено в режиме отладки!");
            Console.WriteLine(new string('═', 50) + "\n");

            _radioService = new RadioService(_config);
            _radioService.Start();

            _isRunning = true;
        }

        public void Stop()
        {
            if (!_isRunning)
                return;

            _radioService.Stop();
            _isRunning = false;
            Core.Logger.Info("=== Strimer Radio остановлен ===");
        }

        private void CopyNativeLibraries()
        {
            Core.Logger.Info("Копирование нативных библиотек для текущей платформы...");

            try
            {
                // Определяем папку с библиотеками для текущей платформы
                string platformFolder = GetPlatformFolder();
                string libsFolder = Path.Combine(_config.BaseDirectory, "bass_dll", platformFolder);

                if (!Directory.Exists(libsFolder))
                {
                    Core.Logger.Error($"Папка с библиотеками не найдена: {libsFolder}");
                    Console.WriteLine($"\nОШИБКА: Папка с библиотеками не найдена: {libsFolder}");
                    Console.WriteLine("Убедитесь, что папка 'bass_dll' существует с подпапками для платформ.");
                    Environment.Exit(1);
                    return;
                }

                // Копируем все библиотеки
                var files = Directory.GetFiles(libsFolder);
                bool copiedAny = false;

                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file);
                    string destPath = Path.Combine(_config.BaseDirectory, fileName);

                    try
                    {
                        File.Copy(file, destPath, true);
                        Core.Logger.Info($"  Скопировано: {fileName}");
                        copiedAny = true;
                    }
                    catch (Exception ex)
                    {
                        Core.Logger.Error($"  Не удалось скопировать {fileName}: {ex.Message}");
                    }
                }

                if (copiedAny)
                {
                    Core.Logger.Info("Нативные библиотеки успешно скопированы");
                }
                else
                {
                    Core.Logger.Warning("Ни одна библиотека не была скопирована");
                }
            }
            catch (Exception ex)
            {
                Core.Logger.Error($"Не удалось скопировать библиотеки: {ex.Message}");
                Console.WriteLine($"\nОШИБКА: Не удалось скопировать библиотеки: {ex.Message}");
                Environment.Exit(1);
            }
        }

        private void SetDefaultConfiguration()
        {
            try
            {
                Core.Logger.Info("Установка конфигурации по умолчанию...");

                // Создаем директорию для конфигурации
                Directory.CreateDirectory(_config.ConfigDirectory);

                // Устанавливаем базовые параметры
                _config.IceCastServer = "localhost";
                _config.IceCastPort = "8000";
                _config.IceCastMount = "live";
                _config.IceCastName = "Strimer Radio";
                _config.IceCastGenre = "Various";
                _config.IceCastUser = "source";
                _config.IceCastPassword = "hackme";

                _config.AudioDevice = -1;
                _config.SampleRate = 44100;

                _config.PlaylistFile = "playlist.txt";
                _config.SavePlaylistHistory = true;
                _config.DynamicPlaylist = false;

                _config.ScheduleEnable = false;
                _config.ScheduleFile = "schedule.json";

                _config.OpusBitrate = 128;
                _config.OpusMode = "vbr";
                _config.OpusContentType = "music";
                _config.OpusComplexity = 10;
                _config.OpusFrameSize = "20";

                _config.UseReplayGain = true;
                _config.UseCustomGain = false;

                _config.MyServerEnabled = false;
                _config.MyServerUrl = "";
                _config.MyServerKey = "";

                // Устанавливаем флаг конфигурации
                _config.IsConfigured = true;

                // Сохраняем конфигурацию
                _config.Save();

                Core.Logger.Info("Конфигурация по умолчанию сохранена");

                // Создаем пустой файл плейлиста если его нет
                if (!File.Exists(_config.PlaylistFile))
                {
                    try
                    {
                        File.WriteAllText(_config.PlaylistFile,
                            "# Плейлист Strimer\n" +
                            "# Добавляйте треки в формате: track=C:\\путь\\к\\файлу.mp3?;\n" +
                            "# Пример:\n" +
                            "# track=C:\\Music\\song1.mp3?;\n" +
                            "# track=C:\\Music\\song2.mp3?;\n");
                        Core.Logger.Info($"Создан пустой плейлист: {_config.PlaylistFile}");
                    }
                    catch (Exception ex)
                    {
                        Core.Logger.Error($"Не удалось создать файл плейлиста: {ex.Message}");
                        Console.WriteLine($"\nПРЕДУПРЕЖДЕНИЕ: Не удалось создать файл плейлиста: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Logger.Error($"Не удалось установить конфигурацию по умолчанию: {ex.Message}");
                Console.WriteLine($"\nОШИБКА: Не удалось установить конфигурацию по умолчанию: {ex.Message}");
                Environment.Exit(1);
            }
        }

        private string GetPlatformFolder()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return RuntimeInformation.ProcessArchitecture == Architecture.X64
                    ? @"win\x64"
                    : @"win\x86";
            }
            else // Linux
            {
                var arch = RuntimeInformation.ProcessArchitecture;

                return arch switch
                {
                    Architecture.X64 => "linux/x86_64",
                    Architecture.X86 => "linux/x86",
                    Architecture.Arm64 => "linux/aarch64",
                    Architecture.Arm => "linux/armhf",
                    _ => "unknown"
                };
            }
        }
    }
}