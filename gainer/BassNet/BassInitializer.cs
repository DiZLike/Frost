using System;
using System.IO;
using Un4seen.Bass;

namespace gainer.BassNet
{
    public static class BassInitializer
    {
        private static bool _isInitialized = false;

        public static bool Initialize()
        {
            if (_isInitialized)
                return true;

            // Инициализируем BASS с устройством по умолчанию
            if (!Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero))
            {
                Console.WriteLine($"Ошибка инициализации BASS: {Bass.BASS_ErrorGetCode()}");
                return false;
            }

            Console.WriteLine("BASS успешно инициализирован");
            _isInitialized = true;
            return true;
        }

        public static void LoadPlugins()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            int loadedPlugins = 0;

            // Проверяем наличие библиотек BASS в текущей папке
            if (!File.Exists(Path.Combine(basePath, "bass.dll")))
            {
                Console.WriteLine("Предупреждение: файл bass.dll не найден в папке приложения");
                Console.WriteLine("Пожалуйста, поместите файлы BASS в папку с программой:");
                Console.WriteLine("- bass.dll");
                Console.WriteLine("- bassopus.dll (для поддержки Opus)");
                Console.WriteLine("- bass_aac.dll (для поддержки AAC/M4A)");
                Console.WriteLine("- bassflac.dll (для поддержки FLAC)");
                Console.WriteLine("- и другие по необходимости");
                Console.WriteLine();
            }

            // Загружаем плагины для различных форматов
            string[] plugins = {
                "bassopus.dll",    // Opus
                "bass_aac.dll",    // AAC, MP4
                "bassflac.dll",    // FLAC
                "basswma.dll",     // WMA
                "bass_ape.dll",    // Monkey's Audio
                "basswv.dll",      // WavPack
                "bassmidi.dll",    // MIDI (на всякий случай)
                "bass_alac.dll",   // Apple Lossless
                "bass_mpc.dll",    // Musepack
                "bass_ofr.dll",    // OptimFROG
                "bass_tta.dll",    // True Audio
                "bass_spx.dll",    // Speex
                "bass_ac3.dll",    // AC3
                "bass_dts.dll"     // DTS
            };

            foreach (var plugin in plugins)
            {
                string pluginPath = Path.Combine(basePath, plugin);
                if (File.Exists(pluginPath))
                {
                    int handle = Bass.BASS_PluginLoad(pluginPath);
                    if (handle != 0)
                    {
                        loadedPlugins++;
                        Console.WriteLine($"Загружен плагин: {plugin}");
                    }
                }
            }

            Console.WriteLine($"Загружено плагинов: {loadedPlugins}");
            Console.WriteLine();
        }

        public static void Cleanup()
        {
            if (_isInitialized)
            {
                Bass.BASS_Free();
                _isInitialized = false;
            }
        }
    }
}