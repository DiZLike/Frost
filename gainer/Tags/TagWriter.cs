using gainer.Audio;
using System;
using System.Threading;
using TagLib;

namespace gainer.Tags
{
    public class TagWriter
    {
        private readonly string _filePath;
        private readonly bool _autoTagEnabled;

        public TagWriter(string filePath, bool autoTagEnabled = false)
        {
            _filePath = filePath;
            _autoTagEnabled = autoTagEnabled;
        }

        public void SaveAnalysisResults(AudioAnalysisResult results, bool useCustomTag)
        {
            int retryCount = 0;
            const int maxRetries = 5;

            while (retryCount < maxRetries)
            {
                try
                {
                    using (var file = TagLib.File.Create(_filePath))
                    {
                        // Автоматическое заполнение тегов из пути
                        if (_autoTagEnabled)
                        {
                            var autoTagger = new AutoTagger();
                            var tags = autoTagger.ExtractFromPath(_filePath);
                            autoTagger.ApplyTags(file, tags);
                        }

                        // Формируем строку комментария с данными по полосам
                        string comment = FormatResults(results, useCustomTag);

                        if (useCustomTag)
                        {
                            // Кастомный тег - вся информация в комментарии
                            file.Tag.Comment = comment;
                        }
                        else
                        {
                            try
                            {
                                // Стандартный тег ReplayGain
                                file.Tag.ReplayGainTrackGain = results.ReplayGain;
                                // Все остальные данные в комментарий
                                file.Tag.Comment = comment;
                            }
                            catch (NotImplementedException)
                            {
                                // Если формат не поддерживает стандартный тег
                                file.Tag.Comment = comment;
                            }
                        }

                        file.Save();
                    }
                    return;
                }
                catch (System.IO.IOException)
                {
                    retryCount++;
                    if (retryCount >= maxRetries) return;
                    Thread.Sleep(100);
                }
                catch (Exception)
                {
                    return;
                }
            }
        }

        private string FormatResults(AudioAnalysisResult results, bool useCustomTag)
        {
            if (useCustomTag)
            {
                return $"replay-gain={results.ReplayGain:F2}\r\n" +
                       $"rms_main={results.RmsDb:F2}\r\n" +
                       $"lufs={results.IntegratedLoudness:F1}\r\n" +
                       $"main_L={results.MainBand.LeftRmsDb:F2} main_R={results.MainBand.RightRmsDb:F2}\r\n" +
                       $"sub_L={results.SubBand.LeftRmsDb:F2} sub_R={results.SubBand.RightRmsDb:F2}\r\n" +
                       $"low_L={results.LowBand.LeftRmsDb:F2} low_R={results.LowBand.RightRmsDb:F2}\r\n" +
                       $"mid_L={results.MidBand.LeftRmsDb:F2} mid_R={results.MidBand.RightRmsDb:F2}\r\n" +
                       $"high_L={results.HighBand.LeftRmsDb:F2} high_R={results.HighBand.RightRmsDb:F2}";
            }
            else
            {
                return $"Main: L={results.MainBand.LeftRmsDb:F2} R={results.MainBand.RightRmsDb:F2} dB\r\n" +
                       $"Sub: L={results.SubBand.LeftRmsDb:F2} R={results.SubBand.RightRmsDb:F2} dB\r\n" +
                       $"Low: L={results.LowBand.LeftRmsDb:F2} R={results.LowBand.RightRmsDb:F2} dB\r\n" +
                       $"Mid: L={results.MidBand.LeftRmsDb:F2} R={results.MidBand.RightRmsDb:F2} dB\r\n" +
                       $"High: L={results.HighBand.LeftRmsDb:F2} R={results.HighBand.RightRmsDb:F2} dB";
            }
        }
    }
}