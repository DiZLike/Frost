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
                _progressManager.UpdateThreadLine(lineIndex,
                    $"Поток {formattedThreadId}> Начало: {shortFileName}",
                    ConsoleColor.Cyan);

                // 1.1 Чтение аудио
                AudioReader audioReader = new AudioReader(filePath);
                audioReader.ProgressChanged += (progress) =>
                {
                    int percent = (int)(progress * 100);
                    if (Math.Abs(percent - _lastPercentReported) >= 2)
                    {
                        _lastPercentReported = percent;
                        _progressManager.UpdateThreadLine(lineIndex,
                            $"Поток {formattedThreadId}> [{percent}%] Чтение: {shortFileName}",
                            ConsoleColor.Cyan);
                    }
                };

                // Чтение всех полос
                float[] pcmDataMain = audioReader.GetPCMData32(audioReader.MainStream);
                float[] pcmDataSub = audioReader.GetPCMData32(audioReader.SubStream);
                float[] pcmDataLow = audioReader.GetPCMData32(audioReader.LowStream);
                float[] pcmDataMid = audioReader.GetPCMData32(audioReader.MidStream);
                float[] pcmDataHigh = audioReader.GetPCMData32(audioReader.HighStream);
                audioReader.Free();

                if (pcmDataMain.Length == 0)
                {
                    string error = "Нет аудиоданных";
                    _progressManager.UpdateThreadLine(lineIndex,
                        $"Поток {formattedThreadId}> {shortFileName} - {error}",
                        ConsoleColor.Yellow);
                    _statistics.AddFailed($"{fileName} - {error}");
                    Thread.Sleep(2000);
                    return;
                }

                // 2. Анализ всех полос
                var analyzer = new AudioAnalyzer(44100, _args.TargetLufs, _args.UseKFilter);
                analyzer.ProgressChanged += (progress, message) =>
                {
                    int percent = (int)(progress * 100);
                    if (Math.Abs(percent - _lastPercentReported) >= 2)
                    {
                        _lastPercentReported = percent;
                        _progressManager.UpdateThreadLine(lineIndex,
                            $"Поток {formattedThreadId}> [{percent}%] {shortFileName} {message}",
                            ConsoleColor.Yellow);
                    }
                };

                var results = analyzer.Analyze(pcmDataMain, pcmDataSub, pcmDataLow, pcmDataMid, pcmDataHigh);

                // 3. Сохранение в теги
                _progressManager.UpdateThreadLine(lineIndex,
                    $"Поток {formattedThreadId}> 💾 {shortFileName} - сохранение...",
                    ConsoleColor.Blue);

                var tagWriter = new TagWriter(filePath, _args.AutoTagEnabled);
                tagWriter.SaveAnalysisResults(results, _args.UseCustomTag);

                // 4. Вывод результата с информацией о полосах
                string autoTagMessage = _args.AutoTagEnabled ? " + авто-теги" : "";
                string bandsInfo = results.GetBandsInfo();
                _progressManager.UpdateThreadLine(lineIndex,
                    $"Поток {formattedThreadId}> Готово: {shortFileName} - RG: {results.ReplayGain:F2} dB | {bandsInfo}{autoTagMessage}",
                    ConsoleColor.Green);

                _statistics.IncrementSuccess();
                _statistics.AddSuccess($"{fileName} - RG: {results.ReplayGain:F2} dB | {bandsInfo}{autoTagMessage}");
                Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                _progressManager.UpdateThreadLine(lineIndex,
                    $"Поток {formattedThreadId}> {shortFileName} - Ошибка: {ex.Message}",
                    ConsoleColor.Red);

                _statistics.AddFailed($"{fileName} - {ex.Message}");
                Thread.Sleep(2000);
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