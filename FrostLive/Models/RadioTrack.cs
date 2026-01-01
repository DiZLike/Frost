using System;

namespace FrostLive.Models
{
    public class RadioTrack
    {
        public int Number { get; set; }
        public string Artist { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime Play_Time { get; set; }
        public DateTime Add_Time { get; set; }
        public string Link { get; set; } = string.Empty;

        public string DisplayTitle
        {
            get { return $"{Title}"; }
        }

        public string FormattedTime
        {
            get
            {
                if (Play_Time.Year != 1)
                    return Play_Time.ToString("yyyy-MM-dd HH:mm:ss");
                else if (Add_Time.Year != 1)
                    return Add_Time.ToString("yyyy-MM-dd HH:mm:ss");
                else
                    return Play_Time.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
    }
}