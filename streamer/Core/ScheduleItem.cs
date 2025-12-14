using System.Text.Json.Serialization;

namespace Strimer.Core
{
    public class ScheduleItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("playlistPath")]
        public string PlaylistPath { get; set; } = "";

        [JsonPropertyName("startHour")]
        public int StartHour { get; set; }

        [JsonPropertyName("startMinute")]
        public int StartMinute { get; set; }

        [JsonPropertyName("endHour")]
        public int EndHour { get; set; }

        [JsonPropertyName("endMinute")]
        public int EndMinute { get; set; }

        [JsonPropertyName("daysOfWeek")]
        public string[] DaysOfWeek { get; set; } = Array.Empty<string>();

        public TimeSpan StartTime => new TimeSpan(StartHour, StartMinute, 0);
        public TimeSpan EndTime => new TimeSpan(EndHour, EndMinute, 0);

        public bool IsActive(DateTime currentTime)
        {
            // Проверка дня недели
            string currentDay = currentTime.DayOfWeek.ToString().ToLower();
            bool dayMatches = DaysOfWeek.Contains("*") ||
                             DaysOfWeek.Contains(currentDay) ||
                             DaysOfWeek.Contains(currentDay.Substring(0, 3)); // Mon, Tue, etc

            if (!dayMatches)
                return false;

            // Проверка времени
            TimeSpan currentTimeOfDay = currentTime.TimeOfDay;

            // Если интервал не переходит через полночь
            if (StartTime <= EndTime)
            {
                return currentTimeOfDay >= StartTime && currentTimeOfDay < EndTime;
            }
            // Если интервал переходит через полночь (например, 22:00 - 06:00)
            else
            {
                return currentTimeOfDay >= StartTime || currentTimeOfDay < EndTime;
            }
        }

        public override string ToString()
        {
            return $"{Name}: {PlaylistPath} ({StartTime:hh\\:mm} - {EndTime:hh\\:mm})";
        }
    }

    public class ScheduleConfig
    {
        [JsonPropertyName("scheduleItems")]
        public List<ScheduleItem> ScheduleItems { get; set; } = new();
    }
}