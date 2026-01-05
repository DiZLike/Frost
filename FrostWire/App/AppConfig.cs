using FrostWire.App.Config;
using FrostWire.App.Config.Encoders;
using FrostWire.Audio;
using FrostWire.Audio.FX;
using FrostWire.Core;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using TagLib.Ogg.Codecs;
using static Un4seen.Bass.Misc.BaseEncoder;

namespace FrostWire.App
{
    public class AppConfig
    {
        public string OS { get; private set; }
        public string Architecture { get; private set; }
        public string BaseDirectory { get; } = AppDomain.CurrentDomain.BaseDirectory;
        public string ConfigDirectory => Path.Combine(BaseDirectory, "config");
        public bool IsConfigured { get; set; } = false;

        public CAudio Audio { get; set; }
        public CIcecast Icecast { get; set; }
        public CPlaylist Playlist { get; set; }
        public List<BaseEncoder> Encoders { get; set; } = new List<BaseEncoder>();
        public CReplayGain ReplayGain { get; set; }
        public CFirstCompressor FirstCompressor { get; set; }
        public CSecondCompressor SecondCompressor { get; set; }
        public CLimiter Limiter { get; set; }
        public CMyServer MyServer { get; set; }
        public CJingles Jingles { get; set; }
        public CDebug Debug { get; set; }

        public AppConfig()
        {
            Audio = new CAudio();
            Playlist = new CPlaylist();
            Icecast = new CIcecast();
            ReplayGain = new CReplayGain();
            FirstCompressor = new CFirstCompressor();
            SecondCompressor = new CSecondCompressor();
            Limiter = new CLimiter();
            MyServer = new CMyServer();
            Jingles = new CJingles();
            Debug = new CDebug();

            DetectOS();
            CheckConfigOk();
        }

        private void DetectOS()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                OS = "Windows";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                OS = "Linux";
            else
                OS = "Unknown";

            Architecture = RuntimeInformation.ProcessArchitecture.ToString();

            Core.Logger.Info($"[AppConfig] Обнаружена ОС: {OS} ({Architecture})");
        }

