using Strimer.Core;
using System.Diagnostics;
using Un4seen.Bass.AddOn.Tags;

namespace Strimer.Audio
{
    public class TrackLoader
    {
        private readonly BassAudioEngine _audioEngine;
        private readonly ReplayGain _replayGain;

        public TrackLoader(BassAudioEngine audioEngine, ReplayGain replayGain)
        {
            _audioEngine = audioEngine;
            _replayGain = replayGain;
        }

        public LoadedTrackInfo LoadTrack(string filePath)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                Logger.Debug($"[TrackLoader] Загрузка трека: {Path.GetFileName(filePath)}");
                //Thread.Sleep(10000);

                // Создаем аудиопоток
                int streamHandle = _audioEngine.CreateStreamFromFile(filePath);
                if (streamHandle == 0)
                {
                    Logger.Error($"[TrackLoader] Не удалось создать поток для файла: {filePath}");
                    return null;
                }

                // Получаем метаданные
                var tagInfo = _audioEngine.GetTrackTags(filePath);
                if (tagInfo == null)
                {
                    Logger.Warning($"[TrackLoader] Не удалось получить теги для файла: {filePath}");
                    _audioEngine.FreeStream(streamHandle);
                    return null;
                }

                // Применяем ReplayGain
                _replayGain.SetGain(tagInfo);

                // Создаем информацию о треке
                var trackInfo = CreateTrackInfo(tagInfo, filePath);

                stopwatch.Stop();
                Logger.Info($"[Производительность] Трек загружен за {stopwatch.ElapsedMilliseconds} мс");

                return new LoadedTrackInfo
                {
                    StreamHandle = streamHandle,
                    TrackInfo = trackInfo,
                    FilePath = filePath,
                    LoadTime = stopwatch.Elapsed // Используем Elapsed вместо TimeSpan
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.Error($"[TrackLoader] Ошибка загрузки трека: {ex.Message}");
                return null;
            }
        }

        private TrackInfo CreateTrackInfo(TAG_INFO tagInfo, string filePath)
        {
            int year = 0;
            if (!string.IsNullOrEmpty(tagInfo.year))
            {
                try
                {
                    string yearStr = new string(tagInfo.year.Where(char.IsDigit).ToArray());
                    if (!string.IsNullOrEmpty(yearStr))
                    {
                        int.TryParse(yearStr, out year);
                    }
                }
                catch
                {
                    // Игнорируем ошибки парсинга года
                }
            }

            return new TrackInfo
            {
                Artist = !string.IsNullOrWhiteSpace(tagInfo.artist) ? tagInfo.artist : "Unknown Artist",
                Title = !string.IsNullOrWhiteSpace(tagInfo.title) ? tagInfo.title : Path.GetFileNameWithoutExtension(filePath),
                Album = tagInfo.album ?? "",
                Year = year,
                Genre = tagInfo.genre ?? "",
                ReplayGain = tagInfo.replaygain_track_gain,
                Comment = tagInfo.comment ?? ""
            };
        }
    }

    public class LoadedTrackInfo
    {
        public int StreamHandle { get; set; }
        public TrackInfo TrackInfo { get; set; }
        public string FilePath { get; set; } = String.Empty;
        public TimeSpan LoadTime { get; set; }
    }
}