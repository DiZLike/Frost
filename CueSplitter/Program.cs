using CueSplitter.Models;
using CueSplitter.Services;
using System.Diagnostics;

namespace CueSplitter
{
    class Program
    {
        static void Main(string[] args)
        {
            // Для отладки (закомментируйте в релизе)
            if (Debugger.IsAttached && args.Length == 0)
            {
                args = new string[]
                {
                    @"D:\Downloads\Korpiklaani\Albums\2007 - Tervaskanto [Napalm Rec., NPR 212 CD+DVD, Germany]\Korpiklaani - Tervaskanto (Limited Edition).cue",
                    "C:\\Users\\Evgeny\\Desktop\\out"
                };
            }

            Console.WriteLine("=== CUE Sheet Splitter (FLAC Output) ===");
            Console.WriteLine();

            if (args.Length == 0)
            {
                ShowUsage();
                return;
            }

            string cueFilePath = args[0];
            string outputDirectory = args.Length > 1 ? args[1] : GetDefaultOutputDirectory(cueFilePath);

            try
            {
                // Проверяем наличие ffmpeg
                if (!CheckFfmpegAvailability())
                {
                    Console.WriteLine("Ошибка: ffmpeg не найден. Установите ffmpeg и добавьте его в PATH.");
                    Console.WriteLine("Скачать: https://ffmpeg.org/download.html");
                    Console.WriteLine("Или поместите ffmpeg.exe в папку с программой.");
                    return;
                }

                // Парсим CUE файл
                var parser = new CueParser();
                CueSheet cueSheet = parser.ParseCueFile(cueFilePath);

                // Проверяем наличие аудиофайла
                if (string.IsNullOrEmpty(cueSheet.AudioFile))
                {
                    var fileService = new FileService();
                    cueSheet.AudioFile = fileService.FindAudioFile(cueFilePath);

                    if (string.IsNullOrEmpty(cueSheet.AudioFile))
                    {
                        Console.WriteLine("Ошибка: Не удалось найти аудиофайл для CUE!");
                        return;
                    }
                }

                // Выводим информацию
                ShowCueSheetInfo(cueSheet);

                // Подтверждение
                Console.Write("\nПродолжить разделение в FLAC? (y/n): ");
                if (Console.ReadKey().Key != ConsoleKey.Y)
                {
                    Console.WriteLine("\nОтменено.");
                    return;
                }
                Console.WriteLine("\n");

                // Создаем сервисы
                var metadataService = new MetadataService();
                var audioProcessor = new AudioProcessor(metadataService);
                var splitter = new AudioSplitter(audioProcessor);

                // Разделяем аудиофайл
                splitter.SplitAudioFile(cueSheet, outputDirectory);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nОшибка: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Внутренняя ошибка: {ex.InnerException.Message}");
                }
            }

            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        static bool CheckFfmpegAvailability()
        {
            try
            {
                // Сначала пробуем найти ffmpeg в PATH
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-version",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (var process = new Process { StartInfo = processStartInfo })
                {
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(3000); // Таймаут 3 секунды

                    if (process.ExitCode == 0 && output.Contains("ffmpeg version"))
                    {
                        Console.WriteLine($"✓ ffmpeg найден");
                        return true;
                    }
                }

                // Если не найден в PATH, пробуем в текущей директории
                if (File.Exists("ffmpeg.exe"))
                {
                    Console.WriteLine($"ffmpeg найден в текущей папке");
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        static void ShowUsage()
        {
            Console.WriteLine("Использование: CueSplitter <путь к CUE файлу> [выходная папка]");
            Console.WriteLine();
            Console.WriteLine("Примеры:");
            Console.WriteLine("  CueSplitter \"album.cue\"");
            Console.WriteLine("  CueSplitter \"album.cue\" \"C:\\Output\"");
            Console.WriteLine("  CueSplitter \"D:\\Music\\album.cue\" \"D:\\SplitTracks\"");
            Console.WriteLine();
            Console.WriteLine("Требования:");
            Console.WriteLine("  - Установленный ffmpeg в PATH или в папке с программой");
            Console.WriteLine("  - Исходный аудиофайл (FLAC, WAV, APE, WV и др.)");
        }

        static string GetDefaultOutputDirectory(string cueFilePath)
        {
            string cueDirectory = Path.GetDirectoryName(cueFilePath)!;
            string albumName = Path.GetFileNameWithoutExtension(cueFilePath);

            // Очищаем имя альбома от недопустимых символов
            string invalidChars = new string(Path.GetInvalidPathChars());
            foreach (char c in invalidChars)
            {
                albumName = albumName.Replace(c, '_');
            }

            return Path.Combine(cueDirectory, albumName + "_FLAC");
        }

        static void ShowCueSheetInfo(CueSheet cueSheet)
        {
            Console.WriteLine($"Альбом: {cueSheet.Title}");
            Console.WriteLine($"Исполнитель: {cueSheet.Artist}");
            if (!string.IsNullOrEmpty(cueSheet.Genre))
                Console.WriteLine($"Жанр: {cueSheet.Genre}");
            if (cueSheet.Year > 0)
                Console.WriteLine($"Год: {cueSheet.Year}");
            Console.WriteLine($"Аудиофайл: {Path.GetFileName(cueSheet.AudioFile)}");
            Console.WriteLine($"Формат выхода: FLAC");
            Console.WriteLine();
            Console.WriteLine("Треки:");

            foreach (var track in cueSheet.Tracks)
            {
                Console.WriteLine($"[{track.TrackNumber:00}] {track.StartTime:mm\\:ss} - {track.Duration:mm\\:ss} | {track.Title}");
            }
        }
    }
}