using OpusConverter.Config;

public static class DependencyManager
{
    public static void SetupDirectoryStructure(string baseDir, AppConfig config)
    {
        Console.WriteLine("\nПроверка структуры директорий...");

        // Создаем только необходимые директории
        Directory.CreateDirectory(config.InputDirectory);
        Directory.CreateDirectory(config.OutputDirectory);
        Directory.CreateDirectory(Path.Combine(baseDir, "logs"));

        // Проверяем наличие папки с плагинами
        string pluginsDir = Path.Combine(baseDir, "bass", "plugins", "dec");
        if (Directory.Exists(pluginsDir))
        {
            var pluginFiles = Directory.GetFiles(pluginsDir, "*.dll");
            Console.WriteLine($"    Папка с плагинами найдена: {pluginFiles.Length} плагин(ов)");

            // Выводим список найденных плагинов
            foreach (var plugin in pluginFiles)
            {
                Console.WriteLine($"      {Path.GetFileName(plugin)}");
            }
        }
        else
        {
            Console.WriteLine("    Папка с плагинами не найдена: bass/plugins/dec/");
        }

        // Проверяем opusenc
        if (!File.Exists(config.OpusEncPath))
        {
            Console.WriteLine($"\n    Внимание: opusenc.exe не найден по пути:");
            Console.WriteLine($"      {config.OpusEncPath}");
            Console.WriteLine("    Поместите opusenc.exe в папку 'bass' или в корень программы");
        }
        else
        {
            Console.WriteLine($"    opusenc.exe найден: {config.OpusEncPath}");
        }

        // Проверяем Bass.dll
        string bassDll = Path.Combine(baseDir, "bass.dll");
        if (!File.Exists(bassDll))
        {
            Console.WriteLine($"\n    Внимание: bass.dll не найден");
            Console.WriteLine("    Скачайте с https://www.un4seen.com/ и поместите в корень программы");
        }
        else
        {
            Console.WriteLine("    bass.dll найден");
        }

        // Проверяем bassenc_opus.dll (для кодирования в Opus)
        string bassEncOpusDll = Path.Combine(baseDir, "bassenc_opus.dll");
        if (!File.Exists(bassEncOpusDll))
        {
            // Проверяем альтернативное расположение
            bassEncOpusDll = Path.Combine(baseDir, "bass", "bassenc_opus.dll");
        }

        if (File.Exists(bassEncOpusDll))
        {
            Console.WriteLine("    bassenc_opus.dll найден");
        }
        else
        {
            Console.WriteLine("    bassenc_opus.dll не найден - кодирование через BASS невозможно");
        }
    }
}