        public bool LoadConfig()
        {
            string configFile = Path.Combine(ConfigDirectory, "strimer.conf");

            if (!File.Exists(configFile))
            {
                Core.Logger.Warning("[AppConfig] Файл конфигурации не найден!");
                IsConfigured = false;
                return false;
            }

            try
            {
                var lines = File.ReadAllLines(configFile);
                string currentSection = "";

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();

                    // Пропускаем пустые строки и комментарии
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                        continue;

                    // Определяем секцию
                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        currentSection = trimmedLine.Trim('[', ']').ToLower();
                        continue;
                    }

                    var parts = trimmedLine.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim().ToLower();
                        string value = parts[1].Trim().TrimEnd(';');

                        // Обработка пустых значений
                        if (string.IsNullOrEmpty(value))
                        {
                            Core.Logger.Warning($"[AppConfig] Пустое значение для ключа '{key}' в секции '{currentSection}'. Используется значение по умолчанию.");
                            continue;
                        }
                        SetValue(currentSection, key, value);
                    }
                }
                Logger.AppConfig = this;

                Logger.Debug($"[AppConfig] Загружено: IceCast={Icecast.Server}:{Icecast.Port}, Плейлист={Playlist.PlaylistFile}");
                Logger.Debug($"[AppConfig] Аудио: Устройство={Audio.AudioDevice}, Частота={Audio.SampleRate}Гц");
                //Logger.Debug($"[AppConfig] Кодировщик: Opus {Opus.OpusBitrate}кбит/с {Opus.OpusMode}, RG={(ReplayGain.UseReplayGain ? "Вкл" : "Выкл")}");
                return true;
            }
            catch (Exception ex)
            {
                Core.Logger.Error($"[AppConfig] Не удалось загрузить конфигурацию: {ex.Message}");
                IsConfigured = false;
                return false;
            }
        }

        private void SetValue(string section, string key, string value)
        {
            // Упрощенная обработка: учитываем только секцию, без префиксов в ключах
            switch (section)
            {
                case "audio":
                    switch (key)
                    {
                        case "device":
                            if (int.TryParse(value, out int deviceId))
                                Audio.AudioDevice = deviceId;
                            break;
                        case "frequency":
                            if (int.TryParse(value, out int freq))
                                Audio.SampleRate = freq;
                            break;
                    }
                    break;

                case "icecast":
                    switch (key)
                    {
                        case "server":
                            Icecast.Server = value;
                            break;
                        case "port":
                            Icecast.Port = value;
                            break;
                        case "name":
                            Icecast.Name = value;
                            break;
                        case "genre":
                            Icecast.Genre = value;
                            break;
                        case "username":
                            Icecast.User = value;
                            break;
                        case "password":
                            Icecast.Password = value;
                            break;
                    }
                    break;

                case "playlist":
                    switch (key)
                    {
                        case "list":
                            Playlist.PlaylistFile = value;
                            break;
                        case "dynamic_playlist":
                            Playlist.DynamicPlaylist = value.ToLower() == "yes";
                            break;
                        case "save_playlist_history":
                            Playlist.SavePlaylistHistory = value.ToLower() == "yes";
                            break;
                        case "schedule_enable":
                            Playlist.ScheduleEnable = value.ToLower() == "yes";
                            break;
                        case "schedule":
                            Playlist.ScheduleFile = value;
                            break;
                    }
                    break;

                case string s when s.StartsWith("encoder:"):
                    string encoderMount = section["encoder:".Length..];
                    BaseEncoder? encoder = GetOrCreateEncoder(encoderMount);

                    if (encoder is COpus opus)
                    {
                        switch (key.ToLower())
                        {
                            case "type":
                                encoder.Type = value.ToLower();
                                break;
                            case "enabled":
                                encoder.Enabled = value.ToLower() == "yes";
                                break;
                            case "bitrate":
                                if (int.TryParse(value, out int bitrate))
                                    opus.Bitrate = bitrate;
                                break;
                            case "bitrate_mode":
                                opus.Mode = value;
                                break;
                            case "content_type":
                                opus.ContentType = value;
                                break;
                            case "complexity":
                                if (int.TryParse(value, out int complexity))
                                    opus.Complexity = complexity;
                                break;
                            case "framesize":
                                opus.FrameSize = value;
                                break;
                        }
                    }
                    break;

                case "replaygain":
                    switch (key)
                    {
                        case "use_replay_gain":
                            ReplayGain.UseReplayGain = value.ToLower() == "yes";
                            break;
                        case "use_custom_gain":
                            ReplayGain.UseCustomGain = value.ToLower() == "yes";
                            break;
                    }
                    break;

                case "firstcompressor":
                    switch (key)
                    {
                        case "enable":
                            FirstCompressor.Enable = value.ToLower() == "yes";
                            break;
                        case "adaptive":
                            FirstCompressor.Adaptive = value.ToLower() == "yes";
                            break;
                        case "threshold":
                            FirstCompressor.Threshold = float.Parse(value.Replace(".", ","));
                            break;
                        case "ratio":
                            FirstCompressor.Ratio = float.Parse(value.Replace(".", ","));
                            break;
                        case "attack":
                            FirstCompressor.Attack = float.Parse(value.Replace(".", ","));
                            break;
                        case "release":
                            FirstCompressor.Release = float.Parse(value.Replace(".", ","));
                            break;
                        case "gain":
                            FirstCompressor.Gain = float.Parse(value.Replace(".", ","));
                            break;
                    }
                    break;

                case "secondcompressor":
                    switch (key)
                    {
                        case "enable":
                            SecondCompressor.Enable = value.ToLower() == "yes";
                            break;
                        case "threshold":
                            SecondCompressor.Threshold = float.Parse(value.Replace(".", ","));
                            break;
                        case "ratio":
                            SecondCompressor.Ratio = float.Parse(value.Replace(".", ","));
                            break;
                        case "attack":
                            SecondCompressor.Attack = float.Parse(value.Replace(".", ","));
                            break;
                        case "release":
                            SecondCompressor.Release = float.Parse(value.Replace(".", ","));
                            break;
                        case "gain":
                            SecondCompressor.Gain = float.Parse(value.Replace(".", ","));
                            break;
                    }
                    break;

                case "limiter":
                    switch (key)
                    {
                        case "enable":
                            Limiter.Enable = value.ToLower() == "yes";
                            break;
                        case "threshold":
                            Limiter.Threshold = float.Parse(value.Replace(".", ","));
                            break;
                        case "release":
                            Limiter.Release = float.Parse(value.Replace(".", ","));
                            break;
                        case "gain":
                            Limiter.Gain = float.Parse(value.Replace(".", ","));
                            break;
                    }
                    break;

                case "myserver":  // Более краткое название, как в старом формате
                    switch (key)
                    {
                        case "enable":
                            MyServer.MyServerEnabled = value.ToLower() == "yes";
                            break;
                        case "server":
                            MyServer.MyServerUrl = value;
                            break;
                        case "key":
                            MyServer.MyServerKey = value;
                            break;
                        case "add_song_info_page":
                            MyServer.MyAddSongInfoPage = value;
                            break;
                        case "add_song_info_number_var":
                            MyServer.MyAddSongInfoNumberVar = value;
                            break;
                        case "add_song_info_title_var":
                            MyServer.MyAddSongInfoTitleVar = value;
                            break;
                        case "add_song_info_artist_var":
                            MyServer.MyAddSongInfoArtistVar = value;
                            break;
                        case "add_song_info_link_var":
                            MyServer.MyAddSongInfoLinkVar = value;
                            break;
                        case "add_song_info_link_folder_on_server":
                            MyServer.MyAddSongInfoLinkFolderOnServer = value;
                            break;
                        case "remove_file_prefix":
                            MyServer.MyRemoveFilePrefix = value;
                            break;
                    }
                    break;

                case "jingles":
                    switch (key)
                    {
                        case "enable":
                            Jingles.JinglesEnable = value.ToLower() == "yes";
                            break;
                        case "file":
                            Jingles.JingleConfigFile = value;
                            break;
                        case "frequency":
                            if (int.TryParse(value, out int frequency))
                                Jingles.JingleFrequency = frequency;
                            break;
                        case "random":
                            Jingles.JinglesRandom = value.ToLower() == "yes";
                            break;
                    }
                    break;

                case "debug":
                    switch (key)
                    {
                        case "enable":
                            Debug.DebugEnable = value.ToLower() == "yes";
                            break;
                        case "stack_view":
                            Debug.DebugStackView = value.ToLower() == "yes";
                            break;
                    }
                    break;

                default:
                    Core.Logger.Warning($"[AppConfig] Неизвестная секция конфигурации: {section}");
                    break;
            }
        }
        public bool SetConfigOk()
        {
            try
            {
                Directory.CreateDirectory(ConfigDirectory);
                File.WriteAllText(Path.Combine(ConfigDirectory, "ok"), DateTime.Now.ToString("o"));
                return IsConfigured = true;
            }
            catch
            {
                return IsConfigured = false;
            }
        }
        private bool CheckConfigOk()
        {
            if (string.IsNullOrEmpty(ConfigDirectory) || !Directory.Exists(ConfigDirectory))
                return false;
            return IsConfigured = File.Exists(Path.Combine(ConfigDirectory, "ok"));
        }
        private BaseEncoder? GetOrCreateEncoder(string mount)
        {
            bool exist = Encoders.Any(e => e.Mount == mount);
            if (exist)
                return null;
            // Создаем новый (можно определить тип позже)
            var newEncoder = new COpus { Mount = mount };
            Encoders.Add(newEncoder);
            return newEncoder;
        }
    }
}