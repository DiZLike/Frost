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

                        // Формируем строку комментария в новом формате
                        string comment = $"replay-gain={results.ReplayGain:F2}\r\nrms={results.RmsDb:F2}";

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
                                // RMS сохраняем в комментарий
                                file.Tag.Comment = results.RmsDb.ToString("F2");
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
    }
}