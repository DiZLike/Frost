namespace RadioStationManager.Configuration
{
    public class RadioConfig
    {
        public string ServerUrl { get; set; } = "http://r.dlike.ru/add-track";
        public string ApiKey { get; set; } = "up6jlo4bj6e8yy96w6w3iq84";
        public string ServerPath { get; set; } = "/mnt/sd/radio/main/";
        public string DownloadLink { get; set; } = "http://rpi.dlike.ru:82/download/main/";
        public string PlaylistPath { get; set; } = @"C:\playlists\main.pls";
    }
}