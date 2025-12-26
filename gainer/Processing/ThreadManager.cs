using gainer.BassNet;

namespace gainer.Processing
{
    public class ThreadManager
    {
        private readonly CommandLineArgs _args;
        private readonly StatisticsCollector _statistics;
        private readonly ConsoleProgressManager _progressManager;
        private readonly AudioFileProcessor _processor;

        public ThreadManager(CommandLineArgs args, StatisticsCollector statistics, ConsoleProgressManager progressManager)
        {
            _args = args;
            _statistics = statistics;
            _progressManager = progressManager;
            _processor = new AudioFileProcessor(args, statistics, progressManager);
        }

        public void ProcessAllFiles()
        {
            // Получаем все файлы рекурсивно
            string[] audioFiles = AudioFileProcessor.GetAudioFiles(_args.FolderPath);

            if (audioFiles.Length == 0)
                throw new InvalidOperationException($"В папке {_args.FolderPath} не найдено поддерживаемых аудиофайлов");

            _statistics.TotalFiles = audioFiles.Length;

            Console.WriteLine($"Найдено файлов: {_statistics.TotalFiles}");
            Console.WriteLine($"Папка: {_args.FolderPath}");
            Console.WriteLine($"K-фильтр: {(_args.UseKFilter ? "включен" : "выключен")}");
            Console.WriteLine($"Тип тегов: {(_args.UseCustomTag ? "кастомный" : "стандартный")}");
            Console.WriteLine($"Целевое значение: {_args.TargetLufs} LUFS");
            Console.WriteLine($"Потоков обработки: {Environment.ProcessorCount}");
            Console.WriteLine();

            // Инициализируем BASS
            if (!BassInitializer.Initialize())
                throw new InvalidOperationException("Не удалось инициализировать BASS");

            // Загружаем плагины
            BassInitializer.LoadPlugins();

            int maxThreads = Environment.ProcessorCount;
            _progressManager.InitializeConsole(maxThreads);
            _progressManager.PrintProgress();

            // Запускаем параллельную обработку
            var options = new ParallelOptions { MaxDegreeOfParallelism = maxThreads };

            try
            {
                Parallel.ForEach(audioFiles, options, file =>
                {
                    try
                    {
                        _processor.ProcessFile(file);
                    }
                    catch (Exception ex)
                    {
                        _progressManager.PrintError(Path.GetFileName(file), ex.Message);
                    }
                });
            }
            finally
            {
                BassInitializer.Cleanup();
                //_progressManager.CleanupConsole(maxThreads);
            }
        }
    }
}