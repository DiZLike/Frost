using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostWire.App.Config
{
    public class CSchedule
    {
        // Расписание
        public bool ScheduleEnable { get; set; } = false;
        public string ScheduleFile { get; set; } = "schedule.json";
    }
}
