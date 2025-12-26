using System;
using System.Threading;
using TagLib;

namespace gainer.Tags
{
    public class TagWriter
    {
        private readonly string _filePath;

        public TagWriter(string filePath)
        {
            _filePath = filePath;
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
                        if (useCustomTag)
                        {
                            // Кастомный тег в комментарии
                            file.Tag.Comment = $"replay-gain={gain}";
                            //Console.WriteLine($"  Сохранен кастомный тег: replay-gain={gain}");
                        }
                        else
                        {
                            try
                            {
                                // Стандартный тег Replay Gain
                                file.Tag.ReplayGainTrackGain = gain;
                                //Console.WriteLine($"  Сохранен стандартный тег ReplayGain: {gain} dB");
                            }
                            catch (NotImplementedException)
                            {
                                // Если формат не поддерживает стандартный тег, используем кастомный
                                file.Tag.Comment = $"replay-gain={gain}";
                                //Console.WriteLine($"  Формат не поддерживает ReplayGain тег, сохранено в комментарии: replay-gain={gain}");
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
                        //Console.WriteLine($"  Ошибка при сохранении тегов после {maxRetries} попыток: {ex.Message}");
                        return;
                    }

                    // Ждем немного перед повторной попыткой
                    //Console.WriteLine($"  Файл занят, повторная попытка {retryCount}/{maxRetries} через 100мс...");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    //Console.WriteLine($"  Ошибка при сохранении тегов: {ex.Message}");
                    return;
                }
            }
        }
    }
}