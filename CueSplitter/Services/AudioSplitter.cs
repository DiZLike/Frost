using NAudio.Wave;
using CueSplitter.Models;

namespace CueSplitter.Services
{
    public class AudioSplitter
    {
        private readonly IAudioProcessor _audioProcessor;

        public AudioSplitter(IAudioProcessor audioProcessor)
        {
            _audioProcessor = audioProcessor;
        }

        public void SplitAudioFile(CueSheet cueSheet, string outputDirectory)
        {
            if (string.IsNullOrEmpty(cueSheet.AudioFile) || !File.Exists(cueSheet.AudioFile))
            {
                throw new FileNotFoundException($"Аудиофайл не найден: {cueSheet.AudioFile}");
            }

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            Console.WriteLine($"Обработка: {Path.GetFileName(cueSheet.AudioFile)}");
            Console.WriteLine($"Треков: {cueSheet.Tracks.Count}");
            Console.WriteLine();

            // Определяем формат исходного файла
            string inputExtension = Path.GetExtension(cueSheet.AudioFile).ToLower();

            // Читаем весь файл в память
            byte[] audioData;
            WaveFormat waveFormat;

            using (var reader = new AudioFileReader(cueSheet.AudioFile))
            {
                waveFormat = reader.WaveFormat;

                // Читаем весь файл в память
                audioData = new byte[reader.Length];
                int bytesRead = reader.Read(audioData, 0, audioData.Length);

                if (bytesRead != audioData.Length)
                {
                    Console.WriteLine($"Внимание: Прочитано {bytesRead} байт вместо {audioData.Length}");
                }
            }

            // Обрабатываем каждый трек
            for (int i = 0; i < cueSheet.Tracks.Count; i++)
            {
                var track = cueSheet.Tracks[i];
                string outputFileName = GenerateOutputFileName(cueSheet, track, ".flac");
                string outputPath = Path.Combine(outputDirectory, outputFileName);
                string tempWavPath = Path.ChangeExtension(outputPath, ".wav");

                Console.WriteLine($"[{track.TrackNumber:00}] {track.Title} ({track.Duration:mm\\:ss})");

                // Извлекаем трек во временный WAV файл
                ExtractTrackToWav(audioData, waveFormat, cueSheet, track, tempWavPath,
                    i < cueSheet.Tracks.Count - 1 ? cueSheet.Tracks[i + 1].StartTime : TimeSpan.Zero);

                Console.WriteLine($"    Создание FLAC: {outputFileName}");

                // Конвертируем в FLAC с метаданными
                _audioProcessor.ConvertToFlac(tempWavPath, outputPath, cueSheet, track);

                Console.WriteLine($"    Готово");
            }

            Console.WriteLine("\nВсе треки успешно созданы!");
        }

        private void ExtractTrackToWav(byte[] audioData, WaveFormat waveFormat,
            CueSheet cueSheet, CueTrack track, string outputPath, TimeSpan nextTrackStartTime)
        {
            // Вычисляем позицию начала в байтах
            long startByte = (long)(track.StartTime.TotalSeconds * waveFormat.AverageBytesPerSecond);
            startByte = startByte - (startByte % waveFormat.BlockAlign);

            // Вычисляем позицию окончания
            long endByte;
            if (nextTrackStartTime != TimeSpan.Zero)
            {
                endByte = (long)(nextTrackStartTime.TotalSeconds * waveFormat.AverageBytesPerSecond);
                endByte = endByte - (endByte % waveFormat.BlockAlign);
            }
            else
            {
                endByte = audioData.Length;
                endByte = endByte - (endByte % waveFormat.BlockAlign);
            }

            // Проверяем корректность
            if (startByte >= audioData.Length || endByte > audioData.Length || startByte >= endByte)
            {
                Console.WriteLine($"Ошибка: Некорректные границы трека {track.TrackNumber}");
                return;
            }

            long bytesToExtract = endByte - startByte;

            // Извлекаем данные трека
            byte[] trackData = new byte[bytesToExtract];
            Buffer.BlockCopy(audioData, (int)startByte, trackData, 0, (int)bytesToExtract);

            // Создаем WAV файл
            CreateWavFile(outputPath, trackData, waveFormat);
        }

        private void CreateWavFile(string filePath, byte[] audioData, WaveFormat waveFormat)
        {
            using (var memoryStream = new MemoryStream(audioData))
            using (var rawReader = new RawSourceWaveStream(memoryStream, waveFormat))
            using (var writer = new WaveFileWriter(filePath, waveFormat))
            {
                rawReader.CopyTo(writer);
            }
        }

        private string GenerateOutputFileName(CueSheet cueSheet, CueTrack track, string extension = ".flac")
        {
            string artist = !string.IsNullOrEmpty(track.Artist) ? track.Artist : cueSheet.Artist;
            string title = !string.IsNullOrEmpty(track.Title) ? track.Title : $"Track {track.TrackNumber:00}";

            // Улучшенный формат имени файла
            string fileName = $"{track.TrackNumber:00}. {artist} - {title}";

            // Убираем недопустимые символы
            string invalidChars = new string(Path.GetInvalidFileNameChars());
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }

            // Заменяем двойные пробелы и обрезаем
            fileName = fileName.Replace("  ", " ").Trim();

            // Ограничиваем длину
            if (fileName.Length > 200)
            {
                fileName = fileName.Substring(0, 200);
            }

            return $"{fileName}{extension}";
        }
    }
}