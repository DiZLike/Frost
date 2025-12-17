using Strimer.App;
using Strimer.Core;
using System.Net.Http;

namespace Strimer.Services
{
    public class MyServerClient
    {
        private readonly AppConfig _config;
        private readonly HttpClient _httpClient;
        private readonly bool _enabled;

        public MyServerClient(AppConfig config)
        {
            _config = config;
            _enabled = config.MyServerEnabled && !string.IsNullOrWhiteSpace(config.MyServerUrl);

            if (_enabled)
            {
                _httpClient = new HttpClient();
                _httpClient.Timeout = TimeSpan.FromSeconds(5);
                Logger.Info("MyServer client initialized");
            }
        }

        public void SendTrackInfo(int trackNumber, string artist, string title, string filename)
        {
            if (!_enabled)
                return;

            try
            {
                // Строим URL для отправки
                string url = $"{_config.MyServerUrl}/{_config.MyAddSongInfoPage}?" +
                           $"key={Uri.EscapeDataString(_config.MyServerKey)}&" +
                           $"{_config.MyAddSongInfoNumberVar}={trackNumber}&" +
                           $"{_config.MyAddSongInfoArtistVar}={Uri.EscapeDataString(artist)}&" +
                           $"{_config.MyAddSongInfoTitleVar}={Uri.EscapeDataString(title)}&" +
                           $"{_config.MyAddSongInfoLinkVar}={Uri.EscapeDataString(_config.MyAddSongInfoLinkFolderOnServer + filename)}";

                // Отправляем GET запрос (синхронно)
                var response = _httpClient.GetAsync(url).Result;

                if (response.IsSuccessStatusCode)
                {
                    string responseText = response.Content.ReadAsStringAsync().Result;
                    //Logger.Info($"Track info sent to MyServer: {responseText.Trim()}");
                }
                else
                {
                    Logger.Warning($"Failed to send track info: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error sending to MyServer: {ex.Message}");
            }
        }
    }
}