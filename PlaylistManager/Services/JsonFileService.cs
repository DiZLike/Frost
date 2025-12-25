// Services/JsonFileService.cs
using Newtonsoft.Json;
using PlaylistManager.Models;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace PlaylistManager.Services
{
    public class JsonFileService
    {
        public ScheduleConfig LoadSchedule(string filePath)
        {
            if (!File.Exists(filePath))
                return new ScheduleConfig { ScheduleItems = new List<ScheduleItem>() };

            string json = File.ReadAllText(filePath);
            var config = JsonConvert.DeserializeObject<ScheduleConfig>(json);

            // УДАЛЯЕМ ДУБЛИКАТЫ ДНЕЙ НЕДЕЛИ ПРИ ЗАГРУЗКЕ
            if (config?.ScheduleItems != null)
            {
                foreach (var item in config.ScheduleItems)
                {
                    item.DaysOfWeek.RemoveAt(0);

                    // Если есть "*", оставляем только один "*"
                    if (item.DaysOfWeek.Contains("*"))
                    {
                        item.DaysOfWeek = new List<string> { "*" };
                    }
                    else
                    {
                        // Удаляем дубликаты
                        item.DaysOfWeek = item.DaysOfWeek.Distinct().ToList();
                    }
                }
            }

            return config ?? new ScheduleConfig { ScheduleItems = new List<ScheduleItem>() };
        }

        public void SaveSchedule(string filePath, ScheduleConfig config)
        {
            // УБЕЖДАЕМСЯ, ЧТО НЕТ ДУБЛИКАТОВ ПЕРЕД СОХРАНЕНИЕМ
            if (config?.ScheduleItems != null)
            {
                foreach (var item in config.ScheduleItems)
                {
                    if (item.DaysOfWeek != null && item.DaysOfWeek.Count > 0)
                    {
                        // Если есть "*", оставляем только один "*"
                        if (item.DaysOfWeek.Contains("*"))
                        {
                            item.DaysOfWeek = new List<string> { "*" };
                        }
                        else
                        {
                            // Удаляем дубликаты
                            item.DaysOfWeek = item.DaysOfWeek.Distinct().ToList();
                        }
                    }
                    else
                    {
                        item.DaysOfWeek = new List<string> { "*" };
                    }
                }
            }

            string json = JsonConvert.SerializeObject(config,
                new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore
                });
            File.WriteAllText(filePath, json);
        }
    }
}