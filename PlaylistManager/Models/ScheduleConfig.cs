// ScheduleConfig.cs
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PlaylistManager.Models
{
    public class ScheduleConfig
    {
        [JsonProperty("ScheduleItems")]
        public List<ScheduleItem> ScheduleItems { get; set; } = new List<ScheduleItem>();
    }
}