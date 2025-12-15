using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using RadioStationManager.Configuration;
using RadioStationManager.Models;
using RadioStationManager.Services;

namespace RadioStationManager.Services
{
    public class ServerUploader
    {
        private readonly RadioConfig _config;
        private readonly HttpClient _httpClient;

        public ServerUploader(RadioConfig config)
        {
            _config = config;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<UploadResult> UploadFileAsync(string filePath)
        {
            try
            {
                var tag = AudioTagReader.ReadTags(filePath);
                string fileName = Path.GetFileName(filePath);
                string serverPath = $"{_config.ServerPath}{fileName}";
                string downloadLink = $"{_config.DownloadLink}{fileName}";

                return await SendToServerAsync(tag, serverPath, downloadLink);
            }
            catch (Exception ex)
            {
                return new UploadResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        private async Task<UploadResult> SendToServerAsync(
            AudioTag tag,
            string serverPath,
            string downloadLink)
        {
            try
            {
                string url = $"{_config.ServerUrl}?key={HttpUtility.UrlEncode(_config.ApiKey)}" +
                            $"&artist={HttpUtility.UrlEncode(tag.Artist)}" +
                            $"&title={HttpUtility.UrlEncode(tag.Title)}" +
                            $"&path={HttpUtility.UrlEncode(serverPath)}" +
                            $"&link={HttpUtility.UrlEncode(downloadLink)}";

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string responseData = await response.Content.ReadAsStringAsync();

                    return new UploadResult
                    {
                        Success = true,
                        Response = responseData
                    };
                }
                else
                {
                    return new UploadResult
                    {
                        Success = false,
                        Error = $"HTTP {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new UploadResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }
    }
}