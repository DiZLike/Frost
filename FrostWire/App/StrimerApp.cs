using FrostWire.Services;
using System.Runtime.InteropServices;

namespace FrostWire.App
{
    public class StrimerApp
    {
        private AppConfig _config;
        private RadioService _radioService;
        private bool _isRunning;

        public RadioService RadioService => _radioService;

        public void Run()
        {
            Core.Logger.Info("[App] Запуск Strimer Radio...");

            // 1. Загрузка конфигурации
            _config = new AppConfig();
            // 2. Если не настроено - автоматически копируем библиотеки
            if (!_config.IsConfigured)
            {
                Core.Logger.Info("[App] Приложение не настроено...");

                // Копируем нативные библиотеки
                CopyNativeLibraries();

                // 3. Выводим сообщение о необходимости настройки и завершаем работу
                Console.WriteLine("\n" + new string('═', 50));
                Console.WriteLine("Пожалуйста, настройте приложение перед запуском:");
                Console.WriteLine();
                Console.WriteLine("1. Отредактируйте файл конфигурации:");
                Console.WriteLine($"   {Path.Combine(_config.ConfigDirectory, "strimer.conf")}");
                Console.WriteLine();
                Console.WriteLine("2. Добавьте музыкальные файлы в плейлист:");
                Console.WriteLine("   Формат: track=C:\\путь\\к\\файлу.mp3?;");
                Console.WriteLine();
                Console.WriteLine("3. Настройте параметры IceCast сервера при необходимости.");
                _config.SetConfigOk();

                Environment.Exit(0);
                return; // Дополнительная гарантия выхода
            }
            if (!_config.LoadConfig())
                throw new Exception("Ошибка загрузки конфигурации!");

            // 4. Запускаем радио сервис (только если уже настроено)
            Console.WriteLine("\n" + new string('═', 50));
            Console.WriteLine("  ЗАПУСК РАДИО-СТРИМА");
            if (_config.Debug.DebugEnable)
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
            Core.Logger.Info("[App] === Strimer Radio остановлен ===");
        }

        private void CopyNativeLibraries()
        {
            Core.Logger.Info("[App] Копирование нативных библиотек для текущей платформы...");

            try
            {
                // Определяем папку с библиотеками для текущей платформы
                string platformFolder = GetPlatformFolder();
                string libsFolder = Path.Combine(_config.BaseDirectory, "bass_dll", platformFolder);

                if (!Directory.Exists(libsFolder))
                {
                    Core.Logger.Error($"[App] Папка с библиотеками не найдена: {libsFolder}");
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
                        Core.Logger.Info($"[App] Скопировано: {fileName}");
                        copiedAny = true;
                    }
                    catch (Exception ex)
                    {
                        Core.Logger.Error($"[App] Не удалось скопировать {fileName}: {ex.Message}");
                    }
                }

                if (copiedAny)
                {
                    Core.Logger.Info("[App] Нативные библиотеки успешно скопированы");
                }
                else
                {
                    Core.Logger.Warning("[App] Ни одна библиотека не была скопирована");
                }
            }
            catch (Exception ex)
            {
                Core.Logger.Error($"[App] Не удалось скопировать библиотеки: {ex.Message}");
                Console.WriteLine($"\nОШИБКА: Не удалось скопировать библиотеки: {ex.Message}");
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