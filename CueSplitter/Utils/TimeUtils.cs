namespace CueSplitter.Utils
{
    public static class TimeUtils
    {
        public static TimeSpan CueTimeToTimeSpan(string cueTime)
        {
            // Формат: MM:SS:FF (минуты:секунды:фреймы, 75 фреймов = 1 секунда)
            var parts = cueTime.Split(':');
            if (parts.Length != 3)
                throw new FormatException($"Неправильный формат времени CUE: {cueTime}");

            int minutes = int.Parse(parts[0]);
            int seconds = int.Parse(parts[1]);
            int frames = int.Parse(parts[2]);

            double totalSeconds = minutes * 60 + seconds + (frames / 75.0);
            return TimeSpan.FromSeconds(totalSeconds);
        }

        public static string TimeSpanToCueTime(TimeSpan timeSpan)
        {
            int totalFrames = (int)(timeSpan.TotalSeconds * 75);
            int minutes = totalFrames / (75 * 60);
            int seconds = (totalFrames % (75 * 60)) / 75;
            int frames = totalFrames % 75;

            return $"{minutes:D2}:{seconds:D2}:{frames:D2}";
        }
    }
}