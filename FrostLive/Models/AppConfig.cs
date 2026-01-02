namespace FrostLive.Models
{
    public class AppConfig
    {
        public string RadioStreamUrl { get; set; }
        public string RadioApiBaseUrl { get; set; }
        public string HistoryApiEndpoint { get; set; }
        public string TracksApiEndpoint { get; set; }
        public bool AutoPlay { get; set; }
        public bool EnableOpusPlugin { get; set; }

        public AppConfig()
        {
            RadioStreamUrl = "http://r.dlike.ru:8000/live";
            RadioApiBaseUrl = "http://r.dlike.ru";
            HistoryApiEndpoint = "/get-history";
            TracksApiEndpoint = "/get-alltracks";
            AutoPlay = true;
            EnableOpusPlugin = true;
        }
    }
}