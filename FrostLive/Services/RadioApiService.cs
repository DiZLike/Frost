using FrostLive.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace FrostLive.Services
{
    public class RadioApiService
    {
        private readonly HttpClient _httpClient;
        private readonly AppConfig _config;

        public RadioApiService(AppConfig config)
        {
            _config = config;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(config.RadioApiBaseUrl),
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        public async Task<List<RadioTrack>> GetHistoryAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(_config.HistoryApiEndpoint);
                var tracks = JsonConvert.DeserializeObject<List<RadioTrack>>(response);
                return tracks ?? new List<RadioTrack>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"History API error: {ex.Message}");
                return new List<RadioTrack>();
            }
        }

        public async Task<List<RadioTrack>> GetNewTracksAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(_config.TracksApiEndpoint);
                var tracks = JsonConvert.DeserializeObject<List<RadioTrack>>(response);
                return tracks ?? new List<RadioTrack>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Tracks API error: {ex.Message}");
                return new List<RadioTrack>();
            }
        }

        public async Task<string> GetCurrentSongAsync()
        {
            var history = await GetHistoryAsync();
            return history.Count > 0 ? history[0].DisplayTitle : "No song playing";
        }
    }
}