using System;
using System.Collections.Generic;
using System.Threading;

namespace gainer.Processing
{
    public class ConsoleProgressManager
    {
        private readonly object _consoleLock = new object();
        private readonly StatisticsCollector _statistics;

        // Для отслеживания позиций вывода
        private int _progressLine = -1;
        private int _statusLine = -1;
        private readonly Dictionary<int, string> _threadLines = new Dictionary<int, string>();

        // Для отслеживания занятости строк
        private readonly Dictionary<int, int> _threadToLineMap = new Dictionary<int, int>(); // threadId -> lineIndex
        private readonly Dictionary<int, int> _lineToThreadMap = new Dictionary<int, int>(); // lineIndex -> threadId (обратная мапа)
        private readonly Dictionary<int, int> _threadDisplayIdMap = new Dictionary<int, int>(); // threadId -> displayId
        private int _nextDisplayId = 1;
        private readonly object _threadMapLock = new object();

        public ConsoleProgressManager(StatisticsCollector statistics)
        {
            _statistics = statistics;
        }

        public void InitializeConsole(int maxThreads)
        {
            lock (_consoleLock)
            {
                Console.WriteLine("=== НАЧАЛО ОБРАБОТКИ ===");

                // Резервируем строку для прогресса
                _progressLine = Console.CursorTop;
                Console.WriteLine(); // Пустая строка для прогресс-бара

                // Резервируем строки для статусов потоков
                _statusLine = Console.CursorTop;
                for (int i = 0; i < maxThreads; i++)
                {
                    Console.WriteLine(new string(' ', Console.WindowWidth - 1));
                    _threadLines[i] = "";
                }

                Console.WriteLine("---"); // Разделитель между активной обработкой и логом
                Console.WriteLine();
            }
        }

        public void CleanupConsole(int maxThreads)
        {
            lock (_consoleLock)
            {
                int savedLeft = Console.CursorLeft;
                int savedTop = Console.CursorTop;

                // Очищаем только используемые строки
                Console.SetCursorPosition(0, _progressLine + 1);
                for (int i = 0; i < _threadLines.Count; i++)
                {
                    Console.WriteLine(new string(' ', Console.WindowWidth));
                }

                Console.SetCursorPosition(savedLeft, savedTop);
            }

            // Очищаем все внутренние структуры
            lock (_threadMapLock)
            {
                _threadToLineMap.Clear();
                _lineToThreadMap.Clear();
                // Оставляем displayIdMap для истории
            }
        }

        public void RegisterThread(int threadId)
        {
            lock (_threadMapLock)
            {
                // Если поток уже зарегистрирован - ничего не делаем
                if (_threadToLineMap.ContainsKey(threadId))
                    return;

                // Назначаем display ID если нужно
                if (!_threadDisplayIdMap.ContainsKey(threadId))
                {
                    _threadDisplayIdMap[threadId] = _nextDisplayId++;
                }

                // 1. Пытаемся найти совершенно свободную строку
                int freeLineIndex = -1;
                for (int i = 0; i < _threadLines.Count; i++)
                {
                    if (!_lineToThreadMap.ContainsKey(i))
                    {
                        freeLineIndex = i;
                        break;
                    }
                }

                // 2. Если нет свободных - ищем строку, которая "визуально" свободна (пустая на экране)
                if (freeLineIndex == -1)
                {
                    for (int i = 0; i < _threadLines.Count; i++)
                    {
                        if (string.IsNullOrEmpty(_threadLines[i]))
                        {
                            freeLineIndex = i;
                            break;
                        }
                    }
                }

                // 3. Если всё равно нет - занимаем строку с наименьшим displayId (самый старый поток)
                if (freeLineIndex == -1)
                {
                    int oldestThreadId = -1;
                    int oldestDisplayId = int.MaxValue;

                    foreach (var kvp in _lineToThreadMap)
                    {
                        int thread = kvp.Value;
                        int displayId = _threadDisplayIdMap[thread];
                        if (displayId < oldestDisplayId)
                        {
                            oldestDisplayId = displayId;
                            oldestThreadId = thread;
                            freeLineIndex = kvp.Key;
                        }
                    }
                }

                // 4. Освобождаем строку от предыдущего владельца если нужно
                if (_lineToThreadMap.TryGetValue(freeLineIndex, out int oldThreadId))
                {
                    _threadToLineMap.Remove(oldThreadId);
                    _lineToThreadMap.Remove(freeLineIndex);
                }

                // 5. Назначаем строку новому потоку
                _threadToLineMap[threadId] = freeLineIndex;
                _lineToThreadMap[freeLineIndex] = threadId;
            }
        }

        public (int displayId, int lineIndex) GetThreadInfo(int threadId)
        {
            lock (_threadMapLock)
            {
                RegisterThread(threadId);
                return (_threadDisplayIdMap[threadId], _threadToLineMap[threadId]);
            }
        }

        public void UpdateThreadLine(int lineIndex, string message, ConsoleColor color)
        {
            lock (_consoleLock)
            {
                int savedLeft = Console.CursorLeft;
                int savedTop = Console.CursorTop;

                int linePosition = _statusLine + lineIndex;
                if (linePosition >= 0 && linePosition < Console.BufferHeight)
                {
                    Console.SetCursorPosition(0, linePosition);
                    Console.Write(new string(' ', Console.WindowWidth - 1));
                    Console.SetCursorPosition(0, linePosition);
                    Console.ForegroundColor = color;
                    Console.Write(message.PadRight(Console.WindowWidth - 1));
                    Console.ResetColor();
                    _threadLines[lineIndex] = message;
                }

                Console.SetCursorPosition(savedLeft, savedTop);
            }
        }

        public void ClearThreadLine(int lineIndex)
        {
            lock (_consoleLock)
            {
                int savedLeft = Console.CursorLeft;
                int savedTop = Console.CursorTop;

                int linePosition = _statusLine + lineIndex;
                if (linePosition >= 0 && linePosition < Console.BufferHeight)
                {
                    Console.SetCursorPosition(0, linePosition);
                    Console.Write(new string(' ', Console.WindowWidth - 1));
                    _threadLines[lineIndex] = "";
                }

                Console.SetCursorPosition(savedLeft, savedTop);
            }
        }

        public void PrintProgress()
        {
            lock (_consoleLock)
            {
                int savedLeft = Console.CursorLeft;
                int savedTop = Console.CursorTop;

                if (_progressLine >= 0)
                {
                    Console.SetCursorPosition(0, _progressLine);

                    int current = _statistics.ProcessedFiles;
                    int total = _statistics.TotalFiles;
                    double percentage = total > 0 ? (current * 100.0 / total) : 0;

                    int barWidth = 40;
                    int progressWidth = total > 0 ? (int)(barWidth * current / total) : 0;
                    string progressBar = new string('█', progressWidth) +
                                       new string('░', barWidth - progressWidth);

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($"Прогресс: [{progressBar}] {percentage:F1}% ({current}/{total})");

                    int remaining = Console.WindowWidth - Console.CursorLeft - 1;
                    if (remaining > 0)
                    {
                        Console.Write(new string(' ', remaining));
                    }

                    Console.ResetColor();
                }

                Console.SetCursorPosition(savedLeft, savedTop);
            }
        }

        public void PrintError(string fileName, string errorMessage)
        {
            lock (_consoleLock)
            {
                int currentTop = Console.CursorTop;
                Console.SetCursorPosition(0, _statusLine + _threadLines.Count + 2);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{fileName}: {errorMessage}");
                Console.ResetColor();
                Console.SetCursorPosition(0, currentTop);
            }
        }
    }
}