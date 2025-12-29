using System;
using System.IO;
using System.Linq;
using System.Threading;
using gainer.Audio;
using gainer.Tags;

namespace gainer.Processing
{
    public class AudioFileProcessor
    {
        private readonly CommandLineArgs _args;
        private readonly StatisticsCollector _statistics;
        private readonly ConsoleProgressManager _progressManager;
        private int _lastPercentReported = -1;

        public AudioFileProcessor(CommandLineArgs args, StatisticsCollector statistics, ConsoleProgressManager progressManager)
        {
            _args = args;
            _statistics = statistics;
            _progressManager = progressManager;
        }

        public void ProcessFile(string filePath)
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            var (displayId, lineIndex) = _progressManager.GetThreadInfo(threadId);

            string fileName = Path.GetFileName(filePath);
            string shortFileName = fileName.Length > 30 ? fileName.Substring(0, 27) + "..." : fileName;
            string formattedThreadId = displayId.ToString().PadLeft(2, '0');

            try
            {
                _progressManager.UpdateThreadLine(lineIndex, $"Поток {formattedThreadId}> Начало: {shortFileName}", ConsoleColor.Cyan);

                // 1. Чтение аудио
                using (var audioReader = new AudioReader(filePath))
                {
                    // Подписываемся на прогресс чтения
                    audioReader.ProgressChanged += (progress) =>
                    {
                        int percent = (int)(progress * 100);
                        if (Math.Abs(percent - _lastPercentReported) >= 2 || percent == 50 || percent == 0)
                        {
                            _lastPercentReported = percent;
                            _progressManager.UpdateThreadLine(lineIndex,
                                $"Поток {formattedThreadId}> [{percent}%] {shortFileName}",
                                ConsoleColor.Cyan);
                        }
                    };
                    float[] pcmData = audioReader.GetPCMData32();

                    if (pcmData.Length == 0)
                    {
                        string error = "Нет аудиоданных";
                        _progressManager.UpdateThreadLine(lineIndex, $"Поток {formattedThreadId}> {shortFileName} - {error}", ConsoleColor.Yellow);
                        _statistics.AddFailed($"{fileName} - {error}");
                        Thread.Sleep(2000);
                        return;
                    }

                    // 2. Расчет Replay Gain
                    var replayGain = new ReplayGainCalculator(44100, _args.TargetLufs);
                    replayGain.ProgressChanged += (progress, message) =>
                    {
                        int percent = (int)(progress * 100);
                        if (Math.Abs(percent - _lastPercentReported) >= 2 || percent >= 90 || percent <= 50)
                        {
                            _lastPercentReported = percent;
                            string shortMessage = message.Length > 20 ?
                                message.Substring(0, 17) + "..." : message;

                            _progressManager.UpdateThreadLine(lineIndex,
                                $"Поток {formattedThreadId}> [{percent}%] {shortFileName} {message}",
                                ConsoleColor.Yellow);
                        }
                    };

                    double gainValue = _args.UseKFilter ?
                        replayGain.CalculateWithKFilter(pcmData) :
                        replayGain.Calculate(pcmData);

                    // 3. Сохранение в теги
                    _progressManager.UpdateThreadLine(lineIndex,
                        $"Поток {formattedThreadId}> 💾 {shortFileName} - сохранение тегов...",
                        ConsoleColor.Blue);

                    var tagWriter = new TagWriter(filePath, _args.AutoTagEnabled);
                    tagWriter.SaveReplayGain(gainValue, _args.UseCustomTag);

                    // Выводим результат
                    string autoTagMessage = _args.AutoTagEnabled ? " + авто-теги" : "";
                    _progressManager.UpdateThreadLine(lineIndex,
                        $"Поток {formattedThreadId}> Готово: {shortFileName} - {gainValue:F2} dB{autoTagMessage}",
                        ConsoleColor.Green);

                    _statistics.IncrementSuccess();
                    _statistics.AddSuccess($"{fileName} - {gainValue:F2} dB{autoTagMessage}");
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                _progressManager.UpdateThreadLine(lineIndex,
                    $"Поток {formattedThreadId}> {shortFileName} - Ошибка",
                    ConsoleColor.Red);

                _statistics.AddFailed($"{fileName} - {ex.Message}");
                Thread.Sleep(2000);
                throw;
            }
            finally
            {
                _statistics.IncrementProcessed();
                _progressManager.PrintProgress();
                _progressManager.ClearThreadLine(lineIndex);
                _lastPercentReported = -1;
            }
        }

        public static bool IsSupportedAudioFile(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            string[] supportedExtensions = {
                ".mp3", ".flac", ".wav", ".opus", ".m4a",
                ".aac", ".ogg", ".wma", ".mp4", ".m4b",
                ".ape", ".wv"
            };

            return supportedExtensions.Contains(ext);
        }

        public static string[] GetAudioFiles(string folderPath)
        {
            return Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                .Where(f => IsSupportedAudioFile(f))
                .ToArray();
        }
    }
}