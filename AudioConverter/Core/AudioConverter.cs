using OpusConverter.Config;
using Un4seen.Bass;

namespace OpusConverter.Core
{
    public class AudioConverter : IDisposable
    {
        private readonly AppConfig _config;
        private readonly FileProcessor _fileProcessor;
        private bool _disposed = false;

        public AudioConverter(AppConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _fileProcessor = new FileProcessor(config);
            InitializeBass();
            LoadPlugins();
        }

        private void LoadPlugins()
        {
            string pluginsDir = Path.Combine(_config.BaseDirectory, "bass", "plugins", "dec");

            if (!Directory.Exists(pluginsDir))
            {
                Console.WriteLine("    Папка с плагинами декодирования не найдена: " + pluginsDir);

                // Проверяем альтернативное расположение
                pluginsDir = Path.Combine(_config.BaseDirectory, "plugins");
                if (!Directory.Exists(pluginsDir))
                {
                    return;
                }
            }

            var pluginFiles = Directory.GetFiles(pluginsDir, "*.dll");

            if (pluginFiles.Length == 0)
            {
                Console.WriteLine("    Плагины декодирования не найдены в: " + pluginsDir);
                return;
            }

            Console.WriteLine("\n    Загрузка плагинов декодирования...");

            int loadedCount = 0;
            foreach (var pluginFile in pluginFiles)
            {
                try
                {
                    string pluginName = Path.GetFileName(pluginFile);
                    if (Bass.BASS_PluginLoad(pluginFile) != 0)
                    {
                        Console.WriteLine($"        {pluginName}");
                        loadedCount++;
                    }
                    else
                    {
                        var errorCode = Bass.BASS_ErrorGetCode();
                        Console.WriteLine($"        {pluginName} - ошибка загрузки: {errorCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"        {Path.GetFileName(pluginFile)}: {ex.Message}");
                }
            }

            Console.WriteLine($"    Загружено плагинов: {loadedCount}/{pluginFiles.Length}");
        }

        private void InitializeBass()
        {
            try
            {
                if (!Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero))
                {
                    throw new Exception($"Ошибка инициализации BASS: {Bass.BASS_ErrorGetCode()}");
                }

                Console.WriteLine("    BASS успешно инициализирован");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    Ошибка BASS: {ex.Message}");
                throw;
            }
        }

        public void ConvertAll()
        {
            // Находим обычные аудиофайлы
            var audioFiles = _fileProcessor.FindAudioFiles();

            int totalTasks = audioFiles.Count;
            _fileProcessor.InitializeProgress(totalTasks);

            if (totalTasks == 0)
            {
                Console.WriteLine($"\n    Файлы не найдены в: {_config.InputDirectory}");
                return;
            }

            PrintHeader(audioFiles.Count);

            // Обрабатываем обычные файлы
            Parallel.ForEach(audioFiles, new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            }, file =>
            {
                try
                {
                    ConvertSingleFile(file);
                }
                catch (Exception ex)
                {
                    lock (this)
                    {
                        _fileProcessor.IncrementProcessed();
                        _fileProcessor.IncrementFailed();
                        UpdateProgress();
                        Console.WriteLine($"        Ошибка {Path.GetFileName(file)}: {ex.Message}");
                    }
                }
            });

            PrintSummary();
        }

        private void PrintHeader(int audioFilesCount)
        {
            Console.WriteLine($"\n{"=".PadRight(60, '=')}");
            Console.WriteLine("НАЧАЛО КОНВЕРТАЦИИ");
            Console.WriteLine($"{"=".PadRight(60, '=')}");
            Console.WriteLine($"    Файлов: {audioFilesCount}");
            Console.WriteLine($"    Режим: {_config.OpusSettings.Mode}");
            Console.WriteLine($"    Тип аудио: {_config.OpusSettings.AudioType}");
            Console.WriteLine($"    Битрейт: {_config.OpusSettings.Bitrate} кбит/с");
            Console.WriteLine($"    Кадр: {_config.OpusSettings.FrameSize} мс");
            Console.WriteLine($"    Сложность: {_config.OpusSettings.Complexity}");
            Console.WriteLine($"    Паттерн имен: {_config.OutputFilenamePattern}");
            Console.WriteLine($"    Потоков: {Environment.ProcessorCount}");
            Console.WriteLine($"{"=".PadRight(60, '=')}\n");
        }

        private void UpdateProgress()
        {
            Console.Write($"\r    {_fileProcessor.GetProgressText()}");
        }

        private void PrintSummary()
        {
            Console.WriteLine($"\n\n{"=".PadRight(60, '=')}");
            Console.WriteLine("КОНВЕРТАЦИЯ ЗАВЕРШЕНА");
            Console.WriteLine($"{"=".PadRight(60, '=')}");
            Console.WriteLine(_fileProcessor.GetSummary());
            Console.WriteLine($"\n    Результат: {_config.OutputDirectory}");
            Console.WriteLine($"{"=".PadRight(60, '=')}");
        }

        private void ConvertSingleFile(string inputFile)
        {
            var converter = new SingleFileConverter(_config, _fileProcessor);
            converter.Convert(inputFile);
        }

        public void Dispose()
        {
            if (_disposed) return;

            Bass.BASS_Free();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}