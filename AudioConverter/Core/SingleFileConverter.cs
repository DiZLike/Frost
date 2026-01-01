using OpusConverter.Config;
using OpusConverter.Utilities;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Enc;

namespace OpusConverter.Core
{
    public class SingleFileConverter
    {
        private readonly AppConfig _config;
        private readonly FileProcessor _fileProcessor;

        public SingleFileConverter(AppConfig config, FileProcessor fileProcessor)
        {
            _config = config;
            _fileProcessor = fileProcessor;
        }

        public void Convert(string inputFile)
        {
            string outputFileName;
            TagLib.Tag fileTag = null;

            // Получаем метаданные для формирования имени файла
            if (!string.IsNullOrWhiteSpace(_config.OutputFilenamePattern))
            {
                try
                {
                    using (var sourceFile = TagLib.File.Create(inputFile))
                    {
                        fileTag = sourceFile.Tag;
                        outputFileName = _fileProcessor.GenerateOutputFileName(inputFile, fileTag);
                    }
                }
                catch
                {
                    // Если не удалось прочитать метаданные, используем оригинальное имя
                    outputFileName = Path.GetFileNameWithoutExtension(inputFile) + ".opus";
                }
            }
            else
            {
                outputFileName = Path.GetFileNameWithoutExtension(inputFile) + ".opus";
            }

            string outputFile;
            if (!string.IsNullOrWhiteSpace(_config.OutputFilenamePattern) &&
                (_config.OutputFilenamePattern.Contains('/') || _config.OutputFilenamePattern.Contains('\\')))
            {
                // Используем новый метод для создания структуры папок
                outputFile = _fileProcessor.GenerateOutputFilePath(inputFile, fileTag);
            }
            else
            {
                // Старый метод для простых имен
                outputFile = Path.Combine(_config.OutputDirectory, outputFileName);
            }

            if (File.Exists(outputFile) && !_config.OverwriteExisting)
            {
                lock (_fileProcessor)
                {
                    _fileProcessor.IncrementProcessed();
                    UpdateProgress();
                }
                Console.WriteLine($"\n        Пропуск: {Path.GetFileName(inputFile)} (уже существует)");
                return;
            }

            ConvertFile(inputFile, outputFile, fileTag);
        }

        private void ConvertFile(string inputFile, string outputFile, TagLib.Tag tag = null)
        {
            int stream = 0;
            int encoder = 0;

            try
            {
                // Проверяем существование файла
                if (!File.Exists(inputFile))
                    throw new Exception($"Файл не найден: {inputFile}");

                // Открываем аудиофайл
                stream = Bass.BASS_StreamCreateFile(inputFile, 0, 0,
                    BASSFlag.BASS_DEFAULT | BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);

                if (stream == 0)
                    throw new Exception($"Ошибка открытия: {Bass.BASS_ErrorGetCode()}");

                // Создаем выходную директорию если нужно
                Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

                // Подготавливаем параметры для opusenc
                string opusencPath = _config.OpusEncPath;
                if (string.IsNullOrEmpty(opusencPath))
                {
                    throw new Exception("opusenc не найден. Укажите путь в конфигурации.");
                }

                // Подготавливаем аргументы командной строки
                string arguments = $"--bitrate {_config.OpusSettings.Bitrate} " +
                                  $"--{_config.OpusSettings.Mode} " +
                                  $"--comp {_config.OpusSettings.Complexity} " +
                                  $"--framesize {_config.OpusSettings.FrameSize} " +
                                  $"--quiet " + // подавляем вывод opusenc в консоль
                                  $"- \"{outputFile}\"";

                // Используем BASS_Encode_Start для запуска внешнего encoder
                encoder = BassEnc.BASS_Encode_Start(
                    stream,
                    $"{opusencPath} {arguments}",
                    0,
                    null,
                    IntPtr.Zero
                );

                if (encoder == 0)
                {
                    throw new Exception($"Ошибка запуска encoder: {Bass.BASS_ErrorGetCode()}");
                }

                // Обрабатываем данные
                byte[] buffer = new byte[65536];
                int bytesRead;
                do
                {
                    bytesRead = Bass.BASS_ChannelGetData(stream, buffer, buffer.Length);
                }
                while (bytesRead > 0);

                // Останавливаем кодирование
                BassEnc.BASS_Encode_Stop(encoder);
                encoder = 0;

                // Копируем метаданные
                if (_config.CopyMetadata && File.Exists(outputFile) && tag != null)
                {
                    MetadataManager.CopyMetadata(inputFile, outputFile);
                }

                // Удаляем исходный файл если нужно
                if (_config.DeleteSource)
                {
                    File.Delete(inputFile);
                }

                lock (_fileProcessor)
                {
                    _fileProcessor.IncrementProcessed();
                    _fileProcessor.IncrementSuccessful();
                    UpdateProgress();
                }

                Console.WriteLine($"\n        {Path.GetFileName(inputFile)} -> {Path.GetFileName(outputFile)}");
            }
            catch (Exception ex)
            {
                // Останавливаем encoder если он запущен
                if (encoder != 0)
                {
                    BassEnc.BASS_Encode_Stop(encoder);
                }

                // Освобождаем stream если он создан
                if (stream != 0)
                {
                    Bass.BASS_StreamFree(stream);
                }

                // Удаляем частично созданный файл
                if (File.Exists(outputFile))
                {
                    try { File.Delete(outputFile); } catch { }
                }

                throw;
            }
        }

        private void UpdateProgress()
        {
            Console.Write($"\r    {_fileProcessor.GetProgressText()}");
        }
    }
}