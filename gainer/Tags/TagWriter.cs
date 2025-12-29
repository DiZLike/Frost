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

        public void SaveReplayGain(double gain, bool useCustomTag)
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

                        if (useCustomTag)
                        {
                            // Кастомный тег в комментарии
                            file.Tag.Comment = $"replay-gain={gain}";
                        }
                        else
                        {
                            try
                            {
                                // Стандартный тег Replay Gain
                                file.Tag.ReplayGainTrackGain = gain;
                            }
                            catch (NotImplementedException)
                            {
                                // Если формат не поддерживает стандартный тег, используем кастомный
                                file.Tag.Comment = $"replay-gain={gain}";
                            }
                        }

                        file.Save();
                    }
                    return; // Успешно, выходим из цикла
                }
                catch (System.IO.IOException ex)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        return;
                    }
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