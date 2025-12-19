using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DirectoryStructureExporter
{
    class Program
    {
        // Список игнорируемых папок (системные, временные и т.д.)
        private static readonly HashSet<string> IgnoredDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Системные папки Windows
            "Windows",
            "System32",
            "SysWOW64",
            "System Volume Information",
            "$Recycle.Bin",
            "$WinREAgent",
            "Recovery",
            "Boot",
            "Config.Msi",
            
            // Временные файлы и кэши
            "Temp",
            "Temporary Internet Files",
            "Recent",
            "Prefetch",
            "SoftwareDistribution",
            "DeliveryOptimization",
            
            // Кэши приложений
            "AppData",
            "Local Settings",
            "Application Data",
            
            // Папки .NET и Visual Studio
            "bin",
            "obj",
            ".vs",
            ".git",
            ".svn",
            "node_modules",
            "__pycache__",
            ".idea",
            
            // Скрытые системные
            "$RECYCLE.BIN",
            "System Volume Information",
            "pagefile.sys",
            "hiberfil.sys",
            "swapfile.sys",
            
            // Другие системные
            "Windows.old",
            "MSOCache",
            "Intel",
            "AMD",
            "NVIDIA",
            
            // Временные пользовательские
            "Downloads",
            "Temp",
            "tmp"
        };

        // Список игнорируемых расширений файлов
        private static readonly HashSet<string> IgnoredExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".tmp",
            ".temp",
            ".log",
            ".cache",
            ".db",
            ".dll",
            ".exe",
            ".sys",
            ".dat",
            ".ini"
        };

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            Console.WriteLine("=== Экспорт структуры каталога (с фильтрацией системных папок) ===");
            Console.WriteLine("Перетащите папку на исполняемый файл или введите путь вручную:");

            string path;

            if (args.Length > 0)
            {
                path = args[0];
                Console.WriteLine($"Получен путь: {path}");
            }
            else
            {
                Console.Write("Введите путь к папке: ");
                path = Console.ReadLine();

                // Удаляем кавычки, если они есть (при копировании пути)
                if (!string.IsNullOrEmpty(path) && path.Length > 1 && path[0] == '"' && path[path.Length - 1] == '"')
                {
                    path = path.Substring(1, path.Length - 2);
                }
            }

            // Проверяем существование пути
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                Console.WriteLine("Ошибка: указанная папка не существует или путь пустой!");
                Console.ReadKey();
                return;
            }

            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(path);
                string folderName = dirInfo.Name;
                string outputFile = $"{folderName}_structure_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

                Console.WriteLine($"\nАнализируем: {path}");
                Console.WriteLine($"Игнорируемые папки: {string.Join(", ", IgnoredDirectories.OrderBy(d => d).Take(10))}...");
                Console.WriteLine($"Выходной файл: {outputFile}");
                Console.WriteLine(new string('-', 50));

                // Счетчики
                int totalDirs = 0;
                int totalFiles = 0;
                int ignoredDirs = 0;
                int ignoredFiles = 0;

                // Генерируем структуру
                string structure = GenerateDirectoryStructure(path, "", ref totalDirs, ref totalFiles, ref ignoredDirs, ref ignoredFiles);

                // Добавляем заголовок с информацией
                StringBuilder finalOutput = new StringBuilder();
                finalOutput.AppendLine($"Структура каталога: {path}");
                finalOutput.AppendLine($"Дата создания: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                finalOutput.AppendLine($"Игнорируемые папки: {ignoredDirs}");
                finalOutput.AppendLine($"Игнорируемые файлы: {ignoredFiles}");
                finalOutput.AppendLine(new string('=', 60));
                finalOutput.AppendLine();
                finalOutput.Append(structure);

                // Записываем в файл
                File.WriteAllText(outputFile, finalOutput.ToString(), Encoding.UTF8);

                // Выводим статистику
                Console.WriteLine("\n" + new string('=', 50));
                Console.WriteLine("СТАТИСТИКА:");
                Console.WriteLine($"Всего папок: {totalDirs}");
                Console.WriteLine($"Всего файлов: {totalFiles}");
                Console.WriteLine($"Игнорировано папок: {ignoredDirs}");
                Console.WriteLine($"Игнорировано файлов: {ignoredFiles}");
                Console.WriteLine($"Учтено папок: {totalDirs - ignoredDirs}");
                Console.WriteLine($"Учтено файлов: {totalFiles - ignoredFiles}");
                Console.WriteLine(new string('=', 50));
                Console.WriteLine($"\nГотово! Структура сохранена в файл: {outputFile}");
                Console.WriteLine($"Полный путь: {Path.GetFullPath(outputFile)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nОшибка: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }

            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        static string GenerateDirectoryStructure(string path, string indent,
            ref int totalDirs, ref int totalFiles, ref int ignoredDirs, ref int ignoredFiles)
        {
            StringBuilder result = new StringBuilder();

            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(path);

                // Пропускаем скрытые и системные папки (кроме корневой)
                if (indent != "" && (dirInfo.Attributes.HasFlag(FileAttributes.Hidden) ||
                                     dirInfo.Attributes.HasFlag(FileAttributes.System)))
                {
                    ignoredDirs++;
                    return string.Empty;
                }

                // Проверяем, не входит ли папка в список игнорируемых
                if (indent != "" && IgnoredDirectories.Contains(dirInfo.Name))
                {
                    result.AppendLine($"{indent}🚫 [ИГНОРИРОВАНО] {dirInfo.Name}/");
                    ignoredDirs++;
                    return result.ToString();
                }

                // Добавляем текущую папку
                result.AppendLine($"{indent}📁 {dirInfo.Name}/");
                totalDirs++;

                string newIndent = indent + "  │  ";
                string fileIndent = indent + "  ├── ";
                string lastFileIndent = indent + "  └── ";
                string dirIndent = indent + "  │  ";
                string lastDirIndent = indent + "     ";

                try
                {
                    // Получаем подпапки (исключая системные и скрытые)
                    var subDirectories = dirInfo.GetDirectories()
                        .Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden) &&
                                   !d.Attributes.HasFlag(FileAttributes.System) &&
                                   !IgnoredDirectories.Contains(d.Name))
                        .OrderBy(d => d.Name)
                        .ToList();

                    // Получаем файлы
                    var files = dirInfo.GetFiles()
                        .Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden) &&
                                   !f.Attributes.HasFlag(FileAttributes.System) &&
                                   !IgnoredExtensions.Contains(f.Extension))
                        .OrderBy(f => f.Name)
                        .ToList();

                    // Обрабатываем файлы
                    for (int i = 0; i < files.Count; i++)
                    {
                        bool isLastFile = (i == files.Count - 1) && (subDirectories.Count == 0);
                        string currentIndent = isLastFile ? lastFileIndent : fileIndent;

                        string size = FormatFileSize(files[i].Length);
                        result.AppendLine($"{currentIndent}📄 {files[i].Name} ({size})");
                        totalFiles++;
                    }

                    // Рекурсивно обрабатываем подпапки
                    for (int i = 0; i < subDirectories.Count; i++)
                    {
                        bool isLastDir = (i == subDirectories.Count - 1);
                        string currentDirIndent = isLastDir ? lastDirIndent : dirIndent;
                        string nextLevelIndent = isLastDir ? indent + "     " : indent + "  │  ";

                        // Пропускаем слишком глубокую вложенность (опционально)
                        if (indent.Length > 100) // Ограничение глубины
                        {
                            result.AppendLine($"{currentDirIndent}📁 {subDirectories[i].Name}/ [глубина ограничена]");
                            ignoredDirs++;
                            continue;
                        }

                        string subStructure = GenerateDirectoryStructure(
                            subDirectories[i].FullName,
                            nextLevelIndent,
                            ref totalDirs,
                            ref totalFiles,
                            ref ignoredDirs,
                            ref ignoredFiles
                        );

                        if (!string.IsNullOrEmpty(subStructure))
                        {
                            result.Append(subStructure);
                        }
                        else if (!IgnoredDirectories.Contains(subDirectories[i].Name))
                        {
                            // Если папка пуста (после фильтрации)
                            result.AppendLine($"{currentDirIndent}📁 {subDirectories[i].Name}/ [пусто]");
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    result.AppendLine($"{fileIndent}⛔ [НЕТ ДОСТУПА]");
                    ignoredDirs++;
                }
                catch (PathTooLongException)
                {
                    result.AppendLine($"{fileIndent}⚠️ [СЛИШКОМ ДЛИННЫЙ ПУТЬ]");
                    ignoredDirs++;
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is PathTooLongException)
            {
                // Игнорируем ошибки доступа и длинных путей
                ignoredDirs++;
            }
            catch (Exception ex)
            {
                result.AppendLine($"{indent}❌ Ошибка: {ex.Message}");
            }

            return result.ToString();
        }

        static string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 B";

            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return bytes < 1024 ? $"{bytes} B" : $"{len:0.##} {sizes[order]}";
        }
    }
}