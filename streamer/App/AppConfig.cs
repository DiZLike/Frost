using System.Runtime.InteropServices;

namespace Strimer.App
{
    public class AppConfig
    {
        public string OS { get; private set; }
        public string Architecture { get; private set; }
        public bool IsConfigured { get; set; }

        // Пути
        public string BaseDirectory { get; } = AppDomain.CurrentDomain.BaseDirectory;
        public string ConfigDirectory => Path.Combine(BaseDirectory, "config");

        // IceCast настройки
        public string IceCastServer { get; set; } = "localhost";
        public string IceCastPort { get; set; } = "8000";
        public string IceCastMount { get; set; } = "live";
        public string IceCastUser { get; set; } = "source";
        public string IceCastPassword { get; set; } = "hackme";
        public string IceCastName { get; set; } = "Strimer Radio";
        public string IceCastGenre { get; set; } = "Various";

        // Аудио настройки
        public int AudioDevice { get; set; } = -1;
        public int SampleRate { get; set; } = 44100;

        // Плейлист
        public string PlaylistFile { get; set; } = "playlist.txt";
        public bool DynamicPlaylist { get; set; } = false;
        public bool SavePlaylistHistory { get; set; } = true;

        // Расписание (НОВОЕ)
        public bool ScheduleEnable { get; set; } = false;
        public string ScheduleFile { get; set; } = "schedule.json";

        // Opus настройки
        public int OpusBitrate { get; set; } = 128;
        public string OpusMode { get; set; } = "vbr";
        public string OpusContentType { get; set; } = "music";
        public int OpusComplexity { get; set; } = 10;
        public string OpusFrameSize { get; set; } = "20";

        // Replay Gain
        public bool UseReplayGain { get; set; } = true;
        public bool UseCustomGain { get; set; } = false;

        // MyServer
        public bool MyServerEnabled { get; set; } = false;
        public string MyServerUrl { get; set; } = "";
        public string MyServerKey { get; set; } = "";
        public string MyAddSongInfoPage { get; set; } = "";
        public string MyAddSongInfoNumberVar { get; set; } = "";
        public string MyAddSongInfoTitleVar { get; set; } = "";
        public string MyAddSongInfoArtistVar { get; set; } = "";
        public string MyAddSongInfoLinkVar { get; set; } = "";
        public string MyAddSongInfoLinkFolderOnServer { get; set; } = "";

        public AppConfig()
        {
            DetectOS();
            LoadConfig();
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

            Core.Logger.Info($"Detected OS: {OS} ({Architecture})");
        }

        private void LoadConfig()
        {
            string configFile = Path.Combine(ConfigDirectory, "strimer.conf");

            if (!File.Exists(configFile))
            {
                Core.Logger.Warning("Configuration file not found. Using defaults.");
                IsConfigured = false;
                return;
            }

            try
            {
                var lines = File.ReadAllLines(configFile);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    var parts = line.Trim().Split('=', 2);
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim().TrimEnd(';');

                        SetValue(key, value);
                    }
                }

