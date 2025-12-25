// ScheduleItem.cs
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PlaylistManager.Models
{
    public class ScheduleItem
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("playlistPath")]
        public string PlaylistPath { get; set; }

        [JsonProperty("startHour")]
        public int StartHour { get; set; }

        [JsonProperty("startMinute")]
        public int StartMinute { get; set; }

        [JsonProperty("endHour")]
        public int EndHour { get; set; }

        [JsonProperty("endMinute")]
        public int EndMinute { get; set; }

        [JsonProperty("daysOfWeek")]
        public List<string> DaysOfWeek { get; set; } = new List<string> { "*" };
    }
}