                Core.Logger.Info("Configuration loaded");
            }
            catch (Exception ex)
            {
                Core.Logger.Error($"Failed to load config: {ex.Message}");
                IsConfigured = false;
            }
        }

        private void SetValue(string key, string value)
        {
            switch (key.ToLower())
            {
                case "app.configured":
                    string cleanValue = value.ToLower().Trim();
                    IsConfigured = (cleanValue == "yes" || cleanValue == "true" || cleanValue == "1");
                    Core.Logger.Info($"Config: app.configured='{value}' -> parsed as '{cleanValue}' -> IsConfigured={IsConfigured}");
                    break;

                case "icecast.server":
                    IceCastServer = value;
                    break;
                case "icecast.port":
                    IceCastPort = value;
                    break;
                case "icecast.link":
                    IceCastMount = value;
                    break;
                case "icecast.name":
                    IceCastName = value;
                    break;
                case "icecast.genre":
                    IceCastGenre = value;
                    break;
                case "icecast.username":
                    IceCastUser = value;
                    break;
                case "icecast.password":
                    IceCastPassword = value;
                    break;

                case "device.device":
                    if (int.TryParse(value, out int deviceId))
                        AudioDevice = deviceId;
                    break;
                case "device.frequency":
                    if (int.TryParse(value, out int freq))
                        SampleRate = freq;
                    break;

                case "radio.playlist":
                    PlaylistFile = value;
                    break;
                case "radio.dynamic_playlist":
                    DynamicPlaylist = value.ToLower() == "yes";
                    Core.Logger.Info($"Config: DynamicPlaylist = {DynamicPlaylist}");
                    break;
                case "radio.save_playlist_history":
                    SavePlaylistHistory = value.ToLower() == "yes";
                    break;

                // НОВЫЕ ПАРАМЕТРЫ РАСПИСАНИЯ
                case "radio.schedule_enable":
                    ScheduleEnable = value.ToLower() == "yes";
                    Core.Logger.Info($"Config: ScheduleEnable = {ScheduleEnable}");
                    break;
                case "radio.schedule":
                    ScheduleFile = value;
                    break;

                case "opus.bitrate":
                    if (int.TryParse(value, out int bitrate))
                        OpusBitrate = bitrate;
                    break;
                case "opus.bitrate_mode":
                    OpusMode = value;
                    break;
                case "opus.content_type":
                    OpusContentType = value;
                    break;
                case "opus.complexity":
                    if (int.TryParse(value, out int complexity))
                        OpusComplexity = complexity;
                    break;
                case "opus.framesize":
                    OpusFrameSize = value;
                    break;

                case "radio.use_replay_gain":
                    UseReplayGain = value.ToLower() == "yes";
                    Core.Logger.Info($"Config: UseReplayGain = {UseReplayGain}");
                    break;
                case "radio.use_custom_gain":
                    UseCustomGain = value.ToLower() == "yes";
                    Core.Logger.Info($"Config: UseCustomGain = {UseCustomGain}");
                    break;

                case "mysrv.enable":
                    MyServerEnabled = value.ToLower() == "yes";
                    break;
                case "mysrv.server":
                    MyServerUrl = value;
                    break;
                case "mysrv.key":
                    MyServerKey = value;
                    break;
                case "mysrv.add_song_info_page":
                    MyAddSongInfoPage = value;
                    break;
                case "mysrv.add_song_info_number_var":
                    MyAddSongInfoNumberVar = value;
                    break;
                case "mysrv.add_song_info_title_var":
                    MyAddSongInfoTitleVar = value;
                    break;
                case "mysrv.add_song_info_artist_var":
                    MyAddSongInfoArtistVar = value;
                    break;
                case "mysrv.add_song_info_link_var":
                    MyAddSongInfoLinkVar = value;
                    break;
                case "mysrv.add_song_info_link_folder_on_server":
                    MyAddSongInfoLinkFolderOnServer = value;
                    break;
            }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(ConfigDirectory);

                var lines = new List<string>
                {
                    "# Strimer Radio Configuration",
                    "# Generated on " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    "",
                    "[App]",
                    $"app.configured=yes;",
                    "",
                    "[IceCast]",
                    $"icecast.server={IceCastServer};",
                    $"icecast.port={IceCastPort};",
                    $"icecast.link={IceCastMount};",
                    $"icecast.name={IceCastName};",
                    $"icecast.genre={IceCastGenre};",
                    $"icecast.username={IceCastUser};",
                    $"icecast.password={IceCastPassword};",
                    "",
                    "[Audio]",
                    $"device.device={AudioDevice};",
                    $"device.frequency={SampleRate};",
                    "",
                    "[Playlist]",
                    $"radio.playlist={PlaylistFile};",
                    $"radio.save_playlist_history={(SavePlaylistHistory ? "yes" : "no")};",
                    $"radio.schedule_enable={(ScheduleEnable ? "yes" : "no")};",
                    "",
                    "[Encoder]",
                    $"opus.bitrate={OpusBitrate};",
                    $"opus.bitrate_mode={OpusMode};",
                    $"opus.content_type={OpusContentType};",
                    $"opus.complexity={OpusComplexity};",
                    $"opus.framesize={OpusFrameSize};",
                    "",
                    "[Audio Processing]",
                    $"radio.use_replay_gain={(UseReplayGain ? "yes" : "no")};",
                    $"radio.use_custom_gain={(UseCustomGain ? "yes" : "no")};",
                    "",
                    "[External Services]",
                    $"mysrv.enable={(MyServerEnabled ? "yes" : "no")};",
                    $"mysrv.server={MyServerUrl};",
                    $"mysrv.key={MyServerKey};"
                };

                File.WriteAllLines(Path.Combine(ConfigDirectory, "strimer.conf"), lines);
                Core.Logger.Info("Configuration saved");
            }
            catch (Exception ex)
            {
                Core.Logger.Error($"Failed to save config: {ex.Message}");
            }
        }
    }
}